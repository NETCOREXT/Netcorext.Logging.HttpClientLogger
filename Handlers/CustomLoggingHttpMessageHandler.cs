using System.Net;
using Netcorext.Logging.HttpClientLogger.Internals;

namespace Netcorext.Logging.HttpClientLogger;

public class CustomLoggingHttpMessageHandler : DelegatingHandler
{
    private readonly CustomLoggingOptions _options;
    private readonly ILogger _logger;

    public CustomLoggingHttpMessageHandler(CustomLoggingOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var stopwatch = ValueStopwatch.StartNew();

        // Not using a scope here because we always expect this to be at the end of the pipeline, thus there's
        // not really anything to surround.
        Log.RequestStart(_options, _logger, request);
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        Log.RequestEnd(_options, _logger, response, stopwatch.GetElapsedTime());

        return response;
    }

    private static class Log
    {
        public static class EventIds
        {
            public static readonly EventId RequestStart = new(100, "RequestStart");
            public static readonly EventId RequestEnd = new(101, "RequestEnd");

            public static readonly EventId RequestHeader = new(102, "RequestHeader");
            public static readonly EventId ResponseHeader = new(103, "ResponseHeader");

            public static readonly EventId RequestContent = new(104, "RequestContent");
            public static readonly EventId ResponseContent = new(105, "ResponseContent");
        }

        private static readonly Action<ILogger, HttpMethod, Uri, Exception> _requestStart = LoggerMessage.Define<HttpMethod, Uri>(
                                                                                                                                  LogLevel.Information,
                                                                                                                                  EventIds.RequestStart,
                                                                                                                                  "Sending HTTP request {HttpMethod} {Uri}");

        private static readonly Action<ILogger, double, HttpStatusCode, Exception> _requestEnd = LoggerMessage.Define<double, HttpStatusCode>(
                                                                                                                                              LogLevel.Information,
                                                                                                                                              EventIds.RequestEnd,
                                                                                                                                              "Received HTTP response after {ElapsedMilliseconds}ms - {StatusCode}");

        private static readonly Action<ILogger, double, HttpStatusCode, Exception> _requestEndTooSlow = LoggerMessage.Define<double, HttpStatusCode>(
                                                                                                                                                     LogLevel.Warning,
                                                                                                                                                     EventIds.RequestEnd,
                                                                                                                                                     "Received HTTP response too slow, elapsed: {ElapsedMilliseconds}ms - {StatusCode}");

        public static void RequestStart(CustomLoggingOptions options, ILogger logger, HttpRequestMessage request)
        {
            _requestStart(logger, request.Method, request.RequestUri, null);

            if (options.LogRequestHeader && logger.IsEnabled(LogLevel.Debug))
                logger.Log(
                           LogLevel.Debug,
                           EventIds.RequestHeader,
                           new HttpHeadersLogValue(Kind.Request, request.Headers, request.Content?.Headers),
                           null,
                           (state, ex) => state.ToString());

            if (options.LogRequestBody && logger.IsEnabled(LogLevel.Debug))
                logger.Log(
                           LogLevel.Debug,
                           EventIds.RequestContent,
                           new HttpContentLogValue(Kind.Request, request.Content),
                           null,
                           (state, ex) => state.ToString());
        }

        public static void RequestEnd(CustomLoggingOptions options, ILogger logger, HttpResponseMessage response, TimeSpan duration)
        {
            if (duration.TotalMilliseconds < options.SlowRequestLoggingThreshold)
            {
                _requestEnd(logger, duration.TotalMilliseconds, response.StatusCode, null);
            }
            else
            {
                _requestEndTooSlow(logger, duration.TotalMilliseconds, response.StatusCode, null);
            }

            if (options.LogResponseHeader && logger.IsEnabled(LogLevel.Debug))
                logger.Log(
                           LogLevel.Debug,
                           EventIds.ResponseHeader,
                           new HttpHeadersLogValue(Kind.Response, response.Headers, response.Content?.Headers),
                           null,
                           (state, ex) => state.ToString());

            if (options.LogResponseBody && logger.IsEnabled(LogLevel.Debug))
                logger.Log(
                           LogLevel.Debug,
                           EventIds.ResponseContent,
                           new HttpContentLogValue(Kind.Response, response.Content),
                           null,
                           (state, ex) => state.ToString());
        }
    }
}