using server.Utils;

namespace tests;

public class WWebJsWhatsAppNotificationTests
{
    [Theory]
    [InlineData("65992327494", "556592327494")]
    [InlineData("11987654321", "551187654321")]
    [InlineData("5565987654321", "556587654321")]
    public void NormalizeBrazilMobileWithNinthDigit_RemovesFirstMobileNine_WhenApplicable(string input, string expected)
    {
        var normalized = WWebJsWhatsAppNotification.NormalizeBrazilMobileWithNinthDigit(input);

        Assert.Equal(expected, normalized);
    }
}
