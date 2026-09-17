using Dominio;

namespace Dominio.Tests;

[TestClass]
public class ConcurrenciaDeModeloTests
{
    private const int Solicitantes = 20;
    private const int StockInicial = 3;
    private const int EsperaDeHiloEnMilisegundos = 5000;
    private const int RepeticionesDeAtomicidad = 100;

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private Usuario creadora = null!;
    private Modelo modelo = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        creadora = new Usuario("jferreiro", "edison2026");
        modelo = CrearModeloConStock(StockInicial);
    }

    [TestMethod]
    public void VariosHilosCompitiendoPorElStockNoLoSobrevenden()
    {
        List<Reserva> reservas = ReservasResueltasEnParalelo();

        VerificarResolucionCompetitiva(reservas, StockInicial);
    }

    [TestMethod]
    public void StockUnoAnteVeinteResolucionesConfirmaExactamenteUna()
    {
        modelo = CrearModeloConStock(1);

        List<Reserva> reservas = ReservasResueltasEnParalelo();

        VerificarResolucionCompetitiva(reservas, 1);
    }

    [TestMethod]
    public void VariosHilosCancelandoLaMismaReservaLaLiberanUnaVez()
    {
        Reserva reserva = modelo.RegistrarReserva(creadora, reloj);
        reserva.Resolver();
        Vehiculo unidad = reserva.VehiculoAsignado!;

        List<EjecucionDeHilo> ejecuciones = EjecutarIntentos(
            Solicitantes, _ => reserva.Cancelar());

        Assert.AreEqual(1, ejecuciones.Count(ejecucion => ejecucion.Completo));
        Assert.AreEqual(Solicitantes - 1, ejecuciones.Count(ejecucion =>
            ejecucion.Falla is Dominio.Excepciones.TransicionDeReservaInvalidaException));
        Assert.AreEqual("Cancelada", reserva.NombreDelEstado());
        Assert.IsNull(reserva.VehiculoAsignado);
        Assert.IsTrue(unidad.EstaDisponible);
        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
    }

    [TestMethod]
    public void ResolverYCancelarLaMismaPendienteSiempreDejanEstadoValido()
    {
        for (int repeticion = 0; repeticion < RepeticionesDeAtomicidad; repeticion++)
        {
            VerificarCarreraEntreResolucionYCancelacion();
        }
    }

    private void VerificarResolucionCompetitiva(
        List<Reserva> reservas, int confirmacionesEsperadas)
    {
        List<Reserva> confirmadas = reservas.FindAll(reserva => reserva.EstaConfirmada);
        List<Vehiculo> unidades = confirmadas.Select(
            reserva => reserva.VehiculoAsignado!).ToList();

        Assert.AreEqual(confirmacionesEsperadas, confirmadas.Count);
        Assert.AreEqual(Solicitantes - confirmacionesEsperadas, reservas.Count(
            reserva => reserva.NombreDelEstado() == "Rechazada"));
        Assert.AreEqual(confirmacionesEsperadas, unidades.Select(unidad => unidad.Id).Distinct().Count());
        Assert.IsTrue(unidades.TrueForAll(unidad => !unidad.EstaDisponible));
        Assert.AreEqual(confirmacionesEsperadas, modelo.UnidadesReservadas);
        Assert.AreEqual(0, modelo.UnidadesDisponibles);
    }

    [TestMethod]
    public void CancelarEnParaleloDevuelveTodasLasUnidadesAlPool()
    {
        List<Reserva> reservas = ReservasResueltasEnParalelo();
        List<Reserva> confirmadas = reservas.FindAll(reserva => reserva.EstaConfirmada);

        EjecutarEnParalelo(confirmadas.Count, indice => confirmadas[indice].Cancelar());

        Assert.AreEqual(StockInicial, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
        Assert.AreEqual(0, modelo.ReservasActivas());
    }

    [TestMethod]
    public void ModificarYReservarEnParaleloNoDejaUnaModificacionParcial()
    {
        for (int repeticion = 0; repeticion < RepeticionesDeAtomicidad; repeticion++)
        {
            VerificarCarreraEntreModificacionYReserva();
        }
    }

    private void VerificarCarreraEntreModificacionYReserva()
    {
        Modelo modeloEnDisputa = CrearModeloParaAtomicidad();
        Exception? fallaDeModificacion = null;
        Exception? fallaDeReserva = null;
        Reserva? reservaCreada = null;
        using Barrier largada = new Barrier(2);

        Thread modificacion = new Thread(() => EjecutarModificacion(
            modeloEnDisputa, largada, out fallaDeModificacion));
        Thread reserva = new Thread(() => EjecutarReserva(
            modeloEnDisputa, largada, out reservaCreada, out fallaDeReserva));

        modificacion.Start();
        reserva.Start();
        Assert.IsTrue(modificacion.Join(EsperaDeHiloEnMilisegundos),
            "La modificacion no termino: posible deadlock");
        Assert.IsTrue(reserva.Join(EsperaDeHiloEnMilisegundos),
            "La reserva no termino: posible deadlock");

        Assert.IsNull(fallaDeReserva);
        Assert.IsTrue(reservaCreada!.EstaConfirmada);
        VerificarResultadoAtomico(modeloEnDisputa, fallaDeModificacion);
    }

    private Modelo CrearModeloParaAtomicidad()
    {
        Modelo modeloCreado = new Modelo(
            new DatosDeModelo("Edison A1", "Original", 45000m, Momento, string.Empty), creadora);
        modeloCreado.AgregarUnidades(StockInicial);
        return modeloCreado;
    }

    private Modelo CrearModeloConStock(int cantidad)
    {
        Modelo modeloCreado = new Modelo(
            new DatosDeModelo("Edison S1", "Sedan electrico", 45000m,
                new DateTime(2027, 3, 1), string.Empty),
            creadora);
        modeloCreado.AgregarUnidades(cantidad);
        return modeloCreado;
    }

    private static void VerificarCarreraEntreResolucionYCancelacion()
    {
        RelojFalso reloj = new RelojFalso(Momento);
        Usuario usuario = new Usuario("jferreiro", "edison2026");
        Modelo modelo = new Modelo(
            new DatosDeModelo("Edison R1", "Sedan", 45000m, Momento, string.Empty), usuario);
        modelo.AgregarUnidades(1);
        Reserva reserva = modelo.RegistrarReserva(usuario, reloj);
        ResultadoResolucion resultado = ResultadoResolucion.SinCambios;

        EjecutarEnParalelo(2, indice =>
        {
            if (indice == 0)
            {
                resultado = reserva.Resolver();
                if (resultado == ResultadoResolucion.Confirmada)
                {
                    usuario.Registrar(Actividad.Compra(reserva.NombreDelModelo(), reloj));
                }

                return;
            }

            reserva.Cancelar();
        });

        Assert.AreEqual("Cancelada", reserva.NombreDelEstado());
        Assert.IsNull(reserva.VehiculoAsignado);
        Assert.AreEqual(1, modelo.UnidadesDisponibles);
        Assert.AreEqual(0, modelo.UnidadesReservadas);
        Assert.AreEqual(resultado == ResultadoResolucion.Confirmada ? 1 : 0,
            usuario.Actividades().Count(actividad => actividad.Tipo == TipoActividad.Compra));
    }

    private void EjecutarModificacion(
        Modelo modeloEnDisputa, Barrier largada, out Exception? falla)
    {
        falla = null;
        try
        {
            largada.SignalAndWait();
            DatosDeModelo nuevos = new DatosDeModelo(
                "Edison A1", "Modificado", 39990m, Momento.AddDays(1), string.Empty);
            modeloEnDisputa.ModificarDatos(new FichaDeModelo(nuevos, 1), creadora);
        }
        catch (Dominio.Excepciones.DominioException error)
        {
            falla = error;
        }
    }

    private void EjecutarReserva(
        Modelo modeloEnDisputa,
        Barrier largada,
        out Reserva? reservaCreada,
        out Exception? falla)
    {
        reservaCreada = null;
        falla = null;
        try
        {
            largada.SignalAndWait();
            reservaCreada = modeloEnDisputa.RegistrarReserva(creadora, reloj);
            reservaCreada.Resolver();
        }
        catch (Exception error)
        {
            falla = error;
        }
    }

    private static void VerificarResultadoAtomico(Modelo modeloEnDisputa, Exception? falla)
    {
        Assert.AreEqual(1, modeloEnDisputa.UnidadesReservadas);
        Assert.IsTrue(modeloEnDisputa.StockTotal >= modeloEnDisputa.UnidadesReservadas);

        if (falla is null)
        {
            Assert.AreEqual("Modificado", modeloEnDisputa.Descripcion);
            Assert.AreEqual(39990m, modeloEnDisputa.Precio);
            Assert.AreEqual(1, modeloEnDisputa.StockTotal);
            return;
        }

        Assert.IsInstanceOfType<Dominio.Excepciones.DominioException>(falla);
        Assert.AreEqual("Original", modeloEnDisputa.Descripcion);
        Assert.AreEqual(45000m, modeloEnDisputa.Precio);
        Assert.AreEqual(StockInicial, modeloEnDisputa.StockTotal);
    }

    private List<Reserva> ReservasResueltasEnParalelo()
    {
        Reserva[] reservas = new Reserva[Solicitantes];

        EjecutarEnParalelo(Solicitantes, indice =>
        {
            reservas[indice] = modelo.RegistrarReserva(creadora, reloj);
            reservas[indice].Resolver();
        });

        return reservas.ToList();
    }

    private static void EjecutarEnParalelo(int cantidadDeHilos, Action<int> trabajo)
    {
        List<EjecucionDeHilo> ejecuciones = EjecutarIntentos(cantidadDeHilos, trabajo);

        Assert.IsTrue(ejecuciones.All(ejecucion => ejecucion.Falla is null),
            PrimeraFalla(ejecuciones));
    }

    private static List<EjecucionDeHilo> EjecutarIntentos(
        int cantidadDeHilos, Action<int> trabajo)
    {
        using Barrier largada = new Barrier(cantidadDeHilos);
        List<EjecucionDeHilo> ejecuciones = Enumerable.Range(0, cantidadDeHilos)
            .Select(_ => new EjecucionDeHilo())
            .ToList();
        List<Thread> hilos = ejecuciones.Select((ejecucion, indice) => new Thread(
            () => ejecucion.Ejecutar(() =>
            {
                largada.SignalAndWait();
                trabajo(indice);
            }))).ToList();

        hilos.ForEach(hilo => hilo.Start());
        hilos.ForEach(hilo => Assert.IsTrue(hilo.Join(EsperaDeHiloEnMilisegundos),
            "Un Thread no termino: posible deadlock"));

        return ejecuciones;
    }

    private static string PrimeraFalla(List<EjecucionDeHilo> ejecuciones)
    {
        Exception? falla = ejecuciones.FirstOrDefault(
            ejecucion => ejecucion.Falla is not null)?.Falla;
        return falla is null ? string.Empty : $"Un hilo fallo: {falla}";
    }

    private sealed class EjecucionDeHilo
    {
        public bool Completo { get; private set; }

        public Exception? Falla { get; private set; }

        public void Ejecutar(Action trabajo)
        {
            try
            {
                trabajo();
                Completo = true;
            }
            catch (Exception falla)
            {
                Falla = falla;
            }
        }
    }
}
