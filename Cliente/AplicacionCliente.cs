using Cliente.Excepciones;
using Cliente.Menu;
using Cliente.Vistas;

namespace Cliente;

internal static class AplicacionCliente
{
    public static void Ejecutar(
        ConexionConServidor conexion, DirectorioDeDescargasDeInstancia descargas)
    {
        EstadoDelMenu estado = new();
        estado.Inicializar(
            MenuAnonimo.Crear(conexion, estado),
            MenuAutenticado.Crear(conexion, estado, descargas));
        CicloDeMenu(estado);
    }

    private static void CicloDeMenu(EstadoDelMenu estado)
    {
        while (estado.SeguirEjecutando)
        {
            estado.MenuActual.Mostrar();
            IOpcionDeMenu? opcion = estado.MenuActual.ElegirOpcion();
            if (opcion is null)
            {
                estado.Terminar();
                return;
            }

            PausarSiCorresponde(estado, Ejecutar(opcion));
        }
    }

    // Ante una excepcion, la opcion no llega a decir si necesita pausa: se pausa igual
    // para que el error alcance a leerse antes de redibujar el menu.
    private static bool Ejecutar(IOpcionDeMenu opcion)
    {
        try
        {
            opcion.Ejecutar();
            return opcion.RequierePausa;
        }
        catch (OperacionRechazadaException exception)
        {
            Consola.Error(exception.Message);
            return true;
        }
    }

    private static void PausarSiCorresponde(EstadoDelMenu estado, bool requierePausa)
    {
        if (estado.SeguirEjecutando && requierePausa)
            LectorDeConsola.EsperarEnter();
    }
}
