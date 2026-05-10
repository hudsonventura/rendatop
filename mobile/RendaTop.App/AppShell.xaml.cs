using Microsoft.Extensions.DependencyInjection;

namespace RendaTop.App;

public partial class AppShell : Shell
{
	private readonly IServiceProvider _services;

	public AppShell(IServiceProvider services)
	{
		_services = services;
		InitializeComponent();

		Items.Add(CreateShellContent<Pages.SplashPage>("splash", "Splash"));
		Items.Add(CreateShellContent<Pages.LoginPage>("login", "Entrar"));
		Items.Add(CreateShellContent<Pages.SignupPage>("signup", "Criar conta"));
		Items.Add(CreateShellContent<Pages.DashboardPlaceholderPage>("dashboard", "Dashboard"));

		Routing.RegisterRoute(nameof(Pages.SignupVerificationPage), typeof(Pages.SignupVerificationPage));
	}

	private ShellContent CreateShellContent<TPage>(string route, string title)
		where TPage : Page
	{
		return new ShellContent
		{
			Route = route,
			Title = title,
			ContentTemplate = new DataTemplate(() => _services.GetRequiredService<TPage>())
		};
	}
}
