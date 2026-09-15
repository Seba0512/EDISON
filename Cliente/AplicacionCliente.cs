using Cliente.Excepciones;
using Cliente.Menu;
using Cliente.Menu.Opciones;

namespace Cliente;

internal static class AplicacionCliente
{
    public static void Ejecutar(ConexionConServidor conexion)
    {
        EstadoDelMenu estado = new();
        estado.Inicializar(CrearMenuAnonimo(conexion, estado), CrearMenuAutenticado(conexion, estado));
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
        }
    }

    private static MenuDeOpciones CrearMenuAnonimo(ConexionConServidor conexion, EstadoDelMenu estado)
        => new([
            new OpcionRegistrarse(conexion),
            new OpcionIniciarSesion(conexion, estado),
            new OpcionDeDiagnostico(conexion),
            new OpcionSalir(estado)
        ]);

    private static MenuDeOpciones CrearMenuAutenticado(ConexionConServidor conexion, EstadoDelMenu estado)
        => new([
            new OpcionNoDisponible("Alta de modelo"),
            new OpcionNoDisponible("Modificar modelo"),
            new OpcionNoDisponible("Eliminar modelo"),
            new OpcionNoDisponible("Buscar modelos"),
            new OpcionNoDisponible("Consultar modelo"),
            new OpcionNoDisponible("Solicitar reserva"),
            new OpcionNoDisponible("Cancelar reserva"),
            new OpcionNoDisponible("Mis reservas"),
            new OpcionNoDisponible("Historial"),
            new OpcionDeDiagnostico(conexion),
            new OpcionCerrarSesion(conexion, estado),
            new OpcionSalir(estado)
        ]);
}
