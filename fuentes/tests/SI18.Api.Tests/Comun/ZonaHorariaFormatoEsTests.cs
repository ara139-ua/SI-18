using System.Globalization;
using System.Reflection;
using SI18.Contratos.Comun;
using SI18.Dominio.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>
/// <c>FormatoEs</c> está en <c>SI18.Contratos</c>, que no puede depender de <c>SI18.Dominio</c>, así que
/// repite la zona horaria en lugar de leer <see cref="ValoresNegocio.ZonaHoraria"/>. Estas pruebas
/// comprueban que las dos coinciden (constitución, principio III; converge 2026-11-26, F2 → T078).
/// </summary>
public class ZonaHorariaFormatoEsTests
{
    [Fact]
    public void FormatoEs_usa_la_zona_de_ValoresNegocio()
    {
        var zonas = typeof(FormatoEs)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(campo => campo.FieldType == typeof(TimeZoneInfo))
            .Select(campo => (TimeZoneInfo)campo.GetValue(null)!)
            .ToList();

        var zona = Assert.Single(zonas);
        Assert.Equal(ValoresNegocio.ZonaHoraria, zona.Id);
    }

    [Fact]
    public void FormatoEs_convierte_igual_que_ValoresNegocio_cada_hora_de_2026_y_2027()
    {
        // Cubre los cuatro cambios de hora (29/03/2026, 25/10/2026, 28/03/2027 y 31/10/2027).
        var zona = TimeZoneInfo.FindSystemTimeZoneById(ValoresNegocio.ZonaHoraria);
        var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var utc = inicio; utc < fin; utc = utc.AddHours(1))
        {
            var esperado = TimeZoneInfo.ConvertTimeFromUtc(utc, zona)
                .ToString("dd'/'MM'/'yyyy HH':'mm", CultureInfo.InvariantCulture);
            Assert.Equal(esperado, FormatoEs.FechaHora(utc));
        }
    }
}
