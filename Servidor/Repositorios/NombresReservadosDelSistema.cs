namespace Servidor.Repositorios;

// Windows no permite estos nombres ni siquiera con extensión. La lista estaba copiada
// carácter por carácter en el validador de archivos y en el de carpetas.
internal static class NombresReservadosDelSistema
{
    private static readonly HashSet<string> Nombres = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static bool Contiene(string nombre)
        => Nombres.Contains(nombre);

    // Windows también reserva el nombre cuando viene con extensión o con espacios al final.
    public static bool EsReservado(string nombre)
        => Contiene(nombre.Split('.')[0].TrimEnd(' '));
}
