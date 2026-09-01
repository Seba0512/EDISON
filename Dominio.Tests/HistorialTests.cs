using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class HistorialTests
{
    private const string Usuario = "jferreiro";
    private const string NombreModelo = "Edison S1";

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private Historial historial = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        historial = new Historial(Usuario);
    }

    [TestMethod]
    public void UnHistorialNuevoEstaVacio()
    {
        Assert.AreEqual(0, historial.Actividades.Count);
    }

    [TestMethod]
    public void RegistrarAgregaLaActividadAlHistorial()
    {
        historial.Registrar(RegistroActividad.InicioSesion(Usuario, reloj));

        Assert.AreEqual(1, historial.Actividades.Count);
        Assert.AreEqual(TipoActividad.InicioSesion, historial.Actividades[0].Tipo);
    }

    [TestMethod]
    public void LasActividadesQuedanEnElOrdenEnQueSeRegistraron()
    {
        historial.Registrar(RegistroActividad.InicioSesion(Usuario, reloj));
        reloj.Avanzar(TimeSpan.FromMinutes(1));
        historial.Registrar(RegistroActividad.ModeloCreado(Usuario, NombreModelo, reloj));
        reloj.Avanzar(TimeSpan.FromMinutes(1));
        historial.Registrar(RegistroActividad.ReservaCreada(Usuario, NombreModelo, reloj));

        Assert.AreEqual(TipoActividad.InicioSesion, historial.Actividades[0].Tipo);
        Assert.AreEqual(TipoActividad.ModeloCreado, historial.Actividades[1].Tipo);
        Assert.AreEqual(TipoActividad.ReservaCreada, historial.Actividades[2].Tipo);
    }

    [TestMethod]
    public void AceptaActividadesDelUsuarioSinImportarMayusculas()
    {
        historial.Registrar(RegistroActividad.InicioSesion("JFerreiro", reloj));

        Assert.AreEqual(1, historial.Actividades.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void RegistrarUnaActividadDeOtroUsuarioFalla()
    {
        historial.Registrar(RegistroActividad.InicioSesion("otra", reloj));
    }

    [TestMethod]
    public void EsDeReconoceAlUsuarioIgnorandoMayusculas()
    {
        Assert.IsTrue(historial.EsDe("JFerreiro"));
        Assert.IsFalse(historial.EsDe("otra"));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUnHistorialSinUsuarioFalla()
    {
        _ = new Historial("  ");
    }
}
