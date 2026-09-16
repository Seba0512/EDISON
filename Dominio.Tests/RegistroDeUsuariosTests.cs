using Dominio;
using Dominio.Excepciones;
using Dominio.Tests.Dobles;

namespace Dominio.Tests;

[TestClass]
public class RegistroDeUsuariosTests
{
    private const string Nombre = "jferreiro";
    private const string Clave = "edison2026";

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    private RelojFalso reloj = null!;
    private RegistroDeUsuarios registro = null!;

    [TestInitialize]
    public void Inicializar()
    {
        reloj = new RelojFalso(Momento);
        registro = new RegistroDeUsuarios(new RepositorioDeUsuariosEnMemoria(), reloj);
    }

    [TestMethod]
    public void RegistrarDevuelveElUsuarioCreado()
    {
        Usuario usuario = registro.Registrar(Nombre, Clave);

        Assert.AreEqual(Nombre, usuario.NombreUsuario);
    }

    [TestMethod]
    [ExpectedException(typeof(UsuarioDuplicadoException))]
    public void RegistrarDosVecesElMismoNombreFalla()
    {
        registro.Registrar(Nombre, Clave);

        registro.Registrar(Nombre, "otra-clave");
    }

    [TestMethod]
    [ExpectedException(typeof(UsuarioDuplicadoException))]
    public void ElNombreDeUsuarioEsUnicoSinImportarMayusculas()
    {
        registro.Registrar(Nombre, Clave);

        registro.Registrar("JFerreiro", Clave);
    }

    [TestMethod]
    public void IniciarSesionDevuelveElUsuarioYLoRegistraEnSuHistorial()
    {
        registro.Registrar(Nombre, Clave);

        Usuario usuario = registro.IniciarSesion(Nombre, Clave);

        Assert.AreEqual(Nombre, usuario.NombreUsuario);
        Assert.AreEqual(TipoActividad.InicioSesion, usuario.Actividades()[0].Tipo);
    }

    [TestMethod]
    [ExpectedException(typeof(CredencialesInvalidasException))]
    public void IniciarSesionConClaveIncorrectaFalla()
    {
        registro.Registrar(Nombre, Clave);

        registro.IniciarSesion(Nombre, "clave-incorrecta");
    }

    [TestMethod]
    [ExpectedException(typeof(CredencialesInvalidasException))]
    public void IniciarSesionConUsuarioInexistenteDaElMismoError()
    {
        registro.IniciarSesion("nadie", Clave);
    }

    [TestMethod]
    public void CerrarSesionQuedaRegistradoEnElHistorial()
    {
        Usuario usuario = registro.Registrar(Nombre, Clave);

        registro.CerrarSesion(usuario);

        Assert.AreEqual(TipoActividad.CierreSesion, registro.HistorialDe(usuario)[0].Tipo);
    }

    [TestMethod]
    public void ElHistorialArrancaVacio()
    {
        Usuario usuario = registro.Registrar(Nombre, Clave);

        Assert.AreEqual(0, registro.HistorialDe(usuario).Count);
    }
}
