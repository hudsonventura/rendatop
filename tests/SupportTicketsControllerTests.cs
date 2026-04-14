using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Controllers;
using server.Domain;
using server.Utils;

namespace tests;

public class SupportTicketsControllerTests
{
    [Fact]
    public async Task Create_PersistsTicketMessageSnapshotsHistoryAndAttachments()
    {
        using var fixture = new SupportTicketsFixture();
        var controller = fixture.CreateController(fixture.CommonUser);

        var result = await controller.Create(new CreateSupportTicketRequest
        {
            subject = "Problema com a conta",
            body_html = "<p>Olá <script>alert(1)</script><strong>time</strong></p>",
            attachments =
            [
                CreateFormFile("evidencia.png", "image/png", "fake-image")
            ]
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<SupportTicketDetailResponse>(created.Value);

        Assert.Equal("Problema com a conta", response.subject);
        Assert.Equal(SupportTicketStatus.AguardandoAtendimento, response.status);
        Assert.Single(response.messages);
        Assert.Equal(fixture.CommonUser.name, response.messages[0].sender_user_name);
        Assert.DoesNotContain("script", response.messages[0].body_html, StringComparison.OrdinalIgnoreCase);
        Assert.Single(response.messages[0].attachments);
        Assert.Single(response.status_history);
        Assert.Equal(fixture.CommonUser.name, response.status_history[0].actor_user_name);

        using var assertionContext = fixture.CreateAssertionContext();
        var savedTicket = assertionContext.support_tickets.Single();
        var savedMessage = assertionContext.support_ticket_messages.Single();
        var savedHistory = assertionContext.support_ticket_status_history.Single();

        Assert.Equal(fixture.CommonUser.name, savedTicket.requester_user_name);
        Assert.Equal(fixture.CommonUser.name, savedMessage.sender_user_name);
        Assert.Equal(fixture.CommonUser.name, savedHistory.actor_user_name);
    }

    [Fact]
    public async Task ChangeStatus_ThrowsForCommonUser()
    {
        using var fixture = new SupportTicketsFixture();
        var ticket = fixture.SeedTicket(fixture.CommonUser, SupportTicketStatus.AguardandoAtendimento);
        var controller = fixture.CreateController(fixture.CommonUser);

        var exception = await Assert.ThrowsAsync<ExpectedException>(async () =>
            await controller.ChangeStatus(ticket.id, new ChangeSupportTicketStatusRequest(SupportTicketStatus.EmAtendimento), CancellationToken.None));

        Assert.Equal("Acesso permitido apenas para administradores.", exception.Message);
    }

    [Fact]
    public async Task AddMessage_ThrowsWhenCommonUserRepliesOutsideAwaitingUserResponse()
    {
        using var fixture = new SupportTicketsFixture();
        var ticket = fixture.SeedTicket(fixture.CommonUser, SupportTicketStatus.AguardandoAtendimento);
        var controller = fixture.CreateController(fixture.CommonUser);

        var exception = await Assert.ThrowsAsync<ExpectedException>(async () =>
            await controller.AddMessage(ticket.id, new AddSupportTicketMessageRequest
            {
                body_html = "<p>Posso complementar</p>"
            }, CancellationToken.None));

        Assert.Equal("Você só pode responder quando o chamado estiver aguardando sua resposta.", exception.Message);
    }

    [Fact]
    public async Task AddMessage_ReopensTicketToAwaitingAttendanceWhenUserReplies()
    {
        using var fixture = new SupportTicketsFixture();
        var ticket = fixture.SeedTicket(fixture.CommonUser, SupportTicketStatus.AguardandoRespostaUsuario);
        var controller = fixture.CreateController(fixture.CommonUser);

        var result = await controller.AddMessage(ticket.id, new AddSupportTicketMessageRequest
        {
            body_html = "<p>Segue a resposta do usuário</p>"
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SupportTicketDetailResponse>(okResult.Value);

        Assert.Equal(SupportTicketStatus.AguardandoAtendimento, response.status);
        Assert.Equal(2, response.messages.Count);
        Assert.Equal(fixture.CommonUser.name, response.messages.Last().sender_user_name);
        Assert.Equal(SupportTicketStatus.AguardandoAtendimento, response.status_history.Last().to_status);
        Assert.Equal(SupportTicketChangeSource.SystemOnUserReply, response.status_history.Last().source);
    }

    [Fact]
    public async Task ChangeStatus_ArchivesTicketAndPersistsActorName()
    {
        using var fixture = new SupportTicketsFixture();
        var ticket = fixture.SeedTicket(fixture.CommonUser, SupportTicketStatus.EmAtendimento);
        var controller = fixture.CreateController(fixture.AdminUser);

        var result = await controller.ChangeStatus(ticket.id, new ChangeSupportTicketStatusRequest(SupportTicketStatus.Encerrado), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SupportTicketDetailResponse>(okResult.Value);

        Assert.True(response.is_archived);
        Assert.Equal(SupportTicketStatus.Encerrado, response.status);
        Assert.Equal(fixture.AdminUser.name, response.status_history.Last().actor_user_name);
    }

    [Fact]
    public async Task DownloadAttachment_RestrictsAccessToOwnerOrAdmin()
    {
        using var fixture = new SupportTicketsFixture();
        var ticket = fixture.SeedTicket(fixture.CommonUser, SupportTicketStatus.AguardandoAtendimento);
        fixture.SeedAttachment(ticket, fixture.CommonUser);

        var ownerController = fixture.CreateController(fixture.CommonUser);
        var attachmentId = fixture.Context.support_ticket_message_attachments.Select(x => x.id).Single();

        var fileResult = Assert.IsType<FileContentResult>(ownerController.DownloadAttachment(attachmentId));
        Assert.Equal("evidencia.png", fileResult.FileDownloadName);

        var outsider = fixture.SeedUser("other@example.com", "Outro Usuario", UserType.Common);
        var outsiderController = fixture.CreateController(outsider);

        var exception = Assert.Throws<ExpectedException>(() => outsiderController.DownloadAttachment(attachmentId));
        Assert.Equal("Você não tem acesso a este anexo.", exception.Message);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "attachments", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class SupportTicketsFixture : IDisposable
    {
        private readonly DbContextOptions<Context> _options;

        public Context Context { get; }
        public User AdminUser { get; }
        public User CommonUser { get; }

        public SupportTicketsFixture()
        {
            _options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

            Context = new Context(_options);
            AdminUser = SeedUser("admin@example.com", "Admin", UserType.Admin);
            CommonUser = SeedUser("user@example.com", "Cliente", UserType.Common);
        }

        public SupportTicketsController CreateController(User user)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = user;

            var controller = new SupportTicketsController(
                new HttpContextAccessor { HttpContext = httpContext },
                new TestContextFactory(_options));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        public User SeedUser(string email, string name, UserType userType)
        {
            var user = new User
            {
                name = name,
                email = email,
                password = "secret",
                user_type = userType,
                auth_provider = AuthProvider.Password
            };

            Context.users.Add(user);
            Context.SaveChanges();
            return user;
        }

        public SupportTicket SeedTicket(User requester, SupportTicketStatus status)
        {
            var now = DateTime.UtcNow.AddMinutes(-10);
            var ticket = new SupportTicket
            {
                requester_user_id = requester.id,
                requester_user_name = requester.name,
                requester_user_email = requester.email,
                subject = "Chamado teste",
                status = status,
                created_at = now,
                updated_at = now,
                last_message_at = now,
                archived_at = status is SupportTicketStatus.Encerrado or SupportTicketStatus.Cancelado ? now : null
            };

            Context.support_tickets.Add(ticket);
            Context.support_ticket_messages.Add(new SupportTicketMessage
            {
                ticket = ticket,
                sender_user_id = requester.id,
                sender_user_type = requester.user_type,
                sender_user_name = requester.name,
                body_html = "<p>Mensagem inicial</p>",
                body_text = "Mensagem inicial",
                created_at = now
            });
            Context.support_ticket_status_history.Add(new SupportTicketStatusHistory
            {
                ticket = ticket,
                actor_user_id = requester.id,
                actor_user_name = requester.name,
                from_status = null,
                to_status = status,
                source = SupportTicketChangeSource.SystemOnCreate,
                created_at = now
            });
            Context.SaveChanges();
            return ticket;
        }

        public void SeedAttachment(SupportTicket ticket, User sender)
        {
            var message = Context.support_ticket_messages.First(x => x.ticket_id == ticket.id);
            Context.support_ticket_message_attachments.Add(new SupportTicketMessageAttachment
            {
                message_id = message.id,
                file_name = "evidencia.png",
                content_type = "image/png",
                size_bytes = 20,
                is_image = true,
                content = Encoding.UTF8.GetBytes("attachment")
            });
            Context.SaveChanges();
        }

        public Context CreateAssertionContext() => new(_options);

        public void Dispose()
        {
            Context.Dispose();
        }
    }

    private sealed class TestContextFactory : IDbContextFactory<Context>
    {
        private readonly DbContextOptions<Context> _options;

        public TestContextFactory(DbContextOptions<Context> options)
        {
            _options = options;
        }

        public Context CreateDbContext()
        {
            return new Context(_options);
        }
    }
}
