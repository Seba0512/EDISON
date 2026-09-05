using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;
using Dominio.Tests.Dobles;

namespace Dominio.Tests;

[TestClass]
public class CentralDeReservasTests
{
    private const string NombreModelo = "Edison S1";
    private const int StockInicial = 2;
    private const int SegundosDeAprobacion = 15;
    private const int EsperaDeHiloEnMilisegundos = 5000;

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private RepositorioDeUsuariosEnMemoria usuarios = null!;
    private RepositorioDeModelosEnMemoria modelos = null!;
    private CentralDeReservas central = null!;
    private Usuario solicitante = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        usuarios = new RepositorioDeUsuariosEnMemoria();
        modelos = new RepositorioDeModelosEnMemoria();
        central = new CentralDeReservas(
            new RepositorioDeReservasEnMemoria(), modelos, usuarios, reloj, SegundosDeAprobacion);

        solicitante = new Usuario("jferreiro", "edison2026");
        usuarios.Agregar(solicitante);
        modelos.Agregar(ModeloConStock(StockInicial));
    }

    private Modelo ModeloConStock(int stock)
    {
        DatosDeModelo datos = new DatosDeModelo(
            NombreModelo, "Sedan electrico", 45000m, new DateTime(2027, 3, 1), string.Empty);
        Modelo modelo = new Modelo(datos, solicitante);
        modelo.AgregarUnidades(stock);

        return modelo;
    }

    private void VencerElPlazo()
    {
        reloj.Avanzar(TimeSpan.FromSeconds(SegundosDeAprobacion + 1));
    }

    [TestMethod]
    public void SolicitarDejaLaReservaPendienteSinTomarUnidad()
    {
        central.SolicitarReserva(NombreModelo, solicitante);

        Modelo modelo = modelos.ObtenerPorNombre(NombreModelo);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(1, central.ReservasDe(solicitante).Count);
        Assert.IsTrue(central.ReservasDe(solicitante)[0].EstaPendiente);
    }

    [TestMethod]
    public void SolicitarQuedaRegistradoEnElHistorial()
    {
        central.SolicitarReserva(NombreModelo, solicitante);

        Assert.AreEqual(TipoActividad.ReservaCreada, solicitante.Actividades()[0].Tipo);
    }

    [TestMethod]
    [ExpectedException(typeof(ModeloInexistenteException))]
    public void SolicitarSobreUnModeloInexistenteFalla()
    {
        central.SolicitarReserva("Edison X2", solicitante);
    }

    [TestMethod]
    public void ResolverVencidasNoTocaLasQueTodaviaEstanEnPlazo()
    {
        central.SolicitarReserva(NombreModelo, solicitante);

        central.ResolverVencidas();

        Assert.IsTrue(central.ReservasDe(solicitante)[0].EstaPendiente);
    }

    [TestMethod]
    public void AlVencerElPlazoLaReservaSeConfirmaYTomaUnaUnidad()
    {
        central.SolicitarReserva(NombreModelo, solicitante);
        VencerElPlazo();

        central.ResolverVencidas();

        Assert.IsTrue(central.ReservasDe(solicitante)[0].EstaConfirmada);
        Assert.AreEqual(StockInicial - 1, modelos.ObtenerPorNombre(NombreModelo).UnidadesDisponibles);
    }

    /// <summary>CR10: una reserva confirmada es la compra, y queda registrada como tal.</summary>
    [TestMethod]
    public void LaConfirmacionQuedaRegistradaComoCompra()
    {
        central.SolicitarReserva(NombreModelo, solicitante);
        VencerElPlazo();

        central.ResolverVencidas();

        Assert.AreEqual(TipoActividad.Compra, solicitante.Actividades()[1].Tipo);
    }

    [TestMethod]
    public void ResolverVencidasEnDosThreadsRegistraUnaSolaCompra()
    {
        using RepositorioDeReservasCoordinado repositorio =
            new RepositorioDeReservasCoordinado();
        CentralDeReservas centralConcurrente = new CentralDeReservas(
            repositorio, modelos, usuarios, reloj, SegundosDeAprobacion);
        centralConcurrente.SolicitarReserva(NombreModelo, solicitante);
        VencerElPlazo();

        List<EjecucionDeHilo> ejecuciones = EjecutarEnDosThreads(
            centralConcurrente.ResolverVencidas);

        Assert.IsTrue(ejecuciones.All(ejecucion => ejecucion.Falla is null));
        Assert.AreEqual(1, solicitante.Actividades().Count(
            actividad => actividad.Tipo == TipoActividad.Compra));
        Assert.AreEqual(1, modelos.ObtenerPorNombre(NombreModelo).UnidadesReservadas);
    }

    /// <summary>
    /// La carrera de CR5 resuelta en orden de creacion: con stock 2 y tres solicitudes, se confirman
    /// las dos primeras y se rechaza la tercera. Gana quien pidio antes, no quien se resuelve antes.
    /// </summary>
    [TestMethod]
    public void ConMasSolicitudesQueStockSeConfirmanLasPrimerasEnOrdenDeCreacion()
    {
        Guid primera = central.SolicitarReserva(NombreModelo, solicitante);
        reloj.Avanzar(TimeSpan.FromSeconds(1));
        Guid segunda = central.SolicitarReserva(NombreModelo, solicitante);
        reloj.Avanzar(TimeSpan.FromSeconds(1));
        Guid tercera = central.SolicitarReserva(NombreModelo, solicitante);
        VencerElPlazo();

        central.ResolverVencidas();

        Assert.IsTrue(ReservaCon(primera).EstaConfirmada);
        Assert.IsTrue(ReservaCon(segunda).EstaConfirmada);
        Assert.IsFalse(ReservaCon(tercera).EstaActiva);
        Assert.AreEqual(0, modelos.ObtenerPorNombre(NombreModelo).UnidadesDisponibles);
    }

    [TestMethod]
    public void CancelarUnaConfirmadaDevuelveLaUnidadAlPool()
    {
        Guid id = central.SolicitarReserva(NombreModelo, solicitante);
        VencerElPlazo();
        central.ResolverVencidas();

        central.CancelarReserva(id, solicitante);

        Assert.AreEqual(StockInicial, modelos.ObtenerPorNombre(NombreModelo).UnidadesDisponibles);
        Assert.AreEqual(TipoActividad.ReservaCancelada, UltimaActividad().Tipo);
    }

    /// <summary>CR6: una reserva rechazada no se puede cancelar.</summary>
    [TestMethod]
    [ExpectedException(typeof(TransicionDeReservaInvalidaException))]
    public void CancelarUnaRechazadaFalla()
    {
        central.SolicitarReserva(NombreModelo, solicitante);
        central.SolicitarReserva(NombreModelo, solicitante);
        Guid tercera = central.SolicitarReserva(NombreModelo, solicitante);
        VencerElPlazo();
        central.ResolverVencidas();

        central.CancelarReserva(tercera, solicitante);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CancelarLaReservaDeOtroUsuarioFalla()
    {
        Guid id = central.SolicitarReserva(NombreModelo, solicitante);
        Usuario intruso = new Usuario("sgomez", "edison2026");

        central.CancelarReserva(id, intruso);
    }

    [TestMethod]
    public void UnUsuarioSinReservasRecibeUnaListaVaciaYNoNull()
    {
        Usuario sinReservas = new Usuario("sgomez", "edison2026");

        Assert.AreEqual(0, central.ReservasDe(sinReservas).Count);
    }

    private Reserva ReservaCon(Guid id)
    {
        return central.ReservasDe(solicitante).First(reserva => reserva.Id == id);
    }

    private Actividad UltimaActividad()
    {
        return solicitante.Actividades()[^1];
    }

    private static List<EjecucionDeHilo> EjecutarEnDosThreads(Action accion)
    {
        List<EjecucionDeHilo> ejecuciones =
            new List<EjecucionDeHilo> { new(), new() };
        List<Thread> hilos = ejecuciones
            .Select(ejecucion => new Thread(() => ejecucion.Ejecutar(accion)))
            .ToList();

        hilos.ForEach(hilo => hilo.Start());
        hilos.ForEach(hilo => Assert.IsTrue(hilo.Join(EsperaDeHiloEnMilisegundos),
            "Un Thread no termino: posible deadlock"));

        return ejecuciones;
    }

    private sealed class EjecucionDeHilo
    {
        public Exception? Falla { get; private set; }

        public void Ejecutar(Action accion)
        {
            try
            {
                accion();
            }
            catch (Exception falla)
            {
                Falla = falla;
            }
        }
    }

    private sealed class RepositorioDeReservasCoordinado : IRepositorioDeReservas, IDisposable
    {
        private readonly RepositorioDeReservasEnMemoria repositorio = new();
        private readonly Barrier consultasPendientes = new(2);

        public void Agregar(Reserva reserva)
        {
            repositorio.Agregar(reserva);
        }

        public Reserva ObtenerPorId(Guid idReserva)
        {
            return repositorio.ObtenerPorId(idReserva);
        }

        public IReadOnlyList<Reserva> DeUsuario(string nombreUsuario)
        {
            return repositorio.DeUsuario(nombreUsuario);
        }

        public IReadOnlyList<Reserva> Pendientes()
        {
            IReadOnlyList<Reserva> pendientes = repositorio.Pendientes();
            if (!consultasPendientes.SignalAndWait(EsperaDeHiloEnMilisegundos))
            {
                throw new TimeoutException("No se coordinaron las consultas de reservas pendientes");
            }

            return pendientes;
        }

        public void Dispose()
        {
            consultasPendientes.Dispose();
        }
    }
}
