namespace Servidor.Repositorios;

// Una limpieza que falla nunca puede tapar el error original ni voltear una operación ya
// confirmada: solo se deja constancia. Este aviso estaba duplicado en dos clases.
internal static class DiagnosticoDeLimpieza
{
    public static void Informar(string mensaje)
    {
        try
        {
            Console.Error.WriteLine(mensaje);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            // Si ni siquiera se puede escribir el diagnóstico, no queda nada más por hacer.
        }
    }
}
