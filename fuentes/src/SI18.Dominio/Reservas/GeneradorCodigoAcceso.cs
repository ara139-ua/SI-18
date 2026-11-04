using System.Buffers.Text;
using System.Security.Cryptography;

namespace SI18.Dominio.Reservas;

/// <summary>
/// Código de acceso (QR) de una reserva confirmada (research.md R14; RF-017): opaco, aleatorio y no
/// deducible. 16 bytes criptográficos (128 bits) en Base64url sin relleno: 22 caracteres.
/// </summary>
public static class GeneradorCodigoAcceso
{
    public const int BytesAleatorios = 16;
    public const int Longitud = 22;

    public static string Nuevo() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(BytesAleatorios));
}
