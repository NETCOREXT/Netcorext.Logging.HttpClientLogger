namespace Netcorext.Logging.HttpClientLogger;

public class CustomLoggingOptions
{
    public bool LogRequestHeader { get; set; }
    public bool LogRequestBody { get; set; }
    public bool LogResponseHeader { get; set; }
    public bool LogResponseBody { get; set; }
    public long SlowRequestLoggingThreshold { get; set; } = 2 * 1000;
}