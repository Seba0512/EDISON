using Dominio;
using Dominio.Excepciones;
using Dominio.Tests.Dobles;

namespace Dominio.Tests;

[TestClass]
public class CicloDeVidaModeloConcurrenteTests
{
    private const string NombreModelo = "Edison S1";
    private const string Clave = "edison2026";
    private const string ImagenVigente = "edison-vieja.png";
    private const string ImagenNueva = "edison-nueva.png";
    private const string ImagenAlternativa = "edison-alternativa.png";
    private const int EsperaDeOperacionEnMilisegundos = 1000;
    private const int EsperaDeHiloEnMilisegundos = 5000;

    private static readonly DateTime Momento =
        new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RepositorioDeModelosEnMemoria modelos = null!;
    private RepositorioDeReservasEnMemoria reservas = null!;
    private CatalogoDeModelos catalogo = null!;
    private CentralDeReservas central = null!;
    private Usuario creadora = null!;
    private Usuario solicitante = null!;
    private Modelo modelo = null!;

    [TestInitialize]
    public void Inicializar()
    {
        modelos = new RepositorioDeModelosEnMemoria();
        reservas = new RepositorioDeReservasEnMemoria();
        RepositorioDeUsuariosEnMemoria usuarios = new RepositorioDeUsuariosEnMemoria();
        creadora = new Usuario("jferreiro", Clave);
        solicitante = new Usuario("sgomez", Clave);
        usuarios.Agregar(creadora);
        usuarios.Agregar(solicitante);

        catalogo = new CatalogoDeModelos(
            modelos, new AlmacenDeImagenesFalso(), new RelojFalso(Momento));
        central = new CentralDeReservas(
            reservas, modelos, usuarios, new RelojFalso(Momento), 15);
        catalogo.AltaDeModelo(Ficha("Original", 45000m), creadora);
        modelo = modelos.ObtenerPorNombre(NombreModelo);
    }

    [TestMethod]
    public void EliminarYReservarEnParaleloNoDejanReservaHuerfana()
    {
        var resultado = EjecutarEnParalelo(
            () => catalogo.EliminarModelo(NombreModelo, creadora),
            () => central.SolicitarReserva(NombreModelo, solicitante));

        Assert.AreEqual(1, CantidadDeExitos(resultado));
        bool eliminado = !modelos.Existe(NombreModelo);
        Assert.AreEqual(eliminado ? 0 : 1, reservas.DeUsuario("sgomez").Count);
        Assert.AreEqual(eliminado, resultado.Primera.Completo);
    }

    [TestMethod]
    public void SiLaReservaGanaLaBajaFallaPorReservaActiva()
    {
        using ManualResetEventSlim reservaRegistrada = new(false);
        EjecucionDeHilo alta = new EjecucionDeHilo();
        EjecucionDeHilo baja = new EjecucionDeHilo();
        Thread hiloDeAlta = new Thread(() =>
        {
            alta.Ejecutar(() => central.SolicitarReserva(NombreModelo, solicitante));
            reservaRegistrada.Set();
        });
        Thread hiloDeBaja = new Thread(() => baja.Ejecutar(() =>
        {
            EsperarSenal(reservaRegistrada);
            catalogo.EliminarModelo(NombreModelo, creadora);
        }));

        hiloDeBaja.Start();
        hiloDeAlta.Start();

        Assert.IsTrue(hiloDeAlta.Join(EsperaDeHiloEnMilisegundos));
        Assert.IsTrue(hiloDeBaja.Join(EsperaDeHiloEnMilisegundos));
        Assert.IsTrue(alta.Completo);
        Assert.IsInstanceOfType<DominioException>(baja.Falla);
        Assert.IsTrue(modelos.Existe(NombreModelo));
        Assert.AreEqual(1, reservas.DeUsuario("sgomez").Count);
    }

    [TestMethod]
    public void SiLaBajaGanaUnaNuevaSolicitudFalla()
    {
        using ManualResetEventSlim bajaCompletada = new(false);
        EjecucionDeHilo baja = new EjecucionDeHilo();
        EjecucionDeHilo alta = new EjecucionDeHilo();
        Thread hiloDeBaja = new Thread(() =>
        {
            baja.Ejecutar(() => catalogo.EliminarModelo(NombreModelo, creadora));
            bajaCompletada.Set();
        });
        Thread hiloDeAlta = new Thread(() => alta.Ejecutar(() =>
        {
            EsperarSenal(bajaCompletada);
            central.SolicitarReserva(NombreModelo, solicitante);
        }));

        hiloDeAlta.Start();
        hiloDeBaja.Start();

        Assert.IsTrue(hiloDeBaja.Join(EsperaDeHiloEnMilisegundos));
        Assert.IsTrue(hiloDeAlta.Join(EsperaDeHiloEnMilisegundos));
        Assert.IsTrue(baja.Completo);
        Assert.IsInstanceOfType<ModeloInexistenteException>(alta.Falla);
        Assert.IsFalse(modelos.Existe(NombreModelo));
        Assert.AreEqual(0, reservas.DeUsuario("sgomez").Count);
    }

