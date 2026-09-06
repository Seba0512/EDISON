using Dominio;
using Dominio.Busqueda;
using Dominio.Fachada;
using Dominio.Tests.Dobles;

namespace Dominio.Tests;

/// <summary>
/// Recorrido completo por la fachada, con los tres colaboradores reales y repositorios en memoria.
/// Es la prueba de que el dominio queda armable y usable de punta a punta mediante contratos
/// expresados exclusivamente con objetos y tipos propios del modelo.
/// </summary>
[TestClass]
public class EdisonTests
{
    private const string NombreModelo = "Edison S1";
    private const int SegundosDeAprobacion = 15;

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private AlmacenDeImagenesFalso almacen = null!;
    private Edison edison = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        almacen = new AlmacenDeImagenesFalso();

        RepositorioDeUsuariosEnMemoria usuarios = new RepositorioDeUsuariosEnMemoria();
        RepositorioDeModelosEnMemoria modelos = new RepositorioDeModelosEnMemoria();
        RepositorioDeReservasEnMemoria reservas = new RepositorioDeReservasEnMemoria();

        edison = new Edison(
            new RegistroDeUsuarios(usuarios, reloj),
            new CatalogoDeModelos(modelos, almacen, reloj),
            new CentralDeReservas(reservas, modelos, usuarios, reloj, SegundosDeAprobacion));
    }

    private static FichaDeModelo Ficha(int stock)
    {
        DatosDeModelo datos = new DatosDeModelo(
            NombreModelo, "Sedan electrico", 45000m, new DateTime(2027, 3, 1), string.Empty);

        return new FichaDeModelo(datos, stock);
    }

    [TestMethod]
    public void UnUsuarioSeRegistraIniciaSesionYCierra()
    {
        Usuario registrado = edison.Registrar("jferreiro", "edison2026");
        Usuario logueado = edison.IniciarSesion("jferreiro", "edison2026");
        edison.CerrarSesion(logueado);

        Assert.AreEqual(registrado.NombreUsuario, logueado.NombreUsuario);
        Assert.AreEqual(2, edison.HistorialDe(logueado).Count);
    }

    /// <summary>
    /// El recorrido que va a correr el plan de testing de la defensa: alta, consulta, reserva,
    /// aprobacion diferida, cancelacion y baja, todo por la fachada.
    /// </summary>
    [TestMethod]
    public void RecorridoCompletoDeAltaReservaCancelacionYBaja()
    {
        Usuario vendedora = edison.Registrar("jferreiro", "edison2026");
        Usuario compradora = edison.Registrar("sgomez", "edison2026");

        edison.AltaDeModelo(Ficha(1), vendedora);
        Assert.AreEqual(1, edison.Consultar(NombreModelo).UnidadesDisponibles);

        Guid reserva = edison.SolicitarReserva(NombreModelo, compradora);
        reloj.Avanzar(TimeSpan.FromSeconds(SegundosDeAprobacion + 1));
        edison.ResolverVencidas();

        Assert.IsTrue(edison.ReservasDe(compradora)[0].EstaConfirmada);
        Assert.AreEqual(0, edison.Consultar(NombreModelo).UnidadesDisponibles);

        edison.CancelarReserva(reserva, compradora);
        Assert.AreEqual(1, edison.Consultar(NombreModelo).UnidadesDisponibles);

        edison.EliminarModelo(NombreModelo, vendedora);
        Assert.AreEqual(0, edison.Buscar(new CriterioQueAceptaTodo()).Count);
    }

    [TestMethod]
    public void BuscarFiltraElCatalogoConElCriterioRecibido()
    {
        Usuario vendedora = edison.Registrar("jferreiro", "edison2026");
        edison.AltaDeModelo(Ficha(2), vendedora);

        CriterioCompuesto criterio = new CriterioCompuesto();
        criterio.Agregar(new CriterioPorNombre("Edison"));
        criterio.Agregar(new CriterioPorUnidadesDisponibles(1));

        Assert.AreEqual(1, edison.Buscar(criterio).Count);
    }

    [TestMethod]
    public void ModificarElModeloSeVeEnLaConsulta()
    {
        Usuario vendedora = edison.Registrar("jferreiro", "edison2026");
        edison.AltaDeModelo(Ficha(2), vendedora);

        DatosDeModelo nuevos = new DatosDeModelo(
            NombreModelo, "Sedan electrico renovado", 39990m, new DateTime(2027, 6, 1), string.Empty);
        edison.ModificarModelo(new FichaDeModelo(nuevos, 5), vendedora);

        Modelo modelo = edison.Consultar(NombreModelo);
        Assert.AreEqual(39990m, modelo.Precio);
        Assert.AreEqual(5, modelo.StockTotal);
    }
}
