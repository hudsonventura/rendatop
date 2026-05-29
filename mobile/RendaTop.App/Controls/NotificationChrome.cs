using RendaTop.App.Services;

namespace RendaTop.App.Controls;

public static class NotificationChrome
{
    public static NotificationTitleView Apply(ContentPage page, string title, NotificationService notifications)
    {
        page.Title = title;
        var titleView = new NotificationTitleView(title, notifications);
        Shell.SetTitleView(page, titleView);
        return titleView;
    }

}
