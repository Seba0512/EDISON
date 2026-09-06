using Dominio.Busqueda;

namespace Dominio.Tests;

/// <summary>
/// Los criterios se prueban contra un <see cref="VehiculoDePrueba"/> y no contra un
/// <see cref="Dominio.Modelo"/>: si estas pruebas compilan, es porque la busqueda de CR7 depende de
/// la interfaz y no de la clase concreta del catalogo.
/// </summary>
[TestClass]
public class CriteriosBusquedaTests
{
    private static readonly VehiculoDePrueba Vehiculo = new VehiculoDePrueba
    {
        Nombre = "Edison S1",
        Precio = 45000m,
        FechaEntregaPrometida = new DateTime(2027, 3, 1),
        UnidadesDisponibles = 2
    };

    [TestMethod]
    public void CriterioPorNombreRecibeElTextoBuscado()
    {
        Assert.IsTrue(new CriterioPorNombre("edison").Cumple(Vehiculo));
    }

    [TestMethod]
    public void CriterioPorPrecioMaximoRecibeUnDecimal()
    {
        Assert.IsTrue(new CriterioPorPrecioMaximo(50000m).Cumple(Vehiculo));
    }

    [TestMethod]
    public void CriterioPorPrecioMinimoRecibeUnDecimal()
    {
        Assert.IsTrue(new CriterioPorPrecioMinimo(40000m).Cumple(Vehiculo));
    }

    [TestMethod]
    public void CriterioPorUnidadesDisponiblesRecibeUnEntero()
    {
        Assert.IsTrue(new CriterioPorUnidadesDisponibles(1).Cumple(Vehiculo));
    }

    [TestMethod]
    public void CriterioPorFechaDeEntregaMaximaRecibeUnaFecha()
    {
        Assert.IsTrue(new CriterioPorFechaDeEntregaMaxima(new DateTime(2027, 4, 1)).Cumple(Vehiculo));
    }

    [TestMethod]
    public void CriterioPorPrecioMinimoRechazaUnValorNegativo()
    {
        Assert.ThrowsException<Dominio.Excepciones.DominioException>(
            () => new CriterioPorPrecioMinimo(-1m));
    }

    [TestMethod]
    public void CriterioPorUnidadesDisponiblesRechazaUnValorNegativo()
    {
        Assert.ThrowsException<Dominio.Excepciones.DominioException>(
            () => new CriterioPorUnidadesDisponibles(-1));
    }

    [TestMethod]
    public void ElCriterioCompuestoFiltraUnaColeccionQueNoContieneModelos()
    {
        CriterioCompuesto criterio = new CriterioCompuesto();
        criterio.Agregar(new CriterioPorPrecioMaximo(30000m));

        List<VehiculoDePrueba> encontrados = new List<VehiculoDePrueba> { Vehiculo }
            .Where(vehiculo => criterio.Cumple(vehiculo))
            .ToList();

        Assert.AreEqual(0, encontrados.Count);
    }
}
