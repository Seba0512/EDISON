using Dominio;
using Dominio.Busqueda;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class BusquedaVehiculosTests
{
    private List<Modelo> catalogo = null!;
    private BusquedaVehiculos busqueda = null!;

    [TestInitialize]
    public void Inicializar()
    {
        busqueda = new BusquedaVehiculos();
        catalogo = new List<Modelo>
        {
            ModeloCon("Edison S1", 45000m, new DateTime(2027, 3, 1), 2),
            ModeloCon("Edison X2", 72000m, new DateTime(2027, 9, 1), 5),
            ModeloCon("Volta Uno", 28000m, new DateTime(2026, 12, 1), 0)
        };
    }

    private static readonly Usuario Duenia = new Usuario("jferreiro", "edison2026");

    private static Modelo ModeloCon(string nombre, decimal precio, DateTime entrega, int stock)
    {
        FichaModelo ficha = new FichaModelo(nombre, "Vehiculo electrico", precio, entrega, string.Empty);
        Modelo modelo = new Modelo(ficha, Duenia);
        if (stock > 0)
        {
            modelo.AgregarUnidades(stock);
        }

        return modelo;
    }

    private List<Modelo> Resultado()
    {
        return busqueda.Filtrar(catalogo).ToList();
    }

    [TestMethod]
    public void SinCriteriosDevuelveTodoElCatalogo()
    {
        Assert.AreEqual(catalogo.Count, Resultado().Count);
    }

    [TestMethod]
    public void FiltraPorNombreParcialIgnorandoMayusculas()
    {
        busqueda.AgregarCriterio(new CriterioNombreContiene("edison"));

        Assert.AreEqual(2, Resultado().Count);
    }

    [TestMethod]
    public void FiltraPorPrecioMaximo()
    {
        busqueda.AgregarCriterio(new CriterioPrecioMaximo(50000m));

        List<Modelo> encontrados = Resultado();

        Assert.AreEqual(2, encontrados.Count);
        Assert.IsFalse(encontrados.Exists(modelo => modelo.SeLlama("Edison X2")));
    }

    [TestMethod]
    public void FiltraPorPrecioMinimo()
    {
        busqueda.AgregarCriterio(new CriterioPrecioMinimo(50000m));

        List<Modelo> encontrados = Resultado();

        Assert.AreEqual(1, encontrados.Count);
        Assert.IsTrue(encontrados[0].SeLlama("Edison X2"));
    }

    [TestMethod]
    public void FiltraPorUnidadesDisponiblesDejandoAfueraLosSinStock()
    {
        busqueda.AgregarCriterio(new CriterioUnidadesDisponibles(1));

        List<Modelo> encontrados = Resultado();

        Assert.AreEqual(2, encontrados.Count);
        Assert.IsFalse(encontrados.Exists(modelo => modelo.SeLlama("Volta Uno")));
    }

    [TestMethod]
    public void FiltraPorFechaDeEntregaLimite()
    {
        busqueda.AgregarCriterio(new CriterioFechaEntregaHasta(new DateTime(2027, 4, 1)));

        Assert.AreEqual(2, Resultado().Count);
    }

    [TestMethod]
    public void LosCriteriosSeCombinanConY()
    {
        busqueda.AgregarCriterio(new CriterioNombreContiene("edison"));
        busqueda.AgregarCriterio(new CriterioPrecioMaximo(50000m));
        busqueda.AgregarCriterio(new CriterioUnidadesDisponibles(1));

        List<Modelo> encontrados = Resultado();

        Assert.AreEqual(3, busqueda.CantidadDeCriterios);
        Assert.AreEqual(1, encontrados.Count);
        Assert.IsTrue(encontrados[0].SeLlama("Edison S1"));
    }

    [TestMethod]
    public void UnaCombinacionSinCoincidenciasDevuelveVacioYNoNull()
    {
        busqueda.AgregarCriterio(new CriterioPrecioMinimo(100000m));

        List<Modelo> encontrados = Resultado();

        Assert.IsNotNull(encontrados);
        Assert.AreEqual(0, encontrados.Count);
    }

    [TestMethod]
    public void LasUnidadesReservadasDejanDeContarComoDisponibles()
    {
        RelojFalso reloj = new RelojFalso(new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc));
        Modelo edisonS1 = catalogo[0];
        for (int confirmadas = 0; confirmadas < 2; confirmadas++)
        {
            Reserva reserva = new Reserva(Duenia, edisonS1, reloj);
            edisonS1.RegistrarReserva(reserva);
            reserva.Resolver();
        }

        busqueda.AgregarCriterio(new CriterioUnidadesDisponibles(1));

        Assert.IsFalse(Resultado().Exists(modelo => modelo.SeLlama("Edison S1")));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUnCriterioDeNombreVacioFalla()
    {
        _ = new CriterioNombreContiene("   ");
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUnCriterioDePrecioMaximoNoPositivoFalla()
    {
        _ = new CriterioPrecioMaximo(0m);
    }
}
