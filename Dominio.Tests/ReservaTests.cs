using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class ReservaTests
{
    private const string NombreUsuario = "jferreiro";
    private const string NombreModelo = "Edison S1";
    private const int StockInicial = 2;

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private Usuario usuaria = null!;
    private Modelo modelo = null!;
    private Reserva reserva = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        usuaria = new Usuario(NombreUsuario, "edison2026");
        modelo = new Modelo(FichaDe(NombreModelo), usuaria);
        modelo.AgregarUnidades(StockInicial);
        reserva = ReservaRegistradaEn(modelo);
    }

    private static FichaModelo FichaDe(string nombre)
    {
        return new FichaModelo(nombre, "Sedan electrico", 45000m, new DateTime(2027, 3, 1), string.Empty);
    }

    private Reserva ReservaRegistradaEn(Modelo unModelo)
    {
        Reserva nueva = new Reserva(usuaria, unModelo, reloj);
        unModelo.RegistrarReserva(nueva);
        return nueva;
    }

    [TestMethod]
    public void LaReservaNacePendienteYActiva()
    {
        Assert.AreEqual(EstadoReserva.Pendiente, reserva.Estado);
        Assert.IsTrue(reserva.EstaPendiente);
        Assert.IsTrue(reserva.EstaActiva);
    }

    [TestMethod]
    public void LaReservaNaceConLasDosFechasIguales()
    {
        Assert.AreEqual(reserva.FechaCreacion, reserva.UltimaActualizacion);
    }

    [TestMethod]
    public void LaReservaTomaLaHoraDelRelojQueRecibe()
    {
        Assert.AreEqual(Momento, reserva.FechaCreacion);
    }

    [TestMethod]
    public void LosAccesoresDeleganSinExponerElModeloNiElUsuario()
    {
        Assert.AreEqual(NombreUsuario, reserva.NombreDelUsuario());
        Assert.AreEqual(NombreModelo, reserva.NombreDelModelo());
    }

    [TestMethod]
    public void ResolverConStockLaConfirmaYTomaUnaUnidad()
    {
        reserva.Resolver();

        Assert.IsTrue(reserva.EstaConfirmada);
        Assert.AreEqual(1, modelo.UnidadesReservadas);
        Assert.AreEqual(StockInicial - 1, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    public void ResolverSinStockLaRechaza()
    {
        Modelo modeloSinStock = new Modelo(FichaDe("Edison X2"), usuaria);
        Reserva sinUnidades = ReservaRegistradaEn(modeloSinStock);

        sinUnidades.Resolver();

        Assert.AreEqual(EstadoReserva.Rechazada, sinUnidades.Estado);
        Assert.IsFalse(sinUnidades.EstaActiva);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void ResolverUnaReservaNoRegistradaEnElModeloFalla()
    {
        Reserva noRegistrada = new Reserva(usuaria, modelo, reloj);

        noRegistrada.Resolver();
    }

    [TestMethod]
    public void CancelarUnaPendienteNoTocaElStock()
    {
        reserva.Cancelar();

        Assert.AreEqual(EstadoReserva.Cancelada, reserva.Estado);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    public void CancelarUnaConfirmadaDevuelveLaUnidadAlModelo()
    {
        reserva.Resolver();

        reserva.Cancelar();

        Assert.AreEqual(EstadoReserva.Cancelada, reserva.Estado);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CancelarUnaRechazadaFalla()
    {
        reserva.Rechazar();

        reserva.Cancelar();
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CancelarDosVecesFalla()
    {
        reserva.Cancelar();

        reserva.Cancelar();
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void ConfirmarUnaReservaYaConfirmadaFalla()
    {
        reserva.Confirmar();

        reserva.Confirmar();
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void RechazarUnaReservaCanceladaFalla()
    {
        reserva.Cancelar();

        reserva.Rechazar();
    }

    [TestMethod]
    public void ResolverActualizaLaUltimaActualizacion()
    {
        DateTime antes = reserva.UltimaActualizacion;
        reloj.Avanzar(TimeSpan.FromSeconds(30));

        reserva.Resolver();

        Assert.IsTrue(reserva.UltimaActualizacion > antes);
        Assert.AreEqual(reserva.FechaCreacion, antes);
    }

    [TestMethod]
    public void VencioElPlazoEsVerdaderoConEsperaCero()
    {
        Assert.IsTrue(reserva.VencioElPlazo(0));
    }

    [TestMethod]
    public void ElPlazoVenceRecienCuandoElRelojPasaLosSegundosDeEspera()
    {
        Assert.IsFalse(reserva.VencioElPlazo(30));

        reloj.Avanzar(TimeSpan.FromSeconds(29));
        Assert.IsFalse(reserva.VencioElPlazo(30));

        reloj.Avanzar(TimeSpan.FromSeconds(1));
        Assert.IsTrue(reserva.VencioElPlazo(30));
    }

    [TestMethod]
    public void VencioElPlazoEsFalsoSiYaNoEstaPendiente()
    {
        reserva.Resolver();

        Assert.IsFalse(reserva.VencioElPlazo(0));
    }

    [TestMethod]
    public void FueHechaPorReconoceAlUsuarioIgnorandoMayusculas()
    {
        Assert.IsTrue(reserva.FueHechaPor("JFerreiro"));
        Assert.IsFalse(reserva.FueHechaPor("otra"));
    }

    [TestMethod]
    public void EsDelModeloReconoceElNombreDelModelo()
    {
        Assert.IsTrue(reserva.EsDelModelo(NombreModelo));
        Assert.IsFalse(reserva.EsDelModelo("Edison X2"));
    }
}
