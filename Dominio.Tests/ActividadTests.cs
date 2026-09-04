using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class ActividadTests
{
    private const string Usuario = "jferreiro";
    private const string NombreModelo = "Edison S1";

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
    }

    [TestMethod]
    public void InicioSesionRegistraElTipoYElUsuario()
    {
        Actividad actividad = Actividad.InicioSesion(Usuario, reloj);

        Assert.AreEqual(TipoActividad.InicioSesion, actividad.Tipo);
        Assert.AreEqual(Usuario, actividad.NombreUsuario);
    }

    [TestMethod]
    public void ModeloCreadoIncluyeElNombreDelModeloEnElDetalle()
    {
        Actividad actividad = Actividad.ModeloCreado(Usuario, NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.ModeloCreado, actividad.Tipo);
        StringAssert.Contains(actividad.Detalle, NombreModelo);
    }

    [TestMethod]
    public void CompraQuedaRegistradaComoTalCuandoSeConfirmaLaReserva()
    {
        Actividad actividad = Actividad.Compra(Usuario, NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.Compra, actividad.Tipo);
        StringAssert.Contains(actividad.Detalle, NombreModelo);
    }

    [TestMethod]
    public void CadaActividadTieneIdentidadPropia()
    {
        Actividad primera = Actividad.InicioSesion(Usuario, reloj);
        Actividad segunda = Actividad.InicioSesion(Usuario, reloj);

        Assert.AreNotEqual(primera.Id, segunda.Id);
    }

    [TestMethod]
    public void CadaActividadSeSellaConLaHoraDelReloj()
    {
        Actividad primera = Actividad.InicioSesion(Usuario, reloj);
        reloj.Avanzar(TimeSpan.FromMinutes(5));
        Actividad segunda = Actividad.CierreSesion(Usuario, reloj);

        Assert.AreEqual(Momento, primera.FechaHora);
        Assert.IsTrue(segunda.FechaHora > primera.FechaHora);
    }

    [TestMethod]
    public void EsDeReconoceAlUsuarioIgnorandoMayusculas()
    {
        Actividad actividad = Actividad.ReservaCreada(Usuario, NombreModelo, reloj);

        Assert.IsTrue(actividad.EsDe("JFerreiro"));
        Assert.IsFalse(actividad.EsDe("otro"));
    }

    [TestMethod]
    public void LasActividadesDeReservaYCancelacionSeDistinguen()
    {
        Actividad creada = Actividad.ReservaCreada(Usuario, NombreModelo, reloj);
        Actividad cancelada = Actividad.ReservaCancelada(Usuario, NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.ReservaCreada, creada.Tipo);
        Assert.AreEqual(TipoActividad.ReservaCancelada, cancelada.Tipo);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUnaActividadSinUsuarioFalla()
    {
        _ = Actividad.InicioSesion("  ", reloj);
    }
}
