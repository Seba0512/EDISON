namespace Dominio;

public enum EstadoDeVehiculo
{
    Disponible,
    Reservada
}

public class Vehiculo
{
    public Guid Id { get; }

    public string NombreModelo { get; }

    public EstadoDeVehiculo Estado { get; private set; }

    internal Vehiculo(string nombreModelo)
    {
        Id = Guid.NewGuid();
        NombreModelo = nombreModelo;
        Estado = EstadoDeVehiculo.Disponible;
    }

    public bool EstaDisponible => Estado == EstadoDeVehiculo.Disponible;

    internal void Reservar()
    {
        if (!EstaDisponible)
        {
            throw new DominioException($"La unidad {Id} ya esta reservada");
        }

        Estado = EstadoDeVehiculo.Reservada;
    }

    internal void Liberar()
    {
        if (EstaDisponible)
        {
            throw new DominioException($"La unidad {Id} ya estaba disponible");
        }

        Estado = EstadoDeVehiculo.Disponible;
    }
}

public class DatosDeModelo
{
    private const int LargoMinimoNombre = 2;
    private const int LargoMaximoNombre = 60;
    private const int LargoMaximoDescripcion = 500;

    public string Nombre { get; }

    public string Descripcion { get; }

    public decimal Precio { get; }

    public DateTime FechaEntregaPrometida { get; }

    public string NombreArchivoImagen { get; }

    public DatosDeModelo(
        string nombre,
        string descripcion,
        decimal precio,
        DateTime fechaEntregaPrometida,
        string nombreArchivoImagen)
    {
        Nombre = NombreValidado(nombre);
        Descripcion = DescripcionValidada(descripcion);
        Precio = PrecioValidado(precio);
        FechaEntregaPrometida = fechaEntregaPrometida;
        NombreArchivoImagen = nombreArchivoImagen?.Trim() ?? string.Empty;
    }

    public bool TieneImagen => NombreArchivoImagen.Length > 0;

    public bool DescribeAlMismoModeloQue(DatosDeModelo otrosDatos)
    {
        return Nombre.Equals(otrosDatos.Nombre, StringComparison.OrdinalIgnoreCase);
    }

    private static string NombreValidado(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DominioException("El nombre del modelo no puede estar vacio");
        }

        string nombreSinEspacios = nombre.Trim();
        if (nombreSinEspacios.Length is < LargoMinimoNombre or > LargoMaximoNombre)
        {
            throw new DominioException(
                $"El nombre del modelo debe tener entre {LargoMinimoNombre} y {LargoMaximoNombre} caracteres");
        }

        return nombreSinEspacios;
    }

    // La descripcion es opcional: una cadena vacia significa que el modelo no tiene descripcion.
    private static string DescripcionValidada(string descripcion)
    {
        string descripcionSinEspacios = descripcion?.Trim() ?? string.Empty;
        if (descripcionSinEspacios.Length > LargoMaximoDescripcion)
        {
            throw new DominioException(
                $"La descripcion no puede superar los {LargoMaximoDescripcion} caracteres");
        }

        return descripcionSinEspacios;
    }

    private static decimal PrecioValidado(decimal precio)
    {
        decimal precioRedondeado = Math.Round(precio, 0, MidpointRounding.AwayFromZero);
        if (precioRedondeado <= 0)
        {
            throw new DominioException("El precio debe ser mayor a cero");
        }

        return precioRedondeado;
    }
}

public class FichaDeModelo
{
    public DatosDeModelo Datos { get; }

    public int Stock { get; }

    public FichaDeModelo(DatosDeModelo datos, int stock)
    {
        if (stock <= 0)
        {
            throw new DominioException("El stock debe ser mayor a cero");
        }

        Datos = datos;
        Stock = stock;
    }

    public string Nombre => Datos.Nombre;
}

