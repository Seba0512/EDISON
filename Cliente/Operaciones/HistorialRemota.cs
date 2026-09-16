using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Operaciones;

internal sealed class HistorialRemota
{
    private readonly ConexionConServidor _conexion;

    public HistorialRemota(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public IReadOnlyList<ActividadListada> Ejecutar()
    {
        byte[] cuerpo = _conexion.EnviarSolicitudYLeerCuerpo(Comandos.ConsultarHistorial, []);
        return CodecDeHistorial.DecodificarResultado(cuerpo);
    }
}
