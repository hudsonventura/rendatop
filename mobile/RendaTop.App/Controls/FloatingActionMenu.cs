using System.Windows.Input;

namespace RendaTop.App.Controls;

public sealed class FloatingActionMenu : ContentView
{
    public static readonly BindableProperty AddCommandProperty =
        BindableProperty.Create(nameof(AddCommand), typeof(ICommand), typeof(FloatingActionMenu), null, propertyChanged: OnCommandChanged);

    public static readonly BindableProperty EditCommandProperty =
        BindableProperty.Create(nameof(EditCommand), typeof(ICommand), typeof(FloatingActionMenu), null, propertyChanged: OnCommandChanged);

    public static readonly BindableProperty RedeemCommandProperty =
        BindableProperty.Create(nameof(RedeemCommand), typeof(ICommand), typeof(FloatingActionMenu), null, propertyChanged: OnCommandChanged);

    public static readonly BindableProperty ReinvestCommandProperty =
        BindableProperty.Create(nameof(ReinvestCommand), typeof(ICommand), typeof(FloatingActionMenu), null, propertyChanged: OnCommandChanged);

    public static readonly BindableProperty ArchiveCommandProperty =
        BindableProperty.Create(nameof(ArchiveCommand), typeof(ICommand), typeof(FloatingActionMenu), null, propertyChanged: OnCommandChanged);

    public static readonly BindableProperty DeleteCommandProperty =
        BindableProperty.Create(nameof(DeleteCommand), typeof(ICommand), typeof(FloatingActionMenu), null, propertyChanged: OnCommandChanged);

    private readonly BoxView _backdrop;
    private readonly VerticalStackLayout _menu;
    private readonly ImageButton _fabButton;
    private readonly Button _addButton;
    private readonly Button _editButton;
    private readonly Button _redeemButton;
    private readonly Button _reinvestButton;
    private readonly Button _archiveButton;
    private readonly Button _deleteButton;
    private bool _isOpen;

    public FloatingActionMenu()
    {
        _backdrop = new BoxView
        {
            BackgroundColor = Color.FromArgb("#01000000"),
            IsVisible = false
        };
        _backdrop.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SetOpen(false)) });

        _addButton = CreatePrimaryButton("Novo investimento", "icon_add_white.svg");
        _editButton = CreatePrimaryButton("Editar", "icon_edit_white.svg");
        _redeemButton = CreateSecondaryButton("Resgatar", "icon_redeem_dark.svg");
        _reinvestButton = CreateSecondaryButton("Reinvestir", "icon_reinvest_dark.svg");
        _archiveButton = CreateSecondaryButton("Arquivar", "icon_archive_dark.svg");
        _deleteButton = CreateDangerButton("Excluir", "icon_delete_dark.svg");

        _menu = new VerticalStackLayout
        {
            Spacing = 10,
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 72),
            IsVisible = false,
            ZIndex = 11,
            Children =
            {
                _addButton,
                _editButton,
                _redeemButton,
                _reinvestButton,
                _archiveButton,
                _deleteButton
            }
        };

        _fabButton = new ImageButton
        {
            Source = "icon_menu_white.svg",
            WidthRequest = 60,
            HeightRequest = 60,
            CornerRadius = 30,
            BackgroundColor = Color.FromArgb("#EC4899"),
            Padding = 18,
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.End,
            ZIndex = 12
        };
        _fabButton.Clicked += (_, _) => SetOpen(!_isOpen);

        Content = new Grid
        {
            Padding = new Thickness(18, 16, 18, 18),
            ZIndex = 10,
            Children =
            {
                _backdrop,
                _menu,
                _fabButton
            }
        };

        RefreshButtons();
    }

    public ICommand? AddCommand
    {
        get => (ICommand?)GetValue(AddCommandProperty);
        set => SetValue(AddCommandProperty, value);
    }

    public ICommand? EditCommand
    {
        get => (ICommand?)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand? RedeemCommand
    {
        get => (ICommand?)GetValue(RedeemCommandProperty);
        set => SetValue(RedeemCommandProperty, value);
    }

    public ICommand? ReinvestCommand
    {
        get => (ICommand?)GetValue(ReinvestCommandProperty);
        set => SetValue(ReinvestCommandProperty, value);
    }

    public ICommand? ArchiveCommand
    {
        get => (ICommand?)GetValue(ArchiveCommandProperty);
        set => SetValue(ArchiveCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    private static void OnCommandChanged(BindableObject bindable, object? oldValue, object? newValue)
        => ((FloatingActionMenu)bindable).RefreshButtons();

    private void RefreshButtons()
    {
        WireButton(_addButton, AddCommand, visible: true);
        WireButton(_editButton, EditCommand, visible: EditCommand is not null);
        WireButton(_redeemButton, RedeemCommand, visible: RedeemCommand is not null);
        WireButton(_reinvestButton, ReinvestCommand, visible: ReinvestCommand is not null);
        WireButton(_archiveButton, ArchiveCommand, visible: ArchiveCommand is not null);
        WireButton(_deleteButton, DeleteCommand, visible: DeleteCommand is not null);
    }

    private void WireButton(Button button, ICommand? command, bool visible)
    {
        button.IsVisible = visible;
        button.Command = command is null ? null : new Command(() =>
        {
            SetOpen(false);
            if (command.CanExecute(null))
                command.Execute(null);
        });
    }

    private void SetOpen(bool isOpen)
    {
        _isOpen = isOpen;
        _backdrop.IsVisible = isOpen;
        _menu.IsVisible = isOpen;
        _fabButton.Source = isOpen ? "icon_close_white.svg" : "icon_menu_white.svg";
    }

    private static Button CreatePrimaryButton(string text, string icon)
        => new()
        {
            Text = text,
            ImageSource = icon,
            ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 10),
            BackgroundColor = Color.FromArgb("#111827"),
            TextColor = Colors.White,
            CornerRadius = 18,
            Padding = new Thickness(16, 12)
        };

    private static Button CreateSecondaryButton(string text, string icon)
        => new()
        {
            Text = text,
            ImageSource = icon,
            ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 10),
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#CBD5E1"),
            BorderWidth = 1,
            TextColor = Color.FromArgb("#111827"),
            CornerRadius = 18,
            Padding = new Thickness(16, 12)
        };

    private static Button CreateDangerButton(string text, string icon)
        => new()
        {
            Text = text,
            ImageSource = icon,
            ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 10),
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#FCA5A5"),
            BorderWidth = 1,
            TextColor = Color.FromArgb("#991B1B"),
            CornerRadius = 18,
            Padding = new Thickness(16, 12)
        };
}
