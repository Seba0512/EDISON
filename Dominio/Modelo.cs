using Dominio.Excepciones;
using Dominio.Tiempo;

namespace Dominio;

public class Modelo
{
    // Este candado cuida los datos, el stock y las reservas. Nunca se toma junto al de un repositorio.
    private readonly object _candado = new();
    private readonly StockDeUnidades _stock;
    private readonly List<Reserva> _reservas = new();
    private readonly Usuario _creador;
    private DatosDeModelo _datos;
    private bool _eliminado;

    public Modelo(DatosDeModelo datos, Usuario creador)
    {
        _datos = datos;
        _creador = creador;
        _stock = new StockDeUnidades(datos.Nombre);
    }

    public string Nombre => _datos.Nombre;

    public LecturaDeModelo TomarLectura()
    {
        lock (_candado)
        {
            int unidadesDisponibles = _stock.Disponibles;
            int stockTotal = _stock.Total;
            return new LecturaDeModelo(
                _datos.Nombre,
                _datos.Descripcion,
                _datos.Precio,
                _datos.FechaEntregaPrometida,
                stockTotal,
                unidadesDisponibles,
                stockTotal - unidadesDisponibles,
                _datos.NombreArchivoImagen);
        }
    }

    public void AgregarUnidades(int cantidad)
    {
        lock (_candado)
        {
            ExigirActivo();
            _stock.Agregar(cantidad);
        }
    }

    public void ModificarDatos(FichaDeModelo ficha, Usuario solicitante)
    {
        lock (_candado)
        {
            ValidarModificacion(ficha, solicitante);
            _stock.AjustarA(ficha.Stock);
            // La imagen no forma parte de esta operación y se conserva desde el estado protegido.
            _datos = DatosModificadosConImagenVigente(ficha.Datos);
        }
    }

    // Swap atómico bajo el candado: serializa subidas concurrentes de imagen y siempre devuelve
    // exactamente la imagen que quedó reemplazada, para que quien llama la borre sin fugas.
    public string CambiarImagen(Usuario solicitante, string nuevoNombreArchivoImagen)
    {
        lock (_candado)
        {
            ExigirActivo();
            ExigirPropietario(solicitante);
            ExigirUnidadDisponibleParaCambiarImagen();

            string nombreArchivoAnterior = _datos.NombreArchivoImagen;
            _datos = new DatosDeModelo(
                _datos.Nombre,
                _datos.Descripcion,
                _datos.Precio,
                _datos.FechaEntregaPrometida,
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
            _datos.NombreArchivoImagen);
    }

    // Solo la usan las pruebas para observar el invariante: ninguna unidad reservada de mas.
    internal int ReservasActivas()
    {
        lock (_candado)
        {
            return ContarReservasActivas();
        }
    }

    internal string MarcarComoEliminado(Usuario solicitante)
    {
        lock (_candado)
        {
            ExigirActivo();
            ExigirPropietario(solicitante);
            if (ContarReservasActivas() > 0)
            {
                throw new DominioException(
                    $"No se puede eliminar el modelo {Nombre} porque tiene reservas activas");
            }

            // Se guarda el nombre de la imagen para borrar el archivo después, ya fuera del candado.
            string nombreArchivoImagen = _datos.NombreArchivoImagen;
            _eliminado = true;

            return nombreArchivoImagen;
        }
    }

    internal void RestaurarTrasBajaFallida()
    {
        lock (_candado)
        {
            _eliminado = false;
        }
    }

    public Reserva RegistrarReserva(Usuario solicitante, IReloj reloj)
    {
        lock (_candado)
        {
            ExigirActivo();
            Reserva reserva = new Reserva(solicitante, this, reloj);
            _reservas.Add(reserva);

            return reserva;
        }
    }

    public ResultadoResolucion ResolverReserva(Reserva reserva)
    {
        lock (_candado)
        {
            ExigirQueLaReservaSeaPropia(reserva);
            if (!reserva.EstaPendiente)
            {
                return ResultadoResolucion.SinCambios;
            }

            ExigirActivo();
            if (!_stock.HayAlgunaDisponible)
            {
                reserva.Rechazar();
                return ResultadoResolucion.Rechazada;
            }

            Vehiculo unidad = _stock.TomarUna();
            reserva.Confirmar(unidad);
            return ResultadoResolucion.Confirmada;
        }
    }

    public void CancelarReserva(Reserva reserva)
    {
        lock (_candado)
        {
            ExigirActivo();
            ExigirQueLaReservaSeaPropia(reserva);
            var estadoCancelado = reserva.PrepararCancelacion();
            if (reserva.VehiculoAsignado is Vehiculo unidad)
            {
                _stock.Liberar(unidad);
            }

            reserva.CompletarCancelacion(estadoCancelado);
        }
    }

    // Reserva lo llama con el candado ya tomado. Volver a tomarlo en el mismo hilo no traba.
    internal bool ContieneUnidad(Vehiculo unidad)
    {
        lock (_candado)
        {
            return _stock.Contiene(unidad);
        }
    }

    internal LecturaDeReserva TomarLecturaDe(Reserva reserva)
    {
        lock (_candado)
        {
            ExigirQueLaReservaSeaPropia(reserva);
            return reserva.CrearLectura(_datos.Nombre);
        }
    }

    // Siempre se llama con el candado ya tomado, por eso no lo vuelve a tomar.
    private int ContarReservasActivas()
    {
        return _reservas.Count(reserva => reserva.EstaActiva);
    }

    private void ExigirPropietario(Usuario usuario)
    {
        if (!_creador.SeLlama(usuario.NombreUsuario))
        {
            throw new DominioException(
                $"Solo el creador del modelo {Nombre} puede modificarlo o eliminarlo");
        }
    }

    private void ExigirActivo()
    {
        if (_eliminado)
        {
            throw new DominioException($"El modelo {Nombre} fue eliminado");
        }
    }

    private void ExigirMismoNombre(DatosDeModelo datosNuevos)
    {
        if (!_datos.DescribeAlMismoModeloQue(datosNuevos))
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
        if (stockNuevo != _stock.Total)
        {
            ExigirStockSuperiorAReservasActivas(stockNuevo);
        }
    }

    // Se permite reponer stock para que un modelo agotado no quede congelado para siempre.
    private void ExigirDisponibleOStockEnAumento(int stockNuevo)
    {
        if (!_stock.HayAlgunaDisponible && stockNuevo <= _stock.Total)
        {
            throw new DominioException(
                $"No se puede modificar el modelo {Nombre} porque no quedan unidades disponibles");
        }
    }

    private void ExigirUnidadDisponibleParaCambiarImagen()
    {
        if (!_stock.HayAlgunaDisponible)
        {
            throw new DominioException(
                $"No se puede cambiar la imagen del modelo {Nombre} porque no quedan unidades disponibles");
        }
    }

    private void ExigirQueLaReservaSeaPropia(Reserva reserva)
    {
        if (!_reservas.Contains(reserva))
        {
            throw new DominioException(
                $"La reserva {reserva.Id} no esta registrada en el modelo {Nombre}");
        }
    }
}
