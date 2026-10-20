using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Servicios;

// Todo el estado del sistema vive acá, en memoria, y los tres services lo comparten.
// El candado es único y global, como recomienda el curso: quien opera sobre el estado lo toma
// una vez y adentro solo hace leer-validar-modificar, nunca I/O ni esperas. Es un
// SemaphoreSlim porque un lock no puede contener await: los servicios lo toman con WaitAsync.
public sealed class EstadoEnMemoria
{
    public SemaphoreSlim Candado { get; } = new(1, 1);

    public Dictionary<string, Usuario> Usuarios { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, VehiculoModelo> Modelos { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<Guid, Reserva> Reservas { get; } = new();

    // Los métodos de acá abajo se llaman siempre con el candado ya tomado.
    public Usuario? BuscarUsuario(string nombreUsuario)
    {
        Usuarios.TryGetValue(nombreUsuario?.Trim() ?? string.Empty, out Usuario? usuario);
        return usuario;
    }

    public VehiculoModelo ObtenerModelo(string nombreModelo)
    {
        string clave = nombreModelo?.Trim() ?? string.Empty;
        if (!Modelos.TryGetValue(clave, out VehiculoModelo? modelo))
        {
            throw new DominioException($"No existe el modelo {clave}");
        }

        modelo.ExigirActivo();
        return modelo;
    }

    public Reserva ObtenerReserva(Guid idReserva)
    {
        if (!Reservas.TryGetValue(idReserva, out Reserva? reserva))
        {
            throw new DominioException($"No existe la reserva {idReserva}");
        }

        return reserva;
    }

    public int ContarConfirmadas(VehiculoModelo modelo)
    {
        return Reservas.Values.Count(reserva =>
            reserva.EsDelModelo(modelo) && reserva.Estado == EstadoDeReserva.Confirmada);
    }

    public int ContarActivas(VehiculoModelo modelo)
    {
        return Reservas.Values.Count(reserva => reserva.EsDelModelo(modelo) && reserva.EstaActiva);
    }
}
