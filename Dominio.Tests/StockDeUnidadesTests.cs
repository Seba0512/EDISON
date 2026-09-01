using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

/// <summary>
/// El stock se prueba solo, sin construir una <see cref="FichaModelo"/> ni un <see cref="Modelo"/>:
/// esa independencia es la ganancia de haberlo separado del catalogo (SRP).
/// </summary>
[TestClass]
public class StockDeUnidadesTests
{
    private const string NombreModelo = "Edison S1";
    private const int CantidadInicial = 3;

    private StockDeUnidades stock = null!;

    [TestInitialize]
    public void Inicializar()
    {
        stock = new StockDeUnidades(NombreModelo);
        stock.Agregar(CantidadInicial);
    }

    [TestMethod]
    public void ElStockNuevoNaceVacio()
    {
        StockDeUnidades vacio = new StockDeUnidades(NombreModelo);

        Assert.AreEqual(0, vacio.Total);
        Assert.IsFalse(vacio.HayAlgunaDisponible);
        Assert.IsTrue(vacio.NoHayNingunaReservada);
    }

    [TestMethod]
    public void AgregarCreaUnidadesDisponibles()
    {
        Assert.AreEqual(CantidadInicial, stock.Total);
        Assert.AreEqual(CantidadInicial, stock.Disponibles);
        Assert.AreEqual(0, stock.Reservadas);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void AgregarUnaCantidadNoPositivaFalla()
    {
        stock.Agregar(0);
    }

    [TestMethod]
    public void AsignarMueveUnaUnidadDeDisponibleAReservada()
    {
        Guid reserva = Guid.NewGuid();

        stock.AsignarUnaA(reserva);

        Assert.AreEqual(CantidadInicial - 1, stock.Disponibles);
        Assert.AreEqual(1, stock.Reservadas);
        Assert.IsTrue(stock.TieneUnaAsignadaA(reserva));
    }

    [TestMethod]
    public void LiberarDevuelveLaUnidadAlStockDisponible()
    {
        Guid reserva = Guid.NewGuid();
        stock.AsignarUnaA(reserva);

        stock.LiberarLaDe(reserva);

        Assert.AreEqual(CantidadInicial, stock.Disponibles);
        Assert.IsFalse(stock.TieneUnaAsignadaA(reserva));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void LiberarUnaReservaSinUnidadAsignadaFalla()
    {
        stock.LiberarLaDe(Guid.NewGuid());
    }

    [TestMethod]
    public void AjustarHaciaAbajoSoloQuitaUnidadesDisponibles()
    {
        Guid reserva = Guid.NewGuid();
        stock.AsignarUnaA(reserva);

        stock.AjustarA(1);

        Assert.AreEqual(1, stock.Total);
        Assert.AreEqual(1, stock.Reservadas);
        Assert.IsTrue(stock.TieneUnaAsignadaA(reserva));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void AjustarPorDebajoDeLasReservadasFalla()
    {
        stock.AsignarUnaA(Guid.NewGuid());
        stock.AsignarUnaA(Guid.NewGuid());

        stock.AjustarA(1);
    }
}
