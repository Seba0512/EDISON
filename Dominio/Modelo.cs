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
/// por la reserva. <see cref="Monitor"/> es reentrante, asi que una cancelacion puede devolver su
/// unidad concreta sin bloquearse a si misma.
///
/// El candado del modelo nunca se sostiene a la vez que el de un repositorio: nadie llama a un
/// repositorio teniendo tomado este candado. Esa separacion evita imponer un orden global de locks.
/// </summary>
public class Modelo : IVehiculoBuscable
{
    private readonly object candado = new();
    private readonly StockDeUnidades stock;
    private readonly List<Reserva> reservas = new();
    private readonly Usuario creador;
    private DatosDeModelo datos;
    private bool eliminado;

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
            ExigirActivo();
            stock.Agregar(cantidad);
        }
    }

    /// <summary>
    /// Modificacion atomica de CR3. Valida propietario, datos, disponibilidad y stock antes de
    /// cambiar el agregado; luego aplica ambos cambios bajo una unica adquisicion del candado. El
    /// stock objetivo debe superar las reservas activas, por lo que la modificacion deja al menos
    /// una unidad libre.
    ///
    /// <para>
    /// Supuesto de diseno: la letra exige poder modificar el modelo "mientras sigan habiendo
    /// unidades disponibles". Tomada al pie de la letra, esa regla dejaria a un modelo agotado
    /// (sin unidades libres) congelado para siempre, sin forma de reponer stock. Por eso se agrega
    /// una clausula explicita: la modificacion tambien se permite cuando el stock nuevo supera al
    /// stock total actual, es decir, cuando la propia modificacion repone unidades y saca al modelo
    /// del estado de agotado. En los hechos esta segunda clausula queda cubierta por la exigencia de
    /// que el stock nuevo supere las reservas activas (un modelo agotado tiene tantas o mas reservas
    /// activas que unidades totales), pero se deja expresada igual porque documenta la intencion de
    /// negocio de forma independiente de esa otra regla.
    /// </para>
    /// </summary>
    public void Modificar(FichaDeModelo ficha, Usuario solicitante)
    {
        lock (candado)
        {
            ExigirActivo();
            ExigirPropietario(solicitante);
            ExigirMismoNombre(ficha.Datos);
            ExigirDisponibleOStockEnAumento(ficha.Stock);
            ExigirStockSuperiorAReservasActivas(ficha.Stock);

            stock.AjustarA(ficha.Stock);
            datos = ficha.Datos;
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
            return !eliminado && ContarReservasActivas() == 0;
        }
    }

    /// <summary>
    /// Hace efectiva la baja dentro del agregado (CR4). La comprobacion de reservas y el cambio de
    /// ciclo de vida y la captura de la imagen vigente comparten el mismo candado que el alta de
    /// reservas y la modificacion, por lo que solo una de esas operaciones puede ganar la carrera.
    /// Devuelve el nombre inmutable necesario para completar la baja fuera del agregado.
    /// </summary>
    internal string MarcarComoEliminado(Usuario solicitante)
    {
        lock (candado)
        {
            ExigirActivo();
            ExigirPropietario(solicitante);
            if (ContarReservasActivas() > 0)
            {
                throw new DominioException(
                    $"No se puede eliminar el modelo {Nombre} porque tiene reservas activas");
            }

            string nombreArchivoImagen = datos.NombreArchivoImagen;
            eliminado = true;

            return nombreArchivoImagen;
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
            ExigirActivo();
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
    /// Una reserva que dejo de estar pendiente mientras esperaba su turno devuelve
    /// <see cref="ResultadoResolucion.SinCambios"/>: no toca stock, estado ni timestamp.
    /// </summary>
    public ResultadoResolucion ResolverReserva(Reserva reserva)
    {
        lock (candado)
        {
            ExigirQueLaReservaSeaPropia(reserva);
            if (!reserva.EstaPendiente)
            {
                return ResultadoResolucion.SinCambios;
            }

            ExigirActivo();
            if (!stock.HayAlgunaDisponible)
            {
                reserva.Rechazar();
                return ResultadoResolucion.Rechazada;
            }

            Vehiculo unidad = stock.TomarUna();
            reserva.Confirmar(unidad);
            return ResultadoResolucion.Confirmada;
        }
    }

    /// <summary>
    /// Cancelacion de CR6 bajo el candado del modelo. La reserva consulta a su State y conoce la
    /// unidad asignada; el modelo manipula su stock y luego le indica completar la transicion.
    /// </summary>
    public void CancelarReserva(Reserva reserva)
    {
        lock (candado)
        {
            ExigirActivo();
            ExigirQueLaReservaSeaPropia(reserva);
            var estadoCancelado = reserva.PrepararCancelacion();
            if (reserva.VehiculoAsignado is Vehiculo unidad)
            {
                stock.Liberar(unidad);
            }

            reserva.CompletarCancelacion(estadoCancelado);
        }
    }

    internal bool ContieneUnidad(Vehiculo unidad)
    {
        lock (candado)
        {
            return stock.Contiene(unidad);
        }
    }

    private int ContarReservasActivas()
    {
        return reservas.Count(reserva => reserva.EstaActiva);
    }

    private void ExigirActivo()
    {
        if (eliminado)
        {
            throw new DominioException($"El modelo {Nombre} fue eliminado");
        }
    }

    private void ExigirMismoNombre(DatosDeModelo datosNuevos)
    {
        if (!datos.DescribeAlMismoModeloQue(datosNuevos))
        {
            throw new DominioException("El nombre del modelo no se puede modificar");
        }
    }

    private void ExigirStockSuperiorAReservasActivas(int stockNuevo)
    {
        int activas = ContarReservasActivas();
        if (stockNuevo <= activas)
        {
            throw new DominioException(
                $"El stock debe ser mayor a las {activas} reservas activas del modelo {Nombre}");
        }
    }

    /// <summary>
    /// Guardia de disponibilidad de CR3: ver el comentario XML de <see cref="Modificar"/> para el
    /// supuesto de diseno detras de la segunda clausula.
    /// </summary>
    private void ExigirDisponibleOStockEnAumento(int stockNuevo)
    {
        if (!stock.HayAlgunaDisponible && stockNuevo <= stock.Total)
        {
            throw new DominioException(
                $"No se puede modificar el modelo {Nombre} porque no quedan unidades disponibles");
        }
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
