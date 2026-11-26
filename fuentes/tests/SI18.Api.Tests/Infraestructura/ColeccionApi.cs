namespace SI18.Api.Tests.Infraestructura;

/// <summary>
/// Las pruebas de integración comparten una única <see cref="FabricaApi"/> (un contenedor por ejecución) y
/// se ejecutan en serie dentro de la colección.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionApi : ICollectionFixture<FabricaApi>
{
    public const string Nombre = "API";
}
