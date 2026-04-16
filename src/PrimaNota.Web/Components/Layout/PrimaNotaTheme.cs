using MudBlazor;

namespace PrimaNota.Web.Components.Layout;

/// <summary>
/// Central MudBlazor theme used by the application. Modelled after the reference
/// design mockup: dark indigo AppBar, light neutral drawer, rounded cards.
/// </summary>
public static class PrimaNotaTheme
{
    /// <summary>Gets the configured <see cref="MudTheme"/>.</summary>
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            // Brand
            Primary = "#1a1b41",
            PrimaryContrastText = "#ffffff",
            Secondary = "#3b82f6",
            SecondaryContrastText = "#ffffff",
            Tertiary = "#6366f1",

            // Semantic
            Info = "#3b82f6",
            Success = "#10b981",
            Warning = "#f59e0b",
            Error = "#ef4444",
            Dark = "#1a1b41",

            // Surfaces
            Background = "#f9fafb",
            Surface = "#ffffff",
            AppbarBackground = "#1a1b41",
            AppbarText = "#ffffff",
            DrawerBackground = "#f3f4f6",
            DrawerText = "#1f2937",
            DrawerIcon = "#6b7280",

            // Text
            TextPrimary = "#111827",
            TextSecondary = "#6b7280",
            TextDisabled = "#9ca3af",

            // Lines
            LinesDefault = "#e5e7eb",
            LinesInputs = "#d1d5db",
            TableLines = "#f3f4f6",
            TableStriped = "#fafafa",
            Divider = "#e5e7eb",

            // Actions
            ActionDefault = "#6b7280",
            ActionDisabled = "rgba(0,0,0,0.26)",
            ActionDisabledBackground = "rgba(0,0,0,0.08)",
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            AppbarHeight = "64px",
            DrawerWidthLeft = "256px",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[]
                {
                    "-apple-system",
                    "BlinkMacSystemFont",
                    "Segoe UI",
                    "Roboto",
                    "Helvetica",
                    "Arial",
                    "sans-serif",
                },
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "normal",
            },
            H1 = new H1Typography { FontSize = "2rem", FontWeight = "600" },
            H2 = new H2Typography { FontSize = "1.75rem", FontWeight = "600" },
            H3 = new H3Typography { FontSize = "1.5rem", FontWeight = "600" },
            H4 = new H4Typography { FontSize = "1.5rem", FontWeight = "600" },
            H5 = new H5Typography { FontSize = "1.25rem", FontWeight = "600" },
            H6 = new H6Typography { FontSize = "1rem", FontWeight = "600" },
            Button = new ButtonTypography { TextTransform = "none", FontWeight = "500" },
        },

        Shadows = new Shadow(),
    };
}
