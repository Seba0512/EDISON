using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Servicios;

// Ciclo de vida de las reservas: solicitud, resolución diferida y cancelación.
public class ServicioDeReservas
{
    // Regla del negocio, no del entorno: toda reserva se resuelve este tiempo después de pedirla.
    private static readonly TimeSpan PlazoDeResolucion = TimeSpan.FromSeconds(30);

    private readonly EstadoEnMemoria _estado;

    public ServicioDeReservas(EstadoEnMemoria estado)
    {
        _estado = estado;
    }

    public Guid SolicitarReserva(string nombreModelo, Usuario solicitante)
    {
        Reserva reserva;
        lock (_estado.Candado)
        {
            VehiculoModelo modelo = _estado.ObtenerModelo(nombreModelo);
            reserva = new Reserva(solicitante, modelo);
            _estado.Reservas.Add(reserva.Id, reserva);
            solicitante.Registrar(Actividad.ReservaCreada(modelo.Nombre));
        }

        // El modelo del curso: un Thread por reserva que duerme el plazo y después la resuelve.
        Thread resolutor = new(() => DormirYResolver(reserva.Id))
        {
            IsBackground = true,
            Name = $"Reserva {reserva.Id}"
        };
        resolutor.Start();
        return reserva.Id;
    }

    public void CancelarReserva(Guid idReserva, Usuario solicitante)
    {
        lock (_estado.Candado)
        {
            Reserva reserva = _estado.ObtenerReserva(idReserva);
            if (!reserva.FueHechaPor(solicitante.NombreUsuario))
            {
                throw new DominioException("Solo se pueden cancelar las reservas propias");
            }

            reserva.Cancelar();
            solicitante.Registrar(Actividad.ReservaCancelada(reserva.NombreDelModelo()));
        }
    }

    public IReadOnlyList<LecturaDeReserva> ConsultarReservasDe(Usuario usuario)
    {
        lock (_estado.Candado)
        {
            return _estado.Reservas.Values
                .Where(reserva => reserva.FueHechaPor(usuario.NombreUsuario))
                .Select(reserva => reserva.TomarLectura())
                .ToList();
        }
    }

    private void DormirYResolver(Guid idReserva)
    {
        try
        {
            Thread.Sleep(PlazoDeResolucion);
            ResolverReserva(idReserva);
        }
        catch (Exception exception)
        {
            // Sin este catch la excepción mataría el Thread en silencio y la reserva quedaría Pendiente.
            Console.Error.WriteLine($"No se pudo resolver la reserva {idReserva}: {exception}");
        }
    }

    private void ResolverReserva(Guid idReserva)
    {
        lock (_estado.Candado)
        {
            // Si la cancelaron antes de que venciera el plazo, ya no hay nada que resolver.
            if (!_estado.Reservas.TryGetValue(idReserva, out Reserva? reserva) || !reserva.EstaPendiente)
            {
                return;
            }

            int disponibles = reserva.Modelo.Stock - _estado.ContarConfirmadas(reserva.Modelo);
            if (disponibles > 0)
            {
                reserva.Confirmar();
                // Solo una confirmación cuenta como compra en el historial. Un rechazo no registra nada.
                reserva.Solicitante.Registrar(Actividad.Compra(reserva.NombreDelModelo()));
            }
            else
            {
                reserva.Rechazar();
            }
        }
    }
}
