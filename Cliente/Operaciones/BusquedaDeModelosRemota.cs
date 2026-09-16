using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

/// <summary>
/// CMD 13 del lado cliente (CR7): esconde detrás de un método de dominio la codificación de los
/// filtros, el viaje por <see cref="ConexionConServidor"/> y la decodificación del resultado, para que
/// la opción de menú que la use no vea bytes ni sockets.
/// </summary>
internal sealed class BusquedaDeModelosRemota
{
    private readonly ConexionConServidor _conexion;

    public BusquedaDeModelosRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public IReadOnlyList<ModeloListado> Ejecutar(FiltrosDeBusqueda filtros)
    {
        byte[] datos = CodecDeBusquedaDeModelos.Codificar(filtros);
        byte[] cuerpo = _conexion.EnviarSolicitudYLeerCuerpo(Comandos.BuscarModelos, datos);
        return CodecDeBusquedaDeModelos.DecodificarResultado(cuerpo);
    }
}
