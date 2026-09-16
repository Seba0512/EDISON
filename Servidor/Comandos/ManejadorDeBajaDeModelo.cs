using Common.Protocolo;
using Dominio;
using Dominio.Fachada;

namespace Servidor.Comandos;

/// <summary>
/// CMD 12: baja de un modelo (CR4/SR3). Decodifica el nombre pedido y delega todo en
/// <see cref="ICatalogoDeModelos.EliminarModelo"/>, que ya exige que el solicitante sea el
/// propietario y que el modelo no tenga reservas activas (lanzando <see cref="Dominio.Excepciones.DominioException"/>
/// si no, traducida sola a ER por <see cref="DespachadorDeComandos"/>) y borra la imagen asociada si
/// existía. El manejador no conoce esas reglas: sólo traduce protocolo a dominio y viceversa.
/// </summary>
internal sealed class ManejadorDeBajaDeModelo : ManejadorAutenticado
{
    private readonly ICatalogoDeModelos _catalogoDeModelos;

    public ManejadorDeBajaDeModelo(ICatalogoDeModelos catalogoDeModelos)
    {
        _catalogoDeModelos = catalogoDeModelos;
    }

    protected override RespuestaDelServidor ResponderAutenticado(SolicitudRecibida solicitud, Usuario usuario)
    {
        string nombreModelo = CodecDeBajaDeModelo.Decodificar(solicitud.Datos.Span);
        _catalogoDeModelos.EliminarModelo(nombreModelo, usuario);
        return RespuestaDelServidor.Ok($"Modelo '{nombreModelo}' eliminado");
    }
}
