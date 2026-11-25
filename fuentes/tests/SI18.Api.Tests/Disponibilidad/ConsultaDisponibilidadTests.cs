using Microsoft.Extensions.DependencyInjection;
using SI18.Api.Modulos.Disponibilidad;
using SI18.Api.Tests.Infraestructura;
using SI18.Dominio.Comun;
using static SI18.Api.Tests.Disponibilidad.DatosDisponibilidad;

namespace SI18.Api.Tests.Disponibilidad;

/// <summary>
/// <c>IConsultaDisponibilidad</c>, que usará HU-02, da exactamente lo mismo que la búsqueda (spec-001 RF-013;
/// CE-004; tasks.md T016). Polideportivo Este, «Pádel», 24/12/2026.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConsultaDisponibilidadTests(FabricaApi fabrica)
{
    private const string Este = "Polideportivo Este";
    private static readonly DateOnly Fecha = new(2026, 12, 24);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatosDisponibilidad _datos = new(fabrica);

    private async Task<T> Con<T>(Func<IConsultaDisponibilidad, ServicioDisponibilidad, Task<T>> accion)
    {
        using var ambito = fabrica.Services.CreateScope();
        return await accion(
            ambito.ServiceProvider.GetRequiredService<IConsultaDisponibilidad>(),
            ambito.ServiceProvider.GetRequiredService<ServicioDisponibilidad>());
    }

    [Fact]
    public async Task Las_franjas_libres_de_la_busqueda_son_libres_para_HU_02_y_al_mismo_precio()
    {
        var deporte = await _datos.Deporte("Pádel");
        var centro = await _datos.Centro(Este);

        await Con(async (consulta, servicio) =>
        {
            var resultado = await servicio.Buscar(deporte, centro, Fecha, TipoTarifa.NoEmpadronado, Ct);
            Assert.NotEmpty(resultado.Pistas);

            foreach (var pista in resultado.Pistas)
            {
                var precio = await consulta.Precio(pista.InstalacionId, TipoTarifa.NoEmpadronado, Ct);
                foreach (var franja in pista.Franjas)
                {
                    Assert.Null(await consulta.MotivoNoDisponible(pista.InstalacionId, franja.InicioUtc, Ct));
                    Assert.Equal(franja.Precio, precio);
                }
            }

            return 0;
        });
    }

    [Fact]
    public async Task Ocupada_cerrada_y_bloqueada_dan_su_motivo_y_no_salen_en_la_busqueda()
    {
        var pista = await _datos.Pista(Este, "Pista de pádel 1");
        await _datos.Reserva(pista, Utc(Fecha, 9), EstadoPrueba.Confirmada);
        await _datos.Cierre(pista, Utc(Fecha, 10), Utc(Fecha, 11));
        await _datos.FranjaFija(pista, [DiaIso(Fecha)], 12, 13);
        var deporte = await _datos.Deporte("Pádel");
        var centro = await _datos.Centro(Este);

        await Con(async (consulta, servicio) =>
        {
            Assert.Equal(CodigosError.FranjaOcupada, await consulta.MotivoNoDisponible(pista, Utc(Fecha, 9), Ct));
            Assert.Equal(CodigosError.InstalacionCerrada, await consulta.MotivoNoDisponible(pista, Utc(Fecha, 10), Ct));
            Assert.Equal(CodigosError.FranjaBloqueadaClub, await consulta.MotivoNoDisponible(pista, Utc(Fecha, 12), Ct));

            var horas = (await servicio.Buscar(deporte, centro, Fecha, TipoTarifa.Empadronado, Ct))
                .Pistas.Single(p => p.InstalacionId == pista).Franjas.Select(f => f.HoraInicio).ToList();
            Assert.DoesNotContain("09:00", horas);
            Assert.DoesNotContain("10:00", horas);
            Assert.DoesNotContain("12:00", horas);
            return 0;
        });
    }

    [Fact]
    public async Task Con_franja_fija_y_cierre_a_la_vez_gana_el_cierre_por_el_orden_de_RF_011()
    {
        var pista = await _datos.Pista(Este, "Pista de pádel 2");
        await _datos.FranjaFija(pista, [DiaIso(Fecha)], 19, 21);
        await _datos.Cierre(pista, Utc(Fecha, 19), Utc(Fecha, 20));

        await Con(async (consulta, _) =>
        {
            Assert.Equal(CodigosError.InstalacionCerrada, await consulta.MotivoNoDisponible(pista, Utc(Fecha, 19), Ct));
            Assert.Equal(CodigosError.FranjaBloqueadaClub, await consulta.MotivoNoDisponible(pista, Utc(Fecha, 20), Ct));
            return 0;
        });
    }

    [Theory]
    [InlineData(18, 30)]   // no es en punto
    [InlineData(22, 0)]    // fuera del horario: la última franja empieza a las 21:00
    public async Task Una_hora_que_no_es_inicio_de_franja_lanza_peticion_invalida(int hora, int minuto)
    {
        var pista = await _datos.Pista(Este, "Pista de pádel 1");

        var error = await Con((consulta, _) =>
            Assert.ThrowsAsync<ErrorNegocio>(() => consulta.MotivoNoDisponible(pista, Utc(Fecha, hora, minuto), Ct)));

        Assert.Equal(CodigosError.PeticionInvalida, error.Codigo);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task Un_instante_que_no_es_UTC_lanza_peticion_invalida(DateTimeKind tipo)
    {
        var pista = await _datos.Pista(Este, "Pista de pádel 1");
        var noUtc = DateTime.SpecifyKind(Utc(Fecha, 10), tipo);

        var error = await Con((consulta, _) =>
            Assert.ThrowsAsync<ErrorNegocio>(() => consulta.MotivoNoDisponible(pista, noUtc, Ct)));

        Assert.Equal(CodigosError.PeticionInvalida, error.Codigo);
    }

    [Fact]
    public async Task Una_instalacion_inexistente_lanza_no_encontrado()
    {
        var error = await Con((consulta, _) =>
            Assert.ThrowsAsync<ErrorNegocio>(() => consulta.MotivoNoDisponible(999999, Utc(Fecha, 10), Ct)));

        Assert.Equal(CodigosError.NoEncontrado, error.Codigo);
    }

    [Fact]
    public async Task El_pabellon_no_es_reservable_por_franjas()
    {
        var pabellon = await _datos.Pista(Este, "Pabellón");

        var motivo = await Con((consulta, _) => consulta.MotivoNoDisponible(pabellon, Utc(Fecha, 10), Ct));

        Assert.Equal(CodigosError.InstalacionNoReservable, motivo);
    }
}
