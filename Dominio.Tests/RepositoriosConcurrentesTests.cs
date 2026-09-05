using Dominio;
using Dominio.Excepciones;
using Dominio.Tests.Dobles;

namespace Dominio.Tests;

[TestClass]
public class RepositoriosConcurrentesTests
{
    private const string Clave = "edison2026";
    private const int CantidadDeAltas = 200;

    private static readonly DateTime Momento =
        new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void RegistrarElMismoUsuarioEnParaleloTieneUnSoloExito()
    {
        RepositorioDeUsuariosEnMemoria repositorio = new RepositorioDeUsuariosEnMemoria();
        RegistroDeUsuarios registro = new RegistroDeUsuarios(repositorio, new RelojFalso(Momento));

        var resultado = EjecutarDosVeces(
            () => registro.Registrar("jferreiro", Clave));

        Assert.AreEqual(1, resultado.Exitos.Count);
        Assert.AreEqual(1, resultado.Fallas.Count);
        Assert.IsInstanceOfType<UsuarioDuplicadoException>(resultado.Fallas[0]);
        Assert.AreSame(resultado.Exitos[0], repositorio.ObtenerPorNombre("JFERREIRO"));
    }

    [TestMethod]
    public void CrearElMismoModeloEnParaleloTieneUnSoloExito()
    {
        RepositorioDeModelosEnMemoria repositorio = new RepositorioDeModelosEnMemoria();
        Usuario creadora = new Usuario("jferreiro", Clave);
        CatalogoDeModelos catalogo = CrearCatalogo(repositorio);

        var resultado = EjecutarDosVeces(() =>
        {
            catalogo.AltaDeModelo(Ficha("Edison S1"), creadora);
            return catalogo.Consultar("Edison S1");
        });

        Assert.AreEqual(1, resultado.Exitos.Count);
        Assert.AreEqual(1, resultado.Fallas.Count);
        Assert.IsInstanceOfType<DominioException>(resultado.Fallas[0]);
        Assert.AreEqual(1, repositorio.Todos().Count);
    }

    [TestMethod]
    public void AgregarLaMismaReservaEnParaleloTieneUnSoloExito()
    {
        RepositorioDeReservasEnMemoria repositorio = new RepositorioDeReservasEnMemoria();
        Usuario usuario = new Usuario("jferreiro", Clave);
        Modelo modelo = CrearModelo("Edison S1", usuario);
        Reserva reserva = modelo.RegistrarReserva(usuario, new RelojFalso(Momento));

        var resultado = EjecutarDosVeces(() =>
        {
            repositorio.Agregar(reserva);
            return reserva;
        });

        Assert.AreEqual(1, resultado.Exitos.Count);
        Assert.AreEqual(1, resultado.Fallas.Count);
        Assert.AreSame(reserva, repositorio.ObtenerPorId(reserva.Id));
    }

    [TestMethod]
    public void ConsultarModelosMientrasSeAgreganEsSeguro()
    {
        RepositorioDeModelosEnMemoria repositorio = new RepositorioDeModelosEnMemoria();
        Usuario creadora = new Usuario("jferreiro", Clave);

        EjecutarEnParalelo(
            () => AgregarModelos(repositorio, creadora),
            () => ConsultarModelos(repositorio));

        Assert.AreEqual(CantidadDeAltas, repositorio.Todos().Count);
    }

    [TestMethod]
    public void ConsultarReservasMientrasSeAgreganEsSeguro()
    {
        RepositorioDeReservasEnMemoria repositorio = new RepositorioDeReservasEnMemoria();
        Usuario usuario = new Usuario("jferreiro", Clave);
        Modelo modelo = CrearModelo("Edison S1", usuario);
        RelojFalso reloj = new RelojFalso(Momento);

        EjecutarEnParalelo(
            () => AgregarReservas(repositorio, modelo, usuario, reloj),
            () => ConsultarReservas(repositorio));

        Assert.AreEqual(CantidadDeAltas, repositorio.Pendientes().Count);
    }

