using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

/// <summary>
/// Solicitud, cancelacion, consulta y resolucion diferida de reservas (CR5, CR6, CR9).
///
/// Responsabilidad en una frase, sin "y": administrar el ciclo de vida de las reservas. Igual que el
/// catalogo, orquesta pero no decide: confirmar o rechazar lo resuelve el <see cref="Modelo"/>, que
/// es quien tiene las unidades, y si una reserva puede cancelarse lo resuelve su propio estado.
///
/// El plazo de aprobacion y el reloj son estado propio, no parametros: quien llama a
/// <see cref="ResolverVencidas"/> es un thread que solo golpea cada tanto y no tiene por que conocer
/// ninguna regla de negocio.
///
/// El constructor recibe cinco dependencias, mas de las dos que admite el criterio de diseño del
/// curso. La excepcion esta acotada a la inyeccion de colaboradores: agruparlos en un objeto
/// parametro solo escondería cuantos son, y el criterio apunta a los metodos de comportamiento, que
/// aca reciben dos o menos.
/// </summary>
public class CentralDeReservas : IGestionDeReservas, IResolucionDeReservas
{
    private readonly IRepositorioDeReservas reservas;
    private readonly IRepositorioDeModelos modelos;
    private readonly IRepositorioDeUsuarios usuarios;
    private readonly IReloj reloj;
    private readonly int segundosDeAprobacion;

    public CentralDeReservas(
        IRepositorioDeReservas reservas,
        IRepositorioDeModelos modelos,
        IRepositorioDeUsuarios usuarios,
        IReloj reloj,
        int segundosDeAprobacion)
    {
        this.reservas = reservas;
        this.modelos = modelos;
        this.usuarios = usuarios;
        this.reloj = reloj;
        this.segundosDeAprobacion = segundosDeAprobacion;
    }

    /// <summary>
    /// Alta de una solicitud (CR5). Nace pendiente y no toma ninguna unidad todavia: la toma recien
    /// al confirmarse, que es lo que hace que varias solicitudes compitan por el mismo stock.
    /// </summary>
    public Guid SolicitarReserva(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = modelos.ObtenerPorNombre(nombreModelo);
        Reserva reserva = modelo.RegistrarReserva(solicitante, reloj);
        reservas.Agregar(reserva);

        solicitante.Registrar(Actividad.ReservaCreada(modelo.Nombre, reloj));

        return reserva.Id;
    }

    /// <summary>
    /// Cancelacion de CR6. La reserva delega en su modelo, que la cancela bajo su candado y, si
    /// estaba confirmada, devuelve la unidad al pool.
    /// </summary>
    public void CancelarReserva(Guid idReserva, Usuario solicitante)
    {
        Reserva reserva = reservas.ObtenerPorId(idReserva);
        ExigirQueSeaSuya(reserva, solicitante);

        reserva.Cancelar();

        solicitante.Registrar(Actividad.ReservaCancelada(reserva.NombreDelModelo(), reloj));
    }

    public IReadOnlyList<Reserva> ReservasDe(Usuario usuario)
    {
        return reservas.DeUsuario(usuario.NombreUsuario);
    }

    /// <summary>
    /// Resolucion diferida de CR5. Toma la foto de las pendientes del repositorio, la suelta y recien
    /// entonces opera modelo por modelo: nunca se toca un repositorio teniendo el candado de un
    /// modelo, que es la regla que evita los deadlocks.
    ///
    /// Se resuelven en orden de creacion (FIFO) para que, cuando varias solicitudes compiten por el
    /// mismo stock, gane la que llego primero y no la que el planificador haya despertado antes.
    /// </summary>
    public void ResolverVencidas()
    {
        List<Reserva> vencidas = reservas.Pendientes()
            .Where(reserva => reserva.VencioElPlazo(segundosDeAprobacion))
            .OrderBy(reserva => reserva.FechaCreacion)
            .ToList();

        vencidas.ForEach(Resolver);
    }

    private void Resolver(Reserva reserva)
    {
        ResultadoResolucion resultado = reserva.Resolver();
        if (resultado == ResultadoResolucion.Confirmada)
        {
            RegistrarLaCompra(reserva);
        }
    }

    /// <summary>Una reserva confirmada es la compra que pide registrar CR10.</summary>
    private void RegistrarLaCompra(Reserva reserva)
    {
        Usuario comprador = usuarios.ObtenerPorNombre(reserva.NombreDelUsuario());
        comprador.Registrar(Actividad.Compra(reserva.NombreDelModelo(), reloj));
    }

    private static void ExigirQueSeaSuya(Reserva reserva, Usuario solicitante)
    {
        if (!reserva.FueHechaPor(solicitante.NombreUsuario))
        {
            throw new DominioException("Solo se pueden cancelar las reservas propias");
        }
    }
}
