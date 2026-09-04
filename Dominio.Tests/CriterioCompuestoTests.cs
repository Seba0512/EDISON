using Dominio;
using Dominio.Busqueda;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class CriterioCompuestoTests
{
    private static readonly Usuario Duenia = new Usuario("jferreiro", "edison2026");

    private List<Modelo> catalogo = null!;
    private CriterioCompuesto criterio = null!;

    [TestInitialize]
    public void Inicializar()
    {
        criterio = new CriterioCompuesto();
        catalogo = new List<Modelo>
        {
            ModeloCon("Edison S1", 45000m, new DateTime(2027, 3, 1), 2),
            ModeloCon("Edison X2", 72000m, new DateTime(2027, 9, 1), 5),
            ModeloCon("Volta Uno", 28000m, new DateTime(2026, 12, 1), 0)
        };
    }

    private static Modelo ModeloCon(string nombre, decimal precio, DateTime entrega, int stock)
    {
        DatosDeModelo datos = new DatosDeModelo(nombre, "Vehiculo electrico", precio, entrega, string.Empty);
        Modelo modelo = new Modelo(datos, Duenia);
        if (stock > 0)
        {
            modelo.AgregarUnidades(stock);
        }

        return modelo;
    }

    private List<Modelo> Resultado()
    {
        return catalogo.Where(modelo => criterio.Cumple(modelo)).ToList();
    }

    [TestMethod]
    public void SinCriteriosAceptaTodoElCatalogo()
    {
        Assert.AreEqual(catalogo.Count, Resultado().Count);
    }

    [TestMethod]
    public void FiltraPorNombreParcialIgnorandoMayusculas()
    {
        criterio.Agregar(new CriterioPorNombre("edison"));

        Assert.AreEqual(2, Resultado().Count);
    }

    [TestMethod]
    public void FiltraPorPrecioMaximo()
    {
        criterio.Agregar(new CriterioPorPrecioMaximo(50000m));

        List<Modelo> encontrados = Resultado();

        Assert.AreEqual(2, encontrados.Count);
        Assert.IsFalse(encontrados.Exists(modelo => modelo.SeLlama("Edison X2")));
    }

    [TestMethod]
    public void FiltraPorPrecioMinimo()
    {
        criterio.Agregar(new CriterioPorPrecioMinimo(50000m));

        List<Modelo> encontrados = Resultado();

        Assert.AreEqual(1, encontrados.Count);
        Assert.IsTrue(encontrados[0].SeLlama("Edison X2"));
    }

    [TestMethod]
    public void FiltraPorUnidadesDisponiblesDejandoAfueraLosSinStock()
    {
        criterio.Agregar(new CriterioPorUnidadesDisponibles(1));

        List<Modelo> encontrados = Resultado();

        Assert.AreEqual(2, encontrados.Count);
        Assert.IsFalse(encontrados.Exists(modelo => modelo.SeLlama("Volta Uno")));
    }

    [TestMethod]
    public void FiltraPorFechaDeEntregaLimite()
    {
        criterio.Agregar(new CriterioPorFechaDeEntregaMaxima(new DateTime(2027, 4, 1)));

        Assert.AreEqual(2, Resultado().Count);
    }

    [TestMethod]
    public void LosCriteriosSeCombinanConY()
    {
        criterio.Agregar(new CriterioPorNombre("edison"));
        criterio.Agregar(new CriterioPorPrecioMaximo(50000m));
        criterio.Agregar(new CriterioPorUnidadesDisponibles(1));

        List<Modelo> encontrados = Resultado();

        Assert.AreEqual(3, criterio.CantidadDeCriterios);
        Assert.AreEqual(1, encontrados.Count);
        Assert.IsTrue(encontrados[0].SeLlama("Edison S1"));
    }

    [TestMethod]
    public void UnaCombinacionSinCoincidenciasDevuelveVacioYNoNull()
    {
        criterio.Agregar(new CriterioPorPrecioMinimo(100000m));

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

        criterio.Agregar(new CriterioPorUnidadesDisponibles(1));

        Assert.IsFalse(Resultado().Exists(modelo => modelo.SeLlama("Edison S1")));
    }

    [TestMethod]
    public void ElCriterioQueAceptaTodoAceptaCualquierVehiculo()
    {
        CriterioQueAceptaTodo aceptaTodo = new CriterioQueAceptaTodo();

        Assert.IsTrue(catalogo.TrueForAll(modelo => aceptaTodo.Cumple(modelo)));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUnCriterioDeNombreVacioFalla()
    {
        _ = new CriterioPorNombre("   ");
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUnCriterioDePrecioMaximoNoPositivoFalla()
    {
        _ = new CriterioPorPrecioMaximo(0m);
    }
}
