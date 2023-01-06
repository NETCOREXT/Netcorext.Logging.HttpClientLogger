using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Netcorext.Logging.HttpClientLogger;

public static class HttpClientBuilderExtension
{
    public static IHttpClientBuilder AddLoggingHttpMessage(this IHttpClientBuilder builder)
    {
        builder.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
        builder.Services.TryAddSingleton<CustomLoggingOptions>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, CustomLoggingHttpMessageHandlerBuilderFilter>());

        return builder;
    }

    public static IHttpClientBuilder AddLoggingHttpMessage(this IHttpClientBuilder builder, Action<IServiceProvider, CustomLoggingOptions>? configure)
    {
        builder.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
        builder.Services.TryAddSingleton<CustomLoggingOptions>(provider =>
                                                               {
                                                                   var options = new CustomLoggingOptions();
                                                                   
                                                                   configure?.Invoke(provider, options);

                                                                   return options;
                                                               });
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, CustomLoggingHttpMessageHandlerBuilderFilter>());

        return builder;
    }
}