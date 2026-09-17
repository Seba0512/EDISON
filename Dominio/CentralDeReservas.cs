using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

public class CentralDeReservas : IGestionDeReservas, IResolucionDeReservas
{
    private readonly IRepositorioDeReservas _reservas;
    private readonly IRepositorioDeModelos _modelos;
    private readonly IRepositorioDeUsuarios _usuarios;
    private readonly IReloj _reloj;
    private readonly int _segundosDeAprobacion;

    public CentralDeReservas(
        IRepositorioDeReservas reservas,
        IRepositorioDeModelos modelos,
        IRepositorioDeUsuarios usuarios,
        IReloj reloj,
        int segundosDeAprobacion)
    {
        _reservas = reservas;
        _modelos = modelos;
        _usuarios = usuarios;
        _reloj = reloj;
        _segundosDeAprobacion = segundosDeAprobacion;
    }

    public Guid SolicitarReserva(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = _modelos.ObtenerPorNombre(nombreModelo);
        Reserva reserva = modelo.RegistrarReserva(solicitante, _reloj);
        _reservas.Agregar(reserva);

        solicitante.Registrar(Actividad.ReservaCreada(modelo.Nombre, _reloj));

        return reserva.Id;
    }

    public void CancelarReserva(Guid idReserva, Usuario solicitante)
    {
        Reserva reserva = _reservas.ObtenerPorId(idReserva);
        ExigirQueSeaSuya(reserva, solicitante);

        reserva.Cancelar();

        solicitante.Registrar(Actividad.ReservaCancelada(reserva.NombreDelModelo(), _reloj));
    }

    public IReadOnlyList<LecturaDeReserva> ConsultarReservasDe(Usuario usuario)
    {
        return _reservas.DeUsuario(usuario.NombreUsuario)
            .Select(reserva => reserva.TomarLectura())
            .ToList();
    }

    public void ResolverVencidas()
    {
        // Se resuelven en orden de llegada. Con poco stock gana la solicitud más vieja.
        List<Reserva> vencidas = _reservas.Pendientes()
            .Where(reserva => reserva.VencioElPlazo(_segundosDeAprobacion))
            .OrderBy(reserva => reserva.FechaCreacion)
            .ToList();

        vencidas.ForEach(Resolver);
    }

    private void Resolver(Reserva reserva)
    {
        ResultadoResolucion resultado = reserva.Resolver();
        // Solo una confirmación cuenta como compra en el historial. Un rechazo no registra nada.
        if (resultado == ResultadoResolucion.Confirmada)
        {
            RegistrarLaCompra(reserva);
        }
    }

    private void RegistrarLaCompra(Reserva reserva)
    {
        Usuario comprador = _usuarios.ObtenerPorNombre(reserva.NombreDelUsuario());
        comprador.Registrar(Actividad.Compra(reserva.NombreDelModelo(), _reloj));
    }

    private static void ExigirQueSeaSuya(Reserva reserva, Usuario solicitante)
    {
        if (!reserva.FueHechaPor(solicitante.NombreUsuario))
        {
            throw new DominioException("Solo se pueden cancelar las reservas propias");
        }
    }
}
