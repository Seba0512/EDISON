using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class RegistroActividadTests
{
    private const string Usuario = "jferreiro";
    private const string NombreModelo = "Edison S1";

    [TestMethod]
    public void InicioSesionRegistraElTipoYElUsuario()
    {
        RegistroActividad actividad = RegistroActividad.InicioSesion(Usuario);

        Assert.AreEqual(TipoActividad.InicioSesion, actividad.Tipo);
        Assert.AreEqual(Usuario, actividad.NombreUsuario);
    }

    [TestMethod]
    public void ModeloCreadoIncluyeElNombreDelModeloEnElDetalle()
    {
        RegistroActividad actividad = RegistroActividad.ModeloCreado(Usuario, NombreModelo);

        Assert.AreEqual(TipoActividad.ModeloCreado, actividad.Tipo);
        StringAssert.Contains(actividad.Detalle, NombreModelo);
    }

    [TestMethod]
    public void CompraQuedaRegistradaComoTalCuandoSeConfirmaLaReserva()
    {
        RegistroActividad actividad = RegistroActividad.Compra(Usuario, NombreModelo);

        Assert.AreEqual(TipoActividad.Compra, actividad.Tipo);
        StringAssert.Contains(actividad.Detalle, NombreModelo);
    }

    [TestMethod]
    public void CadaActividadTieneIdentidadPropia()
    {
        RegistroActividad primera = RegistroActividad.InicioSesion(Usuario);
        RegistroActividad segunda = RegistroActividad.InicioSesion(Usuario);

        Assert.AreNotEqual(primera.Id, segunda.Id);
    }

    [TestMethod]
    public void EsDeReconoceAlUsuarioIgnorandoMayusculas()
    {
        RegistroActividad actividad = RegistroActividad.ReservaCreada(Usuario, NombreModelo);

        Assert.IsTrue(actividad.EsDe("JFerreiro"));
        Assert.IsFalse(actividad.EsDe("otro"));
    }

    [TestMethod]
    public void LasActividadesDeReservaYCancelacionSeDistinguen()
    {
        RegistroActividad creada = RegistroActividad.ReservaCreada(Usuario, NombreModelo);
        RegistroActividad cancelada = RegistroActividad.ReservaCancelada(Usuario, NombreModelo);

        Assert.AreEqual(TipoActividad.ReservaCreada, creada.Tipo);
        Assert.AreEqual(TipoActividad.ReservaCancelada, cancelada.Tipo);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUnaActividadSinUsuarioFalla()
    {
        _ = RegistroActividad.InicioSesion("  ");
    }
}
