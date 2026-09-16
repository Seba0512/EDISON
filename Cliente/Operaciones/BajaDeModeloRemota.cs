using Common.Protocolo;

namespace Cliente.Operaciones;

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
