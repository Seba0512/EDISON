using Servidor.Composicion;
using ComandosDelProtocolo = Common.Protocolo.Comandos;

namespace Servidor.Comandos.Registro;

internal static class RegistroDeManejadores
{
    public static IReadOnlyDictionary<int, IManejadorDeComando> Crear(ServiciosDelServidor servicios)
        => new Dictionary<int, IManejadorDeComando>
        {
            [ComandosDelProtocolo.Diagnostico] = new ManejadorDeDiagnostico(),
            [ComandosDelProtocolo.Registrar] = new ManejadorDeRegistro(servicios.Autenticacion),
            [ComandosDelProtocolo.IniciarSesion] = new ManejadorDeInicioDeSesion(servicios.Autenticacion),
            [ComandosDelProtocolo.CerrarSesion] = new ManejadorDeCierreDeSesion(servicios.Autenticacion),
            [ComandosDelProtocolo.AltaDeModelo] = new ManejadorDeAltaDeModelo(servicios.CatalogoDeModelos),
            [ComandosDelProtocolo.ModificarModelo] = new ManejadorDeModificacionDeModelo(servicios.CatalogoDeModelos),
            [ComandosDelProtocolo.BajaDeModelo] = new ManejadorDeBajaDeModelo(servicios.CatalogoDeModelos),
            [ComandosDelProtocolo.BuscarModelos] = new ManejadorDeBusquedaDeModelos(servicios.CatalogoDeModelos),
            [ComandosDelProtocolo.ConsultarModelo] = new ManejadorDeConsultaDeModelo(servicios.CatalogoDeModelos),
            [ComandosDelProtocolo.SubirImagen] = new ManejadorDeSubidaDeImagen(servicios.CatalogoDeModelos, servicios.RepositorioDeImagenes),
            [ComandosDelProtocolo.DescargarImagen] = new ManejadorDeDescargaDeImagen(servicios.CatalogoDeModelos, servicios.RepositorioDeImagenes),
            [ComandosDelProtocolo.SolicitarReserva] = new ManejadorDeSolicitudDeReserva(servicios.GestionDeReservas),
            [ComandosDelProtocolo.CancelarReserva] = new ManejadorDeCancelacionDeReserva(servicios.GestionDeReservas),
            [ComandosDelProtocolo.ConsultarMisReservas] = new ManejadorDeConsultaDeReservas(servicios.GestionDeReservas),
            [ComandosDelProtocolo.ConsultarHistorial] = new ManejadorDeHistorial(servicios.HistorialDeUsuario)
        };
}
