using Dominio.Busqueda;
using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Raiz del agregado del catalogo (CR2, CR3, CR4): sus datos descriptivos, su creador, sus unidades
/// fisicas y sus reservas. Contener las reservas lo vuelve el Experto en Informacion de todas las
/// reglas que cruzan reservas con stock: cuantas reservas activas pesan sobre el modelo (CR3), si
/// puede eliminarse (CR4) y si una solicitud vencida se confirma o se rechaza (CR5).
///
/// Las unidades viven en un <see cref="StockDeUnidades"/> al que el modelo delega (SRP): los datos
/// cambia cuando el vendedor corrige datos, el stock cambia cuando se reserva o se ajusta.
/// Implementa <see cref="IVehiculoBuscable"/> para que los criterios de CR7 puedan filtrarlo sin
/// conocer esta clase concreta.
/// </summary>
public class Modelo : IVehiculoBuscable
{
    private readonly StockDeUnidades stock;
    private readonly List<Reserva> reservas = new();
    private readonly Usuario creador;
    private DatosDeModelo datos;

    public Modelo(DatosDeModelo datos, Usuario creador)
    {
        this.datos = datos;
        this.creador = creador;
        stock = new StockDeUnidades(datos.Nombre);
    }

    public string Nombre => datos.Nombre;

    public string Descripcion => datos.Descripcion;

    public decimal Precio => datos.Precio;

    public DateTime FechaEntregaPrometida => datos.FechaEntregaPrometida;

    public string NombreArchivoImagen => datos.NombreArchivoImagen;

    public bool TieneImagen => datos.TieneImagen;

    public int StockTotal => stock.Total;

    public int UnidadesDisponibles => stock.Disponibles;

    public int UnidadesReservadas => stock.Reservadas;

    public bool TieneUnidadDisponible => stock.HayAlgunaDisponible;

    public bool SeLlama(string nombreModelo)
    {
        return Nombre.Equals(nombreModelo?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool FueCreadoPor(string nombreUsuario)
    {
        return creador.SeLlama(nombreUsuario);
    }

    public bool EsDe(Usuario usuario)
    {
        return FueCreadoPor(usuario.NombreUsuario);
    }

    /// <summary>Guardia de CR3 y CR4: solo el creador puede modificar o eliminar el modelo.</summary>
    public void ExigirPropietario(Usuario usuario)
    {
        if (!EsDe(usuario))
        {
            throw new DominioException(
                $"Solo el creador del modelo {Nombre} puede modificarlo o eliminarlo");
        }
    }

    public void AgregarUnidades(int cantidad)
    {
        stock.Agregar(cantidad);
    }

    /// <summary>Modificacion de los datos descriptivos (CR3), permitida mientras haya stock libre.</summary>
    public void ActualizarDatos(DatosDeModelo datosNuevos)
    {
        if (!datos.DescribeAlMismoModeloQue(datosNuevos))
        {
            throw new DominioException("El nombre del modelo no se puede modificar");
        }

        if (!TieneUnidadDisponible)
        {
            throw new DominioException(
                $"No se puede modificar el modelo {Nombre} porque no quedan unidades disponibles");
        }

        datos = datosNuevos;
    }

    /// <summary>
    /// Ajuste de stock de CR3: el stock nuevo debe superar todas las reservas activas, incluidas
    /// las pendientes, que todavia no ocupan unidad pero van a competir por una.
    /// </summary>
    public void AjustarStock(int stockNuevo)
    {
        int activas = ReservasActivas();
        if (stockNuevo <= activas)
        {
            throw new DominioException(
                $"El stock debe ser mayor a las {activas} reservas activas del modelo {Nombre}");
        }

        stock.AjustarA(stockNuevo);
    }

    public int ReservasActivas()
    {
        return reservas.Count(reserva => reserva.EstaActiva);
    }

    /// <summary>Condicion de baja de CR4: sin reservas activas, ni siquiera pendientes.</summary>
    public bool PuedeEliminarse()
    {
        return ReservasActivas() == 0;
    }

    /// <summary>Alta de una solicitud (CR5). El modelo solo acepta reservas hechas sobre el.</summary>
    public void RegistrarReserva(Reserva reserva)
    {
        if (!reserva.EsDelModelo(Nombre))
        {
            throw new DominioException(
                $"La reserva de {reserva.NombreDelModelo()} no pertenece al modelo {Nombre}");
        }

        reservas.Add(reserva);
    }

    /// <summary>
    /// Decision de CR5 en un unico metodo: con unidad libre la reserva se confirma y toma una
    /// unidad; sin stock se rechaza. Asi, bajo el candado del modelo, ninguna otra solicitud puede
    /// meterse entre mirar el stock y tomar la unidad.
    /// </summary>
    public void ResolverReserva(Reserva reserva)
    {
        ExigirQueLaReservaSeaPropia(reserva);
        if (!TieneUnidadDisponible)
        {
            reserva.Rechazar();
            return;
        }

        stock.TomarUna();
        reserva.Confirmar();
    }

    /// <summary>Devuelve al pool la unidad de una reserva confirmada que se cancela (CR6).</summary>
    public void LiberarUnidad()
    {
        stock.LiberarUna();
    }

    private void ExigirQueLaReservaSeaPropia(Reserva reserva)
    {
        if (!reservas.Contains(reserva))
        {
            throw new DominioException(
                $"La reserva {reserva.Id} no esta registrada en el modelo {Nombre}");
        }
    }
}
