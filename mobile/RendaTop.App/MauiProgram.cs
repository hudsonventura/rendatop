using Microcharts.Maui;
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
			.UseMicrocharts()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<AppConfig>();
		builder.Services.AddSingleton<ConnectivityService>();
		builder.Services.AddSingleton<ApiClient>();
		builder.Services.AddSingleton<LocalSnapshotStore>();
		builder.Services.AddSingleton<SessionService>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<InvestmentCacheService>();
		builder.Services.AddSingleton<InvestmentService>();
		builder.Services.AddSingleton<SharedInvestmentDocumentService>();
		builder.Services.AddSingleton<CalendarService>();
		builder.Services.AddSingleton<NotificationService>();
		builder.Services.AddSingleton<WalletService>();
		builder.Services.AddSingleton<RecurringInvestmentService>();
		builder.Services.AddSingleton<MoneyBoxService>();
		builder.Services.AddSingleton<UserSettingsService>();
		builder.Services.AddSingleton<SubscriptionService>();
		builder.Services.AddSingleton<SupportTicketService>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddTransient<SplashPage>();
		builder.Services.AddTransient<WelcomePage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SignupPage>();
		builder.Services.AddTransient<SignupVerificationPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<CalendarPage>();
		builder.Services.AddTransient<CalendarDayEventsPage>();
		builder.Services.AddTransient<CalendarEventDetailsPage>();
		builder.Services.AddTransient<MyInvestmentsPage>();
		builder.Services.AddTransient<NotificationsPage>();
		builder.Services.AddTransient<RecurringInvestmentsPage>();
		builder.Services.AddTransient<MoneyBoxesPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<SubscriptionPage>();
		builder.Services.AddTransient<SubscriptionCheckoutPage>();
		builder.Services.AddTransient<SupportPage>();
		builder.Services.AddTransient<CreateSupportTicketPage>();
		builder.Services.AddTransient<SupportTicketDetailsPage>();
		builder.Services.AddTransient<AddInvestmentPage>();
		builder.Services.AddTransient<EditRecurringInvestmentPage>();
		builder.Services.AddTransient<EditMoneyBoxPage>();
		builder.Services.AddTransient<InvestmentDetailsPage>();
		builder.Services.AddTransient<RedeemInvestmentPage>();
		builder.Services.AddTransient<EditRedemptionPage>();

		return builder.Build();
	}
}
