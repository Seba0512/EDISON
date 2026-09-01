using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Usuario registrado del sistema (CR1). El nombre es la clave natural y es inmutable: identifica al
/// creador de los modelos (CR3, CR4), al duenio de las reservas (CR9) y al del historial (CR10).
/// </summary>
public class Usuario
{
    private const int LargoMinimoNombre = 3;
    private const int LargoMaximoNombre = 20;
    private const int LargoMinimoPassword = 4;

    private readonly string password;

    public string NombreUsuario { get; }

    public Usuario(string nombreUsuario, string password)
    {
        NombreUsuario = NombreValidado(nombreUsuario);
        this.password = PasswordValidada(password);
    }

    public bool SeLlama(string nombreUsuario)
    {
        return NombreUsuario.Equals(nombreUsuario?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool CoincideLaPassword(string passwordIngresada)
    {
        return password.Equals(passwordIngresada, StringComparison.Ordinal);
    }

    private static string NombreValidado(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new DominioException("El nombre de usuario no puede estar vacio");
        }

        string nombreSinEspacios = nombreUsuario.Trim();
        if (nombreSinEspacios.Length is < LargoMinimoNombre or > LargoMaximoNombre)
        {
            throw new DominioException(
                $"El nombre de usuario debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres");
        }

        return nombreSinEspacios;
    }

    private static string PasswordValidada(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < LargoMinimoPassword)
        {
            throw new DominioException($"La password debe tener al menos {LargoMinimoPassword} caracteres");
        }

        return password;
    }
}
