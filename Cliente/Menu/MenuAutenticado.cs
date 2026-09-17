using Cliente.Menu.Opciones;

namespace Cliente.Menu;

internal static class MenuAutenticado
{
    public static MenuDeOpciones Crear(
        ConexionConServidor conexion,
        EstadoDelMenu estado,
        DirectorioDeDescargasDeInstancia descargas)
        => new(
            () => $"Sesión de {estado.NombreUsuarioAutenticado}",
            [
                new OpcionAltaDeModelo(conexion),
                new OpcionModificarModelo(conexion),
                new OpcionEliminarModelo(conexion),
                new OpcionBuscarModelos(conexion),
                new OpcionConsultarModelo(conexion, estado, descargas),
                new OpcionSolicitarReserva(conexion),
                new OpcionCancelarReserva(conexion),
                new OpcionMisReservas(conexion),
                new OpcionHistorial(conexion),
                new OpcionDeDiagnostico(conexion),
                new OpcionCerrarSesion(conexion, estado)
            ]);
}
