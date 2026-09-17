using Cliente.Menu.Opciones;

namespace Cliente.Menu;

internal static class MenuAnonimo
{
    public static MenuDeOpciones Crear(ConexionConServidor conexion, EstadoDelMenu estado)
        => new(
            () => "Menú principal",
            [
                new OpcionRegistrarse(conexion),
                new OpcionIniciarSesion(conexion, estado),
                new OpcionDeDiagnostico(conexion),
                new OpcionSalir(estado)
            ]);
}
