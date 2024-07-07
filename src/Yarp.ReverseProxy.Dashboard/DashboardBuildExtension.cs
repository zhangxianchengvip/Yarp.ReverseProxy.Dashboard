using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using System.Text;

namespace Yarp.ReverseProxy.Dashboard
{
    public static class DashboardBuildExtension
    {
        private const string EmbeddedFileNamespace = "Yarp.ReverseProxy.Dashboard.wwwroot.build";

        public static IApplicationBuilder UseYarpDashboard(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app); 

            var provider = app.ApplicationServices;

            var options = provider.GetService<DashboardOptions>();

            if (options == null) return app;

            app.UseStaticFiles(new StaticFileOptions
            {
                //RequestPath = options.PathMatch,
                FileProvider = new EmbeddedFileProvider(options.GetType().Assembly, EmbeddedFileNamespace)
            });

            var endpointRouteBuilder = (IEndpointRouteBuilder)app.Properties["__EndpointRouteBuilder"]!;

            endpointRouteBuilder.MapGet
            (
                pattern: options.PathMatch,
                requestDelegate: httpContext =>
                {
                    var path = httpContext.Request.Path.Value;

                    var redirectUrl = string.IsNullOrEmpty(path) || path.EndsWith("/")
                        ? "index.html"
                        : $"{path.Split('/').Last()}/index.html";

                    httpContext.Response.StatusCode = 301;
                    httpContext.Response.Headers["Location"] = redirectUrl;
                    return Task.CompletedTask;
                }
            ).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);

            endpointRouteBuilder.MapGet
            (
                pattern: options.PathMatch + "/index.html",
                requestDelegate: async httpContext =>
                {
                    httpContext.Response.StatusCode = 200;
                    httpContext.Response.ContentType = "text/html;charset=utf-8";

                    await using var stream = options.GetType().Assembly
                        .GetManifestResourceStream(EmbeddedFileNamespace + ".index.html");

                    if (stream == null) throw new InvalidOperationException();

                    using var sr = new StreamReader(stream);
                    var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
                    htmlBuilder.Replace("%(servicePrefix)", options.PathBase + options.PathMatch + "/api");
                    htmlBuilder.Replace("%(pollingInterval)", options.StatsPollingInterval.ToString());
                    await httpContext.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
                }
            ).AllowAnonymousIf(options.AllowAnonymousExplicit, options.AuthorizationPolicy);

            //new RouteActionProvider(endpointRouteBuilder, options).MapDashboardRoutes();

            return app;
        }

        private static IEndpointConventionBuilder AllowAnonymousIf(
            this IEndpointConventionBuilder builder,
            bool allowAnonymous,
            params string?[] authorizationPolicies)
        {
            if (allowAnonymous) return builder.AllowAnonymous();

            var validAuthorizationPolicies = authorizationPolicies
                .Where(policy => !string.IsNullOrEmpty(policy))!
                .ToArray<string>();

            if (validAuthorizationPolicies.Length == 0)
            {
                throw new InvalidOperationException(
                    "If Dashboard Options does not explicitly allow anonymous requests, the Authorization Policy must be configured.");
            }

            return builder.RequireAuthorization(validAuthorizationPolicies);
        }
    }
}