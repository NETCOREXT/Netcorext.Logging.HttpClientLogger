using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Netcorext.Logging.HttpClientLogger;

public static class HttpClientBuilderExtension
{
    public static IHttpClientBuilder AddLoggingHttpMessage(this IHttpClientBuilder builder)
    {
        builder.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, CustomLoggingHttpMessageHandlerBuilderFilter>());

        return builder;
    }
}