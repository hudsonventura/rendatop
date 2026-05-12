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
		Items.Add(CreateShellContent<Pages.LoginPage>("login", "Entrar", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.SignupPage>("signup", "Criar conta", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.AddInvestmentPage>("add-investment", "Novo Investimento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.InvestmentDetailsPage>("investment-details", "Detalhes do Investimento", showInFlyout: false));
		Items.Add(CreateFlyoutItem<Pages.DashboardPage>("dashboard", "Dashboard"));
		Items.Add(CreateFlyoutItem<Pages.MyInvestmentsPage>("meus-investimentos", "Meus Investimentos"));
		Items.Add(CreateFutureFlyoutItem("investimentos-recorrentes", "Investimentos Recorrentes", "Automacoes de aportes conforme permissao do plano."));
		Items.Add(CreateFutureFlyoutItem("cofrinhos", "Cofrinhos", "Organizacao da carteira por objetivos."));
		Items.Add(CreateFutureFlyoutItem("calendar", "Calendario", "Vencimentos e compartilhamento ICS conforme plano."));
		Items.Add(CreateFutureFlyoutItem("notifications", "Notificacoes", "Central de alertas e preferencias."));
		Items.Add(CreateFutureFlyoutItem("settings", "Configuracoes", "Dados da conta, seguranca e canais de aviso."));
		Items.Add(CreateFutureFlyoutItem("subscription", "Assinatura", "Planos Free, Plus e Pro."));
		Items.Add(CreateFutureFlyoutItem("atendimento", "Atendimento", "Chamados e mensagens de suporte."));

		Routing.RegisterRoute(nameof(Pages.SignupVerificationPage), typeof(Pages.SignupVerificationPage));
		Routing.RegisterRoute(nameof(Pages.AddInvestmentPage), typeof(Pages.AddInvestmentPage));
		Routing.RegisterRoute(nameof(Pages.InvestmentDetailsPage), typeof(Pages.InvestmentDetailsPage));
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
