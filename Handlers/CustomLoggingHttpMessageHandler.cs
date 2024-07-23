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
        private static readonly Action<ILogger, HttpMethod, Uri?, Exception?> LogRequestStart = LoggerMessage.Define<HttpMethod, Uri?>(LogLevel.Information,
                                                                                                                                       LoggerEventIds.RequestStart,
                                                                                                                                       "Sending HTTP request {HttpMethod} {Uri}");

        private static readonly Action<ILogger, double, HttpStatusCode, Exception?> LogRequestEnd = LoggerMessage.Define<double, HttpStatusCode>(LogLevel.Information,
                                                                                                                                                 LoggerEventIds.RequestEnd,
                                                                                                                                                 "Received HTTP response after {ElapsedMilliseconds}ms - {StatusCode}");

        private static readonly Action<ILogger, double, HttpStatusCode, Exception?> LogRequestEndTooSlow = LoggerMessage.Define<double, HttpStatusCode>(LogLevel.Warning,
                                                                                                                                                        LoggerEventIds.RequestEnd,
                                                                                                                                                        "Received HTTP response too slow, elapsed: {ElapsedMilliseconds}ms - {StatusCode}");

        public static void RequestStart(CustomLoggingOptions options, ILogger logger, HttpRequestMessage request)
        {
            LogRequestStart(logger, request.Method, request.RequestUri, null);

            if (options.LogRequestHeader && logger.IsEnabled(LogLevel.Information))
                logger.Log(LogLevel.Information,
                           LoggerEventIds.RequestHeader,
                           new HttpHeadersLogValue(Kind.Request, request.Headers, request.Content?.Headers),
                           null,
                           (state, ex) => state.ToString());

            if (options.LogRequestBody && logger.IsEnabled(LogLevel.Information))
                logger.Log(LogLevel.Information,
                           LoggerEventIds.RequestContent,
                           new HttpContentLogValue(Kind.Request, request.Content),
                           null,
                           (state, ex) => state.ToString());
        }

        public static void RequestEnd(CustomLoggingOptions options, ILogger logger, HttpResponseMessage response, TimeSpan duration)
        {
            if (duration.TotalMilliseconds < options.SlowRequestLoggingThreshold)
            {
                LogRequestEnd(logger, duration.TotalMilliseconds, response.StatusCode, null);
            }
            else
            {
                LogRequestEndTooSlow(logger, duration.TotalMilliseconds, response.StatusCode, null);
            }

            if (options.LogResponseHeader && logger.IsEnabled(LogLevel.Information))
                logger.Log(LogLevel.Information,
                           LoggerEventIds.ResponseHeader,
                           new HttpHeadersLogValue(Kind.Response, response.Headers, response.Content?.Headers),
                           null,
                           (state, ex) => state.ToString());

            if (options.LogResponseBody && logger.IsEnabled(LogLevel.Debug))
                logger.Log(LogLevel.Information,
                           LoggerEventIds.ResponseContent,
                           new HttpContentLogValue(Kind.Response, response.Content),
                           null,
                           (state, ex) => state.ToString());
        }
    }
}
