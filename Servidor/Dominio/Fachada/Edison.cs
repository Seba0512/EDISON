using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Fachada;

public sealed record FiltroDeModelos(
    string? Texto,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo,
    DateTime? FechaEntregaMaxima,
    int? MinimoDisponibles)
{
    public static FiltroDeModelos SinFiltros => new(null, null, null, null, null);
}

// Fachada y estado del sistema en un solo lugar: usuarios, modelos y reservas viven en estos
// diccionarios. Cada candado cuida solo su diccionario y nunca se toma junto al candado de un Modelo.
public class Edison
{
    private readonly Dictionary<string, Usuario> _usuarios = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Modelo> _modelos = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, Reserva> _reservas = new();
    private readonly object _candadoDeUsuarios = new();
    private readonly object _candadoDeModelos = new();
    private readonly object _candadoDeReservas = new();
    private readonly IReloj _reloj;
    private readonly int _segundosDeAprobacion;

    public Edison(IReloj reloj, int segundosDeAprobacion)
    {
        _reloj = reloj;
        _segundosDeAprobacion = segundosDeAprobacion;
    }

    public Usuario Registrar(string nombreUsuario, string clave)
    {
        Usuario usuario = new Usuario(nombreUsuario, clave);
        lock (_candadoDeUsuarios)
        {
            if (_usuarios.ContainsKey(usuario.NombreUsuario))
            {
                throw new DominioException($"Ya existe un usuario llamado {usuario.NombreUsuario}");
            }

            _usuarios.Add(usuario.NombreUsuario, usuario);
        }

        return usuario;
    }

    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
        Usuario? usuario = BuscarUsuario(nombreUsuario);
        // Mismo error si no existe o la clave es incorrecta, para no revelar qué usuarios existen.
        if (usuario is null || !usuario.VerificarClave(clave))
        {
            throw new DominioException("El usuario o la clave son incorrectos");
        }

