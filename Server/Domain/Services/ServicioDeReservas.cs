using Server.Domain.Entities;
using Server.Domain.Enums;
using Server.Domain.Exceptions;
using Server.Domain.Repositories;

namespace Server.Domain.Services;

// Reglas que cruzan entidades: una reserva vincula a un usuario con un modelo, y su resolución
// termina en el historial del comprador.
public class ServicioDeReservas
{
    private readonly RepositoriosDelDominio _repositorios;
    private readonly IReloj _reloj;
    private readonly int _segundosDeAprobacion;

    public ServicioDeReservas(
        RepositoriosDelDominio repositorios, IReloj reloj, int segundosDeAprobacion)
    {
        _repositorios = repositorios;
        _reloj = reloj;
        _segundosDeAprobacion = segundosDeAprobacion;
    }

    public Guid Solicitar(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = _repositorios.Modelos.Obtener(nombreModelo);
        Reserva reserva = modelo.RegistrarReserva(solicitante, _reloj);
        _repositorios.Reservas.Agregar(reserva);
        solicitante.Registrar(Actividad.ReservaCreada(modelo.Nombre, _reloj));
        return reserva.Id;
    }

    public void Cancelar(Guid idReserva, Usuario solicitante)
    {
        Reserva reserva = _repositorios.Reservas.Obtener(idReserva);
        if (!reserva.FueHechaPor(solicitante.NombreUsuario))
        {
            throw new DominioException("Solo se pueden cancelar las reservas propias");
        }

        reserva.Cancelar();
        solicitante.Registrar(Actividad.ReservaCancelada(reserva.NombreDelModelo(), _reloj));
    }

    public IReadOnlyList<LecturaDeReserva> ConsultarDe(Usuario usuario)
    {
        return _repositorios.Reservas.Todas()
            .Where(reserva => reserva.FueHechaPor(usuario.NombreUsuario))
            .Select(reserva => reserva.TomarLectura())
            .ToList();
    }

    public void ResolverVencidas()
    {
        // Se resuelven en orden de llegada. Con poco stock gana la solicitud más vieja.
        List<Reserva> vencidas = _repositorios.Reservas.Todas()
            .Where(reserva => reserva.VencioElPlazo(_segundosDeAprobacion))
            .OrderBy(reserva => reserva.FechaCreacion)
            .ToList();

        foreach (Reserva reserva in vencidas)
        {
            Resolver(reserva);
        }
    }

    private void Resolver(Reserva reserva)
    {
        ResultadoResolucion resultado = reserva.Resolver();
        // Solo una confirmación cuenta como compra en el historial. Un rechazo no registra nada.
        if (resultado == ResultadoResolucion.Confirmada)
        {
            Usuario comprador = _repositorios.Usuarios.Obtener(reserva.NombreDelUsuario());
            comprador.Registrar(Actividad.Compra(reserva.NombreDelModelo(), _reloj));
        }
    }
}