    [TestMethod]
    public void EliminarYModificarDatosEnParaleloConservanLaImagenVigente()
    {
        AlmacenDeImagenesFalso almacen = new AlmacenDeImagenesFalso();
        CatalogoDeModelos catalogoConImagen = PrepararModeloConImagen(almacen);
        FichaDeModelo cambios = Ficha("Modificado", 39990m, ImagenNueva);

        var resultado = EjecutarEnParalelo(
            () => catalogoConImagen.EliminarModelo(NombreModelo, creadora),
            () => catalogoConImagen.ModificarModelo(cambios, creadora));

        Assert.IsTrue(resultado.Primera.Completo);
        Assert.IsFalse(modelos.Existe(NombreModelo));
        CollectionAssert.AreEqual(new[] { ImagenVigente }, almacen.Borrados.ToArray());
        Assert.ThrowsException<DominioException>(() => modelo.ModificarDatos(cambios, creadora));
    }

    [TestMethod]
    public void SiCambiarImagenGanaLaBajaEliminaLaImagenNueva()
    {
        AlmacenDeImagenesFalso almacen = new AlmacenDeImagenesFalso();
        CatalogoDeModelos catalogoConImagen = PrepararModeloConImagen(almacen);
        using ManualResetEventSlim bajaIniciada = new(false);
        using ManualResetEventSlim permitirBaja = new(false);
        EjecucionDeHilo baja = new EjecucionDeHilo();
        EjecucionDeHilo modificacion = new EjecucionDeHilo();
        Thread hiloDeBaja = new Thread(() => baja.Ejecutar(() =>
        {
            bajaIniciada.Set();
            EsperarSenal(permitirBaja);
            catalogoConImagen.EliminarModelo(NombreModelo, creadora);
        }));
        Thread hiloDeModificacion = new Thread(() => modificacion.Ejecutar(
            () => catalogoConImagen.CambiarImagenDeModelo(
                NombreModelo, ImagenNueva, creadora)));

        hiloDeBaja.Start();
        Assert.IsTrue(bajaIniciada.Wait(EsperaDeHiloEnMilisegundos));
        hiloDeModificacion.Start();
        bool modificacionTermino = hiloDeModificacion.Join(EsperaDeHiloEnMilisegundos);
        permitirBaja.Set();
        bool bajaTermino = hiloDeBaja.Join(EsperaDeHiloEnMilisegundos);

        Assert.IsTrue(modificacionTermino);
        Assert.IsTrue(bajaTermino);
        Assert.IsTrue(modificacion.Completo);
        Assert.IsTrue(baja.Completo);
        CollectionAssert.AreEqual(new[] { ImagenNueva }, almacen.Borrados.ToArray());
    }

    [TestMethod]
    public void Cmd11PreparadoAntesDeCmd15NoRestauraLaImagenAnterior()
    {
        modelo.CambiarImagen(creadora, ImagenVigente);
        FichaDeModelo cambiosCapturados = Ficha("Modificado", 39990m, ImagenVigente);
        using ManualResetEventSlim cmd11Preparado = new(false);
        using ManualResetEventSlim imagenCambiada = new(false);
        EjecucionDeHilo modificacion = new EjecucionDeHilo();
        Thread hiloCmd11 = new Thread(() => modificacion.Ejecutar(() =>
        {
            cmd11Preparado.Set();
            EsperarSenal(imagenCambiada);
            modelo.ModificarDatos(cambiosCapturados, creadora);
        }));

        hiloCmd11.Start();
        Assert.IsTrue(cmd11Preparado.Wait(EsperaDeHiloEnMilisegundos));
        modelo.CambiarImagen(creadora, ImagenNueva);
        imagenCambiada.Set();

        Assert.IsTrue(hiloCmd11.Join(EsperaDeHiloEnMilisegundos));
        Assert.IsTrue(modificacion.Completo);
        Assert.AreEqual(ImagenNueva, modelo.NombreArchivoImagen);
        Assert.AreEqual("Modificado", modelo.Descripcion);
        Assert.AreEqual(39990m, modelo.Precio);
    }

