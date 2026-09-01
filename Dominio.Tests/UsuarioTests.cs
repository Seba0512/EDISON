using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class UsuarioTests
{
    private const string NombreValido = "jferreiro";
    private const string ClaveValida = "edison2026";

    private static readonly DateTime Momento = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void CrearUsuarioConDatosValidosGuardaElNombre()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);

        Assert.AreEqual(NombreValido, usuario.NombreUsuario);
    }

    [TestMethod]
    public void CrearUsuarioRecortaLosEspaciosDelNombre()
    {
        Usuario usuario = new Usuario("  jferreiro  ", ClaveValida);

        Assert.AreEqual(NombreValido, usuario.NombreUsuario);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUsuarioConNombreVacioFalla()
    {
        _ = new Usuario("   ", ClaveValida);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUsuarioConNombreDemasiadoCortoFalla()
    {
        _ = new Usuario("ab", ClaveValida);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUsuarioConClaveDemasiadoCortaFalla()
    {
        _ = new Usuario(NombreValido, "123");
    }

    [TestMethod]
    public void SeLlamaIgnoraMayusculasYEspacios()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);

        Assert.IsTrue(usuario.SeLlama("  JFerreiro "));
    }

    [TestMethod]
    public void SeLlamaEsFalsoParaOtroNombre()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);

        Assert.IsFalse(usuario.SeLlama("otra"));
    }

    [TestMethod]
    public void VerificarClaveAceptaLaClaveCorrecta()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);

        Assert.IsTrue(usuario.VerificarClave(ClaveValida));
    }

    [TestMethod]
    public void VerificarClaveDistingueMayusculas()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);

        Assert.IsFalse(usuario.VerificarClave("EDISON2026"));
    }

    [TestMethod]
    public void VerificarClaveRechazaLaClaveVacia()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);

        Assert.IsFalse(usuario.VerificarClave(string.Empty));
    }

    [TestMethod]
    public void ElHistorialDeUnUsuarioNuevoEstaVacio()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);

        Assert.AreEqual(0, usuario.Actividades().Count);
    }

    [TestMethod]
    public void RegistrarGuardaLaActividadEnSuHistorial()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);
        RelojFalso reloj = new RelojFalso(Momento);

        usuario.Registrar(Actividad.InicioSesion(usuario.NombreUsuario, reloj));

        Assert.AreEqual(1, usuario.Actividades().Count);
        Assert.AreEqual(TipoActividad.InicioSesion, usuario.Actividades()[0].Tipo);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void RegistrarUnaActividadDeOtroUsuarioFalla()
    {
        Usuario usuario = new Usuario(NombreValido, ClaveValida);
        RelojFalso reloj = new RelojFalso(Momento);

        usuario.Registrar(Actividad.InicioSesion("sgomez", reloj));
    }
}
