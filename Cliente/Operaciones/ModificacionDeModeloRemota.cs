using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

/// <summary>CMD 11: envía la modificación de modelo al servidor (CR3). No conoce bytes ni sockets: delega en <see cref="ConexionConServidor"/>.</summary>
internal sealed class ModificacionDeModeloRemota
{
    private readonly ConexionConServidor _conexion;

    public ModificacionDeModeloRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Ejecutar(DatosDeAltaDeModelo datos)
        => _conexion.EnviarSolicitud(Comandos.ModificarModelo, CodecDeModificacionDeModelo.Codificar(datos));
}
