using Imageflow.Server;

namespace Platform.Extensions;

public static class ImageflowExtensions
{
    public static IApplicationBuilder UseImageflowWithCaching(
        this IApplicationBuilder app,
        string requestPath,
        string? storagePath,
        string cacheControl)
    {
        var imageflowOptions = new ImageflowMiddlewareOptions
        {
            AllowDiskCaching = true,
            AllowCaching = true,
            DefaultCacheControlString = cacheControl
        }.MapPath(requestPath, storagePath);

        app.UseImageflow(imageflowOptions);
        app.UseStaticFiles();

        return app;
    }
}
