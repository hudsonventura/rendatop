using RendaTop.App.Services;

namespace RendaTop.App.Pages;

public partial class SplashPage : ContentPage
{
    private readonly SessionService _session;
    private readonly ConnectivityService _connectivity;
    private readonly SharedInvestmentDocumentService _sharedDocuments;
    private bool _started;

    public SplashPage(SessionService session, ConnectivityService connectivity, SharedInvestmentDocumentService sharedDocuments)
    {
        _session = session;
        _connectivity = connectivity;
		_sharedDocuments = sharedDocuments;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_started)
            return;

        _started = true;
        await Task.Delay(TimeSpan.FromSeconds(1));
        await _session.InitializeAsync();

        var isAuthenticated = _connectivity.IsOffline
            ? await _session.HasOfflineSessionAsync()
            : await _session.IsAuthenticatedAsync();
        _sharedDocuments.MarkNavigationReady();

        if (isAuthenticated && _sharedDocuments.TryGetPendingDocumentId(out var documentId))
        {
            await Shell.Current.GoToAsync($"{nameof(AddInvestmentPage)}?sharedDocumentId={documentId}");
            return;
        }

        await Shell.Current.GoToAsync(isAuthenticated ? "//dashboard" : "//welcome");
    }
}
