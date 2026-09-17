using Dominio;
using Dominio.Tiempo;

namespace Dominio.Tests;

[TestClass]
public class LecturasConcurrentesTests
{
    private const int Repeticiones = 500;
    private const int EsperaDeHiloEnMilisegundos = 5000;

    private static readonly DateTime Momento =
        new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void LecturaDeModeloMantieneLaInvarianteDeStockDuranteCambios()
    {
        Usuario usuario = new Usuario("jferreiro", "edison2026");
        RelojFalso reloj = new RelojFalso(Momento);
        Modelo modelo = CrearModelo(usuario);
        bool termino = false;
        Exception? falla = null;

        Thread cambios = new Thread(() => CambiarStock(modelo, usuario, reloj, ref termino));
        Thread consultas = new Thread(() => ConsultarStock(modelo, ref termino, ref falla));

        cambios.Start();
        consultas.Start();
        ExigirQueTermine(cambios);
        ExigirQueTermine(consultas);

        Assert.IsNull(falla, falla?.ToString());
    }

    [TestMethod]
    public void LecturaDeReservaEsperaLaTransicionCompleta()
    {
        Usuario usuario = new Usuario("jferreiro", "edison2026");
        RelojBloqueante reloj = new RelojBloqueante(Momento);
        Modelo modelo = CrearModelo(usuario);
        Reserva reserva = modelo.RegistrarReserva(usuario, reloj);
        LecturaDeReserva? lectura = null;

        Thread resolucion = new Thread(() => reserva.Resolver());
        resolucion.Start();
        reloj.EsperarTransicionIniciada();
        Thread consulta = new Thread(() => lectura = reserva.TomarLectura());
        consulta.Start();

        reloj.PermitirActualizacion();
        ExigirQueTermine(resolucion);
        ExigirQueTermine(consulta);

        Assert.AreEqual("Confirmada", lectura!.Estado);
        Assert.AreEqual(Momento.AddTicks(1), lectura.UltimaActualizacion);
    }

    private static Modelo CrearModelo(Usuario creador)
    {
        DatosDeModelo datos = new DatosDeModelo(
            "Edison S1", "Sedan electrico", 45000m, Momento.AddMonths(6), string.Empty);
        Modelo modelo = new Modelo(datos, creador);
        modelo.AgregarUnidades(1);
        return modelo;
    }

    private static void CambiarStock(
        Modelo modelo, Usuario usuario, IReloj reloj, ref bool termino)
    {
        for (int repeticion = 0; repeticion < Repeticiones; repeticion++)
        {
            Reserva reserva = modelo.RegistrarReserva(usuario, reloj);
            reserva.Resolver();
            reserva.Cancelar();
        }

        Volatile.Write(ref termino, true);
    }

    private static void ConsultarStock(
        Modelo modelo, ref bool termino, ref Exception? falla)
    {
        try
        {
            do
            {
                LecturaDeModelo lectura = modelo.TomarLectura();
                Assert.AreEqual(
                    lectura.StockTotal,
                    lectura.UnidadesDisponibles + lectura.UnidadesReservadas);
            } while (!Volatile.Read(ref termino));
        }
        catch (Exception error)
        {
            falla = error;
        }
    }

    private static void ExigirQueTermine(Thread hilo)
    {
        Assert.IsTrue(hilo.Join(EsperaDeHiloEnMilisegundos),
            "El Thread no termino: posible deadlock");
    }

    private sealed class RelojBloqueante : IReloj
    {
        private readonly DateTime inicio;
        private readonly ManualResetEventSlim transicionIniciada = new(false);
        private readonly ManualResetEventSlim continuar = new(false);
        private int lecturas;

        public RelojBloqueante(DateTime inicio)
        {
            this.inicio = inicio;
        }

        public DateTime Ahora
        {
            get
            {
                if (Interlocked.Increment(ref lecturas) == 2)
                {
                    transicionIniciada.Set();
                    continuar.Wait(EsperaDeHiloEnMilisegundos);
                }

                return inicio.AddTicks(lecturas - 1);
            }
        }

        public void EsperarTransicionIniciada()
        {
            Assert.IsTrue(transicionIniciada.Wait(EsperaDeHiloEnMilisegundos),
                "La transicion no alcanzo la actualizacion del reloj");
        }

        public void PermitirActualizacion()
        {
            continuar.Set();
        }
    }
}
