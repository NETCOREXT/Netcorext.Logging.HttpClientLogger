namespace Netcorext.Logging.HttpClientLogger.Internals;

internal class LoggerEventIds
{
    public static readonly EventId RequestStart = new(100, "RequestStart");
    public static readonly EventId RequestEnd = new(101, "RequestEnd");

    public static readonly EventId RequestHeader = new(102, "RequestHeader");
    public static readonly EventId ResponseHeader = new(103, "ResponseHeader");

    public static readonly EventId RequestContent = new(104, "RequestContent");
    public static readonly EventId ResponseContent = new(105, "ResponseContent");
}
