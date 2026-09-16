using Cliente.Menu.Opciones;

namespace Cliente.Menu;

/// <summary>
/// Builder del menú autenticado, pre-sembrado en el orden final de CR2-CR10 con placeholders
/// <see cref="OpcionNoDisponible"/>. Cada rama hija reemplaza exactamente una línea por su opción
/// real; las demás líneas no cambian, así que los conflictos posibles entre ramas quedan acotados a
/// líneas adyacentes.
/// </summary>
internal static class MenuAutenticado
{
    public static MenuDeOpciones Crear(ConexionConServidor conexion, EstadoDelMenu estado)
        => new([
            new OpcionAltaDeModelo(conexion),
            new OpcionModificarModelo(conexion),
            new OpcionNoDisponible("Eliminar modelo"),
            new OpcionBuscarModelos(conexion),
            new OpcionConsultarModelo(conexion),
            new OpcionNoDisponible("Solicitar reserva"),
            new OpcionNoDisponible("Cancelar reserva"),
            new OpcionMisReservas(conexion),
            new OpcionHistorial(conexion),
            new OpcionDeDiagnostico(conexion),
            new OpcionCerrarSesion(conexion, estado),
            new OpcionSalir(estado)
        ]);
}
