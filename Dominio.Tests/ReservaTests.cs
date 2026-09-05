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
        modelo = new Modelo(DatosDe(NombreModelo), usuaria);
        modelo.AgregarUnidades(StockInicial);
        reserva = ReservaRegistradaEn(modelo);
    }

    private static DatosDeModelo DatosDe(string nombre)
    {
        return new DatosDeModelo(nombre, "Sedan electrico", 45000m, new DateTime(2027, 3, 1), string.Empty);
    }

    private Reserva ReservaRegistradaEn(Modelo unModelo)
    {
        return unModelo.RegistrarReserva(usuaria, reloj);
    }

    [TestMethod]
    public void LaReservaNacePendienteYActiva()
    {
        Assert.AreEqual("Pendiente", reserva.NombreDelEstado());
        Assert.IsTrue(reserva.EstaPendiente);
        Assert.IsTrue(reserva.EstaActiva);
        Assert.IsNull(reserva.VehiculoAsignado);
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
        Assert.IsNotNull(reserva.VehiculoAsignado);
        Assert.AreEqual(NombreModelo, reserva.VehiculoAsignado.NombreModelo);
        Assert.IsTrue(modelo.ContieneUnidad(reserva.VehiculoAsignado));
        Assert.IsFalse(reserva.VehiculoAsignado.EstaDisponible);
        Assert.AreEqual(1, modelo.UnidadesReservadas);
        Assert.AreEqual(StockInicial - 1, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    public void ResolverSinStockLaRechaza()
    {
        Modelo modeloSinStock = new Modelo(DatosDe("Edison X2"), usuaria);
        Reserva sinUnidades = ReservaRegistradaEn(modeloSinStock);

        sinUnidades.Resolver();

        Assert.AreEqual("Rechazada", sinUnidades.NombreDelEstado());
        Assert.IsFalse(sinUnidades.EstaActiva);
        Assert.IsNull(sinUnidades.VehiculoAsignado);
    }

    [TestMethod]
    public void RechazarUnaPendienteNoLeAsignaUnidad()
    {
        reserva.Rechazar();

        Assert.AreEqual("Rechazada", reserva.NombreDelEstado());
        Assert.IsNull(reserva.VehiculoAsignado);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
    }

    [TestMethod]
    public void ConfirmarConUnidadDeOtroModeloFalla()
    {
        Modelo otroModelo = new Modelo(DatosDe("Edison X2"), usuaria);
        otroModelo.AgregarUnidades(1);
        Reserva ajena = ReservaRegistradaEn(otroModelo);
        ajena.Resolver();

        Assert.ThrowsException<DominioException>(
            () => reserva.Confirmar(ajena.VehiculoAsignado!));
        Assert.IsTrue(reserva.EstaPendiente);
        Assert.IsNull(reserva.VehiculoAsignado);
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
        Reserva confirmada = ReservaRegistradaEn(modelo);
        confirmada.Resolver();
        Vehiculo unidadConfirmada = confirmada.VehiculoAsignado!;

        reserva.Cancelar();

        Assert.AreEqual("Cancelada", reserva.NombreDelEstado());
        Assert.IsNull(reserva.VehiculoAsignado);
        Assert.IsFalse(unidadConfirmada.EstaDisponible);
        Assert.AreEqual(StockInicial - 1, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    public void CancelarUnaConfirmadaDevuelveLaUnidadAlModelo()
    {
        reserva.Resolver();
        Vehiculo unidadAsignada = reserva.VehiculoAsignado!;

        reserva.Cancelar();

        Assert.AreEqual("Cancelada", reserva.NombreDelEstado());
        Assert.IsNull(reserva.VehiculoAsignado);
        Assert.IsTrue(unidadAsignada.EstaDisponible);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
    }

    [TestMethod]
    [ExpectedException(typeof(TransicionDeReservaInvalidaException))]
    public void CancelarUnaRechazadaFalla()
    {
        reserva.Rechazar();

        reserva.Cancelar();
    }

    [TestMethod]
    [ExpectedException(typeof(TransicionDeReservaInvalidaException))]
    public void CancelarDosVecesFalla()
    {
        reserva.Cancelar();

        reserva.Cancelar();
    }

    [TestMethod]
    [ExpectedException(typeof(TransicionDeReservaInvalidaException))]
    public void ConfirmarUnaReservaYaConfirmadaFalla()
    {
        reserva.Resolver();

        reserva.Confirmar(reserva.VehiculoAsignado!);
    }

    [TestMethod]
    public void DosConfirmadasRecibenUnidadesDistintas()
    {
        Reserva segunda = ReservaRegistradaEn(modelo);

        reserva.Resolver();
        segunda.Resolver();

        Assert.IsNotNull(reserva.VehiculoAsignado);
        Assert.IsNotNull(segunda.VehiculoAsignado);
        Assert.AreNotSame(reserva.VehiculoAsignado, segunda.VehiculoAsignado);
        Assert.AreNotEqual(reserva.VehiculoAsignado.Id, segunda.VehiculoAsignado.Id);
    }

    [TestMethod]
    public void CancelarUnaConfirmadaLiberaSoloSuUnidad()
    {
        Reserva segunda = ReservaRegistradaEn(modelo);
        reserva.Resolver();
        segunda.Resolver();
        Vehiculo primeraUnidad = reserva.VehiculoAsignado!;
        Vehiculo segundaUnidad = segunda.VehiculoAsignado!;

        reserva.Cancelar();

        Assert.IsTrue(primeraUnidad.EstaDisponible);
        Assert.IsFalse(segundaUnidad.EstaDisponible);
        Assert.AreEqual(1, modelo.UnidadesDisponibles);
        Assert.AreEqual(1, modelo.UnidadesReservadas);
    }

    [TestMethod]
    public void LaUnidadLiberadaPuedeAsignarseNuevamente()
    {
        reserva.Resolver();
        Vehiculo unidadOriginal = reserva.VehiculoAsignado!;
        reserva.Cancelar();
        Reserva siguiente = ReservaRegistradaEn(modelo);

        siguiente.Resolver();

        Assert.AreSame(unidadOriginal, siguiente.VehiculoAsignado);
        Assert.IsFalse(unidadOriginal.EstaDisponible);
    }

    [TestMethod]
    [ExpectedException(typeof(TransicionDeReservaInvalidaException))]
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
