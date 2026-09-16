using Cliente.Menu.Opciones;

namespace Cliente.Menu;

/// <summary>Builder del menú anónimo (CR1), extraído de <c>AplicacionCliente</c> para que agregar opciones autenticadas no toque esta clase.</summary>
internal static class MenuAnonimo
{
    public static MenuDeOpciones Crear(ConexionConServidor conexion, EstadoDelMenu estado)
        => new([
            new OpcionRegistrarse(conexion),
            new OpcionIniciarSesion(conexion, estado),
            new OpcionDeDiagnostico(conexion),
            new OpcionSalir(estado)
        ]);
}
