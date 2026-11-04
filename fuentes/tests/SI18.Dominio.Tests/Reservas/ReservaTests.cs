using SI18.Dominio.Comun;
using SI18.Dominio.Reservas;

namespace SI18.Dominio.Tests.Reservas;

[Trait("Categoria", "Limites")]
public class ReservaTests
{
    private static readonly DateTime Creada = new(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Inicio = new(2026, 11, 22, 17, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan UnSegundo = TimeSpan.FromSeconds(1);
    private const string Codigo = "codigo-de-prueba-00001";

    private static Reserva Pendiente() => new(1, 1, Inicio, TipoTarifa.Empadronado, 6.00m, Creada);

    private static Reserva Confirmada()
    {
        var r = Pendiente();
        r.Confirmar(Codigo, Creada + TimeSpan.FromMinutes(1));
        return r;
    }

    private static Reserva Caducada()
    {
        var r = Pendiente();
        r.Caducar(Creada + TimeSpan.FromMinutes(1));
        return r;
    }

    private static Reserva Cancelada()
    {
        var r = Confirmada();
        r.Cancelar(MotivoCancelacion.Usuario, Creada + TimeSpan.FromHours(1));
        return r;
    }

    private static void ExigeCodigo(string codigo, Action accion) =>
        Assert.Equal(codigo, Assert.Throws<ErrorNegocio>(accion).Codigo);

    // ---- Creación ----

    [Fact]
    public void Nueva_reserva_esta_pendiente_con_pago_pendiente_y_dura_una_hora()
    {
        var r = Pendiente();

        Assert.Equal(EstadoReserva.PendientePago, r.Estado);
        Assert.Equal(Inicio + TimeSpan.FromHours(1), r.FinUtc);
        Assert.Null(r.CodigoAcceso);
        Assert.Equal(EstadoPago.Pendiente, r.Pago!.Estado);
        Assert.Equal(r.Importe, r.Pago.Importe);
    }

    // ---- Las cuatro transiciones válidas ----

    [Fact]
    public void Confirmar_desde_pendiente_vigente_confirma_y_completa_el_pago()
    {
        var ahora = Creada + TimeSpan.FromMinutes(5);
        var r = Pendiente();

        r.Confirmar(Codigo, ahora);

        Assert.Equal(EstadoReserva.Confirmada, r.Estado);
        Assert.Equal(Codigo, r.CodigoAcceso);
        Assert.Equal(EstadoPago.Completado, r.Pago!.Estado);
        Assert.Equal(ahora, r.Pago.ResueltoEnUtc);
    }

    [Fact]
    public void Caducar_desde_pendiente_caduca_y_deja_el_pago_fallido()
    {
        var ahora = Creada + TimeSpan.FromMinutes(10);
        var r = Pendiente();

        r.Caducar(ahora);

        Assert.Equal(EstadoReserva.Caducada, r.Estado);
        Assert.Null(r.CodigoAcceso);
        Assert.Equal(EstadoPago.Fallido, r.Pago!.Estado);
        Assert.Equal(ahora, r.Pago.ResueltoEnUtc);
    }

    [Fact]
    public void Cancelar_por_el_usuario_1_s_antes_del_inicio_cancela()
    {
        var ahora = Inicio - UnSegundo;
        var r = Confirmada();

        r.Cancelar(MotivoCancelacion.Usuario, ahora);

        Assert.Equal(EstadoReserva.Cancelada, r.Estado);
        Assert.Equal(ahora, r.CanceladaEnUtc);
        Assert.Equal(MotivoCancelacion.Usuario, r.MotivoCancelacion);
        Assert.Equal(EstadoPago.Completado, r.Pago!.Estado); // la devolución es aparte
    }

    [Fact]
    public void Cancelar_por_cierre_despues_del_inicio_cancela()
    {
        var ahora = Inicio + TimeSpan.FromMinutes(30);
        var r = Confirmada();

        r.Cancelar(MotivoCancelacion.CierreMantenimiento, ahora);

        Assert.Equal(EstadoReserva.Cancelada, r.Estado);
        Assert.Equal(MotivoCancelacion.CierreMantenimiento, r.MotivoCancelacion);
    }

    // ---- Límites de tiempo ----

    [Fact]
    public void EstaVigente_a_los_9_59_si_y_a_los_10_00_no()
    {
        var r = Pendiente();

        Assert.True(r.EstaVigente(Creada + TimeSpan.FromMinutes(10) - UnSegundo));
        Assert.False(r.EstaVigente(Creada + TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void EstaVigente_confirmada_siempre_y_caducada_o_cancelada_nunca()
    {
        var mucho = Creada + TimeSpan.FromDays(30);

        Assert.True(Confirmada().EstaVigente(mucho));
        Assert.False(Caducada().EstaVigente(Creada));
        Assert.False(Cancelada().EstaVigente(Creada));
    }

    [Fact]
    public void Confirmar_a_los_10_00_lanza_RESERVA_CADUCADA_y_no_cambia_nada()
    {
        var r = Pendiente();

        ExigeCodigo("RESERVA_CADUCADA", () => r.Confirmar(Codigo, Creada + TimeSpan.FromMinutes(10)));

        Assert.Equal(EstadoReserva.PendientePago, r.Estado);
        Assert.Equal(EstadoPago.Pendiente, r.Pago!.Estado);
    }

    [Fact]
    public void Cancelar_por_el_usuario_en_el_inicio_exacto_lanza_RESERVA_YA_EMPEZADA()
    {
        var r = Confirmada();

        ExigeCodigo("RESERVA_YA_EMPEZADA", () => r.Cancelar(MotivoCancelacion.Usuario, Inicio));

        Assert.Equal(EstadoReserva.Confirmada, r.Estado);
    }

    // ---- Transiciones no permitidas (HU-C4 esc. 3) ----

    [Fact]
    public void Pendiente_a_cancelada_no_esta_permitido()
    {
        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => Pendiente().Cancelar(MotivoCancelacion.Usuario, Creada));
        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => Pendiente().Cancelar(MotivoCancelacion.CierreMantenimiento, Creada));
    }

    [Fact]
    public void Confirmada_no_se_puede_confirmar_ni_caducar()
    {
        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => Confirmada().Confirmar(Codigo, Creada));
        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => Confirmada().Caducar(Creada));
    }

    public static TheoryData<string> Finales => ["caducada", "cancelada"];

    [Theory]
    [MemberData(nameof(Finales))]
    public void Desde_un_estado_final_ningun_cambio_esta_permitido(string final)
    {
        Reserva Nueva() => final == "caducada" ? Caducada() : Cancelada();
        var ahora = Creada + TimeSpan.FromMinutes(2);

        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => Nueva().Confirmar(Codigo, ahora));
        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => Nueva().Caducar(ahora));
        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => Nueva().Cancelar(MotivoCancelacion.Usuario, ahora));
        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => Nueva().Cancelar(MotivoCancelacion.CierreMantenimiento, ahora));
    }

    [Fact]
    public void Un_pago_resuelto_no_se_puede_resolver_otra_vez()
    {
        var pago = Confirmada().Pago!;

        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => pago.Completar(Creada));
        ExigeCodigo("TRANSICION_NO_PERMITIDA", () => pago.Fallar(Creada));
    }
}
