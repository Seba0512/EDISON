using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class UsuarioTests
{
    private const string NombreValido = "jferreiro";
    private const string PasswordValida = "edison2026";

    [TestMethod]
    public void CrearUsuarioConDatosValidosGuardaElNombre()
    {
        Usuario usuario = new Usuario(NombreValido, PasswordValida);

        Assert.AreEqual(NombreValido, usuario.NombreUsuario);
    }

    [TestMethod]
    public void CrearUsuarioRecortaLosEspaciosDelNombre()
    {
        Usuario usuario = new Usuario("  jferreiro  ", PasswordValida);

        Assert.AreEqual(NombreValido, usuario.NombreUsuario);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUsuarioConNombreVacioFalla()
    {
        _ = new Usuario("   ", PasswordValida);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUsuarioConNombreDemasiadoCortoFalla()
    {
        _ = new Usuario("ab", PasswordValida);
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void CrearUsuarioConPasswordDemasiadoCortaFalla()
    {
        _ = new Usuario(NombreValido, "123");
    }

    [TestMethod]
    public void SeLlamaIgnoraMayusculasYEspacios()
    {
        Usuario usuario = new Usuario(NombreValido, PasswordValida);

        Assert.IsTrue(usuario.SeLlama("  JFerreiro "));
    }

    [TestMethod]
    public void SeLlamaEsFalsoParaOtroNombre()
    {
        Usuario usuario = new Usuario(NombreValido, PasswordValida);

        Assert.IsFalse(usuario.SeLlama("otro"));
    }

    [TestMethod]
    public void CoincideLaPasswordEsVerdaderoConLaPasswordCorrecta()
    {
        Usuario usuario = new Usuario(NombreValido, PasswordValida);

        Assert.IsTrue(usuario.CoincideLaPassword(PasswordValida));
    }

    [TestMethod]
    public void CoincideLaPasswordDistingueMayusculas()
    {
        Usuario usuario = new Usuario(NombreValido, PasswordValida);

        Assert.IsFalse(usuario.CoincideLaPassword("EDISON2026"));
    }
}
