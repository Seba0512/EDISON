using Dominio;
using Dominio.Busqueda;
using Dominio.Excepciones;
using Dominio.Tests.Dobles;

namespace Dominio.Tests;

[TestClass]
public class CatalogoDeModelosTests
{
    private const string NombreModelo = "Edison S1";
    private const string NombreModeloAgotado = "Volta Uno";
    private const string Imagen = "edison-s1.png";
    private const int StockInicial = 3;

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaEntrega = new DateTime(2027, 3, 1);

    private RelojFalso reloj = null!;
    private AlmacenDeImagenesFalso almacen = null!;
    private RepositorioDeModelosEnMemoria modelos = null!;
    private CatalogoDeModelos catalogo = null!;
    private Usuario creadora = null!;
    private Usuario otroUsuario = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        almacen = new AlmacenDeImagenesFalso();
        modelos = new RepositorioDeModelosEnMemoria();
        catalogo = new CatalogoDeModelos(modelos, almacen, reloj);
        creadora = new Usuario("jferreiro", "edison2026");
        otroUsuario = new Usuario("sgomez", "edison2026");
    }

    private static FichaDeModelo Ficha(decimal precio, int stock, string imagen)
    {
        DatosDeModelo datos = new DatosDeModelo(
            NombreModelo, "Sedan electrico", precio, FechaEntrega, imagen);

        return new FichaDeModelo(datos, stock);
    }

    private void DarDeAlta()
    {
        catalogo.AltaDeModelo(Ficha(45000m, StockInicial, string.Empty), creadora);
    }

    private void DarDeAltaModeloAgotado()
    {
        DatosDeModelo datos = new DatosDeModelo(
            NombreModeloAgotado,
            "Compacto electrico",
            72000m,
            FechaEntrega.AddMonths(-1),
            string.Empty);
        catalogo.AltaDeModelo(new FichaDeModelo(datos, 1), creadora);
        modelos.ObtenerPorNombre(NombreModeloAgotado).RegistrarReserva(otroUsuario, reloj).Resolver();
    }

    [TestMethod]
    public void AltaDeModeloDejaElModeloConSuStockInicial()
    {
        DarDeAlta();

        LecturaDeModelo modelo = catalogo.ConsultarLectura(NombreModelo);

        Assert.AreEqual(StockInicial, modelo.StockTotal);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(TipoActividad.ModeloCreado, creadora.Actividades()[0].Tipo);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void AltaDeUnNombreYaExistenteFalla()
    {
        DarDeAlta();

        catalogo.AltaDeModelo(Ficha(72000m, 1, string.Empty), otroUsuario);
    }

    [TestMethod]
    [ExpectedException(typeof(ModeloInexistenteException))]
    public void ConsultarUnModeloInexistenteFalla()
    {
        catalogo.ConsultarLectura("Edison X2");
    }

    [TestMethod]
    public void ModificarCambiaLosDatosYElStock()
    {
        DarDeAlta();
        DateTime nuevaFecha = FechaEntrega.AddMonths(2);
        DatosDeModelo datos = new DatosDeModelo(
            NombreModelo, "Sedan electrico renovado", 39990m, nuevaFecha, string.Empty);

        catalogo.ModificarModelo(new FichaDeModelo(datos, StockInicial + 2), creadora);

        LecturaDeModelo modelo = catalogo.ConsultarLectura(NombreModelo);
        Assert.AreEqual("Sedan electrico renovado", modelo.Descripcion);
        Assert.AreEqual(39990m, modelo.Precio);
        Assert.AreEqual(nuevaFecha, modelo.FechaEntregaPrometida);
        Assert.AreEqual(StockInicial + 2, modelo.StockTotal);
        Assert.AreEqual(TipoActividad.ModeloModificado, creadora.Actividades()[1].Tipo);
    }

    [TestMethod]
    public void ModificarUnModeloAjenoNoCambiaNingunaPropiedad()
    {
        DarDeAlta();

        Assert.ThrowsException<DominioException>(
            () => catalogo.ModificarModelo(Ficha(39990m, StockInicial + 2, string.Empty), otroUsuario));

        LecturaDeModelo modelo = catalogo.ConsultarLectura(NombreModelo);
        Assert.AreEqual("Sedan electrico", modelo.Descripcion);
        Assert.AreEqual(45000m, modelo.Precio);
        Assert.AreEqual(FechaEntrega, modelo.FechaEntregaPrometida);
        Assert.AreEqual(StockInicial, modelo.StockTotal);
        Assert.AreEqual(1, creadora.Actividades().Count);
    }

    [TestMethod]
    public void EliminarSacaElModeloDelCatalogo()
    {
        DarDeAlta();

        catalogo.EliminarModelo(NombreModelo, creadora);

        Assert.AreEqual(0, catalogo.Buscar(new CriterioQueAceptaTodo()).Count);
        Assert.AreEqual(TipoActividad.ModeloEliminado, creadora.Actividades()[1].Tipo);
    }

    [TestMethod]
    public void EliminarUnModeloConImagenBorraTambienLaImagen()
    {
        catalogo.AltaDeModelo(Ficha(45000m, StockInicial, Imagen), creadora);

        catalogo.EliminarModelo(NombreModelo, creadora);

        CollectionAssert.AreEqual(new[] { Imagen }, almacen.Borrados.ToArray());
    }

    [TestMethod]
    public void EliminarUnModeloSinImagenNoTocaElAlmacen()
    {
        DarDeAlta();

        catalogo.EliminarModelo(NombreModelo, creadora);

        Assert.AreEqual(0, almacen.Borrados.Count);
    }

    [TestMethod]
    public void SiFallaBorrarLaImagenLaBajaSeCompensaYPuedeReintentarse()
    {
        RepositorioDeModelosEnMemoria repositorio = new RepositorioDeModelosEnMemoria();
        AlmacenDeImagenesConFalla almacenConFalla = new AlmacenDeImagenesConFalla();
        CatalogoDeModelos catalogoConFalla =
            new CatalogoDeModelos(repositorio, almacenConFalla, reloj);
        catalogoConFalla.AltaDeModelo(Ficha(45000m, StockInicial, Imagen), creadora);

        ErrorDeAlmacenamientoDeImagenException error =
            Assert.ThrowsException<ErrorDeAlmacenamientoDeImagenException>(
                () => catalogoConFalla.EliminarModelo(NombreModelo, creadora));

        Assert.IsInstanceOfType<IOException>(error.InnerException);
        Assert.IsTrue(repositorio.Existe(NombreModelo));
        Assert.AreEqual(1, creadora.Actividades().Count);
        Assert.AreEqual(TipoActividad.ModeloCreado, creadora.Actividades()[0].Tipo);

        almacenConFalla.PermitirBorrado();
        catalogoConFalla.EliminarModelo(NombreModelo, creadora);

        Assert.IsFalse(repositorio.Existe(NombreModelo));
        Assert.AreEqual(2, creadora.Actividades().Count);
        Assert.AreEqual(TipoActividad.ModeloEliminado, creadora.Actividades()[1].Tipo);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void EliminarUnModeloAjenoFalla()
    {
        DarDeAlta();

        catalogo.EliminarModelo(NombreModelo, otroUsuario);
    }

    [TestMethod]
    public void BuscarDevuelveSoloLosModelosQueCumplenElCriterio()
    {
        DarDeAlta();

        Assert.AreEqual(1, catalogo.Buscar(new CriterioPorPrecioMaximo(50000m)).Count);
        Assert.AreEqual(0, catalogo.Buscar(new CriterioPorPrecioMaximo(1000m)).Count);
    }

    [TestMethod]
    public void BuscarSinFiltrosSoloDevuelveModelosDisponibles()
    {
        DarDeAlta();
        DarDeAltaModeloAgotado();

        IReadOnlyList<LecturaDeModelo> encontrados = catalogo.Buscar(new CriterioQueAceptaTodo());

        Assert.AreEqual(1, encontrados.Count);
        Assert.IsTrue(encontrados[0].SeLlama(NombreModelo));
    }

    [TestMethod]
    public void BuscarConFiltrosExplicitosNuncaIncluyeUnModeloAgotado()
    {
        DarDeAlta();
        DarDeAltaModeloAgotado();

        AfirmarAgotadoExcluido(new CriterioPorNombre("Volta"));
        AfirmarAgotadoExcluido(new CriterioPorPrecioMinimo(70000m));
        AfirmarAgotadoExcluido(new CriterioPorPrecioMaximo(100000m));
        AfirmarAgotadoExcluido(
            new CriterioPorFechaDeEntregaMaxima(FechaEntrega.AddMonths(-1)));
        AfirmarAgotadoExcluido(new CriterioPorUnidadesDisponibles(0));
    }

    [TestMethod]
    public void BuscarDevuelveLaMismaLecturaQueCumplioElCriterio()
    {
        catalogo.AltaDeModelo(Ficha(45000m, 1, string.Empty), creadora);
        IReadOnlyList<LecturaDeModelo> encontrados =
            catalogo.Buscar(new CriterioPorUnidadesDisponibles(1));

        modelos.ObtenerPorNombre(NombreModelo).RegistrarReserva(otroUsuario, reloj).Resolver();

        Assert.AreEqual(1, encontrados[0].UnidadesDisponibles);
        Assert.AreEqual(0, catalogo.ConsultarLectura(NombreModelo).UnidadesDisponibles);
    }

    [TestMethod]
    public void BuscarSinCoincidenciasDevuelveUnaListaVaciaYNoNull()
    {
        Assert.AreEqual(0, catalogo.Buscar(new CriterioQueAceptaTodo()).Count);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void AltaConFechaDeEntregaHoyOAnteriorFalla()
    {
        DatosDeModelo datos = new DatosDeModelo(NombreModelo, "Sedan electrico", 45000m, Momento.Date, string.Empty);

        catalogo.AltaDeModelo(new FichaDeModelo(datos, StockInicial), creadora);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void ModificarCambiandoAUnaFechaHoyOAnteriorFalla()
    {
        DarDeAlta();
        DatosDeModelo datos = new DatosDeModelo(NombreModelo, "Sedan electrico", 45000m, Momento.Date, string.Empty);

        catalogo.ModificarModelo(new FichaDeModelo(datos, StockInicial), creadora);
    }

    [TestMethod]
    public void ModificarSinCambiarLaFechaNoLaRevalidaAunqueYaHayaVencido()
    {
        DarDeAlta();
        reloj.Avanzar(TimeSpan.FromDays(400));
        DatosDeModelo datos = new DatosDeModelo(
            NombreModelo, "Sedan electrico renovado", 39990m, FechaEntrega, string.Empty);

        catalogo.ModificarModelo(new FichaDeModelo(datos, StockInicial), creadora);

        LecturaDeModelo modelo = catalogo.ConsultarLectura(NombreModelo);
        Assert.AreEqual("Sedan electrico renovado", modelo.Descripcion);
    }

    [TestMethod]
    public void CambiarImagenDeModeloReemplazaLaImagenYRegistraActividad()
    {
        catalogo.AltaDeModelo(Ficha(45000m, StockInicial, Imagen), creadora);

        string anterior = catalogo.CambiarImagenDeModelo(NombreModelo, "nueva.png", creadora);

        Assert.AreEqual(Imagen, anterior);
        Assert.AreEqual("nueva.png", catalogo.ConsultarLectura(NombreModelo).NombreArchivoImagen);
        Assert.AreEqual(TipoActividad.ModeloModificado, creadora.Actividades()[1].Tipo);
    }

    private void AfirmarAgotadoExcluido(ICriterioDeBusqueda criterio)
    {
        IReadOnlyList<LecturaDeModelo> encontrados = catalogo.Buscar(criterio);

        Assert.IsFalse(encontrados.Any(modelo => modelo.SeLlama(NombreModeloAgotado)));
    }

    private sealed class AlmacenDeImagenesConFalla : IAlmacenDeImagenes
    {
        private bool falla = true;

        public void Eliminar(string nombreArchivo)
        {
            if (falla)
            {
                throw new ErrorDeAlmacenamientoDeImagenException(
                    "No se pudo borrar la imagen",
                    new IOException("Falla de disco simulada"));
            }
        }

        public void PermitirBorrado()
        {
            falla = false;
        }
    }
}
