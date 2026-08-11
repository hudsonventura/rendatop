using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using RendaTop.App.Services;

namespace RendaTop.App;

[Activity(Theme = "@style/RendaTop.MainTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _ = ReceiveSharedDocumentAsync(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        _ = ReceiveSharedDocumentAsync(intent);
    }

    private async Task ReceiveSharedDocumentAsync(Intent? intent)
    {
        if (intent is null || !string.Equals(intent.Action, Intent.ActionSend, StringComparison.Ordinal))
            return;

        var sharedUri = GetSharedUri(intent);
        if (sharedUri is null)
            return;

        try
        {
            var service = IPlatformApplication.Current?.Services.GetService<SharedInvestmentDocumentService>();
            if (service is null || ContentResolver is null || CacheDir is null)
                return;

            using var source = ContentResolver.OpenInputStream(sharedUri);
            if (source is null)
                return;

            var contentType = intent.Type ?? ContentResolver.GetType(sharedUri);
            var sourceName = Uri.UnescapeDataString(sharedUri.LastPathSegment ?? "comprovante");
            var fileName = Path.GetFileName(sourceName);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "comprovante";

            var extension = Path.GetExtension(fileName);
            var targetPath = Path.Combine(CacheDir.AbsolutePath!, $"shared-investment-{Guid.NewGuid():N}{extension}");
            await using (var target = File.Create(targetPath))
                await source.CopyToAsync(target);

            service.Add(targetPath, fileName, contentType);
        }
        catch
        {
            // A provider may revoke the temporary URI before it can be read.
        }
    }

    private static Android.Net.Uri? GetSharedUri(Intent intent)
    {
        Android.Net.Uri? sharedUri;
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            sharedUri = intent.GetParcelableExtra(
                Intent.ExtraStream,
                Java.Lang.Class.FromType(typeof(Android.Net.Uri))) as Android.Net.Uri;
        }
        else
        {
#pragma warning disable CA1422
            sharedUri = intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri;
#pragma warning restore CA1422
        }

        return sharedUri ?? intent.ClipData?.GetItemAt(0)?.Uri;
    }
}