    private static CatalogoDeModelos CrearCatalogo(RepositorioDeModelosEnMemoria repositorio)
    {
        return new CatalogoDeModelos(
            repositorio, new Dobles.AlmacenDeImagenesFalso(), new RelojFalso(Momento));
    }

    private static FichaDeModelo Ficha(string nombre)
    {
        DatosDeModelo datos = new DatosDeModelo(
            nombre, "Sedan electrico", 45000m, new DateTime(2027, 3, 1), string.Empty);
        return new FichaDeModelo(datos, 2);
    }

    private static Modelo CrearModelo(string nombre, Usuario creadora)
    {
        Modelo modelo = new Modelo(Ficha(nombre).Datos, creadora);
        modelo.AgregarUnidades(CantidadDeAltas);
        return modelo;
    }

    private static void AgregarModelos(
        RepositorioDeModelosEnMemoria repositorio, Usuario creadora)
    {
        for (int indice = 0; indice < CantidadDeAltas; indice++)
        {
            repositorio.Agregar(CrearModelo($"Edison {indice}", creadora));
        }
    }

    private static void ConsultarModelos(RepositorioDeModelosEnMemoria repositorio)
    {
        for (int consulta = 0; consulta < CantidadDeAltas; consulta++)
        {
            _ = repositorio.Todos();
            _ = repositorio.Existe($"Edison {consulta}");
        }
    }

    private static void AgregarReservas(
        RepositorioDeReservasEnMemoria repositorio,
        Modelo modelo,
        Usuario usuario,
        RelojFalso reloj)
    {
        for (int indice = 0; indice < CantidadDeAltas; indice++)
        {
            repositorio.Agregar(modelo.RegistrarReserva(usuario, reloj));
        }
    }

    private static void ConsultarReservas(RepositorioDeReservasEnMemoria repositorio)
    {
        for (int consulta = 0; consulta < CantidadDeAltas; consulta++)
        {
            _ = repositorio.Pendientes();
            _ = repositorio.DeUsuario("jferreiro");
        }
    }

    private static (List<T> Exitos, List<Exception> Fallas) EjecutarDosVeces<T>(Func<T> accion)
        where T : class
    {
        T?[] exitos = new T?[2];
        Exception?[] fallas = new Exception?[2];

        EjecutarEnParalelo(
            () => EjecutarCapturando(accion, exitos, fallas, 0),
            () => EjecutarCapturando(accion, exitos, fallas, 1));

        return (exitos.Where(exito => exito is not null).Cast<T>().ToList(),
            fallas.Where(falla => falla is not null).Cast<Exception>().ToList());
    }

    private static void EjecutarCapturando<T>(
        Func<T> accion, T?[] exitos, Exception?[] fallas, int indice)
        where T : class
    {
        try
        {
            exitos[indice] = accion();
        }
        catch (Exception falla)
        {
            fallas[indice] = falla;
        }
    }

    private static void EjecutarEnParalelo(Action primera, Action segunda)
    {
        using Barrier largada = new Barrier(2);
        Exception?[] fallas = new Exception?[2];
        Thread primerHilo = CrearHilo(primera, largada, fallas, 0);
        Thread segundoHilo = CrearHilo(segunda, largada, fallas, 1);

        primerHilo.Start();
        segundoHilo.Start();
        primerHilo.Join();
        segundoHilo.Join();

        Assert.IsNull(fallas[0], fallas[0]?.ToString());
        Assert.IsNull(fallas[1], fallas[1]?.ToString());
    }

    private static Thread CrearHilo(
        Action accion, Barrier largada, Exception?[] fallas, int indice)
    {
        return new Thread(() =>
        {
            try
            {
                largada.SignalAndWait();
                accion();
            }
            catch (Exception falla)
            {
                fallas[indice] = falla;
            }
        });
    }
}