    [TestMethod]
    public void Cmd15PreparadoAntesDeCmd11NoRestauraLosDatosAnteriores()
    {
        modelo.CambiarImagen(creadora, ImagenVigente);
        using ManualResetEventSlim cmd15Preparado = new(false);
        using ManualResetEventSlim datosModificados = new(false);
        EjecucionDeHilo subida = new EjecucionDeHilo();
        Thread hiloCmd15 = new Thread(() => subida.Ejecutar(() =>
        {
            cmd15Preparado.Set();
            EsperarSenal(datosModificados);
            modelo.CambiarImagen(creadora, ImagenNueva);
        }));

        hiloCmd15.Start();
        Assert.IsTrue(cmd15Preparado.Wait(EsperaDeHiloEnMilisegundos));
        modelo.ModificarDatos(Ficha("Modificado", 39990m), creadora);
        datosModificados.Set();

        Assert.IsTrue(hiloCmd15.Join(EsperaDeHiloEnMilisegundos));
        Assert.IsTrue(subida.Completo);
        Assert.AreEqual(ImagenNueva, modelo.NombreArchivoImagen);
        Assert.AreEqual("Modificado", modelo.Descripcion);
        Assert.AreEqual(39990m, modelo.Precio);
    }

    [TestMethod]
    public void DosCambiosDeImagenDevuelvenExactamenteLasImagenesSustituidas()
    {
        modelo.CambiarImagen(creadora, ImagenVigente);
        string anteriorPrimera = string.Empty;
        string anteriorSegunda = string.Empty;

        var resultado = EjecutarEnParalelo(
            () => anteriorPrimera = modelo.CambiarImagen(creadora, ImagenNueva),
            () => anteriorSegunda = modelo.CambiarImagen(creadora, ImagenAlternativa));

        Assert.AreEqual(2, CantidadDeExitos(resultado));
        string vigente = modelo.NombreArchivoImagen;
        string sustituida = vigente == ImagenNueva ? ImagenAlternativa : ImagenNueva;
        CollectionAssert.AreEquivalent(
            new[] { ImagenVigente, sustituida },
            new[] { anteriorPrimera, anteriorSegunda });
    }

    [TestMethod]
    public void DosEliminacionesEnParaleloTienenUnSoloExito()
    {
        var resultado = EjecutarEnParalelo(
            () => catalogo.EliminarModelo(NombreModelo, creadora),
            () => catalogo.EliminarModelo(NombreModelo, creadora));

        Assert.AreEqual(1, CantidadDeExitos(resultado));
        Assert.IsFalse(modelos.Existe(NombreModelo));
    }

    [TestMethod]
    public void SolicitarDespuesDeEliminarNoCreaUnaReserva()
    {
        catalogo.EliminarModelo(NombreModelo, creadora);

        Assert.ThrowsException<ModeloInexistenteException>(
            () => central.SolicitarReserva(NombreModelo, solicitante));
        Assert.AreEqual(0, reservas.DeUsuario("sgomez").Count);
    }

    [TestMethod]
    public void UnaReferenciaAnteriorNoPuedeRegistrarDespuesDeLaBaja()
    {
        catalogo.EliminarModelo(NombreModelo, creadora);

        Assert.ThrowsException<DominioException>(
            () => modelo.RegistrarReserva(solicitante, new RelojFalso(Momento)));
        Assert.AreEqual(0, modelo.ReservasActivas());
        Assert.AreEqual(0, reservas.DeUsuario("sgomez").Count);
    }

    [TestMethod]
    public void SiLaBajaGanaCapturaLaImagenVigenteYLiberaElLockAntesDelFilesystem()
    {
        using AlmacenDeImagenesBloqueante almacen = new AlmacenDeImagenesBloqueante();
        CatalogoDeModelos catalogoBloqueante = PrepararModeloConImagen(almacen);
        EjecucionDeHilo baja = new EjecucionDeHilo();
        Thread hiloDeBaja = new Thread(
            () => baja.Ejecutar(() => catalogoBloqueante.EliminarModelo(NombreModelo, creadora)));

        hiloDeBaja.Start();
        Assert.IsTrue(almacen.EsperarInicio());
        Assert.IsTrue(hiloDeBaja.IsAlive);
        EjecucionDeHilo modificacion = new EjecucionDeHilo();
        Thread hiloDeModificacion = new Thread(
            () => modificacion.Ejecutar(
                () => modelo.ModificarDatos(Ficha("Modificado", 39990m, ImagenNueva), creadora)));
        hiloDeModificacion.Start();

        bool modificacionTermino = hiloDeModificacion.Join(EsperaDeOperacionEnMilisegundos);
        string imagenCapturada = almacen.NombreEliminado;
        string imagenEnModelo = modelo.NombreArchivoImagen;
        almacen.PermitirFinalizacion();
        bool bajaTermino = hiloDeBaja.Join(EsperaDeHiloEnMilisegundos);

        Assert.IsTrue(modificacionTermino,
            "La modificacion espero al filesystem: el lock del modelo seguia tomado");
        Assert.IsTrue(bajaTermino);
        Assert.IsTrue(baja.Completo);
        Assert.IsFalse(modelos.Existe(NombreModelo));
        Assert.IsInstanceOfType<DominioException>(modificacion.Falla);
        Assert.AreEqual(ImagenVigente, imagenCapturada);
        Assert.AreEqual(ImagenVigente, imagenEnModelo);
    }

