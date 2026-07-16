namespace InternetProvider.Api.Services;

/// <summary>
/// Global marker class for ILogger. Non-static so ILogger{T} can be used
/// in static endpoint classes. Use: ILogger&lt;LoggerMarker&gt;
/// </summary>
public class LoggerMarker { }