public sealed record LecturaDeModelo(
    string Nombre,
    string Descripcion,
    decimal Precio,
    DateTime FechaEntregaPrometida,
    int StockTotal,
    int UnidadesDisponibles,
    int UnidadesReservadas,
    string NombreArchivoImagen)
{
    public bool TieneImagen => NombreArchivoImagen.Length > 0;

    public bool TieneUnidadDisponible => UnidadesDisponibles > 0;

    public bool SeLlama(string nombreModelo)
    {
        return Nombre.Equals(nombreModelo?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

public class Modelo
{
    // Este candado cuida los datos, las unidades y las reservas. Nunca se toma junto al de Edison.
    private readonly object _candado = new();
    private readonly List<Vehiculo> _unidades = new();
    private readonly List<Reserva> _reservas = new();
    private readonly Usuario _creador;
    private DatosDeModelo _datos;
    private bool _eliminado;

    public Modelo(DatosDeModelo datos, Usuario creador)
    {
        _datos = datos;
        _creador = creador;
    }

    public string Nombre => _datos.Nombre;

    public LecturaDeModelo TomarLectura()
    {
        lock (_candado)
        {
            int disponibles = ContarDisponibles();
            return new LecturaDeModelo(
                _datos.Nombre,
                _datos.Descripcion,
                _datos.Precio,
                _datos.FechaEntregaPrometida,
                _unidades.Count,
                disponibles,
                _unidades.Count - disponibles,
                _datos.NombreArchivoImagen);
        }
    }

    public void AgregarUnidades(int cantidad)
    {
        lock (_candado)
        {
            ExigirActivo();
            if (cantidad <= 0)
            {
                throw new DominioException("La cantidad de unidades a agregar debe ser mayor a cero");
            }

            for (int agregadas = 0; agregadas < cantidad; agregadas++)
            {
                _unidades.Add(new Vehiculo(Nombre));
            }
        }
    }

    public void ModificarDatos(FichaDeModelo ficha, Usuario solicitante)
    {
        lock (_candado)
        {
            ValidarModificacion(ficha, solicitante);
            AjustarStockA(ficha.Stock);
            // La imagen no forma parte de esta operación y se conserva desde el estado protegido.
            _datos = new DatosDeModelo(
                ficha.Datos.Nombre,
                ficha.Datos.Descripcion,
                ficha.Datos.Precio,
                ficha.Datos.FechaEntregaPrometida,
                _datos.NombreArchivoImagen);
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
            if (ContarDisponibles() == 0)
            {
                throw new DominioException(
                    $"No se puede cambiar la imagen del modelo {Nombre} porque no quedan unidades disponibles");
            }

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
            Vehiculo? unidadLibre = _unidades.Find(unidad => unidad.EstaDisponible);
            if (unidadLibre is null)
            {
                reserva.Rechazar();
                return ResultadoResolucion.Rechazada;
            }

            unidadLibre.Reservar();
            reserva.Confirmar(unidadLibre);
            return ResultadoResolucion.Confirmada;
        }
    }

    public void CancelarReserva(Reserva reserva)
    {
        lock (_candado)
        {
            ExigirActivo();
            ExigirQueLaReservaSeaPropia(reserva);
            reserva.ExigirCancelable();
            if (reserva.VehiculoAsignado is Vehiculo unidad)
            {
                LiberarUnidad(unidad);
            }

            reserva.MarcarCancelada();
        }
    }

    // Reserva lo llama con el candado ya tomado. Volver a tomarlo en el mismo hilo no traba.
    internal bool ContieneUnidad(Vehiculo unidad)
    {
        lock (_candado)
        {
            return _unidades.Contains(unidad);
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

    // Los privados de acá abajo se llaman siempre con el candado ya tomado.
    private int ContarDisponibles()
    {
        return _unidades.Count(unidad => unidad.EstaDisponible);
    }

    private int ContarReservasActivas()
    {
        return _reservas.Count(reserva => reserva.EstaActiva);
    }

    private void AjustarStockA(int totalNuevo)
    {
        int reservadas = _unidades.Count - ContarDisponibles();
        if (totalNuevo < reservadas)
        {
            throw new DominioException(
                $"El stock no puede quedar por debajo de las {reservadas} unidades reservadas");
        }

        if (totalNuevo > _unidades.Count)
        {
            int faltantes = totalNuevo - _unidades.Count;
            for (int agregadas = 0; agregadas < faltantes; agregadas++)
            {
                _unidades.Add(new Vehiculo(Nombre));
            }

            return;
        }

        int sobrantes = _unidades.Count - totalNuevo;
        for (int quitadas = 0; quitadas < sobrantes; quitadas++)
        {
            Vehiculo unidadLibre = _unidades.Find(unidad => unidad.EstaDisponible)!;
            _unidades.Remove(unidadLibre);
        }
    }

    private void LiberarUnidad(Vehiculo unidad)
    {
        if (!_unidades.Contains(unidad))
        {
            throw new DominioException($"La unidad {unidad.Id} no pertenece al modelo {Nombre}");
        }

        unidad.Liberar();
    }

    private void ValidarModificacion(FichaDeModelo ficha, Usuario solicitante)
    {
        ExigirActivo();
        ExigirPropietario(solicitante);
        if (!_datos.DescribeAlMismoModeloQue(ficha.Datos))
        {
            throw new DominioException("El nombre del modelo no se puede modificar");
        }

        // Se permite reponer stock para que un modelo agotado no quede congelado para siempre.
        if (ContarDisponibles() == 0 && ficha.Stock <= _unidades.Count)
        {
            throw new DominioException(
                $"No se puede modificar el modelo {Nombre} porque no quedan unidades disponibles");
        }

        if (ficha.Stock != _unidades.Count)
        {
            int activas = ContarReservasActivas();
            if (ficha.Stock <= activas)
            {
                throw new DominioException(
                    $"El stock debe ser mayor a las {activas} reservas activas del modelo {Nombre}");
            }
        }
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

    private void ExigirQueLaReservaSeaPropia(Reserva reserva)
    {
        if (!_reservas.Contains(reserva))
        {
            throw new DominioException(
                $"La reserva {reserva.Id} no esta registrada en el modelo {Nombre}");
        }
    }
}
