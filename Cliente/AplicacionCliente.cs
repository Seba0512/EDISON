using Cliente.Excepciones;
using Cliente.Menu;

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
            // Ante una excepcion, MostrarYEjecutar no llega a devolver nada: se pausa igual para que el error se lea.
            bool requierePausa = true;
            try
            {
                (bool continuar, requierePausa) = estado.MenuActual.MostrarYEjecutar();
                if (!continuar)
                    estado.Terminar();
            }
            catch (OperacionRechazadaException exception)
            {
                Console.WriteLine(exception.Message);
            }

            if (estado.SeguirEjecutando && requierePausa)
                LectorDeConsola.EsperarEnter();
        }
    }
}
