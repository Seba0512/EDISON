using Cliente.Operaciones;
using Cliente.Vistas;

namespace Cliente.Menu.Opciones;

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

        Consola.Exito(_bajaDeModeloRemota.Ejecutar(nombreModelo));
    }
}
