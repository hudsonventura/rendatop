using Microsoft.Extensions.DependencyInjection;
using RendaTop.App.Pages;
using RendaTop.App.Services;

namespace RendaTop.App;

public partial class App : Application
{
	private readonly IServiceProvider _services;
	private readonly SessionService _session;
	private readonly SharedInvestmentDocumentService _sharedDocuments;

	public App(IServiceProvider services)
	{
		_services = services;
		_session = services.GetRequiredService<SessionService>();
		_sharedDocuments = services.GetRequiredService<SharedInvestmentDocumentService>();
		_sharedDocuments.DocumentReceived += OnSharedDocumentReceived;
		InitializeComponent();
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_services.GetRequiredService<AppShell>());
	}

	private async void OnSharedDocumentReceived(object? sender, Guid documentId)
	{
		if (!await _session.IsAuthenticatedAsync() || Shell.Current is null)
			return;

		await MainThread.InvokeOnMainThreadAsync(async () =>
			await Shell.Current.GoToAsync($"{nameof(AddInvestmentPage)}?sharedDocumentId={documentId}"));
	}
}
