namespace Netcorext.Logging.HttpClientLogger;

public class CustomLoggingOptions
{
    public bool LogRequestHeader { get; set; } = true;
    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseHeader { get; set; } = true;
    public bool LogResponseBody { get; set; } = true;
    public long SlowRequestLoggingThreshold { get; set; } = 1000;
}
