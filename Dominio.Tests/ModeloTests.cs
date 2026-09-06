using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class ModeloTests
{
    private const string NombreModelo = "Edison S1";
    private const int StockInicial = 3;

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private Usuario creadora = null!;
    private Modelo modelo = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        creadora = new Usuario("jferreiro", "edison2026");
        modelo = new Modelo(DatosCon(NombreModelo, 45000m), creadora);
        modelo.AgregarUnidades(StockInicial);
    }

    private static DatosDeModelo DatosCon(string nombre, decimal precio)
    {
        return new DatosDeModelo(nombre, "Sedan electrico", precio, new DateTime(2027, 3, 1), string.Empty);
    }

    private static DatosDeModelo DatosModificados()
    {
        return new DatosDeModelo(
            NombreModelo, "Sedan electrico renovado", 39990m, new DateTime(2027, 6, 1), string.Empty);
    }

    private Reserva ReservaRegistrada()
    {
        return modelo.RegistrarReserva(creadora, reloj);
    }

    private Reserva ReservaConfirmada()
    {
        Reserva reserva = ReservaRegistrada();
        reserva.Resolver();
        return reserva;
    }

    [TestMethod]
    public void ElStockInicialCreaEsaCantidadDeUnidadesDisponibles()
    {
        Assert.AreEqual(StockInicial, modelo.StockTotal);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
    }

    [TestMethod]
    public void ResolverConStockConfirmaYReservaUnaUnidad()
    {
        Reserva reserva = ReservaConfirmada();

        Assert.IsTrue(reserva.EstaConfirmada);
        Assert.AreEqual(StockInicial - 1, modelo.UnidadesDisponibles);
        Assert.AreEqual(1, modelo.UnidadesReservadas);
        Assert.AreEqual(StockInicial, modelo.StockTotal);
    }

    [TestMethod]
    public void SinStockLibreLasSolicitudesSeRechazan()
    {
        for (int confirmadas = 0; confirmadas < StockInicial; confirmadas++)
        {
            ReservaConfirmada();
        }

        Reserva excedente = ReservaRegistrada();
        excedente.Resolver();

        Assert.AreEqual("Rechazada", excedente.NombreDelEstado());
        Assert.AreEqual(0, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    public void CancelarReservaConfirmadaDevuelveSuUnidadAlPool()
    {
        Reserva reserva = ReservaConfirmada();

        reserva.Cancelar();

        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
    }

    /// <summary>
    /// El modelo crea sus propias reservas, asi que una reserva nunca puede quedar registrada en un
    /// modelo distinto del suyo: la regla se cumple por construccion y no por validacion.
    /// </summary>
    [TestMethod]
    public void CadaReservaQuedaRegistradaEnElModeloQueLaCreo()
    {
        Modelo otroModelo = new Modelo(DatosCon("Edison X2", 72000m), creadora);
        otroModelo.AgregarUnidades(1);

        Reserva ajena = otroModelo.RegistrarReserva(creadora, reloj);

        Assert.IsTrue(ajena.EsDelModelo("Edison X2"));
        Assert.AreEqual(0, modelo.ReservasActivas());
        Assert.AreEqual(1, otroModelo.ReservasActivas());
    }

    [TestMethod]
    public void AjustarStockHaciaArribaAgregaUnidadesDisponibles()
    {
        modelo.AjustarStock(StockInicial + 2);

        Assert.AreEqual(StockInicial + 2, modelo.StockTotal);
        Assert.AreEqual(StockInicial + 2, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    public void ModificarAplicaDatosYStockComoUnaOperacionCompleta()
    {
        modelo.Modificar(new FichaDeModelo(DatosModificados(), StockInicial + 2), creadora);

        Assert.AreEqual("Sedan electrico renovado", modelo.Descripcion);
        Assert.AreEqual(39990m, modelo.Precio);
        Assert.AreEqual(new DateTime(2027, 6, 1), modelo.FechaEntregaPrometida);
        Assert.AreEqual(StockInicial + 2, modelo.StockTotal);
    }

    [TestMethod]
    public void ErrorDeStockNoModificaLosDatosNiElStock()
    {
        ReservaRegistrada();

        Assert.ThrowsException<DominioException>(
            () => modelo.Modificar(new FichaDeModelo(DatosModificados(), 1), creadora));

        Assert.AreEqual("Sedan electrico", modelo.Descripcion);
        Assert.AreEqual(45000m, modelo.Precio);
        Assert.AreEqual(new DateTime(2027, 3, 1), modelo.FechaEntregaPrometida);
        Assert.AreEqual(StockInicial, modelo.StockTotal);
    }

    [TestMethod]
    public void ErrorDeDatosNoModificaElStockNiLosDatos()
    {
        DatosDeModelo otroNombre = DatosCon("Edison X2", 39990m);

        Assert.ThrowsException<DominioException>(
            () => modelo.Modificar(new FichaDeModelo(otroNombre, StockInicial + 2), creadora));

        Assert.AreEqual(NombreModelo, modelo.Nombre);
        Assert.AreEqual("Sedan electrico", modelo.Descripcion);
        Assert.AreEqual(45000m, modelo.Precio);
        Assert.AreEqual(StockInicial, modelo.StockTotal);
    }

    [TestMethod]
    public void ModificarNuncaDejaElStockEnElNumeroDeReservasActivas()
    {
        ReservaRegistrada();
        ReservaConfirmada();

        Assert.ThrowsException<DominioException>(
            () => modelo.Modificar(new FichaDeModelo(DatosModificados(), 2), creadora));

        Assert.AreEqual(StockInicial, modelo.StockTotal);
        Assert.AreEqual(2, modelo.ReservasActivas());
    }

    [TestMethod]
    public void AjustarStockHaciaAbajoSoloQuitaUnidadesDisponibles()
    {
        ReservaConfirmada();

        modelo.AjustarStock(2);

        Assert.AreEqual(2, modelo.StockTotal);
        Assert.AreEqual(1, modelo.UnidadesReservadas);
        Assert.AreEqual(1, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void AjustarStockQueNoSupereLasReservasActivasFalla()
    {
        ReservaConfirmada();
        ReservaConfirmada();

        modelo.AjustarStock(2);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void UnaReservaPendienteTambienBloqueaLaBajaDeStock()
    {
        ReservaRegistrada();

        modelo.AjustarStock(1);
    }

    [TestMethod]
    public void PuedeEliminarseSoloCuandoNoQuedanReservasActivas()
    {
        Assert.IsTrue(modelo.PuedeEliminarse());

        Reserva reserva = ReservaRegistrada();
        Assert.IsFalse(modelo.PuedeEliminarse());

        reserva.Cancelar();
        Assert.IsTrue(modelo.PuedeEliminarse());
    }

    [TestMethod]
    public void LasReservasActivasCuentanPendientesYConfirmadas()
    {
        ReservaRegistrada();
        ReservaConfirmada();

        Assert.AreEqual(2, modelo.ReservasActivas());
    }

    [TestMethod]
    public void ActualizarDatosCambiaLosDatosDescriptivos()
    {
        modelo.ActualizarDatos(DatosCon(NombreModelo, 39990m));

        Assert.AreEqual(39990m, modelo.Precio);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void ActualizarDatosSinUnidadesDisponiblesFalla()
    {
        for (int confirmadas = 0; confirmadas < StockInicial; confirmadas++)
        {
            ReservaConfirmada();
        }

        modelo.ActualizarDatos(DatosCon(NombreModelo, 39990m));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void ActualizarDatosConOtroNombreFalla()
    {
        modelo.ActualizarDatos(DatosCon("Edison X2", 39990m));
    }

    [TestMethod]
    public void FueCreadoPorReconoceAlCreadorIgnorandoMayusculas()
    {
        Assert.IsTrue(modelo.FueCreadoPor("JFerreiro"));
        Assert.IsFalse(modelo.FueCreadoPor("otra"));
    }

    [TestMethod]
    public void ExigirPropietarioAceptaAlCreadorYRechazaAlResto()
    {
        Usuario otraUsuaria = new Usuario("sgomez", "clave1234");

        modelo.ExigirPropietario(creadora);

        Assert.ThrowsException<DominioException>(() => modelo.ExigirPropietario(otraUsuaria));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearDatosConPrecioNoPositivoFalla()
    {
        _ = DatosCon(NombreModelo, 0m);
    }

    [TestMethod]
    public void ElModeloSinImagenQuedaConCadenaVaciaYNoTieneImagen()
    {
        Assert.AreEqual(string.Empty, modelo.NombreArchivoImagen);
        Assert.IsFalse(modelo.TieneImagen);
    }
}
