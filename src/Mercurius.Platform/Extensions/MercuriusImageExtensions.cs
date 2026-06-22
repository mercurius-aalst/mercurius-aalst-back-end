using Imageflow.Server;

namespace Mercurius.Platform.Extensions;

public static class MercuriusImageExtensions
{
    public static IApplicationBuilder UseMercuriusImages(this WebApplication app)
    {
        var imageflowOptions = new ImageflowMiddlewareOptions
        {
            AllowDiskCaching = true,
            AllowCaching = true,
            DefaultCacheControlString = "public, max-age=31536000"
        }.MapPath("/images", app.Configuration["FileStorage:Location"]);

        app.UseImageflow(imageflowOptions);
        app.UseStaticFiles();

        return app;
    }
}
