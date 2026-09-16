using Common.Protocolo;

namespace Cliente.Operaciones;

/// <summary>
/// CMD 12 del lado del cliente (CR4/SR3): arma el pedido con el nombre del modelo, lo envía por
/// <see cref="ConexionConServidor"/> y devuelve el texto de la respuesta (confirmación u error, ya
/// resuelto por <see cref="ConexionConServidor.EnviarSolicitud"/>). Aísla a la opción de menú de
/// bytes, sockets y del formato exacto del payload.
/// </summary>
internal sealed class BajaDeModeloRemota
{
    private readonly ConexionConServidor _conexion;

    public BajaDeModeloRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Ejecutar(string nombreModelo)
    {
        byte[] solicitud = CodecDeBajaDeModelo.Codificar(nombreModelo);
        return _conexion.EnviarSolicitud(Comandos.BajaDeModelo, solicitud);
    }
}
