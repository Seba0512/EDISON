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
    public void TomarMueveUnaUnidadDeDisponibleAReservada()
    {
        stock.TomarUna();

        Assert.AreEqual(CantidadInicial - 1, stock.Disponibles);
        Assert.AreEqual(1, stock.Reservadas);
        Assert.AreEqual(CantidadInicial, stock.Total);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void TomarSinUnidadesDisponiblesFalla()
    {
        for (int tomadas = 0; tomadas < CantidadInicial; tomadas++)
        {
            stock.TomarUna();
        }

        stock.TomarUna();
    }

    [TestMethod]
    public void LiberarDevuelveUnaUnidadAlPool()
    {
        stock.TomarUna();

        stock.LiberarUna();

        Assert.AreEqual(CantidadInicial, stock.Disponibles);
        Assert.AreEqual(0, stock.Reservadas);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void LiberarSinUnidadesReservadasFalla()
    {
        stock.LiberarUna();
    }

    [TestMethod]
    public void AjustarHaciaAbajoSoloQuitaUnidadesDisponibles()
    {
        stock.TomarUna();

        stock.AjustarA(1);

        Assert.AreEqual(1, stock.Total);
        Assert.AreEqual(1, stock.Reservadas);
        Assert.AreEqual(0, stock.Disponibles);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void AjustarPorDebajoDeLasReservadasFalla()
    {
        stock.TomarUna();
        stock.TomarUna();

        stock.AjustarA(1);
    }
}
