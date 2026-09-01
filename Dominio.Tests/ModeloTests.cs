using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class ModeloTests
{
    private const string NombreModelo = "Edison S1";
    private const string Creador = "jferreiro";
    private const int StockInicial = 3;

    private Modelo modelo = null!;

    [TestInitialize]
    public void Inicializar()
    {
        modelo = new Modelo(FichaCon(NombreModelo, 45000m), Creador);
        modelo.AgregarUnidades(StockInicial);
    }

    private static FichaModelo FichaCon(string nombre, decimal precio)
    {
        return new FichaModelo(nombre, "Sedan electrico", precio, new DateTime(2027, 3, 1), string.Empty);
    }

    [TestMethod]
    public void ElStockInicialCreaEsaCantidadDeUnidadesDisponibles()
    {
        Assert.AreEqual(StockInicial, modelo.StockTotal);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
    }

    [TestMethod]
    public void AsignarUnidadMueveUnaUnidadDeDisponibleAReservada()
    {
        Guid reserva = Guid.NewGuid();

        modelo.AsignarUnidadA(reserva);

        Assert.AreEqual(StockInicial - 1, modelo.UnidadesDisponibles);
        Assert.AreEqual(1, modelo.UnidadesReservadas);
        Assert.AreEqual(StockInicial, modelo.StockTotal);
        Assert.IsTrue(modelo.TieneUnidadAsignadaA(reserva));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void AsignarUnidadSinStockLibreFalla()
    {
        for (int asignadas = 0; asignadas < StockInicial; asignadas++)
        {
            modelo.AsignarUnidadA(Guid.NewGuid());
        }

        modelo.AsignarUnidadA(Guid.NewGuid());
    }

    [TestMethod]
    public void LiberarUnidadDevuelveLaUnidadAlStockDisponible()
    {
        Guid reserva = Guid.NewGuid();
        modelo.AsignarUnidadA(reserva);

        modelo.LiberarUnidadDe(reserva);

        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
        Assert.IsFalse(modelo.TieneUnidadAsignadaA(reserva));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void LiberarUnaReservaSinUnidadAsignadaFalla()
    {
        modelo.LiberarUnidadDe(Guid.NewGuid());
    }

    [TestMethod]
    public void AjustarStockHaciaArribaAgregaUnidadesDisponibles()
    {
        modelo.AjustarStock(StockInicial + 2);

        Assert.AreEqual(StockInicial + 2, modelo.StockTotal);
        Assert.AreEqual(StockInicial + 2, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    public void AjustarStockHaciaAbajoSoloQuitaUnidadesDisponibles()
    {
        Guid reserva = Guid.NewGuid();
        modelo.AsignarUnidadA(reserva);

        modelo.AjustarStock(1);

        Assert.AreEqual(1, modelo.StockTotal);
        Assert.AreEqual(1, modelo.UnidadesReservadas);
        Assert.AreEqual(0, modelo.UnidadesDisponibles);
        Assert.IsTrue(modelo.TieneUnidadAsignadaA(reserva));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void AjustarStockPorDebajoDeLasReservadasFalla()
    {
        modelo.AsignarUnidadA(Guid.NewGuid());
        modelo.AsignarUnidadA(Guid.NewGuid());

        modelo.AjustarStock(1);
    }

    [TestMethod]
    public void ActualizarFichaCambiaLosDatosDescriptivos()
    {
        modelo.ActualizarFicha(FichaCon(NombreModelo, 39990m));

        Assert.AreEqual(39990m, modelo.Precio);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void ActualizarFichaSinUnidadesDisponiblesFalla()
    {
        for (int asignadas = 0; asignadas < StockInicial; asignadas++)
        {
            modelo.AsignarUnidadA(Guid.NewGuid());
        }

        modelo.ActualizarFicha(FichaCon(NombreModelo, 39990m));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void ActualizarFichaConOtroNombreFalla()
    {
        modelo.ActualizarFicha(FichaCon("Edison S2", 39990m));
    }

    [TestMethod]
    public void NoTieneUnidadesReservadasEsFalsoLuegoDeAsignar()
    {
        Assert.IsTrue(modelo.NoTieneUnidadesReservadas);

        modelo.AsignarUnidadA(Guid.NewGuid());

        Assert.IsFalse(modelo.NoTieneUnidadesReservadas);
    }

    [TestMethod]
    public void FueCreadoPorReconoceAlCreadorIgnorandoMayusculas()
    {
        Assert.IsTrue(modelo.FueCreadoPor("JFerreiro"));
        Assert.IsFalse(modelo.FueCreadoPor("otro"));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearModeloSinCreadorFalla()
    {
        _ = new Modelo(FichaCon(NombreModelo, 45000m), "  ");
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearFichaConPrecioNoPositivoFalla()
    {
        _ = FichaCon(NombreModelo, 0m);
    }

    [TestMethod]
    public void LaFichaSinImagenQuedaConCadenaVaciaYNoTieneImagen()
    {
        Assert.AreEqual(string.Empty, modelo.NombreArchivoImagen);
        Assert.IsFalse(modelo.TieneImagen);
    }
}
