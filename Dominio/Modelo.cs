using Dominio.Busqueda;
using Dominio.Excepciones;
using Dominio.Tiempo;

namespace Dominio;

public class Modelo : IVehiculoBuscable
{
    // Este candado cuida los datos, el stock y las reservas. Nunca se toma junto al de un repositorio.
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

    public LecturaDeModelo TomarLectura()
    {
        lock (candado)
        {
            int unidadesDisponibles = stock.Disponibles;
            int stockTotal = stock.Total;
            return new LecturaDeModelo(
                datos.Nombre,
                datos.Descripcion,
                datos.Precio,
                datos.FechaEntregaPrometida,
                stockTotal,
                unidadesDisponibles,
                stockTotal - unidadesDisponibles,
                datos.NombreArchivoImagen);
        }
    }

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

    public void ModificarDatos(FichaDeModelo ficha, Usuario solicitante)
    {
        lock (candado)
        {
            ValidarModificacion(ficha, solicitante);
            stock.AjustarA(ficha.Stock);
            // La imagen no forma parte de esta operación y se conserva desde el estado protegido.
            datos = DatosModificadosConImagenVigente(ficha.Datos);
        }
    }

    // Swap atómico bajo el candado: serializa subidas concurrentes de imagen y siempre devuelve
    // exactamente la imagen que quedó reemplazada, para que quien llama la borre sin fugas.
    public string CambiarImagen(Usuario solicitante, string nuevoNombreArchivoImagen)
    {
        lock (candado)
        {
            ExigirActivo();
            ExigirPropietario(solicitante);
            ExigirUnidadDisponibleParaCambiarImagen();

            string nombreArchivoAnterior = datos.NombreArchivoImagen;
            datos = new DatosDeModelo(
                datos.Nombre,
                datos.Descripcion,
                datos.Precio,
                datos.FechaEntregaPrometida,
                nuevoNombreArchivoImagen);

            return nombreArchivoAnterior;
        }
    }

    private void ValidarModificacion(FichaDeModelo ficha, Usuario solicitante)
    {
        ExigirActivo();
        ExigirPropietario(solicitante);
        ExigirMismoNombre(ficha.Datos);
        ExigirDisponibleOStockEnAumento(ficha.Stock);
        ExigirStockSuperiorAReservasActivasSiCambia(ficha.Stock);
    }

    private DatosDeModelo DatosModificadosConImagenVigente(DatosDeModelo datosNuevos)
    {
        return new DatosDeModelo(
            datosNuevos.Nombre,
            datosNuevos.Descripcion,
            datosNuevos.Precio,
            datosNuevos.FechaEntregaPrometida,
            datos.NombreArchivoImagen);
    }

    public int ReservasActivas()
    {
        lock (candado)
        {
            return ContarReservasActivas();
        }
    }

    public bool PuedeEliminarse()
    {
        lock (candado)
        {
            return !eliminado && ContarReservasActivas() == 0;
        }
    }

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

            // Se guarda el nombre de la imagen para borrar el archivo después, ya fuera del candado.
            string nombreArchivoImagen = datos.NombreArchivoImagen;
            eliminado = true;

            return nombreArchivoImagen;
        }
    }

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

    // Reserva lo llama con el candado ya tomado. Volver a tomarlo en el mismo hilo no traba.
    internal bool ContieneUnidad(Vehiculo unidad)
    {
        lock (candado)
        {
            return stock.Contiene(unidad);
        }
    }

    internal LecturaDeReserva TomarLecturaDe(Reserva reserva)
    {
        lock (candado)
        {
            ExigirQueLaReservaSeaPropia(reserva);
            return reserva.CrearLectura(datos.Nombre);
        }
    }

    // Siempre se llama con el candado ya tomado, por eso no lo vuelve a tomar.
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

    private void ExigirStockSuperiorAReservasActivasSiCambia(int stockNuevo)
    {
        if (stockNuevo != stock.Total)
        {
            ExigirStockSuperiorAReservasActivas(stockNuevo);
        }
    }

    // Se permite reponer stock para que un modelo agotado no quede congelado para siempre.
    private void ExigirDisponibleOStockEnAumento(int stockNuevo)
    {
        if (!stock.HayAlgunaDisponible && stockNuevo <= stock.Total)
        {
            throw new DominioException(
                $"No se puede modificar el modelo {Nombre} porque no quedan unidades disponibles");
        }
    }

    private void ExigirUnidadDisponibleParaCambiarImagen()
    {
        if (!stock.HayAlgunaDisponible)
        {
            throw new DominioException(
                $"No se puede cambiar la imagen del modelo {Nombre} porque no quedan unidades disponibles");
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
