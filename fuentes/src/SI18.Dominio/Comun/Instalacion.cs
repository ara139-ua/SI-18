namespace SI18.Dominio.Comun;

/// <summary>
/// Pista, pabellón, sala o piscina de un centro (data-model.md, «Instalacion»). Precargada.
/// El estado operativa / cerrada por mantenimiento NO es una propiedad: se calcula a partir de los
/// cierres (<c>CierreMantenimiento</c>), para no tener dos fuentes de verdad.
/// </summary>
public class Instalacion
{
    private readonly List<Deporte> _deportes = [];

    private Instalacion() { }

    public Instalacion(int centroId, string nombre, TipoInstalacion tipo, IEnumerable<Deporte> deportes)
    {
        CentroId = centroId;
        Nombre = nombre;
        Tipo = tipo;
        ReservablePorFranjas = EsReservablePorFranjas(tipo);
        _deportes.AddRange(deportes);
    }

    public int Id { get; private set; }

    public int CentroId { get; private set; }

    /// <summary>varchar(100), obligatorio, único por centro.</summary>
    public string Nombre { get; private set; } = null!;

    public TipoInstalacion Tipo { get; private set; }

    /// <summary>Solo las pistas de tenis y pádel se reservan por franjas (RF-012).</summary>
    public bool ReservablePorFranjas { get; private set; }

    /// <summary>Deportes que admite (tabla <c>instalacion_deportes</c>).</summary>
    public IReadOnlyCollection<Deporte> Deportes => _deportes;

    /// <summary>Misma regla que el CHECK de <c>reservable_por_franjas</c>.</summary>
    public static bool EsReservablePorFranjas(TipoInstalacion tipo) =>
        tipo is TipoInstalacion.PistaTenis or TipoInstalacion.PistaPadel;
}
