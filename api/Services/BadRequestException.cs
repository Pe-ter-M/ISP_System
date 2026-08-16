namespace InternetProvider.Api.Services;

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}