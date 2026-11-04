using SI18.Dominio.Comun;

namespace SI18.Dominio.Tests.Comun;

public class CalculadoraTarifaTests
{
    [Theory]
    [InlineData(Rol.Ciudadano, true, TipoTarifa.Empadronado)]
    [InlineData(Rol.Ciudadano, false, TipoTarifa.NoEmpadronado)]
    [InlineData(Rol.RepresentanteClub, false, TipoTarifa.Club)]
    [InlineData(Rol.RepresentanteClub, true, TipoTarifa.Club)] // el club manda sobre el empadronamiento
    public void Ciudadanos_y_representantes_tienen_tarifa(Rol rol, bool empadronado, TipoTarifa esperada)
    {
        Assert.Equal(esperada, CalculadoraTarifa.Para(rol, empadronado));
    }

    [Theory]
    [InlineData(Rol.PersonalCentro, false)]
    [InlineData(Rol.PersonalCentro, true)]
    [InlineData(Rol.TecnicoConcejalia, false)]
    [InlineData(Rol.TecnicoConcejalia, true)]
    public void Personal_y_tecnico_no_tienen_tarifa(Rol rol, bool empadronado)
    {
        Assert.Null(CalculadoraTarifa.Para(rol, empadronado));
    }
}
