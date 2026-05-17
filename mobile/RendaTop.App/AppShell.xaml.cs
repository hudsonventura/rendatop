using Microsoft.Extensions.DependencyInjection;
using RendaTop.App.Services;

namespace RendaTop.App;

public partial class AppShell : Shell
{
	private readonly IServiceProvider _services;

	public AppShell(IServiceProvider services)
	{
		_services = services;
		InitializeComponent();

		FlyoutHeader = CreateFlyoutHeader();
		FlyoutFooter = CreateFlyoutFooter();

		Items.Add(CreateShellContent<Pages.SplashPage>("splash", "Splash", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.WelcomePage>("welcome", "Boas-vindas", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.LoginPage>("login", "Entrar", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.SignupPage>("signup", "Criar conta", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.AddInvestmentPage>("add-investment", "Novo Investimento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.InvestmentDetailsPage>("investment-details", "Detalhes do Investimento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.RedeemInvestmentPage>("redeem-investment", "Criar Resgate", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.EditRedemptionPage>("edit-redemption", "Editar Resgate", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.EditRecurringInvestmentPage>("edit-recurring-investment", "Recorrencia", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.EditMoneyBoxPage>("edit-money-box", "Cofrinho", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.CalendarDayEventsPage>("calendar-day-events", "Eventos do Dia", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.CalendarEventDetailsPage>("calendar-event-details", "Detalhes do Evento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.SubscriptionCheckoutPage>("subscription-checkout", "Pagamento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.StoreSubscriptionCheckoutPage>("store-subscription-checkout", "Pagamento na Loja", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.CreateSupportTicketPage>("create-support-ticket", "Novo Chamado", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.SupportTicketDetailsPage>("support-ticket-details", "Detalhes do Chamado", showInFlyout: false));
		Items.Add(CreateFlyoutItem<Pages.DashboardPage>("dashboard", "Dashboard"));
		Items.Add(CreateFlyoutItem<Pages.CalendarPage>("calendar", "Calendario"));
		Items.Add(CreateFlyoutItem<Pages.MyInvestmentsPage>("meus-investimentos", "Meus Investimentos"));
		Items.Add(CreateFlyoutItem<Pages.NotificationsPage>("notifications", "Notificacoes"));
		Items.Add(CreateFlyoutItem<Pages.RecurringInvestmentsPage>("investimentos-recorrentes", "Investimentos Recorrentes"));
		Items.Add(CreateFlyoutItem<Pages.MoneyBoxesPage>("cofrinhos", "Cofrinhos"));
		Items.Add(CreateFlyoutItem<Pages.SettingsPage>("settings", "Configuracoes"));
		Items.Add(CreateFlyoutItem<Pages.SubscriptionPage>("subscription", "Assinatura"));
		Items.Add(CreateFlyoutItem<Pages.SupportPage>("atendimento", "Atendimento"));

		Routing.RegisterRoute(nameof(Pages.SignupVerificationPage), typeof(Pages.SignupVerificationPage));
		Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
		Routing.RegisterRoute(nameof(Pages.SignupPage), typeof(Pages.SignupPage));
		Routing.RegisterRoute(nameof(Pages.AddInvestmentPage), typeof(Pages.AddInvestmentPage));
		Routing.RegisterRoute(nameof(Pages.InvestmentDetailsPage), typeof(Pages.InvestmentDetailsPage));
		Routing.RegisterRoute(nameof(Pages.RedeemInvestmentPage), typeof(Pages.RedeemInvestmentPage));
		Routing.RegisterRoute(nameof(Pages.EditRedemptionPage), typeof(Pages.EditRedemptionPage));
		Routing.RegisterRoute(nameof(Pages.EditRecurringInvestmentPage), typeof(Pages.EditRecurringInvestmentPage));
		Routing.RegisterRoute(nameof(Pages.EditMoneyBoxPage), typeof(Pages.EditMoneyBoxPage));
		Routing.RegisterRoute(nameof(Pages.CalendarDayEventsPage), typeof(Pages.CalendarDayEventsPage));
		Routing.RegisterRoute(nameof(Pages.CalendarEventDetailsPage), typeof(Pages.CalendarEventDetailsPage));
		Routing.RegisterRoute(nameof(Pages.SubscriptionCheckoutPage), typeof(Pages.SubscriptionCheckoutPage));
		Routing.RegisterRoute(nameof(Pages.StoreSubscriptionCheckoutPage), typeof(Pages.StoreSubscriptionCheckoutPage));
		Routing.RegisterRoute(nameof(Pages.CreateSupportTicketPage), typeof(Pages.CreateSupportTicketPage));
		Routing.RegisterRoute(nameof(Pages.SupportTicketDetailsPage), typeof(Pages.SupportTicketDetailsPage));
	}

	private ShellContent CreateShellContent<TPage>(string route, string title, bool showInFlyout)
		where TPage : Page
	{
		var content = new ShellContent
		{
			Route = route,
			Title = title,
			ContentTemplate = new DataTemplate(() => _services.GetRequiredService<TPage>())
		};

		Shell.SetNavBarIsVisible(content, showInFlyout);
		Shell.SetFlyoutItemIsVisible(content, showInFlyout);
		Shell.SetFlyoutBehavior(content, showInFlyout ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled);
		return content;
	}

	private FlyoutItem CreateFlyoutItem<TPage>(string route, string title)
		where TPage : Page
	{
		var item = new FlyoutItem
		{
			Route = route,
			Title = title,
			FlyoutDisplayOptions = FlyoutDisplayOptions.AsSingleItem,
			Items =
			{
				new ShellContent
				{
					Route = $"{route}-content",
					Title = title,
					ContentTemplate = new DataTemplate(() => _services.GetRequiredService<TPage>())
				}
			}
		};

		return item;
	}

	private FlyoutItem CreateFutureFlyoutItem(string route, string title, string description)
	{
		return new FlyoutItem
		{
			Route = route,
			Title = title,
			FlyoutDisplayOptions = FlyoutDisplayOptions.AsSingleItem,
			Items =
			{
				new ShellContent
				{
					Route = $"{route}-content",
					Title = title,
					ContentTemplate = new DataTemplate(() => new Pages.FuturePage(title, description))
				}
			}
		};
	}

	private static View CreateFlyoutHeader()
	{
		return new Grid
		{
			Padding = new Thickness(20, 24, 20, 12),
			BackgroundColor = Color.FromArgb("#111827"),
			Children =
			{
				new VerticalStackLayout
				{
					Spacing = 10,
					Children =
					{
						new Image
						{
							Source = "brand_icon.svg",
							HeightRequest = 48,
							WidthRequest = 48,
							HorizontalOptions = LayoutOptions.Start
						},
						new Label
						{
							Text = "RendaTop",
							FontSize = 22,
							FontAttributes = FontAttributes.Bold,
							TextColor = Colors.White
						},
						new Label
						{
							Text = "Gestao de investimentos",
							FontSize = 13,
							TextColor = Color.FromArgb("#CBD5E1")
						}
					}
				}
			}
		};
	}

	private View CreateFlyoutFooter()
	{
		var logout = new Button
		{
			Text = "Sair",
			BackgroundColor = Color.FromArgb("#111827"),
			TextColor = Colors.White,
			FontAttributes = FontAttributes.Bold,
			Margin = new Thickness(16, 8, 16, 18)
		};

		logout.Clicked += async (_, _) =>
		{
			logout.IsEnabled = false;
			logout.Text = "Saindo...";
			await _services.GetRequiredService<AuthService>().LogoutAsync();
			FlyoutIsPresented = false;
			await GoToAsync("//login");
			logout.Text = "Sair";
			logout.IsEnabled = true;
		};

		return logout;
	}
}
