using System.Text;

namespace Netcorext.Logging.HttpClientLogger.Internals;

internal class HttpContentLogValue
{
    private readonly Kind _kind;

    private string _formatted;

    public HttpContentLogValue(Kind kind, HttpContent content)
    {
        _kind = kind;
        Content = content;
    }

    public HttpContent Content { get; }

    public override string ToString()
    {
        if (_formatted == null)
        {
            var builder = new StringBuilder();

            var content = "";

            if (Content != null)
            {
                if (Content.Headers.ContentType != null
                 && (Content.Headers.ContentType.MediaType.Contains("text/")
                  || Content.Headers.ContentType.MediaType.Contains("application/json")
                  || Content.Headers.ContentType.MediaType.Contains("application/xml")
                  || Content.Headers.ContentType.MediaType.Contains("application/x-www-form-urlencoded")))
                {
                    content = Content.ReadAsStringAsync()
                                     .ConfigureAwait(false)
                                     .GetAwaiter()
                                     .GetResult();
                }
                else
                {
                    content = $"Content is empty or Content-Type ({Content.Headers.ContentType?.MediaType}) is not support log.";
                }
            }

            builder.AppendLine(_kind == Kind.Request ? "Request Content:" : "Response Content:");
            builder.AppendLine(content);

            _formatted = builder.ToString();
        }

        return _formatted;
    }
}