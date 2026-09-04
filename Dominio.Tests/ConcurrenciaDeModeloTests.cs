using Dominio;

namespace Dominio.Tests;

/// <summary>
/// El modelo es el limite de sincronizacion del agregado (SR2): varias conexiones, cada una en su
/// propio thread, compiten por las mismas unidades. Estas pruebas usan <see cref="Thread"/> explicito
/// como exige la letra, y arrancan a todos los hilos de golpe con una barrera para maximizar la
/// probabilidad de que se pisen.
/// </summary>
[TestClass]
public class ConcurrenciaDeModeloTests
{
    private const int Solicitantes = 20;
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
        modelo = new Modelo(
            new DatosDeModelo("Edison S1", "Sedan electrico", 45000m, new DateTime(2027, 3, 1), string.Empty),
            creadora);
        modelo.AgregarUnidades(StockInicial);
    }

    /// <summary>
    /// La carrera de CR5: mas solicitudes que unidades. Al resolverse todas deben quedar exactamente
    /// <see cref="StockInicial"/> confirmadas y el resto rechazadas, sin que se entregue dos veces la
    /// misma unidad ni sobre ninguna.
    /// </summary>
    [TestMethod]
    public void VariosHilosCompitiendoPorElStockNoLoSobrevenden()
    {
        List<Reserva> reservas = ReservasResueltasEnParalelo();

        int confirmadas = reservas.Count(reserva => reserva.EstaConfirmada);

        Assert.AreEqual(StockInicial, confirmadas);
        Assert.AreEqual(Solicitantes - StockInicial, reservas.Count(reserva => !reserva.EstaActiva));
        Assert.AreEqual(StockInicial, modelo.UnidadesReservadas);
        Assert.AreEqual(0, modelo.UnidadesDisponibles);
    }

    /// <summary>
    /// Cancelar en paralelo las reservas confirmadas devuelve todas las unidades al pool (CR6) y
    /// deja el modelo sin reservas activas, sin perder ni duplicar unidades.
    /// </summary>
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

    /// <summary>Ninguna excepcion se escapa de los hilos: el agregado nunca queda inconsistente.</summary>
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
        using Barrier largada = new Barrier(cantidadDeHilos);
        List<Exception> fallas = new();
        object candadoDeFallas = new();
        List<Thread> hilos = new();

        for (int indice = 0; indice < cantidadDeHilos; indice++)
        {
            int propio = indice;
            Thread hilo = new Thread(() =>
            {
                largada.SignalAndWait();
                try
                {
                    trabajo(propio);
                }
                catch (Exception falla)
                {
                    lock (candadoDeFallas)
                    {
                        fallas.Add(falla);
                    }
                }
            });

            hilos.Add(hilo);
            hilo.Start();
        }

        hilos.ForEach(hilo => hilo.Join());

        Assert.AreEqual(0, fallas.Count, PrimeraFalla(fallas));
    }

    private static string PrimeraFalla(List<Exception> fallas)
    {
        return fallas.Count == 0 ? string.Empty : $"Un hilo fallo: {fallas[0]}";
    }
}
