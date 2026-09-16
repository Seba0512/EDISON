using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

internal sealed class AltaDeModeloRemota
{
    private readonly ConexionConServidor _conexion;

    public AltaDeModeloRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Ejecutar(DatosDeAltaDeModelo datos)
        => _conexion.EnviarSolicitud(Comandos.AltaDeModelo, CodecDeAltaDeModelo.Codificar(datos));
}
