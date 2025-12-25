namespace FindTime.Common;

public class ServiceResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public List<string>? Errors { get; set; }

    // Success response
    public static ServiceResponse<T> SuccessResponse(T data, string message = "Success", int statusCode = 200)
    {
        return new ServiceResponse<T>
        {
            Data = data,
            Success = true,
            Message = message,
            StatusCode = statusCode
        };
    }

    // Error response
    public static ServiceResponse<T> ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ServiceResponse<T>
        {
            Data = default,
            Success = false,
            Message = message,
            StatusCode = statusCode,
            Errors = errors
        };
    }

    // Not Found response
    public static ServiceResponse<T> NotFoundResponse(string message = "Resource not found")
    {
        return new ServiceResponse<T>
        {
            Data = default,
            Success = false,
            Message = message,
            StatusCode = 404
        };
    }

    // Unauthorized response
    public static ServiceResponse<T> UnauthorizedResponse(string message = "Unauthorized")
    {
        return new ServiceResponse<T>
        {
            Data = default,
            Success = false,
            Message = message,
            StatusCode = 401
        };
    }

    // Forbidden response
    public static ServiceResponse<T> ForbiddenResponse(string message = "Forbidden")
    {
        return new ServiceResponse<T>
        {
            Data = default,
            Success = false,
            Message = message,
            StatusCode = 403
        };
    }
}