using OPSSHAssistant.Core.Data;

namespace OPSSHAssistant.Core;

public class OPResult<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool Cancelled { get; set; }

    private OPResult()
    {

    }

    public static OPResult<T> FromCancelled()
    {
        return new OPResult<T>() { Cancelled = true, };
    }

    public static OPResult<T> FromSuccess(T data)
    {
        return new OPResult<T>() { Data = data, Success = true, };
    }

    public static OPResult<T> FromFailed(string errorMessage)
    {
        return new OPResult<T>() { ErrorMessage = errorMessage, };
    }
}
