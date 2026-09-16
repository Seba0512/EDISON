using Cliente.Operaciones;

namespace Cliente.Menu.Opciones;

/// <summary>
/// CMD 12 en el menú autenticado (CR4/SR3): pide el nombre del modelo a eliminar, delega la baja en
/// <see cref="BajaDeModeloRemota"/> y muestra el texto de la respuesta. Nunca ve bytes ni sockets.
/// </summary>
internal sealed class OpcionEliminarModelo : IOpcionDeMenu
{
    private readonly BajaDeModeloRemota _bajaDeModeloRemota;

    public OpcionEliminarModelo(ConexionConServidor conexion)
    {
        _bajaDeModeloRemota = new BajaDeModeloRemota(conexion);
    }

    public string Titulo
        => "Eliminar modelo";

    public void Ejecutar()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a eliminar:");
        if (nombreModelo is null)
            return;

        Console.WriteLine(_bajaDeModeloRemota.Ejecutar(nombreModelo));
    }
}
