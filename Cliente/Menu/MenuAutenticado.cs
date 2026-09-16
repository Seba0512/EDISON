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
            new OpcionNoDisponible("Alta de modelo"),
            new OpcionNoDisponible("Modificar modelo"),
            new OpcionNoDisponible("Eliminar modelo"),
            new OpcionNoDisponible("Buscar modelos"),
            new OpcionNoDisponible("Consultar modelo"),
            new OpcionNoDisponible("Solicitar reserva"),
            new OpcionNoDisponible("Cancelar reserva"),
            new OpcionMisReservas(conexion),
            new OpcionNoDisponible("Historial"),
            new OpcionDeDiagnostico(conexion),
            new OpcionCerrarSesion(conexion, estado),
            new OpcionSalir(estado)
        ]);
}