        usuario.Registrar(Actividad.InicioSesion(_reloj));
        return usuario;
    }

    public void CerrarSesion(Usuario usuario)
    {
        usuario.Registrar(Actividad.CierreSesion(_reloj));
    }

    public IReadOnlyList<Actividad> HistorialDe(Usuario usuario)
    {
        return usuario.Actividades();
    }

    public void AltaDeModelo(FichaDeModelo ficha, Usuario creador)
    {
        ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
        Modelo modelo = new Modelo(ficha.Datos, creador);
        modelo.AgregarUnidades(ficha.Stock);
        lock (_candadoDeModelos)
        {
            if (_modelos.ContainsKey(modelo.Nombre))
            {
                throw new DominioException($"Ya existe un modelo llamado {modelo.Nombre}");
            }

            _modelos.Add(modelo.Nombre, modelo);
        }

        creador.Registrar(Actividad.ModeloCreado(ficha.Nombre, _reloj));
    }

    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        Modelo modelo = ObtenerModelo(ficha.Nombre);
        // Solo se exige futura si la fecha efectivamente cambió, para permitir la edición tardía
        // de otros campos de un modelo cuya fecha ya venció.
        DateTime fechaVigente = modelo.TomarLectura().FechaEntregaPrometida;
        if (ficha.Datos.FechaEntregaPrometida.Date != fechaVigente.Date)
        {
            ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
        }

        modelo.ModificarDatos(ficha, solicitante);
        solicitante.Registrar(Actividad.ModeloModificado(ficha.Nombre, _reloj));
    }

    // Devuelve el nombre del archivo de imagen para que la capa de arriba borre el archivo físico.
    public string EliminarModelo(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = ObtenerModelo(nombreModelo);
        string nombreArchivoImagen = modelo.MarcarComoEliminado(solicitante);
        lock (_candadoDeModelos)
        {
            _modelos.Remove(modelo.Nombre);
        }

        solicitante.Registrar(Actividad.ModeloEliminado(modelo.Nombre, _reloj));
        return nombreArchivoImagen;
    }

    // Devuelve el nombre de la imagen reemplazada para que la capa de arriba borre el archivo viejo.
    public string CambiarImagenDeModelo(
        string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante)
    {
        Modelo modelo = ObtenerModelo(nombreModelo);
        string nombreArchivoAnterior = modelo.CambiarImagen(solicitante, nuevoNombreArchivoImagen);
        solicitante.Registrar(Actividad.ModeloModificado(modelo.Nombre, _reloj));
        return nombreArchivoAnterior;
    }

    public IReadOnlyList<LecturaDeModelo> Buscar(FiltroDeModelos filtro)
    {
        List<Modelo> modelos;
        lock (_candadoDeModelos)
        {
            modelos = _modelos.Values.ToList();
        }

        return modelos
            .Select(modelo => modelo.TomarLectura())
            .Where(modelo => modelo.TieneUnidadDisponible)
            .Where(modelo => Cumple(modelo, filtro))
            .ToList();
    }

    public LecturaDeModelo ConsultarLectura(string nombreModelo)
    {
        return ObtenerModelo(nombreModelo).TomarLectura();
    }

    public Guid SolicitarReserva(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = ObtenerModelo(nombreModelo);
        Reserva reserva = modelo.RegistrarReserva(solicitante, _reloj);
        lock (_candadoDeReservas)
        {
            _reservas.Add(reserva.Id, reserva);
        }

        solicitante.Registrar(Actividad.ReservaCreada(modelo.Nombre, _reloj));
        return reserva.Id;
    }

    public void CancelarReserva(Guid idReserva, Usuario solicitante)
    {
        Reserva reserva = ObtenerReserva(idReserva);
        if (!reserva.FueHechaPor(solicitante.NombreUsuario))
        {
            throw new DominioException("Solo se pueden cancelar las reservas propias");
        }

        reserva.Cancelar();
        solicitante.Registrar(Actividad.ReservaCancelada(reserva.NombreDelModelo(), _reloj));
    }

    public IReadOnlyList<LecturaDeReserva> ConsultarReservasDe(Usuario usuario)
    {
        List<Reserva> propias;
        lock (_candadoDeReservas)
        {
            propias = _reservas.Values
                .Where(reserva => reserva.FueHechaPor(usuario.NombreUsuario))
                .ToList();
        }

        return propias.Select(reserva => reserva.TomarLectura()).ToList();
    }

    public void ResolverVencidas()
    {
        List<Reserva> vencidas;
        lock (_candadoDeReservas)
        {
            // Se resuelven en orden de llegada. Con poco stock gana la solicitud más vieja.
            vencidas = _reservas.Values
                .Where(reserva => reserva.VencioElPlazo(_segundosDeAprobacion))
                .OrderBy(reserva => reserva.FechaCreacion)
                .ToList();
        }

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
            Usuario? comprador = BuscarUsuario(reserva.NombreDelUsuario());
            comprador?.Registrar(Actividad.Compra(reserva.NombreDelModelo(), _reloj));
        }
    }

    private static bool Cumple(LecturaDeModelo modelo, FiltroDeModelos filtro)
    {
        if (filtro.Texto is not null
            && !modelo.Nombre.Contains(filtro.Texto.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filtro.PrecioMinimo is decimal minimo && modelo.Precio < minimo)
        {
            return false;
        }

        if (filtro.PrecioMaximo is decimal maximo && modelo.Precio > maximo)
        {
            return false;
        }

        if (filtro.FechaEntregaMaxima is DateTime fechaLimite
            && modelo.FechaEntregaPrometida > fechaLimite)
        {
            return false;
        }

        if (filtro.MinimoDisponibles is int minimoDisponibles
            && modelo.UnidadesDisponibles < minimoDisponibles)
        {
            return false;
        }

        return true;
    }

    private Usuario? BuscarUsuario(string nombreUsuario)
    {
        lock (_candadoDeUsuarios)
        {
            _usuarios.TryGetValue(nombreUsuario?.Trim() ?? string.Empty, out Usuario? usuario);
            return usuario;
        }
    }

    private Modelo ObtenerModelo(string nombreModelo)
    {
        lock (_candadoDeModelos)
        {
            string clave = nombreModelo?.Trim() ?? string.Empty;
            if (!_modelos.TryGetValue(clave, out Modelo? modelo))
            {
                throw new DominioException($"No existe el modelo {clave}");
            }

            return modelo;
        }
    }

    private Reserva ObtenerReserva(Guid idReserva)
    {
        lock (_candadoDeReservas)
        {
            if (!_reservas.TryGetValue(idReserva, out Reserva? reserva))
            {
                throw new DominioException($"No existe la reserva {idReserva}");
            }

            return reserva;
        }
    }

    private void ExigirFechaDeEntregaFutura(DateTime fechaEntregaPrometida)
    {
        if (fechaEntregaPrometida.Date <= _reloj.Ahora.Date)
        {
            throw new DominioException("La fecha de entrega debe ser futura");
        }
    }
}
