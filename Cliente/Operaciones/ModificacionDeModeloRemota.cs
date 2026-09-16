using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

internal sealed class ModificacionDeModeloRemota
{
    private readonly ConexionConServidor _conexion;

    public ModificacionDeModeloRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Ejecutar(DatosDeAltaDeModelo datos)
        => _conexion.EnviarSolicitud(Comandos.ModificarModelo, CodecDeDatosDeModelo.Codificar(datos));
}
