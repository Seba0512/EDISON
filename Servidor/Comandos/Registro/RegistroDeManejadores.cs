using Servidor.Composicion;

namespace Servidor.Comandos.Registro;

/// <summary>Mapa CMD → manejador (CR1-CR10): los quince comandos del protocolo con su manejador real.</summary>
internal static class RegistroDeManejadores
{
    public static IReadOnlyDictionary<int, IManejadorDeComando> Crear(ServiciosDelServidor servicios)
        => new Dictionary<int, IManejadorDeComando>
        {
            [Common.Protocolo.Comandos.Diagnostico] = new ManejadorDeDiagnostico(),
            [Common.Protocolo.Comandos.Registrar] = new ManejadorDeRegistro(servicios.Autenticacion),
            [Common.Protocolo.Comandos.IniciarSesion] = new ManejadorDeInicioDeSesion(servicios.Autenticacion),
            [Common.Protocolo.Comandos.CerrarSesion] = new ManejadorDeCierreDeSesion(servicios.Autenticacion),
            [Common.Protocolo.Comandos.AltaDeModelo] = new ManejadorDeAltaDeModelo(servicios.CatalogoDeModelos),
            [Common.Protocolo.Comandos.ModificarModelo] = new ManejadorDeModificacionDeModelo(servicios.CatalogoDeModelos),
            [Common.Protocolo.Comandos.BajaDeModelo] = new ManejadorDeBajaDeModelo(servicios.CatalogoDeModelos),
            [Common.Protocolo.Comandos.BuscarModelos] = new ManejadorDeBusquedaDeModelos(servicios.CatalogoDeModelos),
            [Common.Protocolo.Comandos.ConsultarModelo] = new ManejadorDeConsultaDeModelo(servicios.CatalogoDeModelos),
            [Common.Protocolo.Comandos.SubirImagen] = new ManejadorDeSubidaDeImagen(servicios.CatalogoDeModelos, servicios.RepositorioDeImagenes),
            [Common.Protocolo.Comandos.DescargarImagen] = new ManejadorDeDescargaDeImagen(servicios.CatalogoDeModelos, servicios.RepositorioDeImagenes),
            [Common.Protocolo.Comandos.SolicitarReserva] = new ManejadorDeSolicitudDeReserva(servicios.GestionDeReservas),
            [Common.Protocolo.Comandos.CancelarReserva] = new ManejadorDeCancelacionDeReserva(servicios.GestionDeReservas),
            [Common.Protocolo.Comandos.ConsultarMisReservas] = new ManejadorDeConsultaDeReservas(servicios.GestionDeReservas),
            [Common.Protocolo.Comandos.ConsultarHistorial] = new ManejadorDeHistorial(servicios.HistorialDeUsuario)
        };
}
