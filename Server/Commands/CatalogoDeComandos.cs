using Server.Domain.Facade;

namespace Server.Commands;

// Factory: el único lugar que conoce los handlers concretos.
internal static class CatalogoDeComandos
{
    public static DespachadorDeComandos Crear(Edison edison, ImagenesEnDisco imagenes)
    {
        return new DespachadorDeComandos(new IManejadorDeComando[]
        {
            new ManejadorRegistrar(edison),
            new ManejadorIniciarSesion(edison),
            new ManejadorCerrarSesion(edison),
            new ManejadorAltaDeModelo(edison),
            new ManejadorModificarModelo(edison),
            new ManejadorBajaDeModelo(edison, imagenes),
            new ManejadorBuscarModelos(edison),
            new ManejadorConsultarModelo(edison),
            new ManejadorSubirImagen(edison, imagenes),
            new ManejadorDescargarImagen(edison, imagenes),
            new ManejadorSolicitarReserva(edison),
            new ManejadorCancelarReserva(edison),
            new ManejadorConsultarMisReservas(edison),
            new ManejadorConsultarHistorial(edison)
        });
    }
}
