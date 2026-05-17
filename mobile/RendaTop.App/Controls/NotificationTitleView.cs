using Microsoft.Maui.Controls.Shapes;
using RendaTop.App.Services;

namespace RendaTop.App.Controls;

public sealed class NotificationTitleView : Grid
{
    private readonly NotificationService _notifications;
    private readonly Label _titleLabel;
    private readonly Border _badge;
    private readonly Label _badgeLabel;
    private bool _subscribed;

    public NotificationTitleView(string title, NotificationService notifications)
    {
        _notifications = notifications;

        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        ColumnSpacing = 12;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        MinimumHeightRequest = 40;

        _titleLabel = new Label
        {
            Text = title,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827"),
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var bellButton = new ImageButton
        {
            Source = "icon_bell_dark.svg",
            BackgroundColor = Colors.Transparent,
            HeightRequest = 40,
            WidthRequest = 40,
            Padding = 8,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        bellButton.Clicked += OnBellClicked;

        _badgeLabel = new Label
        {
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        _badge = new Border
        {
            BackgroundColor = Color.FromArgb("#DC2626"),
            StrokeThickness = 0,
            Padding = new Thickness(5, 1),
            MinimumWidthRequest = 18,
            HeightRequest = 18,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            TranslationX = 2,
            TranslationY = -2,
            IsVisible = false,
            Content = _badgeLabel,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 9
            }
        };

        var bellHost = new Grid
        {
            WidthRequest = 40,
            HeightRequest = 40,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        bellHost.Children.Add(bellButton);
        bellHost.Children.Add(_badge);

        Add(_titleLabel);
        Add(bellHost);
        _titleLabel.SetValue(ColumnProperty, 0);
        bellHost.SetValue(ColumnProperty, 1);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string PageTitle
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _notifications.RefreshUnreadCountAsync();
        }
        catch
        {
            UpdateBadge(_notifications.UnreadCount);
        }
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (!_subscribed)
        {
            _notifications.UnreadCountChanged += OnUnreadCountChanged;
            _subscribed = true;
        }

        UpdateBadge(_notifications.UnreadCount);
        await RefreshAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (!_subscribed)
            return;

        _notifications.UnreadCountChanged -= OnUnreadCountChanged;
        _subscribed = false;
    }

    private void OnUnreadCountChanged(object? sender, int count)
        => MainThread.BeginInvokeOnMainThread(() => UpdateBadge(count));

    private async void OnBellClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//notifications");

    private void UpdateBadge(int count)
    {
        _badge.IsVisible = count > 0;
        _badgeLabel.Text = count > 99 ? "99+" : count.ToString();
    }
}
