using MudBlazor;

namespace SI18.Cliente.Layout;

/// <summary>
/// Tema de MudBlazor con contraste WCAG AA (principio I; research.md R16). Cada color de texto cumple al
/// menos 4.5:1 sobre su fondo; entre paréntesis, la relación de contraste aproximada.
/// Se usan fuentes del sistema: no se descarga nada de terceros.
/// </summary>
public static class Tema
{
    public static MudTheme Crear() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0B4F8A",              // texto blanco encima (8.3:1)
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#6B3FA0",            // texto blanco encima (7.6:1)
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#00695C",             // texto blanco encima (6.5:1)
            Info = "#0B5CAD",                 // texto blanco encima (6.9:1)
            Success = "#1B6E20",              // texto blanco encima (6.6:1)
            Warning = "#8A4B00",              // texto blanco encima (6.3:1)
            Error = "#B3261E",                // texto blanco encima (6.5:1)
            AppbarBackground = "#0B4F8A",
            AppbarText = "#FFFFFF",
            Background = "#FFFFFF",
            Surface = "#FFFFFF",
            TextPrimary = "#1C1B1F",          // sobre blanco (16.9:1)
            TextSecondary = "#49454F",        // sobre blanco (9.2:1)
            ActionDefault = "#49454F",
            TextDisabled = "#6E6A73",         // sobre blanco (5.3:1)
            LinesInputs = "#6E6A73",          // borde de los campos: contraste no textual ≥ 3:1
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"],
            },
        },
    };
}
