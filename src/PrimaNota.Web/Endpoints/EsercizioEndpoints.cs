namespace PrimaNota.Web.Endpoints;

/// <summary>Minimal-API endpoint to switch the active fiscal year via HTTP cookie.</summary>
internal static class EsercizioEndpoints
{
    /// <summary>Maps the year-switch endpoint.</summary>
    /// <param name="app">Endpoint route builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IEndpointRouteBuilder MapEsercizioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/switch-year/{anno:int}", (int anno, string? returnUrl, HttpContext ctx) =>
        {
            ctx.Response.Cookies.Append(
                "PrimaNota.Esercizio",
                anno.ToString(System.Globalization.CultureInfo.InvariantCulture),
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = ctx.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                });

            var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return Results.Redirect(target);
        });

        return app;
    }
}
