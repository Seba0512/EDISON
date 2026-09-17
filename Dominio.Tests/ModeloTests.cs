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

    private void PrepararModeloConStockUnoYUnaReservaPendiente()
    {
        modelo = new Modelo(DatosCon(NombreModelo, 45000m), creadora);
        modelo.AgregarUnidades(1);
        ReservaRegistrada();
    }

    [TestMethod]
    public void ElStockInicialCreaEsaCantidadDeUnidadesDisponibles()
    {
        Assert.AreEqual(StockInicial, modelo.TomarLectura().StockTotal);
        Assert.AreEqual(StockInicial, modelo.TomarLectura().UnidadesDisponibles);
        Assert.AreEqual(0, modelo.TomarLectura().UnidadesReservadas);
    }

    [TestMethod]
    public void ResolverConStockConfirmaYReservaUnaUnidad()
    {
        Reserva reserva = ReservaConfirmada();

        Assert.IsTrue(reserva.EstaConfirmada);
        Assert.AreEqual(StockInicial - 1, modelo.TomarLectura().UnidadesDisponibles);
        Assert.AreEqual(1, modelo.TomarLectura().UnidadesReservadas);
        Assert.AreEqual(StockInicial, modelo.TomarLectura().StockTotal);
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
        Assert.AreEqual(0, modelo.TomarLectura().UnidadesDisponibles);
    }

    [TestMethod]
    public void CancelarReservaConfirmadaDevuelveSuUnidadAlPool()
    {
        Reserva reserva = ReservaConfirmada();

        reserva.Cancelar();

        Assert.AreEqual(StockInicial, modelo.TomarLectura().UnidadesDisponibles);
        Assert.AreEqual(0, modelo.TomarLectura().UnidadesReservadas);
    }

    [TestMethod]
    public void CadaReservaQuedaRegistradaEnElModeloQueLaCreo()
    {
        Modelo otroModelo = new Modelo(DatosCon("Edison X2", 72000m), creadora);
        otroModelo.AgregarUnidades(1);

        Reserva ajena = otroModelo.RegistrarReserva(creadora, reloj);

        Assert.AreEqual("Edison X2", ajena.TomarLectura().NombreModelo);
        Assert.AreEqual(0, modelo.ReservasActivas());
        Assert.AreEqual(1, otroModelo.ReservasActivas());
    }

    [TestMethod]
    public void ModificarAplicaDatosYStockComoUnaOperacionCompleta()
    {
        modelo.ModificarDatos(new FichaDeModelo(DatosModificados(), StockInicial + 2), creadora);

        Assert.AreEqual("Sedan electrico renovado", modelo.TomarLectura().Descripcion);
        Assert.AreEqual(39990m, modelo.TomarLectura().Precio);
        Assert.AreEqual(new DateTime(2027, 6, 1), modelo.TomarLectura().FechaEntregaPrometida);
        Assert.AreEqual(StockInicial + 2, modelo.TomarLectura().StockTotal);
    }

    [TestMethod]
    public void ModificarDescripcionSinCambiarStockNoValidaContraReservasActivas()
    {
        PrepararModeloConStockUnoYUnaReservaPendiente();
        DatosDeModelo datos = new DatosDeModelo(
            NombreModelo, "Sedan electrico renovado", 45000m,
            new DateTime(2027, 3, 1), string.Empty);

        modelo.ModificarDatos(new FichaDeModelo(datos, 1), creadora);

        Assert.AreEqual("Sedan electrico renovado", modelo.TomarLectura().Descripcion);
        Assert.AreEqual(1, modelo.TomarLectura().StockTotal);
        Assert.AreEqual(1, modelo.ReservasActivas());
    }

    [TestMethod]
    public void ModificarPrecioSinCambiarStockNoValidaContraReservasActivas()
    {
        PrepararModeloConStockUnoYUnaReservaPendiente();
        DatosDeModelo datos = DatosCon(NombreModelo, 39990m);

        modelo.ModificarDatos(new FichaDeModelo(datos, 1), creadora);

        Assert.AreEqual(39990m, modelo.TomarLectura().Precio);
        Assert.AreEqual(1, modelo.TomarLectura().StockTotal);
        Assert.AreEqual(1, modelo.ReservasActivas());
    }

    [TestMethod]
    public void ErrorDeStockNoModificaLosDatosNiElStock()
    {
        ReservaRegistrada();

        Assert.ThrowsException<DominioException>(
            () => modelo.ModificarDatos(new FichaDeModelo(DatosModificados(), 1), creadora));

        Assert.AreEqual("Sedan electrico", modelo.TomarLectura().Descripcion);
        Assert.AreEqual(45000m, modelo.TomarLectura().Precio);
        Assert.AreEqual(new DateTime(2027, 3, 1), modelo.TomarLectura().FechaEntregaPrometida);
        Assert.AreEqual(StockInicial, modelo.TomarLectura().StockTotal);
    }

    [TestMethod]
    public void ErrorDeDatosNoModificaElStockNiLosDatos()
    {
        DatosDeModelo otroNombre = DatosCon("Edison X2", 39990m);

        Assert.ThrowsException<DominioException>(
            () => modelo.ModificarDatos(new FichaDeModelo(otroNombre, StockInicial + 2), creadora));

        Assert.AreEqual(NombreModelo, modelo.Nombre);
        Assert.AreEqual("Sedan electrico", modelo.TomarLectura().Descripcion);
        Assert.AreEqual(45000m, modelo.TomarLectura().Precio);
        Assert.AreEqual(StockInicial, modelo.TomarLectura().StockTotal);
    }

    [TestMethod]
    public void ModificarNuncaDejaElStockEnElNumeroDeReservasActivas()
    {
        ReservaRegistrada();
        ReservaConfirmada();

        Assert.ThrowsException<DominioException>(
            () => modelo.ModificarDatos(new FichaDeModelo(DatosModificados(), 2), creadora));

        Assert.AreEqual(StockInicial, modelo.TomarLectura().StockTotal);
        Assert.AreEqual(2, modelo.ReservasActivas());
    }

    [TestMethod]
    public void ModificarUnModeloAgotadoSinAumentarElStockFalla()
    {
        for (int confirmadas = 0; confirmadas < StockInicial; confirmadas++)
        {
            ReservaConfirmada();
        }

        Assert.ThrowsException<DominioException>(
            () => modelo.ModificarDatos(new FichaDeModelo(DatosModificados(), StockInicial), creadora));

        Assert.AreEqual("Sedan electrico", modelo.TomarLectura().Descripcion);
        Assert.AreEqual(StockInicial, modelo.TomarLectura().StockTotal);
        Assert.AreEqual(0, modelo.TomarLectura().UnidadesDisponibles);
    }

    [TestMethod]
    public void ModificarUnModeloAgotadoAumentandoElStockLoSaneaDelAgotamiento()
    {
        for (int confirmadas = 0; confirmadas < StockInicial; confirmadas++)
        {
            ReservaConfirmada();
        }

        modelo.ModificarDatos(new FichaDeModelo(DatosModificados(), StockInicial + 1), creadora);

        Assert.AreEqual("Sedan electrico renovado", modelo.TomarLectura().Descripcion);
        Assert.AreEqual(StockInicial + 1, modelo.TomarLectura().StockTotal);
        Assert.AreEqual(1, modelo.TomarLectura().UnidadesDisponibles);
    }

    [TestMethod]
    public void NoSePuedeEliminarConReservasActivasYCancelarlasVuelveAHabilitarlo()
    {
        Reserva reserva = ReservaRegistrada();
        Assert.AreEqual(1, modelo.ReservasActivas());
        Assert.ThrowsException<DominioException>(() => modelo.MarcarComoEliminado(creadora));

        reserva.Cancelar();

        Assert.AreEqual(0, modelo.ReservasActivas());
        Assert.AreEqual(string.Empty, modelo.MarcarComoEliminado(creadora));
    }

    [TestMethod]
    public void LasReservasActivasCuentanPendientesYConfirmadas()
    {
        ReservaRegistrada();
        ReservaConfirmada();

        Assert.AreEqual(2, modelo.ReservasActivas());
    }

    [TestMethod]
    public void SoloElCreadorPuedeModificarElModelo()
    {
        Usuario otraUsuaria = new Usuario("sgomez", "clave1234");
        FichaDeModelo ficha = new FichaDeModelo(DatosModificados(), StockInicial);

        modelo.ModificarDatos(ficha, creadora);

        Assert.ThrowsException<DominioException>(() => modelo.ModificarDatos(ficha, otraUsuaria));
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
        Assert.AreEqual(string.Empty, modelo.TomarLectura().NombreArchivoImagen);
        Assert.IsFalse(modelo.TomarLectura().TieneImagen);
    }

    [TestMethod]
    public void ElPrecioSeRedondeaSinDecimales()
    {
        Assert.AreEqual(39990m, DatosCon(NombreModelo, 39990.4m).Precio);
        Assert.AreEqual(39991m, DatosCon(NombreModelo, 39990.5m).Precio);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void UnPrecioQueRedondeaACeroFalla()
    {
        _ = DatosCon(NombreModelo, 0.4m);
    }

    [TestMethod]
    public void CambiarImagenReemplazaLaImagenYDevuelveLaAnterior()
    {
        modelo.CambiarImagen(creadora, "vieja.png");

        string anterior = modelo.CambiarImagen(creadora, "nueva.png");

        Assert.AreEqual("vieja.png", anterior);
        Assert.AreEqual("nueva.png", modelo.TomarLectura().NombreArchivoImagen);
    }

    [TestMethod]
    public void CambiarImagenDeUnModeloSinImagenPreviaDevuelveCadenaVacia()
    {
        string anterior = modelo.CambiarImagen(creadora, "nueva.png");

        Assert.AreEqual(string.Empty, anterior);
        Assert.AreEqual("nueva.png", modelo.TomarLectura().NombreArchivoImagen);
    }

    [TestMethod]
    public void CambiarImagenDeUnModeloAjenoFalla()
    {
        Usuario otraUsuaria = new Usuario("sgomez", "clave1234");

        Assert.ThrowsException<DominioException>(() => modelo.CambiarImagen(otraUsuaria, "nueva.png"));
    }

    [TestMethod]
    public void CambiarImagenDeUnModeloAgotadoFalla()
    {
        for (int confirmadas = 0; confirmadas < StockInicial; confirmadas++)
        {
            ReservaConfirmada();
        }

        Assert.ThrowsException<DominioException>(
            () => modelo.CambiarImagen(creadora, "nueva.png"));

        Assert.AreEqual(string.Empty, modelo.TomarLectura().NombreArchivoImagen);
    }

    [TestMethod]
    public void CambiarImagenDespuesDeReponerStockEsValido()
    {
        for (int confirmadas = 0; confirmadas < StockInicial; confirmadas++)
        {
            ReservaConfirmada();
        }

        modelo.ModificarDatos(
            new FichaDeModelo(DatosModificados(), StockInicial + 1), creadora);

        string anterior = modelo.CambiarImagen(creadora, "nueva.png");

        Assert.AreEqual(string.Empty, anterior);
        Assert.AreEqual("nueva.png", modelo.TomarLectura().NombreArchivoImagen);
    }
}