    private CatalogoDeModelos PrepararModeloConImagen(IAlmacenDeImagenes almacen)
    {
        CatalogoDeModelos catalogoConImagen = new CatalogoDeModelos(
            modelos, almacen, new RelojFalso(Momento));
        modelos.Eliminar(modelo);
        catalogoConImagen.AltaDeModelo(
            Ficha("Original", 45000m, ImagenVigente), creadora);
        modelo = modelos.ObtenerPorNombre(NombreModelo);

        return catalogoConImagen;
    }

    private static FichaDeModelo Ficha(
        string descripcion, decimal precio, string imagen = "")
    {
        DatosDeModelo datos = new DatosDeModelo(
            NombreModelo, descripcion, precio, new DateTime(2027, 3, 1), imagen);
        return new FichaDeModelo(datos, 2);
    }

    private static int CantidadDeExitos(
        (EjecucionDeHilo Primera, EjecucionDeHilo Segunda) resultado)
    {
        return Convert.ToInt32(resultado.Primera.Completo)
               + Convert.ToInt32(resultado.Segunda.Completo);
    }

    private static (EjecucionDeHilo Primera, EjecucionDeHilo Segunda) EjecutarEnParalelo(
        Action primera, Action segunda)
    {
        using Barrier largada = new Barrier(2);
        EjecucionDeHilo primerResultado = new EjecucionDeHilo();
        EjecucionDeHilo segundoResultado = new EjecucionDeHilo();
        Thread primerHilo = new Thread(
            () => primerResultado.Ejecutar(() => Empezar(largada, primera)));
        Thread segundoHilo = new Thread(
            () => segundoResultado.Ejecutar(() => Empezar(largada, segunda)));

        primerHilo.Start();
        segundoHilo.Start();
        bool primeroTermino = primerHilo.Join(EsperaDeHiloEnMilisegundos);
        bool segundoTermino = segundoHilo.Join(EsperaDeHiloEnMilisegundos);

        Assert.IsTrue(primeroTermino, "El primer Thread no termino: posible deadlock");
        Assert.IsTrue(segundoTermino, "El segundo Thread no termino: posible deadlock");
        return (primerResultado, segundoResultado);
    }

    private static void Empezar(Barrier largada, Action accion)
    {
        largada.SignalAndWait();
        accion();
    }

    private static void EsperarSenal(ManualResetEventSlim senal)
    {
        if (!senal.Wait(EsperaDeHiloEnMilisegundos))
        {
            throw new TimeoutException("La coordinacion entre Threads excedio el tiempo esperado");
        }
    }

    private sealed class EjecucionDeHilo
    {
        public bool Completo { get; private set; }

        public Exception? Falla { get; private set; }

        public void Ejecutar(Action accion)
        {
            try
            {
                accion();
                Completo = true;
            }
            catch (Exception falla)
            {
                Falla = falla;
            }
        }
    }

    private sealed class AlmacenDeImagenesBloqueante : IAlmacenDeImagenes, IDisposable
    {
        private readonly ManualResetEventSlim inicio = new(false);
        private readonly ManualResetEventSlim continuar = new(false);

        public string NombreEliminado { get; private set; } = string.Empty;

        public void Eliminar(string nombreArchivo)
        {
            NombreEliminado = nombreArchivo;
            inicio.Set();
            continuar.Wait(EsperaDeHiloEnMilisegundos);
        }

        public bool EsperarInicio()
        {
            return inicio.Wait(EsperaDeHiloEnMilisegundos);
        }

        public void PermitirFinalizacion()
        {
            continuar.Set();
        }

        public void Dispose()
        {
            inicio.Dispose();
            continuar.Dispose();
        }
    }
}
