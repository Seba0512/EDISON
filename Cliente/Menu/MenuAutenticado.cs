using Cliente.Menu.Opciones;

namespace Cliente.Menu;

/// <summary>Builder del menú autenticado: las doce opciones de CR1-CR10 en el orden final del enunciado.</summary>
internal static class MenuAutenticado
{
    public static MenuDeOpciones Crear(ConexionConServidor conexion, EstadoDelMenu estado)
        => new([
            new OpcionAltaDeModelo(conexion),
            new OpcionModificarModelo(conexion),
            new OpcionEliminarModelo(conexion),
            new OpcionBuscarModelos(conexion),
            new OpcionConsultarModelo(conexion),
            new OpcionSolicitarReserva(conexion),
            new OpcionCancelarReserva(conexion),
            new OpcionMisReservas(conexion),
            new OpcionHistorial(conexion),
            new OpcionDeDiagnostico(conexion),
            new OpcionCerrarSesion(conexion, estado),
            new OpcionSalir(estado)
        ]);
}
