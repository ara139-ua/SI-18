using System.Text.Json;
using SI18.Api.Infraestructura;
using SI18.Contratos.Comun;
using SI18.Contratos.Reservas;
using EstadoReservaDominio = SI18.Dominio.Comun.EstadoReserva;

namespace SI18.Api.Tests.Reservas;

/// <summary>
/// La copia de <c>EstadoReserva</c> en SI18.Contratos.Reservas no se separa de la de SI18.Dominio.Comun
/// (specs/002-reservar-pagar tasks.md T003; research.md E12): mismos nombres en el mismo orden, y en JSON el
/// mismo texto que en la base de datos.
/// </summary>
public class EstadoReservaEspejoTests
{
    [Fact]
    public void EstadoReserva_tiene_los_mismos_nombres_en_el_mismo_orden()
    {
        Assert.Equal(Enum.GetNames<EstadoReservaDominio>(), Enum.GetNames<EstadoReserva>());
    }

    [Fact]
    public void En_JSON_viaja_con_el_mismo_texto_que_en_la_base_de_datos()
    {
        var enJson = Enum.GetValues<EstadoReserva>()
            .Select(valor => JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(valor, OpcionesJson.Predeterminadas))!)
            .ToArray();

        Assert.Equal(ConversorEnumSnakeCase<EstadoReservaDominio>.ValoresPermitidos, enJson);
        Assert.Equal(["pendiente_pago", "confirmada", "caducada", "cancelada"], enJson);
    }
}
