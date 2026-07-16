using System.Text.Json.Serialization;

namespace InternetProvider.Api.Services;

public class ApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "OK";

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonIgnore]
    public int HttpStatusCode { get; set; } = 200;

    public static ApiResponse Success(object? data = null, string message = "OK")
    {
        return new ApiResponse { Status = "success", Message = message, Data = data, HttpStatusCode = 200 };
    }

    public static ApiResponse Created(object? data = null, string message = "Created")
    {
        return new ApiResponse { Status = "success", Message = message, Data = data, HttpStatusCode = 201 };
    }

    public static ApiResponse Error(string message, int statusCode = 400)
    {
        return new ApiResponse { Status = "error", Message = message, HttpStatusCode = statusCode };
    }

    public IResult ToResult()
    {
        return HttpStatusCode switch
        {
            201 => Results.Created(string.Empty, this),
            401 => Results.Json(this, statusCode: 401),
            403 => Results.Json(this, statusCode: 403),
            404 => Results.Json(this, statusCode: 404),
            409 => Results.Json(this, statusCode: 409),
            _ => Results.Ok(this)
        };
    }
}
