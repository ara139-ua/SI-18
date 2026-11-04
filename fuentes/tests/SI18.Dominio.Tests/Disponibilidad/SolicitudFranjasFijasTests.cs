using SI18.Dominio.Comun;
using SI18.Dominio.Disponibilidad;

namespace SI18.Dominio.Tests.Disponibilidad;

public class SolicitudFranjasFijasTests
{
    private static readonly DateTime Creada = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Ahora = new(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc);

    private static SolicitudFranjasFijas Nueva(
        short[]? dias = null, TimeOnly? inicio = null, TimeOnly? fin = null) =>
        new(1, 1, 1, dias ?? [1, 3], inicio ?? new TimeOnly(18, 0), fin ?? new TimeOnly(20, 0), Creada);

    [Fact]
    public void Nueva_solicitud_queda_pendiente_sin_fecha_de_resolucion()
    {
        var solicitud = Nueva();

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.ResueltaEnUtc);
    }

    [Fact]
    public void Aprobar_desde_pendiente_la_aprueba_y_rellena_la_resolucion()
    {
        var solicitud = Nueva();

        solicitud.Aprobar(Ahora);

        Assert.Equal(EstadoSolicitud.Aprobada, solicitud.Estado);
        Assert.Equal(Ahora, solicitud.ResueltaEnUtc);
    }

    [Fact]
    public void Rechazar_desde_pendiente_la_rechaza_y_rellena_la_resolucion()
    {
        var solicitud = Nueva();

        solicitud.Rechazar(Ahora);

        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal(Ahora, solicitud.ResueltaEnUtc);
    }

    public static TheoryData<Action<SolicitudFranjasFijas>, Action<SolicitudFranjasFijas>> TransicionesDesdeFinal => new()
    {
        { s => s.Aprobar(Ahora), s => s.Aprobar(Ahora) },
        { s => s.Aprobar(Ahora), s => s.Rechazar(Ahora) },
        { s => s.Rechazar(Ahora), s => s.Aprobar(Ahora) },
        { s => s.Rechazar(Ahora), s => s.Rechazar(Ahora) },
    };

    [Theory]
    [MemberData(nameof(TransicionesDesdeFinal))]
    public void Desde_aprobada_o_rechazada_no_se_puede_resolver_otra_vez(
        Action<SolicitudFranjasFijas> primera, Action<SolicitudFranjasFijas> segunda)
    {
        var solicitud = Nueva();
        primera(solicitud);
        var estadoAntes = solicitud.Estado;

        var error = Assert.Throws<ErrorNegocio>(() => segunda(solicitud));

        Assert.Equal("TRANSICION_NO_PERMITIDA", error.Codigo);
        Assert.Equal(estadoAntes, solicitud.Estado);
    }

    [Theory]
    [InlineData(new short[0])]
    [InlineData(new short[] { 0 })]
    [InlineData(new short[] { 8 })]
    [InlineData(new short[] { 2, 2 })]
    public void Dias_de_la_semana_invalidos_se_rechazan(short[] dias)
    {
        Assert.Throws<ArgumentException>(() => Nueva(dias: dias));
    }

    [Theory]
    [Trait("Categoria", "Limites")]
    [InlineData(8, 0, 22, 0)]
    [InlineData(21, 0, 22, 0)]
    [InlineData(8, 0, 9, 0)]
    public void Horario_en_los_limites_de_apertura_se_admite(int hi, int mi, int hf, int mf)
    {
        var solicitud = Nueva(inicio: new TimeOnly(hi, mi), fin: new TimeOnly(hf, mf));

        Assert.Equal(new TimeOnly(hi, mi), solicitud.HoraInicio);
    }

    [Theory]
    [Trait("Categoria", "Limites")]
    [InlineData(7, 0, 9, 0)]    // empieza antes de abrir
    [InlineData(21, 0, 23, 0)]  // termina después de cerrar
    [InlineData(18, 30, 20, 0)] // no es en punto
    [InlineData(18, 0, 18, 0)]  // fin igual a inicio
    [InlineData(20, 0, 18, 0)]  // fin anterior a inicio
    public void Horario_invalido_se_rechaza(int hi, int mi, int hf, int mf)
    {
        Assert.Throws<ArgumentException>(() => Nueva(inicio: new TimeOnly(hi, mi), fin: new TimeOnly(hf, mf)));
    }
}
