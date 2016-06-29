using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using WebMarkupMin.Core;

namespace StaticWebHelper
{
    public class FingerPrintHandler : IHttpHandler
    {
        private static Regex _regex = new Regex(@"<(link|script|img).+(href|src)=(?<quote>\""|'|)(?<href>[^\""']+\.(css|js|png|jpg|jpeg|gif|ico|svg|json|xml|woff|ttf|eot))\k<quote>.*?>");
        private static string _cdnPath = ConfigurationManager.AppSettings.Get("cdnPath");

        public FingerPrintHandler()
        {
            Uri url;

            if (!string.IsNullOrEmpty(_cdnPath) && !Uri.TryCreate(_cdnPath, UriKind.Absolute, out url))
                throw new UriFormatException("The appSetting 'cdnPath' is not a valid absolute URL");
        }

        public void ProcessRequest(HttpContext context)
        {
            string file = context.Request.PhysicalPath;
            string html = File.ReadAllText(file);

            html = _regex.Replace(html, delegate(Match match)
            {
                return Print(context, match);
            });

            if ("true".Equals(ConfigurationManager.AppSettings.Get("minify"), StringComparison.OrdinalIgnoreCase))
            {
                html = Minify(html);
            }

            context.Response.Write(html);

            DateTime lastWrite = File.GetLastWriteTimeUtc(file);
            SetConditionalGetHeaders(lastWrite, context);

            context.Response.AddFileDependency(file);
            context.Response.Cache.SetValidUntilExpires(true);
            context.Response.Cache.SetCacheability(HttpCacheability.ServerAndPrivate);
        }

        private static string Minify(string html)
        {
            var settings = new HtmlMinificationSettings
            {
                RemoveOptionalEndTags = false,
                AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.Html5,
                RemoveRedundantAttributes = false,
                WhitespaceMinificationMode = WhitespaceMinificationMode.Aggressive,
                MinifyEmbeddedCssCode = true,
                MinifyEmbeddedJsCode = true,
                MinifyInlineCssCode = true,
                MinifyInlineJsCode = true,
            };

            var minifier = new HtmlMinifier(settings);
            MarkupMinificationResult result = minifier.Minify(html, generateStatistics: true);

            if (result.Errors.Count == 0)
                return result.MinifiedContent;

            return html;
        }

        private static string Print(HttpContext context, Match match)
        {
            string value = match.Value;
            string path = match.Groups["href"].Value;
            Uri url;

            if (!IsValidUrl(path, out url))
                return value;

            if (!context.IsDebuggingEnabled && !context.Request.IsLocal) // Disable CDN when debugging is on
                value = AddCdn(context, value, path);

            string physical = context.Server.MapPath(path);
            context.Response.AddFileDependency(physical);

            DateTime lastWrite = File.GetLastWriteTimeUtc(physical);
            int index = path.LastIndexOf('.');

            string pathFingerprint = path.Insert(index, "." + lastWrite.Ticks);
            return value.Replace(path, pathFingerprint);
        }

        private static string AddCdn(HttpContext context, string value, string path)
        {
            if (string.IsNullOrEmpty(_cdnPath))
                return value;

            Uri baseUri = new Uri(_cdnPath.TrimEnd('/') +
                                  Path.GetDirectoryName(context.Request.Path)
                                  .Replace("\\", "/")
                                  .TrimEnd('/') + "/");

            Uri full = new Uri(baseUri, path);

            return value.Replace(path, full.OriginalString);
        }

        private static bool IsValidUrl(string path, out Uri url)
        {
            return Uri.TryCreate(path, UriKind.Relative, out url) &&
                   !url.OriginalString.StartsWith("//"); // Not protocol relative paths since they are absolute
        }

        public static void SetConditionalGetHeaders(DateTime lastModified, HttpContext context)
        {
            HttpResponse response = context.Response;
            HttpRequest request = context.Request;
            lastModified = new DateTime(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second);

            string incomingDate = request.Headers["If-Modified-Since"];

            response.Cache.SetLastModified(lastModified);

            DateTime testDate = DateTime.MinValue;

            if (DateTime.TryParse(incomingDate, out testDate) && testDate == lastModified)
            {
                response.ClearContent();
                response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
                response.SuppressContent = true;
            }
        }

        public bool IsReusable { get { return false; } }
    }
}