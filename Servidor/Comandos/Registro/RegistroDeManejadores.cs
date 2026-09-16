using Servidor.Composicion;

namespace Servidor.Comandos.Registro;

/// <summary>
/// Mapa CMD → manejador (CR1-CR10), pre-sembrado con los quince comandos del mapa final en su orden
/// definitivo. Cada rama hija de CR2-CR10 reemplaza exactamente una línea por su manejador real; los
/// conflictos posibles entre ramas quedan acotados a líneas adyacentes.
/// </summary>
internal static class RegistroDeManejadores
{
    public static IReadOnlyDictionary<int, IManejadorDeComando> Crear(ServiciosDelServidor servicios)
        => new Dictionary<int, IManejadorDeComando>
        {
            [Common.Protocolo.Comandos.Diagnostico] = new ManejadorDeDiagnostico(),
            [Common.Protocolo.Comandos.Registrar] = new ManejadorDeRegistro(servicios.Autenticacion),
            [Common.Protocolo.Comandos.IniciarSesion] = new ManejadorDeInicioDeSesion(servicios.Autenticacion),
            [Common.Protocolo.Comandos.CerrarSesion] = new ManejadorDeCierreDeSesion(servicios.Autenticacion),
            [Common.Protocolo.Comandos.AltaDeModelo] = new ManejadorNoDisponible(Common.Protocolo.Comandos.AltaDeModelo),
            [Common.Protocolo.Comandos.ModificarModelo] = new ManejadorNoDisponible(Common.Protocolo.Comandos.ModificarModelo),
            [Common.Protocolo.Comandos.BajaDeModelo] = new ManejadorNoDisponible(Common.Protocolo.Comandos.BajaDeModelo),
            [Common.Protocolo.Comandos.BuscarModelos] = new ManejadorNoDisponible(Common.Protocolo.Comandos.BuscarModelos),
            [Common.Protocolo.Comandos.ConsultarModelo] = new ManejadorNoDisponible(Common.Protocolo.Comandos.ConsultarModelo),
            [Common.Protocolo.Comandos.SubirImagen] = new ManejadorNoDisponible(Common.Protocolo.Comandos.SubirImagen),
            [Common.Protocolo.Comandos.DescargarImagen] = new ManejadorNoDisponible(Common.Protocolo.Comandos.DescargarImagen),
            [Common.Protocolo.Comandos.SolicitarReserva] = new ManejadorNoDisponible(Common.Protocolo.Comandos.SolicitarReserva),
            [Common.Protocolo.Comandos.CancelarReserva] = new ManejadorNoDisponible(Common.Protocolo.Comandos.CancelarReserva),
            [Common.Protocolo.Comandos.ConsultarMisReservas] = new ManejadorNoDisponible(Common.Protocolo.Comandos.ConsultarMisReservas),
            [Common.Protocolo.Comandos.ConsultarHistorial] = new ManejadorDeHistorial(servicios.HistorialDeUsuario)
        };
}
