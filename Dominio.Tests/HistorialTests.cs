using Dominio;

namespace Dominio.Tests;

[TestClass]
public class HistorialTests
{
    private const string NombreModelo = "Edison S1";
    private const int CantidadDeHilos = 20;
    private const int ActividadesPorHilo = 50;
    private const int EsperaDeHiloEnMilisegundos = 5000;

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private Historial historial = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        historial = new Historial();
    }

    [TestMethod]
    public void UnHistorialNuevoEstaVacio()
    {
        Assert.AreEqual(0, historial.Actividades.Count);
    }

    [TestMethod]
    public void RegistrarAgregaLaActividadAlHistorial()
    {
        historial.Registrar(Actividad.InicioSesion(reloj));

        Assert.AreEqual(1, historial.Actividades.Count);
        Assert.AreEqual(TipoActividad.InicioSesion, historial.Actividades[0].Tipo);
    }

    [TestMethod]
    public void LasActividadesQuedanEnElOrdenEnQueSeRegistraron()
    {
        historial.Registrar(Actividad.InicioSesion(reloj));
        reloj.Avanzar(TimeSpan.FromMinutes(1));
        historial.Registrar(Actividad.ModeloCreado(NombreModelo, reloj));
        reloj.Avanzar(TimeSpan.FromMinutes(1));
        historial.Registrar(Actividad.ReservaCreada(NombreModelo, reloj));

        Assert.AreEqual(TipoActividad.InicioSesion, historial.Actividades[0].Tipo);
        Assert.AreEqual(TipoActividad.ModeloCreado, historial.Actividades[1].Tipo);
        Assert.AreEqual(TipoActividad.ReservaCreada, historial.Actividades[2].Tipo);
    }

    [TestMethod]
    public void ElHistorialConservaLaHoraDeCadaActividad()
    {
        historial.Registrar(Actividad.InicioSesion(reloj));
        reloj.Avanzar(TimeSpan.FromMinutes(30));
        historial.Registrar(Actividad.CierreSesion(reloj));

        Assert.AreEqual(Momento, historial.Actividades[0].FechaHora);
        Assert.IsTrue(historial.Actividades[1].FechaHora > historial.Actividades[0].FechaHora);
    }

    [TestMethod]
    public void RegistrarDesdeVariosThreadsNoPierdeActividades()
    {
        Usuario usuario = new Usuario("jferreiro", "edison2026");
        using Barrier largada = new Barrier(CantidadDeHilos);
        List<EjecucionDeHilo> ejecuciones = Enumerable.Range(0, CantidadDeHilos)
            .Select(_ => new EjecucionDeHilo())
            .ToList();
        List<Thread> hilos = ejecuciones.Select(ejecucion => new Thread(
            () => ejecucion.Ejecutar(() => RegistrarActividades(usuario, largada)))).ToList();

        hilos.ForEach(hilo => hilo.Start());
        EsperarFinalizacion(hilos);

        Assert.IsTrue(ejecuciones.All(ejecucion => ejecucion.Falla is null));
        Assert.AreEqual(CantidadDeHilos * ActividadesPorHilo, usuario.Actividades().Count);
    }

    [TestMethod]
    public void ConsultarMientrasSeRegistraUsaSnapshotsSeguros()
    {
        using ManualResetEventSlim mitadRegistrada = new(false);
        using ManualResetEventSlim consultaIniciada = new(false);
        EjecucionDeHilo escritura = new EjecucionDeHilo();
        EjecucionDeHilo lectura = new EjecucionDeHilo();
        Thread escritor = new Thread(() => escritura.Ejecutar(() =>
            RegistrarEnDosEtapas(mitadRegistrada, consultaIniciada)));
        Thread lector = new Thread(() => lectura.Ejecutar(() =>
            ConsultarDuranteAlta(mitadRegistrada, consultaIniciada)));

        escritor.Start();
        lector.Start();
        EsperarFinalizacion(new List<Thread> { escritor, lector });

        Assert.IsNull(escritura.Falla, escritura.Falla?.ToString());
        Assert.IsNull(lectura.Falla, lectura.Falla?.ToString());
        Assert.AreEqual(ActividadesPorHilo, historial.Actividades.Count);
    }

    [TestMethod]
    public void ConsultaDelHistorialEntregaUnSnapshotIndependiente()
    {
        historial.Registrar(Actividad.InicioSesion(reloj));
        IReadOnlyList<Actividad> snapshot = historial.Actividades;

        historial.Registrar(Actividad.CierreSesion(reloj));

        Assert.AreEqual(1, snapshot.Count);
        ((ICollection<Actividad>)snapshot).Clear();
        Assert.AreEqual(2, historial.Actividades.Count);
    }

    private void RegistrarActividades(Usuario usuario, Barrier largada)
    {
        largada.SignalAndWait();
        for (int indice = 0; indice < ActividadesPorHilo; indice++)
        {
            usuario.Registrar(Actividad.ModeloCreado(NombreModelo, reloj));
        }
    }

    private void RegistrarEnDosEtapas(
        ManualResetEventSlim mitadRegistrada, ManualResetEventSlim consultaIniciada)
    {
        int mitad = ActividadesPorHilo / 2;
        RegistrarCantidad(mitad);
        mitadRegistrada.Set();
        EsperarSenal(consultaIniciada);
        RegistrarCantidad(ActividadesPorHilo - mitad);
    }

    private void ConsultarDuranteAlta(
        ManualResetEventSlim mitadRegistrada, ManualResetEventSlim consultaIniciada)
    {
        EsperarSenal(mitadRegistrada);
        IReadOnlyList<Actividad> snapshot = historial.Actividades;
        _ = snapshot.Count(actividad => actividad.Tipo == TipoActividad.ModeloCreado);
        consultaIniciada.Set();

        for (int consulta = 0; consulta < ActividadesPorHilo; consulta++)
        {
            _ = historial.Actividades.Count;
        }
    }

    private void RegistrarCantidad(int cantidad)
    {
        for (int indice = 0; indice < cantidad; indice++)
        {
            historial.Registrar(Actividad.ModeloCreado(NombreModelo, reloj));
        }
    }

    private static void EsperarSenal(ManualResetEventSlim senal)
    {
        if (!senal.Wait(EsperaDeHiloEnMilisegundos))
        {
            throw new TimeoutException("La coordinacion entre Threads excedio el tiempo esperado");
        }
    }

    private static void EsperarFinalizacion(List<Thread> hilos)
    {
        hilos.ForEach(hilo => Assert.IsTrue(hilo.Join(EsperaDeHiloEnMilisegundos),
            "Un Thread no termino: posible deadlock"));
    }

    private sealed class EjecucionDeHilo
    {
        public Exception? Falla { get; private set; }

        public void Ejecutar(Action accion)
        {
            try
            {
                accion();
            }
            catch (Exception falla)
            {
                Falla = falla;
            }
        }
    }
}
