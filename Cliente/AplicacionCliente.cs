using Cliente.Excepciones;
using Cliente.Menu;

namespace Cliente;

internal static class AplicacionCliente
{
    public static void Ejecutar(ConexionConServidor conexion)
    {
        EstadoDelMenu estado = new();
        estado.Inicializar(MenuAnonimo.Crear(conexion, estado), MenuAutenticado.Crear(conexion, estado));
        CicloDeMenu(estado);
    }

    private static void CicloDeMenu(EstadoDelMenu estado)
    {
        while (estado.SeguirEjecutando)
        {
            try
            {
                if (!estado.MenuActual.MostrarYEjecutar())
                    estado.Terminar();
            }
            catch (OperacionRechazadaException exception)
            {
                Console.WriteLine(exception.Message);
            }

            if (estado.SeguirEjecutando)
                LectorDeConsola.EsperarEnter();
        }
    }
}
