using Dominio.Excepciones;

namespace Dominio.Estados;

// Los estados no guardan datos, por eso alcanza con una sola instancia compartida entre hilos.
public abstract class EstadoDeReserva
{
    public abstract string Nombre { get; }

    public virtual bool EsActiva => false;

    public virtual bool EstaPendiente => false;

    public virtual bool EstaConfirmada => false;

    public virtual EstadoDeReserva Confirmar()
    {
        throw new TransicionDeReservaInvalidaException("confirmar", Nombre);
    }

    public virtual EstadoDeReserva Rechazar()
    {
        throw new TransicionDeReservaInvalidaException("rechazar", Nombre);
    }

    public virtual EstadoDeReserva Cancelar()
    {
        throw new TransicionDeReservaInvalidaException("cancelar", Nombre);
    }
}
