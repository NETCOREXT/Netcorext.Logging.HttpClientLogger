using System.Net;
using Netcorext.Logging.HttpClientLogger.Internals;

namespace Netcorext.Logging.HttpClientLogger;

public class CustomLoggingScopeHttpMessageHandler : DelegatingHandler
{
    private readonly CustomLoggingOptions _options;
    private readonly ILogger _logger;

    public CustomLoggingScopeHttpMessageHandler(CustomLoggingOptions options, ILogger logger)
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

        using (Log.BeginRequestPipelineScope(_logger, request))
        {
            Log.RequestPipelineStart(_options, _logger, request);
            var response = await base.SendAsync(request, cancellationToken);
            Log.RequestPipelineEnd(_options, _logger, response, stopwatch.GetElapsedTime());

            return response;
        }
    }

    private static class Log
    {
        private static readonly Func<ILogger, HttpMethod, Uri?, IDisposable> LogBeginRequestPipelineScope = LoggerMessage.DefineScope<HttpMethod, Uri?>("HTTP {HttpMethod} {Uri}");

        private static readonly Action<ILogger, HttpMethod, Uri?, Exception?> LogRequestPipelineStart = LoggerMessage.Define<HttpMethod, Uri?>(LogLevel.Information,
                                                                                                                                               LoggerEventIds.RequestStart,
                                                                                                                                               "Start processing HTTP request {HttpMethod} {Uri}");

        private static readonly Action<ILogger, double, HttpStatusCode, Exception?> LogRequestPipelineEnd = LoggerMessage.Define<double, HttpStatusCode>(LogLevel.Information,
                                                                                                                                                         LoggerEventIds.RequestEnd,
                                                                                                                                                         "End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}");

        private static readonly Action<ILogger, double, HttpStatusCode, HttpMethod?, Uri?, Exception?> LogRequestPipelineEndTooSlow = LoggerMessage.Define<double, HttpStatusCode, HttpMethod?, Uri?>(LogLevel.Warning,
                                                                                                                                                                                                      LoggerEventIds.RequestEnd,
                                                                                                                                                                                                      "End processing  HTTP request too slow, elapsed: {ElapsedMilliseconds}ms - {StatusCode} - {HttpMethod} {Uri}");


        public static IDisposable BeginRequestPipelineScope(ILogger logger, HttpRequestMessage request)
        {
            return LogBeginRequestPipelineScope(logger, request.Method, request.RequestUri);
        }

        public static void RequestPipelineStart(CustomLoggingOptions options, ILogger logger, HttpRequestMessage request)
        {
            LogRequestPipelineStart(logger, request.Method, request.RequestUri, null);

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

        public static void RequestPipelineEnd(CustomLoggingOptions options, ILogger logger, HttpResponseMessage response, TimeSpan duration)
        {
            if (duration.TotalMilliseconds < options.SlowRequestLoggingThreshold)
            {
                LogRequestPipelineEnd(logger, duration.TotalMilliseconds, response.StatusCode, null);
            }
            else
            {
                LogRequestPipelineEndTooSlow(logger, duration.TotalMilliseconds, response.StatusCode, response.RequestMessage?.Method, response.RequestMessage?.RequestUri, null);
            }

            if (options.LogResponseHeader && logger.IsEnabled(LogLevel.Information))
                logger.Log(
                           LogLevel.Information,
                           LoggerEventIds.RequestHeader,
                           new HttpHeadersLogValue(Kind.Response, response.Headers, response.Content?.Headers),
                           null,
                           (state, ex) => state.ToString());

            if (options.LogResponseBody && logger.IsEnabled(LogLevel.Information))
                logger.Log(
                           LogLevel.Information,
                           LoggerEventIds.ResponseContent,
                           new HttpContentLogValue(Kind.Response, response.Content),
                           null,
                           (state, ex) => state.ToString());
        }
    }
}
