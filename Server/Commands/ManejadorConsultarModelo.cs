using Common;
using Server.Domain.Entities;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorConsultarModelo : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorConsultarModelo(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.ConsultarModelo;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        LecturaDeModelo modelo = _edison.ConsultarLectura(solicitud.DatosComoTexto());
        DetalleDeModelo detalle = new(
            modelo.Nombre,
            modelo.Descripcion,
            modelo.Precio,
            modelo.FechaEntregaPrometida,
            modelo.UnidadesDisponibles,
            modelo.UnidadesReservadas,
            modelo.NombreArchivoImagen);

        return RespuestaDelServidor.Ok(Mensajes.DeDetalleDeModelo(detalle));
    }
}
