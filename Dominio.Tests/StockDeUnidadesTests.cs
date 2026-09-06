using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

/// <summary>
/// El stock se prueba solo, sin construir una <see cref="DatosDeModelo"/> ni un <see cref="Modelo"/>:
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
        Vehiculo unidad = stock.TomarUna();

        Assert.IsFalse(unidad.EstaDisponible);
        Assert.AreEqual(CantidadInicial - 1, stock.Disponibles);
        Assert.AreEqual(1, stock.Reservadas);
        Assert.AreEqual(CantidadInicial, stock.Total);
    }

    [TestMethod]
    [ExpectedException(typeof(SinUnidadesDisponiblesException))]
    public void TomarSinUnidadesDisponiblesFalla()
    {
        for (int tomadas = 0; tomadas < CantidadInicial; tomadas++)
        {
            stock.TomarUna();
        }

        stock.TomarUna();
    }

    /// <summary>
    /// Las excepciones especificas siguen siendo <see cref="DominioException"/>: es lo que permite
    /// que el borde del servidor las atrape todas con un solo catch y siga vivo.
    /// </summary>
    [TestMethod]
    public void SinUnidadesDisponiblesEsUnErrorDeNegocio()
    {
        StockDeUnidades vacio = new StockDeUnidades(NombreModelo);

        Assert.ThrowsException<SinUnidadesDisponiblesException>(() => vacio.TomarUna());
        Assert.IsInstanceOfType<DominioException>(new SinUnidadesDisponiblesException(NombreModelo));
    }

    [TestMethod]
    public void LiberarDevuelveUnaUnidadAlPool()
    {
        Vehiculo unidad = stock.TomarUna();

        stock.Liberar(unidad);

        Assert.IsTrue(unidad.EstaDisponible);
        Assert.AreEqual(CantidadInicial, stock.Disponibles);
        Assert.AreEqual(0, stock.Reservadas);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void LiberarSinUnidadesReservadasFalla()
    {
        Vehiculo unidadDisponible = new Vehiculo(NombreModelo);

        stock.Liberar(unidadDisponible);
    }

    [TestMethod]
    public void LiberarUnaUnidadNoLiberaOtraReservada()
    {
        Vehiculo primera = stock.TomarUna();
        Vehiculo segunda = stock.TomarUna();

        stock.Liberar(primera);

        Assert.IsTrue(primera.EstaDisponible);
        Assert.IsFalse(segunda.EstaDisponible);
        Assert.AreEqual(1, stock.Reservadas);
    }

    [TestMethod]
    public void LiberarUnaUnidadDeOtroStockFallaSinModificarla()
    {
        StockDeUnidades otroStock = new StockDeUnidades("Edison X2");
        otroStock.Agregar(1);
        Vehiculo unidadAjena = otroStock.TomarUna();

        Assert.ThrowsException<DominioException>(() => stock.Liberar(unidadAjena));

        Assert.IsFalse(unidadAjena.EstaDisponible);
        Assert.AreEqual(0, stock.Reservadas);
        Assert.AreEqual(1, otroStock.Reservadas);
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
