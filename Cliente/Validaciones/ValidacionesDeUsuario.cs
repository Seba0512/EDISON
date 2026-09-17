namespace Cliente.Validaciones;

// Replica en el Cliente las reglas de negocio que hoy solo valida el Dominio del lado del
// Servidor (ver Usuario y Credencial). Mismo motivo que ValidacionesDeModelo: el Cliente no
// referencia el ensamblado Dominio, el Servidor sigue siendo la autoridad final.
internal static class ValidacionesDeUsuario
{
    private const int LargoMinimoNombre = 3;
    private const int LargoMaximoNombre = 20;
    private const int LargoMinimoClave = 4;

    public static string? NombreUsuario(string texto)
        => texto.Length is < LargoMinimoNombre or > LargoMaximoNombre
            ? $"El nombre de usuario debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres"
            : null;

    public static string? Clave(string texto)
        => texto.Length < LargoMinimoClave
            ? $"La clave debe tener al menos {LargoMinimoClave} caracteres"
            : null;
}
