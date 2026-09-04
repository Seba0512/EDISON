using Dominio;

namespace Dominio.Tests;

[TestClass]
public class HistorialTests
{
    private const string NombreModelo = "Edison S1";

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private Historial historial = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        historial = new Historial();
    }

    [TestMethod]
    public void UnHistorialNuevoEstaVacio()
    {
        Assert.AreEqual(0, historial.Actividades.Count);
    }

    [TestMethod]
    public void RegistrarAgregaLaActividadAlHistorial()
    {
        historial.Registrar(Actividad.InicioSesion(reloj));

        Assert.AreEqual(1, historial.Actividades.Count);
        Assert.AreEqual(TipoActividad.InicioSesion, historial.Actividades[0].Tipo);
    }

    [TestMethod]
    public void LasActividadesQuedanEnElOrdenEnQueSeRegistraron()
    {
        historial.Registrar(Actividad.InicioSesion(reloj));
        reloj.Avanzar(TimeSpan.FromMinutes(1));
        historial.Registrar(Actividad.ModeloCreado(NombreModelo, reloj));
        reloj.Avanzar(TimeSpan.FromMinutes(1));
        historial.Registrar(Actividad.ReservaCreada(NombreModelo, reloj));

        Assert.AreEqual(TipoActividad.InicioSesion, historial.Actividades[0].Tipo);
        Assert.AreEqual(TipoActividad.ModeloCreado, historial.Actividades[1].Tipo);
        Assert.AreEqual(TipoActividad.ReservaCreada, historial.Actividades[2].Tipo);
    }

    [TestMethod]
    public void ElHistorialConservaLaHoraDeCadaActividad()
    {
        historial.Registrar(Actividad.InicioSesion(reloj));
        reloj.Avanzar(TimeSpan.FromMinutes(30));
        historial.Registrar(Actividad.CierreSesion(reloj));

        Assert.AreEqual(Momento, historial.Actividades[0].FechaHora);
        Assert.IsTrue(historial.Actividades[1].FechaHora > historial.Actividades[0].FechaHora);
    }
}
