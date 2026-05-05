using System.Net;
using System.Text.Json;
using server.Domain;
using server.Utils;

namespace server.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly static List<string> _tags = new() { "GlobalExceptionMiddleware", "Middleware" };


    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger; //Permite o uso de logs. Pode ser removido
    }


    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // Chama o próximo middleware
        }
        catch (ExpectedException ex)
        {
            await HandleExceptionAsync(context, ex, _logger); // Trata a exceção
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, _logger); // Trata a exceção
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, ExpectedException exception, ILogger logger)
    {
        return HandleExceptionAsync(context, exception, exception.StatusCode, logger);
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        return HandleExceptionAsync(context, exception, HttpStatusCode.InternalServerError, logger);
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode, ILogger logger)
    {
        var tags = _tags;
        tags.Add("HTTPResponse");
        var traceId = context.TraceIdentifier;
        logger.LogError(
            exception,
            "Ocorreu uma exceção global no GlobalExceptionMiddleware. TraceId={TraceId} Method={Method} Path={Path} StatusCode={StatusCode} Tags={_tags_}",
            traceId,
            context.Request.Method,
            context.Request.Path,
            (int)statusCode, 
            tags);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        string message = exception.Message;
        var inner = exception.InnerException;
        while (inner is not null)
        {
            message += "\n→ " + inner.Message;
            inner = inner.InnerException;
        }

        return context.Response.WriteAsync(message);
    }
}
