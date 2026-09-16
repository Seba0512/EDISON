using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

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
