using Dominio.Busqueda;
using Dominio.Excepciones;
using Dominio.Tiempo;

namespace Dominio;

/// <summary>
/// Raiz del agregado del catalogo (CR2, CR3, CR4): sus datos descriptivos, su creador, sus unidades
/// fisicas y sus reservas. Contener las reservas lo vuelve el Experto en Informacion de todas las
/// reglas que cruzan reservas con stock: cuantas reservas activas pesan sobre el modelo (CR3), si
/// puede eliminarse (CR4) y si una solicitud vencida se confirma o se rechaza (CR5).
///
/// Las unidades viven en un <see cref="StockDeUnidades"/> al que el modelo delega (SRP): los datos
/// cambian cuando el vendedor los corrige, el stock cambia cuando se reserva o se ajusta.
/// Implementa <see cref="IVehiculoBuscable"/> para que los criterios de CR7 puedan filtrarlo sin
/// conocer esta clase concreta.
///
/// Es el limite de sincronizacion del agregado (SR2). Todo lo que toca sus unidades o sus reservas
/// pasa por su candado privado, consultas incluidas: contar disponibles recorre una lista que otro
/// hilo puede estar modificando. Como corolario, <b>toda transicion de estado de una reserva ocurre
/// dentro del candado de su modelo</b>, y por eso confirmar, rechazar y cancelar entran por aca y no
/// por la reserva. <see cref="Monitor"/> es reentrante, asi que <see cref="ResolverReserva"/> puede
/// terminar llamando a <see cref="LiberarUnidad"/> sin bloquearse a si mismo.
///
/// El orden de adquisicion en todo el sistema es repositorio, despues modelo, nunca al reves: nadie
/// llama a un repositorio teniendo tomado este candado. Es la regla que evita los deadlocks.
/// </summary>
public class Modelo : IVehiculoBuscable
{
    private readonly object candado = new();
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

    public string Descripcion
    {
        get
        {
            lock (candado)
            {
                return datos.Descripcion;
            }
        }
    }

    public decimal Precio
    {
        get
        {
            lock (candado)
            {
                return datos.Precio;
            }
        }
    }

    public DateTime FechaEntregaPrometida
    {
        get
        {
            lock (candado)
            {
                return datos.FechaEntregaPrometida;
            }
        }
    }

    public string NombreArchivoImagen
    {
        get
        {
            lock (candado)
            {
                return datos.NombreArchivoImagen;
            }
        }
    }

    public bool TieneImagen
    {
        get
        {
            lock (candado)
            {
                return datos.TieneImagen;
            }
        }
    }

    public int StockTotal
    {
        get
        {
            lock (candado)
            {
                return stock.Total;
            }
        }
    }

    public int UnidadesDisponibles
    {
        get
        {
            lock (candado)
            {
                return stock.Disponibles;
            }
        }
    }

    public int UnidadesReservadas
    {
        get
        {
            lock (candado)
            {
                return stock.Reservadas;
            }
        }
    }

    public bool TieneUnidadDisponible
    {
        get
        {
            lock (candado)
            {
                return stock.HayAlgunaDisponible;
            }
        }
    }

    /// <summary>El nombre es la clave natural y no cambia, asi que no necesita candado.</summary>
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
        lock (candado)
        {
            stock.Agregar(cantidad);
        }
    }

    /// <summary>
    /// Modificacion de los datos descriptivos (CR3). La letra la permite "mientras sigan habiendo
    /// unidades disponibles", asi que un modelo con todas sus unidades tomadas queda congelado.
    /// </summary>
    public void ActualizarDatos(DatosDeModelo datosNuevos)
    {
        lock (candado)
        {
            if (!datos.DescribeAlMismoModeloQue(datosNuevos))
            {
                throw new DominioException("El nombre del modelo no se puede modificar");
            }

            if (!stock.HayAlgunaDisponible)
            {
                throw new DominioException(
                    $"No se puede modificar el modelo {Nombre} porque no quedan unidades disponibles");
            }

            datos = datosNuevos;
        }
    }

    /// <summary>
    /// Ajuste de stock de CR3: "en caso de actualizar stock este debe ser mayor a la cantidad de
    /// reservas existentes". Se cuentan las activas, que incluyen las pendientes: todavia no ocupan
    /// unidad, pero van a competir por una.
    /// </summary>
    public void AjustarStock(int stockNuevo)
    {
        lock (candado)
        {
            int activas = ContarReservasActivas();
            if (stockNuevo <= activas)
            {
                throw new DominioException(
                    $"El stock debe ser mayor a las {activas} reservas activas del modelo {Nombre}");
            }

            stock.AjustarA(stockNuevo);
        }
    }

    public int ReservasActivas()
    {
        lock (candado)
        {
            return ContarReservasActivas();
        }
    }

    /// <summary>Condicion de baja de CR4: sin reservas activas, ni siquiera pendientes.</summary>
    public bool PuedeEliminarse()
    {
        lock (candado)
        {
            return ContarReservasActivas() == 0;
        }
    }

    /// <summary>
    /// Alta de una solicitud (CR5). El modelo crea la reserva ademas de contenerla (GRASP Creador),
    /// igual que su stock crea las unidades: asi no existe forma de armar una reserva sobre un
    /// modelo y registrarla en otro.
    /// </summary>
    public Reserva RegistrarReserva(Usuario solicitante, IReloj reloj)
    {
        lock (candado)
        {
            Reserva reserva = new Reserva(solicitante, this, reloj);
            reservas.Add(reserva);

            return reserva;
        }
    }

    /// <summary>
    /// Decision de CR5 en un unico metodo: con unidad libre la reserva se confirma y toma una
    /// unidad; sin stock se rechaza. Asi, bajo el candado del modelo, ninguna otra solicitud puede
    /// meterse entre mirar el stock y tomar la unidad.
    ///
    /// Una reserva que dejo de estar pendiente mientras esperaba su turno ya fue resuelta o
    /// cancelada por su usuario: no hay nada que decidir y se la deja como esta.
    /// </summary>
    public void ResolverReserva(Reserva reserva)
    {
        lock (candado)
        {
            ExigirQueLaReservaSeaPropia(reserva);
            if (!reserva.EstaPendiente)
            {
                return;
            }

            if (!stock.HayAlgunaDisponible)
            {
                reserva.Rechazar();
                return;
            }

            stock.TomarUna();
            reserva.Confirmar();
        }
    }

    /// <summary>
    /// Cancelacion de CR6 bajo el candado del modelo. Si la reserva estaba confirmada, es su propio
    /// estado el que devuelve la unidad llamando a <see cref="LiberarUnidad"/>, ya dentro de este
    /// mismo candado.
    /// </summary>
    public void CancelarReserva(Reserva reserva)
    {
        lock (candado)
        {
            ExigirQueLaReservaSeaPropia(reserva);
            reserva.AplicarCancelacion();
        }
    }

    /// <summary>Devuelve al pool la unidad de una reserva confirmada que se cancela (CR6).</summary>
    public void LiberarUnidad()
    {
        lock (candado)
        {
            stock.LiberarUna();
        }
    }

    private int ContarReservasActivas()
    {
        return reservas.Count(reserva => reserva.EstaActiva);
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
