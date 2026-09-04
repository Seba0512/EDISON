using Dominio;

namespace Dominio.Tests;

[TestClass]
public class ActividadTests
{
    private const string NombreModelo = "Edison S1";

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
    }

    [TestMethod]
    public void InicioSesionRegistraElTipo()
    {
        Actividad actividad = Actividad.InicioSesion(reloj);

        Assert.AreEqual(TipoActividad.InicioSesion, actividad.Tipo);
    }

    [TestMethod]
    public void ModeloCreadoIncluyeElNombreDelModeloEnElDetalle()
    {
        Actividad actividad = Actividad.ModeloCreado(NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.ModeloCreado, actividad.Tipo);
        StringAssert.Contains(actividad.Detalle, NombreModelo);
    }

    [TestMethod]
    public void CompraQuedaRegistradaComoTalCuandoSeConfirmaLaReserva()
    {
        Actividad actividad = Actividad.Compra(NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.Compra, actividad.Tipo);
        StringAssert.Contains(actividad.Detalle, NombreModelo);
    }

    [TestMethod]
    public void CadaActividadSeSellaConLaHoraDelReloj()
    {
        Actividad primera = Actividad.InicioSesion(reloj);
        reloj.Avanzar(TimeSpan.FromMinutes(5));
        Actividad segunda = Actividad.CierreSesion(reloj);

        Assert.AreEqual(Momento, primera.FechaHora);
        Assert.IsTrue(segunda.FechaHora > primera.FechaHora);
    }

    [TestMethod]
    public void LasActividadesDeReservaYCancelacionSeDistinguen()
    {
        Actividad creada = Actividad.ReservaCreada(NombreModelo, reloj);
        Actividad cancelada = Actividad.ReservaCancelada(NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.ReservaCreada, creada.Tipo);
        Assert.AreEqual(TipoActividad.ReservaCancelada, cancelada.Tipo);
    }

    [TestMethod]
    public void LasActividadesDeModeloSeDistinguenEntreSi()
    {
        Actividad modificado = Actividad.ModeloModificado(NombreModelo, reloj);
        Actividad eliminado = Actividad.ModeloEliminado(NombreModelo, reloj);

        Assert.AreEqual(TipoActividad.ModeloModificado, modificado.Tipo);
        Assert.AreEqual(TipoActividad.ModeloEliminado, eliminado.Tipo);
        StringAssert.Contains(eliminado.Detalle, NombreModelo);
    }
}
