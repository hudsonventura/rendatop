namespace RendaTop.App.Services;

public sealed class ApiException : Exception
{
    public ApiException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
