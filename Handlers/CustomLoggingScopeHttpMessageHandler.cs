using System.Net;
using Microsoft.Extensions.Logging;
using Netcorext.Logging.HttpClientLogger.Internals;

namespace Netcorext.Logging.HttpClientLogger;

public class CustomLoggingScopeHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger _logger;

    public CustomLoggingScopeHttpMessageHandler(ILogger logger)
    {
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
            Log.RequestPipelineStart(_logger, request);
            var response = await base.SendAsync(request, cancellationToken);
            Log.RequestPipelineEnd(_logger, response, stopwatch.GetElapsedTime());

            return response;
        }
    }

    private static class Log
    {
        public static class EventIds
        {
            public static readonly EventId PipelineStart = new EventId(100, "RequestPipelineStart");
            public static readonly EventId PipelineEnd = new EventId(101, "RequestPipelineEnd");

            public static readonly EventId RequestHeader = new EventId(102, "RequestPipelineRequestHeader");
            public static readonly EventId ResponseHeader = new EventId(103, "RequestPipelineResponseHeader");

            public static readonly EventId RequestContent = new EventId(104, "RequestPipelineContent");
            public static readonly EventId ResponseContent = new EventId(105, "ResponsePipelineContent");
        }

        private static readonly Func<ILogger, HttpMethod, Uri, IDisposable> _beginRequestPipelineScope = LoggerMessage.DefineScope<HttpMethod, Uri>("HTTP {HttpMethod} {Uri}");

        private static readonly Action<ILogger, HttpMethod, Uri, Exception> _requestPipelineStart = LoggerMessage.Define<HttpMethod, Uri>(
                                                                                                                                          LogLevel.Information,
                                                                                                                                          EventIds.PipelineStart,
                                                                                                                                          "Start processing HTTP request {HttpMethod} {Uri}");

        private static readonly Action<ILogger, double, HttpStatusCode, Exception> _requestPipelineEnd = LoggerMessage.Define<double, HttpStatusCode>(
                                                                                                                                                      LogLevel.Information,
                                                                                                                                                      EventIds.PipelineEnd,
                                                                                                                                                      "End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}");

        public static IDisposable BeginRequestPipelineScope(ILogger logger, HttpRequestMessage request)
        {
            return _beginRequestPipelineScope(logger, request.Method, request.RequestUri);
        }

        public static void RequestPipelineStart(ILogger logger, HttpRequestMessage request)
        {
            _requestPipelineStart(logger, request.Method, request.RequestUri, null);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(
                           LogLevel.Trace,
                           EventIds.RequestHeader,
                           new HttpHeadersLogValue(Kind.Request, request.Headers, request.Content?.Headers),
                           null,
                           (state, ex) => state.ToString());

                logger.Log(
                           LogLevel.Trace,
                           EventIds.RequestContent,
                           new HttpContentLogValue(Kind.Request, request.Content),
                           null,
                           (state, ex) => state.ToString());
            }
        }

        public static void RequestPipelineEnd(ILogger logger, HttpResponseMessage response, TimeSpan duration)
        {
            _requestPipelineEnd(logger, duration.TotalMilliseconds, response.StatusCode, null);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(
                           LogLevel.Trace,
                           EventIds.ResponseHeader,
                           new HttpHeadersLogValue(Kind.Response, response.Headers, response.Content?.Headers),
                           null,
                           (state, ex) => state.ToString());

                logger.Log(
                           LogLevel.Trace,
                           EventIds.ResponseContent,
                           new HttpContentLogValue(Kind.Response, response.Content),
                           null,
                           (state, ex) => state.ToString());
            }
        }
    }
}