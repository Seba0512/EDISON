using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class ReservaTests
{
    private const string Usuario = "jferreiro";
    private const string NombreModelo = "Edison S1";

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private Reserva reserva = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        reserva = new Reserva(Usuario, NombreModelo, reloj);
    }

    [TestMethod]
    public void LaReservaNacePendienteYActiva()
    {
        Assert.AreEqual(EstadoReserva.Pendiente, reserva.Estado);
        Assert.IsTrue(reserva.EstaPendiente);
        Assert.IsTrue(reserva.EstaActiva);
        Assert.IsFalse(reserva.OcupaUnaUnidad);
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
    public void ConfirmarPasaAConfirmadaYOcupaUnidad()
    {
        reserva.Confirmar();

        Assert.AreEqual(EstadoReserva.Confirmada, reserva.Estado);
        Assert.IsTrue(reserva.OcupaUnaUnidad);
        Assert.IsTrue(reserva.EstaActiva);
    }

    [TestMethod]
    public void RechazarPasaARechazadaYDejaDeEstarActiva()
    {
        reserva.Rechazar();

        Assert.AreEqual(EstadoReserva.Rechazada, reserva.Estado);
        Assert.IsFalse(reserva.EstaActiva);
        Assert.IsFalse(reserva.OcupaUnaUnidad);
    }

    [TestMethod]
    public void CancelarUnaPendienteLaDejaCancelada()
    {
        reserva.Cancelar();

        Assert.AreEqual(EstadoReserva.Cancelada, reserva.Estado);
    }

    [TestMethod]
    public void CancelarUnaConfirmadaLaDejaCancelada()
    {
        reserva.Confirmar();

        reserva.Cancelar();

        Assert.AreEqual(EstadoReserva.Cancelada, reserva.Estado);
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
    public void ConfirmarActualizaLaUltimaModificacion()
    {
        DateTime antes = reserva.UltimaActualizacion;
        reloj.Avanzar(TimeSpan.FromSeconds(30));

        reserva.Confirmar();

        Assert.IsTrue(reserva.UltimaActualizacion > antes);
        Assert.AreEqual(reserva.FechaCreacion, antes);
    }

    [TestMethod]
    public void VencioElPlazoEsVerdaderoConEsperaCero()
    {
        Assert.IsTrue(reserva.VencioElPlazo(0));
    }

    [TestMethod]
    public void VencioElPlazoEsFalsoSiTodaviaFaltaTiempo()
    {
        Assert.IsFalse(reserva.VencioElPlazo(60));
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
        reserva.Confirmar();

        Assert.IsFalse(reserva.VencioElPlazo(0));
    }

    [TestMethod]
    public void FueHechaPorReconoceAlUsuarioIgnorandoMayusculas()
    {
        Assert.IsTrue(reserva.FueHechaPor("JFerreiro"));
        Assert.IsFalse(reserva.FueHechaPor("otro"));
    }

    [TestMethod]
    public void EsDelModeloReconoceElNombreDelModelo()
    {
        Assert.IsTrue(reserva.EsDelModelo(NombreModelo));
        Assert.IsFalse(reserva.EsDelModelo("Edison S2"));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearReservaSinUsuarioFalla()
    {
        _ = new Reserva("  ", NombreModelo, reloj);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearReservaSinModeloFalla()
    {
        _ = new Reserva(Usuario, "  ", reloj);
    }
}
