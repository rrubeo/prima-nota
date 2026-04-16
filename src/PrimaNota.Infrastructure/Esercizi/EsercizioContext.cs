using Microsoft.AspNetCore.Http;
using PrimaNota.Application.Abstractions;
using PrimaNota.Shared.Clock;

namespace PrimaNota.Infrastructure.Esercizi;

/// <summary>
/// Default <see cref="IEsercizioContext"/> backed by an HTTP cookie that persists the user's
/// selected exercise across requests. Falls back to the Italian current year when no cookie
/// is present or when running outside an HTTP request (background jobs).
/// </summary>
internal sealed class EsercizioContext : IEsercizioContext
{
    private const string CookieName = "PrimaNota.Esercizio";
    private const int MinYear = 2000;
    private const int MaxYear = 2100;

    private readonly IHttpContextAccessor accessor;
    private readonly IDateTimeProvider clock;
    private int? overridden;

    public EsercizioContext(IHttpContextAccessor accessor, IDateTimeProvider clock)
    {
        this.accessor = accessor;
        this.clock = clock;
    }

    /// <inheritdoc />
    public int Anno
    {
        get
        {
            if (overridden.HasValue)
            {
                return overridden.Value;
            }

            var http = accessor.HttpContext;
            if (http is not null &&
                http.Request.Cookies.TryGetValue(CookieName, out var raw) &&
                int.TryParse(raw, out var year) &&
                year is >= MinYear and <= MaxYear)
            {
                return year;
            }

            return clock.TodayItaly.Year;
        }
    }

    /// <inheritdoc />
    public void SwitchTo(int anno)
    {
        if (anno is < MinYear or > MaxYear)
        {
            throw new ArgumentOutOfRangeException(nameof(anno), "Anno fuori range supportato.");
        }

        overridden = anno;

        var http = accessor.HttpContext;
        http?.Response.Cookies.Append(
            CookieName,
            anno.ToString(System.Globalization.CultureInfo.InvariantCulture),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = http.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
            });
    }
}
