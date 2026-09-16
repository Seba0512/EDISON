using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

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

    public Guid SolicitarReserva(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = modelos.ObtenerPorNombre(nombreModelo);
        Reserva reserva = modelo.RegistrarReserva(solicitante, reloj);
        reservas.Agregar(reserva);

        solicitante.Registrar(Actividad.ReservaCreada(modelo.Nombre, reloj));

        return reserva.Id;
    }

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
