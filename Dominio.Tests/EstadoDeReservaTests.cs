using Dominio.Estados;
using Dominio.Excepciones;

namespace Dominio.Tests;

[TestClass]
public class EstadoDeReservaTests
{
    [TestMethod]
    public void PendienteDecideSusTresTransicionesPermitidas()
    {
        Assert.AreSame(Confirmada.Instancia, Pendiente.Instancia.Confirmar());
        Assert.AreSame(Rechazada.Instancia, Pendiente.Instancia.Rechazar());
        Assert.AreSame(Cancelada.Instancia, Pendiente.Instancia.Cancelar());
    }

    [TestMethod]
    public void ConfirmadaSoloPermiteCancelar()
    {
        Assert.AreSame(Cancelada.Instancia, Confirmada.Instancia.Cancelar());
        Assert.ThrowsException<TransicionDeReservaInvalidaException>(
            () => Confirmada.Instancia.Confirmar());
        Assert.ThrowsException<TransicionDeReservaInvalidaException>(
            () => Confirmada.Instancia.Rechazar());
    }

    [TestMethod]
    public void RechazadaRechazaTodasLasTransiciones()
    {
        Assert.ThrowsException<TransicionDeReservaInvalidaException>(
            () => Rechazada.Instancia.Confirmar());
        Assert.ThrowsException<TransicionDeReservaInvalidaException>(
            () => Rechazada.Instancia.Rechazar());
        Assert.ThrowsException<TransicionDeReservaInvalidaException>(
            () => Rechazada.Instancia.Cancelar());
    }

    [TestMethod]
    public void CanceladaRechazaTodasLasTransiciones()
    {
        Assert.ThrowsException<TransicionDeReservaInvalidaException>(
            () => Cancelada.Instancia.Confirmar());
        Assert.ThrowsException<TransicionDeReservaInvalidaException>(
            () => Cancelada.Instancia.Rechazar());
        Assert.ThrowsException<TransicionDeReservaInvalidaException>(
            () => Cancelada.Instancia.Cancelar());
    }
}
