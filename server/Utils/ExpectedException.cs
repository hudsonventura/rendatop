using System.Net;

namespace server.Utils;

public class ExpectedException : Exception
{
    public HttpStatusCode StatusCode { get; set; }

    public ExpectedException(string message, HttpStatusCode StatusCode = HttpStatusCode.BadRequest) : base(message)
    {
        this.StatusCode = StatusCode;
    }
}
