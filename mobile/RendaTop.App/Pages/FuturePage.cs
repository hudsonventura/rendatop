using Microsoft.Maui.Controls.Shapes;

namespace RendaTop.App.Pages;

public sealed class FuturePage : ContentPage
{
    public FuturePage(string title, string description)
    {
        Title = title;
        BackgroundColor = Color.FromArgb("#F8FAFC");

        Content = new Grid
        {
            Padding = 24,
            Children =
            {
                new Border
                {
                    Stroke = Color.FromArgb("#E2E8F0"),
                    BackgroundColor = Colors.White,
                    Padding = 22,
                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                    VerticalOptions = LayoutOptions.Center,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new Label
                            {
                                Text = title,
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#111827")
                            },
                            new Label
                            {
                                Text = description,
                                TextColor = Color.FromArgb("#475569"),
                                LineBreakMode = LineBreakMode.WordWrap
                            },
                            new Label
                            {
                                Text = "Quando esta tela for implementada, o app consultara o backend para respeitar os recursos do plano Free e dos planos pagos.",
                                TextColor = Color.FromArgb("#64748B"),
                                LineBreakMode = LineBreakMode.WordWrap
                            }
                        }
                    }
                }
            }
        };
    }
}
