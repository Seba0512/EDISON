using System.Text;
using Common.Excepciones;

namespace Servidor.Repositorios;

internal static class NombreDeDirectorioDeUsuario
{
    private static readonly HashSet<char> CaracteresInvalidos =
        new("<>:\"/\\|?*".Concat(Enumerable.Range(0, 32).Select(valor => (char)valor)));
    public static string Crear(string nombreUsuario)
    {
        string nombreNormalizado = (nombreUsuario ?? string.Empty).Trim().ToLowerInvariant();
        if (nombreNormalizado.Length == 0)
            throw new ProtocoloException("No se puede crear una carpeta para un usuario vacío");

        return Codificar(nombreNormalizado, NombresReservadosDelSistema.EsReservado(nombreNormalizado));
    }

    public static string Validar(string nombreDirectorio)
    {
        string nombre = (nombreDirectorio ?? string.Empty).Trim();
        if (nombre.Length == 0 || nombre is "." or ".." || nombre != nombreDirectorio)
            throw new ProtocoloException("La referencia interna contiene una carpeta inválida");
        if (nombre.Any(CaracteresInvalidos.Contains) || nombre.EndsWith('.') || nombre.EndsWith(' '))
            throw new ProtocoloException("La referencia interna contiene una carpeta inválida");
        if (NombresReservadosDelSistema.EsReservado(nombre))
            throw new ProtocoloException("La referencia interna contiene una carpeta reservada");

        return nombre;
    }

    private static string Codificar(string nombre, bool nombreReservado)
    {
        StringBuilder resultado = new(nombre.Length);
        for (int indice = 0; indice < nombre.Length; indice++)
        {
            char caracter = nombre[indice];
            bool esUltimo = indice == nombre.Length - 1;
            bool debeEscaparse = caracter == '%' || CaracteresInvalidos.Contains(caracter) ||
                                 esUltimo && caracter is '.' or ' ' || indice == 0 && nombreReservado;

            if (debeEscaparse)
                resultado.Append($"%{(int)caracter:X4}");
            else
                resultado.Append(caracter);
        }

        return resultado.ToString();
    }

}
