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

    public async Task AltaDeModeloAsync(FichaDeModelo ficha, Usuario creador)
    {
        ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
        VehiculoModelo modelo = new VehiculoModelo(ficha, creador);
        await _estado.Candado.WaitAsync();
        try
        {
            if (_estado.Modelos.ContainsKey(modelo.Nombre))
            {
                throw new DominioException($"Ya existe un modelo llamado {modelo.Nombre}");
            }

            _estado.Modelos.Add(modelo.Nombre, modelo);
            creador.Registrar(Actividad.ModeloCreado(ficha.Nombre));
        }
        finally
        {
            _estado.Candado.Release();
        }
    }

    public async Task ModificarModeloAsync(FichaDeModelo ficha, Usuario solicitante)
    {
        await _estado.Candado.WaitAsync();
        try
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
        finally
        {
            _estado.Candado.Release();
        }
    }

    // Se valida y bloquea el modelo bajo el candado; el archivo se borra fuera de él.
    // Solo después se quita el modelo y se registra la baja en el historial.
    public async Task EliminarModeloAsync(string nombreModelo, Usuario solicitante, Action<string> eliminarImagen)
    {
        VehiculoModelo modelo;
        string nombreArchivoImagen;
        await _estado.Candado.WaitAsync();
        try
        {
            modelo = _estado.ObtenerModelo(nombreModelo);
            nombreArchivoImagen =
                modelo.MarcarComoEliminado(solicitante, _estado.ContarActivas(modelo));
        }
        finally
        {
            _estado.Candado.Release();
        }

        try
        {
            eliminarImagen(nombreArchivoImagen);
        }
        catch
        {
            await _estado.Candado.WaitAsync();
            try
            {
                modelo.RestaurarTrasBajaFallida();
            }
            finally
            {
                _estado.Candado.Release();
            }

            throw;
        }

        await _estado.Candado.WaitAsync();
        try
        {
            _estado.Modelos.Remove(modelo.Nombre);
            solicitante.Registrar(Actividad.ModeloEliminado(modelo.Nombre));
        }
        finally
        {
            _estado.Candado.Release();
        }
    }

    // Devuelve el nombre de la imagen reemplazada para que la capa de arriba borre el archivo viejo.
    public async Task<string> CambiarImagenDeModeloAsync(
        string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante)
    {
        await _estado.Candado.WaitAsync();
        try
        {
            VehiculoModelo modelo = _estado.ObtenerModelo(nombreModelo);
            string nombreArchivoAnterior = modelo.CambiarImagen(
                solicitante, nuevoNombreArchivoImagen, _estado.ContarConfirmadas(modelo));
            solicitante.Registrar(Actividad.ModeloModificado(modelo.Nombre));
            return nombreArchivoAnterior;
        }
        finally
        {
            _estado.Candado.Release();
        }
    }

    public async Task<IReadOnlyList<LecturaDeModelo>> BuscarAsync(FiltroDeModelos filtro)
    {
        await _estado.Candado.WaitAsync();
        try
        {
            return _estado.Modelos.Values
                .Where(modelo => !modelo.EstaEliminado)
                .Select(modelo => modelo.TomarLectura(_estado.ContarConfirmadas(modelo)))
                .Where(modelo => modelo.TieneUnidadDisponible)
                .Where(modelo => Cumple(modelo, filtro))
                .ToList();
        }
        finally
        {
            _estado.Candado.Release();
        }
    }

    public async Task<LecturaDeModelo> ConsultarLecturaAsync(string nombreModelo)
    {
        await _estado.Candado.WaitAsync();
        try
        {
            VehiculoModelo modelo = _estado.ObtenerModelo(nombreModelo);
            return modelo.TomarLectura(_estado.ContarConfirmadas(modelo));
        }
        finally
        {
            _estado.Candado.Release();
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
        // La fecha de entrega es una fecha de calendario sin zona: se compara con el día local,
        // igual que el cliente. Con UTC, en Uruguay después de las 21 "hoy" ya sería mañana.
        if (fechaEntregaPrometida.Date <= DateTime.Today)
        {
            throw new DominioException("La fecha de entrega debe ser futura");
        }
    }
}
