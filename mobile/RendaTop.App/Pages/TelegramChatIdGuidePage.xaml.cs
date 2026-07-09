namespace RendaTop.App.Pages;

public partial class TelegramChatIdGuidePage : ContentPage
{
    public TelegramChatIdGuidePage()
    {
        InitializeComponent();
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
            return;
        }

        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }
}
