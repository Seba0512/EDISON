using Dominio.Excepciones;

namespace Dominio.Estados;

/// <summary>
/// State (GoF) del ciclo de vida de una reserva (CR5, CR6). Cada transicion devuelve el estado
/// siguiente; la base rechaza todo con <see cref="TransicionDeReservaInvalidaException"/> y cada
/// subclase sobrescribe unicamente lo que permite. El contrato es uniforme (LSP): ningun llamador
/// pregunta por el tipo del estado ni hace un switch, solo invoca y el estado decide.
///
/// <see cref="Cancelar"/> recibe el modelo porque <see cref="Confirmada"/> es quien sabe que una
/// reserva confirmada tiene una unidad tomada que devolver: ese conocimiento vive en un solo lugar.
///
/// Los estados no tienen estado interno propio, por eso cada uno es una instancia unica via
/// <c>Lazy&lt;T&gt;</c>: es el unico Singleton del trabajo y es el caso legitimo, objetos inmutables
/// y thread-safe por construccion.
/// </summary>
public abstract class EstadoDeReserva
{
    public abstract string Nombre { get; }

    /// <summary>Pendiente o confirmada: lo que CR3 y CR4 cuentan como reserva activa.</summary>
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

    public virtual EstadoDeReserva Cancelar(Modelo modelo)
    {
        throw new TransicionDeReservaInvalidaException("cancelar", Nombre);
    }
}
