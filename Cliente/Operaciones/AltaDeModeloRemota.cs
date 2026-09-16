using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

/// <summary>CMD 10: envía el alta de modelo al servidor (CR2). No conoce bytes ni sockets: delega en <see cref="ConexionConServidor"/>.</summary>
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
