using System.Reflection;
using System.Text;
using Dominio;
using Dominio.Excepciones;

namespace Dominio.Tests;

/// <summary>
/// La credencial no expone su resumen ni su salt, asi que lo que se prueba es su contrato: acepta la
/// clave correcta, rechaza cualquier otra y no guarda la clave en texto plano.
/// </summary>
[TestClass]
public class CredencialTests
{
    private const string Clave = "clave-secreta";

    [TestMethod]
    public void VerificaAceptaLaClaveConLaQueSeCreo()
    {
        Credencial credencial = new Credencial(Clave);

        Assert.IsTrue(credencial.Verifica(Clave));
    }

    [TestMethod]
    public void VerificaRechazaOtraClave()
    {
        Credencial credencial = new Credencial(Clave);

        Assert.IsFalse(credencial.Verifica("otra-clave"));
    }

    [TestMethod]
    public void VerificaDistingueMayusculas()
    {
        Credencial credencial = new Credencial(Clave);

        Assert.IsFalse(credencial.Verifica(Clave.ToUpperInvariant()));
    }

    /// <summary>Una clave vacia se rechaza sin lanzar: no es un error, es una clave incorrecta.</summary>
    [TestMethod]
    public void VerificaRechazaLaClaveVaciaSinLanzar()
    {
        Credencial credencial = new Credencial(Clave);

        Assert.IsFalse(credencial.Verifica(string.Empty));
        Assert.IsFalse(credencial.Verifica(null!));
    }

    [TestMethod]
    public void DosCredencialesConLaMismaClaveSeVerificanPorSeparado()
    {
        Credencial deUnUsuario = new Credencial(Clave);
        Credencial deOtroUsuario = new Credencial(Clave);

        Assert.IsTrue(deUnUsuario.Verifica(Clave));
        Assert.IsTrue(deOtroUsuario.Verifica(Clave));
    }

    [TestMethod]
    [ExpectedException(typeof(DominioException))]
    public void UnaClaveMasCortaQueElMinimoFalla()
    {
        Credencial credencial = new Credencial("abc");
    }

    /// <summary>
    /// Prueba de regresion del requisito que motiva a esta clase: ningun campo puede contener la
    /// clave en texto plano. Mira los campos por reflexion porque el contrato publico, justamente,
    /// no deja ver como se guarda el secreto.
    /// </summary>
    [TestMethod]
    public void LaClaveNoQuedaGuardadaEnTextoPlano()
    {
        Credencial credencial = new Credencial(Clave);
        byte[] claveEnBytes = Encoding.UTF8.GetBytes(Clave);

        foreach (FieldInfo campo in CamposDeInstanciaDe(credencial))
        {
            object? valor = campo.GetValue(credencial);

            Assert.AreNotEqual(Clave, valor as string, $"El campo {campo.Name} guarda la clave");
            Assert.IsFalse(
                valor is byte[] bytes && bytes.AsSpan().IndexOf(claveEnBytes) >= 0,
                $"El campo {campo.Name} contiene los bytes de la clave");
        }
    }

    private static FieldInfo[] CamposDeInstanciaDe(Credencial credencial)
    {
        return credencial.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }
}
