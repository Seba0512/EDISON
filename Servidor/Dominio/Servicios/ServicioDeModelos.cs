using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;

namespace Servidor.Dominio.Servicios;

public sealed record FiltroDeModelos(
    string? Texto,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo,
    DateTime? FechaEntregaMaxima,
    int? MinimoDisponibles)
{
    public static FiltroDeModelos SinFiltros => new(null, null, null, null, null);
}

// Catálogo de modelos: alta, modificación, baja, búsqueda, consulta e imagen.
public class ServicioDeModelos
{
    private readonly EstadoEnMemoria _estado;

    public ServicioDeModelos(EstadoEnMemoria estado)
    {
        _estado = estado;
    }

    public void AltaDeModelo(FichaDeModelo ficha, Usuario creador)
    {
        ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
        VehiculoModelo modelo = new VehiculoModelo(ficha, creador);
        lock (_estado.Candado)
        {
            if (_estado.Modelos.ContainsKey(modelo.Nombre))
            {
                throw new DominioException($"Ya existe un modelo llamado {modelo.Nombre}");
            }

            _estado.Modelos.Add(modelo.Nombre, modelo);
            creador.Registrar(Actividad.ModeloCreado(ficha.Nombre));
        }
    }

    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        lock (_estado.Candado)
        {
            VehiculoModelo modelo = _estado.ObtenerModelo(ficha.Nombre);
            // Solo se exige futura si la fecha efectivamente cambió, para permitir la edición tardía
            // de otros campos de un modelo cuya fecha ya venció.
            if (ficha.Datos.FechaEntregaPrometida.Date != modelo.FechaEntregaVigente.Date)
            {
                ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
            }

            modelo.ModificarDatos(
                ficha, solicitante, _estado.ContarConfirmadas(modelo), _estado.ContarActivas(modelo));
            solicitante.Registrar(Actividad.ModeloModificado(ficha.Nombre));
        }
    }

    // Devuelve el nombre del archivo de imagen para que la capa de arriba borre el archivo físico.
    public string EliminarModelo(string nombreModelo, Usuario solicitante)
    {
        lock (_estado.Candado)
        {
            VehiculoModelo modelo = _estado.ObtenerModelo(nombreModelo);
            string nombreArchivoImagen =
                modelo.MarcarComoEliminado(solicitante, _estado.ContarActivas(modelo));
            _estado.Modelos.Remove(modelo.Nombre);
            solicitante.Registrar(Actividad.ModeloEliminado(modelo.Nombre));
            return nombreArchivoImagen;
        }
    }

    // Devuelve el nombre de la imagen reemplazada para que la capa de arriba borre el archivo viejo.
    public string CambiarImagenDeModelo(
        string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante)
    {
        lock (_estado.Candado)
        {
            VehiculoModelo modelo = _estado.ObtenerModelo(nombreModelo);
            string nombreArchivoAnterior = modelo.CambiarImagen(
                solicitante, nuevoNombreArchivoImagen, _estado.ContarConfirmadas(modelo));
            solicitante.Registrar(Actividad.ModeloModificado(modelo.Nombre));
            return nombreArchivoAnterior;
        }
    }

    public IReadOnlyList<LecturaDeModelo> Buscar(FiltroDeModelos filtro)
    {
        lock (_estado.Candado)
        {
            return _estado.Modelos.Values
                .Select(modelo => modelo.TomarLectura(_estado.ContarConfirmadas(modelo)))
                .Where(modelo => modelo.TieneUnidadDisponible)
                .Where(modelo => Cumple(modelo, filtro))
                .ToList();
        }
    }

    public LecturaDeModelo ConsultarLectura(string nombreModelo)
    {
        lock (_estado.Candado)
        {
            VehiculoModelo modelo = _estado.ObtenerModelo(nombreModelo);
            return modelo.TomarLectura(_estado.ContarConfirmadas(modelo));
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

    private static void ExigirFechaDeEntregaFutura(DateTime fechaEntregaPrometida)
    {
        if (fechaEntregaPrometida.Date <= DateTime.UtcNow.Date)
        {
            throw new DominioException("La fecha de entrega debe ser futura");
        }
    }
}
