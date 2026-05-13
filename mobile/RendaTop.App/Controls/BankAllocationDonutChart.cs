using Microsoft.Maui.Graphics;
using RendaTop.App.Models;

namespace RendaTop.App.Controls;

public sealed class BankAllocationDonutChart : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IReadOnlyList<BankAllocationItem>),
            typeof(BankAllocationDonutChart),
            Array.Empty<BankAllocationItem>(),
            propertyChanged: OnItemsChanged);

    private readonly GraphicsView _graphicsView;
    private readonly Label _centerValueLabel;
    private readonly Label _centerCaptionLabel;
    private readonly DonutDrawable _drawable = new();

    public BankAllocationDonutChart()
    {
        _graphicsView = new GraphicsView
        {
            Drawable = _drawable,
            HeightRequest = 220,
            WidthRequest = 220,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _centerValueLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827")
        };

        _centerCaptionLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 12,
            TextColor = Color.FromArgb("#64748B"),
            Text = "alocado"
        };

        Content = new Grid
        {
            HeightRequest = 220,
            WidthRequest = 220,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                _graphicsView,
                new VerticalStackLayout
                {
                    Spacing = 2,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        _centerValueLabel,
                        _centerCaptionLabel
                    }
                }
            }
        };

        UpdateState();
    }

    public IReadOnlyList<BankAllocationItem> ItemsSource
    {
        get => (IReadOnlyList<BankAllocationItem>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsChanged(BindableObject bindable, object? oldValue, object? newValue)
        => ((BankAllocationDonutChart)bindable).UpdateState();

    private void UpdateState()
    {
        var items = ItemsSource ?? Array.Empty<BankAllocationItem>();
        _drawable.Items = items.Where(item => item.Percent > 0).ToList();
        _graphicsView.Invalidate();

        var top = _drawable.Items
            .OrderByDescending(item => item.Percent)
            .FirstOrDefault();

        _centerValueLabel.Text = top is null ? "0%" : top.PercentText;
        _centerCaptionLabel.Text = top is null ? "sem dados" : top.BankName;
    }

    private sealed class DonutDrawable : IDrawable
    {
        public IReadOnlyList<BankAllocationItem> Items { get; set; } = Array.Empty<BankAllocationItem>();

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
            var stroke = size * 0.18f;
            var padding = stroke / 2f + 6f;
            var rect = new RectF(
                dirtyRect.Center.X - (size / 2f) + padding,
                dirtyRect.Center.Y - (size / 2f) + padding,
                size - (padding * 2f),
                size - (padding * 2f));

            canvas.StrokeSize = stroke;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = Color.FromArgb("#E2E8F0");
            canvas.DrawArc(rect, -90, 359.9f, false, false);

            if (Items.Count == 0)
                return;

            var startAngle = -90f;
            const float gapAngle = 2f;

            foreach (var item in Items)
            {
                var sweep = (float)(item.Percent * 360d);
                if (sweep <= 0.1f)
                    continue;

                var adjustedSweep = Math.Max(1.5f, sweep - gapAngle);
                canvas.StrokeColor = item.DisplayColor;
                canvas.DrawArc(rect, startAngle, adjustedSweep, false, false);
                startAngle += sweep;
            }
        }
    }
}
