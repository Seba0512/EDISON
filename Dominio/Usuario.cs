using Dominio.Excepciones;

namespace Dominio;

public class Usuario
{
    private const int LargoMinimoNombre = 3;
    private const int LargoMaximoNombre = 20;

    private readonly Credencial credencial;
    private readonly Historial historial;

    public string NombreUsuario { get; }

    public Usuario(string nombreUsuario, string clave)
    {
        NombreUsuario = NombreValidado(nombreUsuario);
        credencial = new Credencial(clave);
        historial = new Historial();
    }

    public bool SeLlama(string nombreUsuario)
    {
        return NombreUsuario.Equals(nombreUsuario?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool VerificarClave(string claveIngresada)
    {
        return credencial.Verifica(claveIngresada);
    }

    public void Registrar(Actividad actividad)
    {
        historial.Registrar(actividad);
    }

    public IReadOnlyList<Actividad> Actividades()
    {
        return historial.Actividades;
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
}
