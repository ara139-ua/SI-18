using SI18.Api.Infraestructura;
using SI18.Dominio.Comun;

namespace SI18.Api.Tests.Comun;

/// <summary>El catálogo de CodigosError y la tabla de estados de ManejadorErrores están completos y alineados (sin base de datos).</summary>
public class CatalogoErroresTests
{
    public static TheoryData<string> Codigos() => new(CodigosError.Todos);

    [Fact]
    public void El_catalogo_tiene_los_25_codigos_de_contracts()
    {
        // 22 de 000-core + 3 de spec-005 (contracts/errores.md).
        Assert.Equal(25, CodigosError.Todos.Count);
    }

    [Fact]
    public void Cada_constante_publica_esta_en_el_catalogo()
    {
        var constantes = typeof(CodigosError).GetFields()
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

        Assert.Equal(constantes.Order(), CodigosError.Todos.Order());
    }

    [Theory]
    [MemberData(nameof(Codigos))]
    public void Cada_codigo_tiene_estado_HTTP_titulo_y_detalle(string codigo)
    {
        Assert.True(ManejadorErrores.Estados.ContainsKey(codigo), $"{codigo} no tiene estado HTTP en ManejadorErrores.");
        Assert.False(string.IsNullOrWhiteSpace(CodigosError.Titulo(codigo)));
        Assert.False(string.IsNullOrWhiteSpace(CodigosError.Detalle(codigo)));
    }

    [Fact]
    public void La_tabla_de_estados_no_tiene_codigos_fuera_del_catalogo()
    {
        Assert.Empty(ManejadorErrores.Estados.Keys.Except(CodigosError.Todos));
    }

    [Theory]
    [InlineData(CodigosError.FranjaOcupada, "https://si18.test/errores/franja-ocupada")]
    [InlineData(CodigosError.QrReservaNoConfirmada, "https://si18.test/errores/qr-reserva-no-confirmada")]
    public void El_tipo_es_el_codigo_en_kebab_case(string codigo, string esperado)
    {
        Assert.Equal(esperado, ManejadorErrores.Tipo(codigo));
    }

    [Fact]
    public void Un_codigo_desconocido_falla()
    {
        Assert.Throws<ArgumentException>(() => CodigosError.Detalle("NO_EXISTE"));
    }
}
