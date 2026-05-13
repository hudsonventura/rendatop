using Microsoft.Extensions.Logging;
using RendaTop.App.Pages;
using RendaTop.App.Services;

namespace RendaTop.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<AppConfig>();
		builder.Services.AddSingleton<ApiClient>();
		builder.Services.AddSingleton<SessionService>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<InvestmentCacheService>();
		builder.Services.AddSingleton<InvestmentService>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddTransient<SplashPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SignupPage>();
		builder.Services.AddTransient<SignupVerificationPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<MyInvestmentsPage>();
		builder.Services.AddTransient<AddInvestmentPage>();
		builder.Services.AddTransient<InvestmentDetailsPage>();
		builder.Services.AddTransient<RedeemInvestmentPage>();
		builder.Services.AddTransient<EditRedemptionPage>();

		return builder.Build();
	}
}
