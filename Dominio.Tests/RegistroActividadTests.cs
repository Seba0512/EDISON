using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class RegistroActividadTests
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
        RegistroActividad actividad = RegistroActividad.InicioSesion(Usuario, reloj);

        Assert.AreEqual(TipoActividad.InicioSesion, actividad.Tipo);
        Assert.AreEqual(Usuario, actividad.NombreUsuario);
    }

    [TestMethod]
    public void ModeloCreadoIncluyeElNombreDelModeloEnElDetalle()
    {
        RegistroActividad actividad = RegistroActividad.ModeloCreado(Usuario, NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.ModeloCreado, actividad.Tipo);
        StringAssert.Contains(actividad.Detalle, NombreModelo);
    }

    [TestMethod]
    public void CompraQuedaRegistradaComoTalCuandoSeConfirmaLaReserva()
    {
        RegistroActividad actividad = RegistroActividad.Compra(Usuario, NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.Compra, actividad.Tipo);
        StringAssert.Contains(actividad.Detalle, NombreModelo);
    }

    [TestMethod]
    public void CadaActividadTieneIdentidadPropia()
    {
        RegistroActividad primera = RegistroActividad.InicioSesion(Usuario, reloj);
        RegistroActividad segunda = RegistroActividad.InicioSesion(Usuario, reloj);

        Assert.AreNotEqual(primera.Id, segunda.Id);
    }

    [TestMethod]
    public void CadaActividadSeSellaConLaHoraDelReloj()
    {
        RegistroActividad primera = RegistroActividad.InicioSesion(Usuario, reloj);
        reloj.Avanzar(TimeSpan.FromMinutes(5));
        RegistroActividad segunda = RegistroActividad.CierreSesion(Usuario, reloj);

        Assert.AreEqual(Momento, primera.FechaHora);
        Assert.IsTrue(segunda.FechaHora > primera.FechaHora);
    }

    [TestMethod]
    public void EsDeReconoceAlUsuarioIgnorandoMayusculas()
    {
        RegistroActividad actividad = RegistroActividad.ReservaCreada(Usuario, NombreModelo, reloj);

        Assert.IsTrue(actividad.EsDe("JFerreiro"));
        Assert.IsFalse(actividad.EsDe("otro"));
    }

    [TestMethod]
    public void LasActividadesDeReservaYCancelacionSeDistinguen()
    {
        RegistroActividad creada = RegistroActividad.ReservaCreada(Usuario, NombreModelo, reloj);
        RegistroActividad cancelada = RegistroActividad.ReservaCancelada(Usuario, NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.ReservaCreada, creada.Tipo);
        Assert.AreEqual(TipoActividad.ReservaCancelada, cancelada.Tipo);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUnaActividadSinUsuarioFalla()
    {
        _ = RegistroActividad.InicioSesion("  ", reloj);
    }
}
