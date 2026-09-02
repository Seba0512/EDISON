using Dominio.Busqueda;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class FabricaDeCriteriosTests
{
    private static readonly VehiculoDePrueba Vehiculo = new VehiculoDePrueba
    {
        Nombre = "Edison S1",
        Precio = 45000m,
        FechaEntregaPrometida = new DateTime(2027, 3, 1),
        UnidadesDisponibles = 2
    };

    private FabricaDeCriterios fabrica = null!;

    [TestInitialize]
    public void Inicializar()
    {
        fabrica = new FabricaDeCriterios();
    }

    [TestMethod]
    public void CreaCadaCriterioAPartirDeSuCodigoYSuValorEnTexto()
    {
        Assert.IsTrue(fabrica.Crear(FabricaDeCriterios.PorNombre, "edison").Cumple(Vehiculo));
        Assert.IsTrue(fabrica.Crear(FabricaDeCriterios.PorPrecioMinimo, "40000").Cumple(Vehiculo));
        Assert.IsTrue(fabrica.Crear(FabricaDeCriterios.PorPrecioMaximo, "50000.50").Cumple(Vehiculo));
        Assert.IsTrue(fabrica.Crear(FabricaDeCriterios.PorUnidadesDisponibles, "1").Cumple(Vehiculo));
        Assert.IsTrue(fabrica.Crear(FabricaDeCriterios.PorFechaDeEntregaMaxima, "20270401").Cumple(Vehiculo));
    }

    [TestMethod]
    public void ElCodigoSeReconoceIgnorandoMayusculasYEspacios()
    {
        Assert.IsTrue(fabrica.Crear("  nombre ", "edison").Cumple(Vehiculo));
    }

    [TestMethod]
    public void ElPrecioSeInterpretaConCulturaInvariante()
    {
        ICriterioDeBusqueda criterio = fabrica.Crear(FabricaDeCriterios.PorPrecioMaximo, "45000.00");

        Assert.IsTrue(criterio.Cumple(Vehiculo));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void UnCodigoDesconocidoFalla()
    {
        _ = fabrica.Crear("COLOR", "rojo");
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void UnValorQueNoEsNumeroFalla()
    {
        _ = fabrica.Crear(FabricaDeCriterios.PorPrecioMaximo, "caro");
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void UnaFechaConOtroFormatoFalla()
    {
        _ = fabrica.Crear(FabricaDeCriterios.PorFechaDeEntregaMaxima, "01/04/2027");
    }
}
