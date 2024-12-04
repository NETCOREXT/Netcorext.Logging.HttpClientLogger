namespace Netcorext.Logging.HttpClientLogger;

public class CustomLoggingOptions
{
    public bool LogRequestHeader { get; set; } = true;
    public bool LogRequestBody { get; set; } = false;
    public bool LogResponseHeader { get; set; } = true;
    public bool LogResponseBody { get; set; } = false;
    public long SlowRequestLoggingThreshold { get; set; } = 200;
}
