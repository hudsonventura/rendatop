namespace api.services;

/// <summary>
/// Classe que representa o resultado de uma operação.
/// </summary>
public class Result
{
    /// <summary>
    /// Indica se a operação foi bem sucedida.
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// Indica se a operação falhou.
    /// </summary>
    public bool IsFailure { get; protected set; }

    /// <summary>
    /// Mensagem que descreve o resultado da operação.
    /// </summary>
    public string? Message { get; protected set; }

    /// <summary>
    /// Status da operação.
    /// </summary>
    protected Result.Status _status { get;  set; }

    /// <summary>
    /// Exceção que ocorreu durante a operação.
    /// </summary>
    public Exception? Exception { get;  protected set; }



    public static Result Failure() => new Result<dynamic>(false, null, "Failure");
    public static Result Failure(string error) => new Result<dynamic>(false, null, error);
    public static Result Failure(Exception exception) => new Result<dynamic>(false, null, "SHOW EXCEPTION", exception);
    public static Result Failure(string error, Exception exception) => new Result<dynamic>(false, null, error, exception);
    public static Result Failure(Exception exception, string error) => new Result<dynamic>(false, null, error, exception);
    
    public static Result Failure<T>(string error, T content) => new Result<T>(false, content, error);
    public static Result Failure<T>(T content, string error) => new Result<T>(false, content, error);



    public static Result Success() => new Result<dynamic>(true, null);

    public static Result<T> Success<T>(T content) => new Result<T>(true, content);
    public static Result Success(string message) => new Result<string>(true, default, message);
    public static Result<T> Failure<T>(string message) => new Result<T>(false, default(T), message);
    public static Result<T> Failure<T>(Exception exception) => new Result<T>(false, default(T), null, exception);

    public static Result<T> Failure<T>(string message, Exception exception) => new Result<T>(false, default(T), message, exception);

    public enum Status
    {
        Success,
        Failure
    }

}

public class Result<T> : Result
{
    public T data { get; private set; }

    internal Result(bool isSuccess, T Content, string Message = "Success", Exception Exception = null)
    {
        IsSuccess = isSuccess;
        IsFailure = !isSuccess;
        data = Content;
        Message = Message;
        _status = isSuccess ? Status.Success : Status.Failure;
        Exception = Exception;

        if (Message == "SHOW EXCEPTION" && Exception != null)
        {
            var current = Exception;
            var messages = new List<string>();

            while (current != null)
            {
                messages.Add(current.Message);
                current = current.InnerException;
            }

            Message = string.Join(" --> ", messages);
        }

    }
    public static Result<T> Failure(string message) => new Result<T>(false, default(T), message);
    public static Result<T> Failure(T content) => new Result<T>(false, content);
    public static Result<T> Failure(Exception exception)
    {
        string msg = exception.Message;
        while (exception.InnerException != null)
        {
            exception = exception.InnerException;
            msg += " --> " + exception.Message;
        }
        return new Result<T>(false, default(T), msg, exception);
    }


    public static Result<T> Failure(Exception exception, string error) => new Result<T>(false, default(T), error, exception);
    public static Result<T> Failure(string error, Exception exception) => new Result<T>(false, default(T), error, exception);

}