using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;
using Servidor.Dominio.Repositorios;

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

// Catálogo de modelos: alta, modificación, baja, búsqueda, consulta e imagen. Cada operación
// toma el candado global y dentro lee, valida y guarda, así dos clientes no se pisan.
public class ServicioDeModelos
{
    private readonly IRepositorioDeModelos _modelos;
    private readonly IRepositorioDeReservas _reservas;
    private readonly CandadoGlobal _candado;
    private readonly IImagenesDeModelos _imagenes;

    public ServicioDeModelos(
        IRepositorioDeModelos modelos,
        IRepositorioDeReservas reservas,
        CandadoGlobal candado,
        IImagenesDeModelos imagenes)
    {
        _modelos = modelos;
        _reservas = reservas;
        _candado = candado;
        _imagenes = imagenes;
    }

    public async Task AltaDeModeloAsync(FichaDeModelo ficha, Usuario creador)
    {
        ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
        VehiculoModelo modelo = new VehiculoModelo(ficha, creador);
        await _candado.TomarAsync();
        try
        {
            if (await _modelos.BuscarPorNombreAsync(modelo.Nombre) is not null)
            {
                throw new DominioException($"Ya existe un modelo llamado {modelo.Nombre}");
            }

            await _modelos.AgregarAsync(
                modelo, Actividad.ModeloCreado(ficha.Nombre), creador.NombreUsuario);
        }
        finally
        {
            _candado.Liberar();
        }
    }

    public async Task ModificarModeloAsync(FichaDeModelo ficha, Usuario solicitante)
    {
        await _candado.TomarAsync();
        try
        {
            VehiculoModelo modelo = await _modelos.ObtenerAsync(ficha.Nombre);
            // Solo se exige futura si la fecha efectivamente cambió, para permitir la edición tardía
            // de otros campos de un modelo cuya fecha ya venció.
            if (ficha.Datos.FechaEntregaPrometida.Date != modelo.FechaEntregaVigente.Date)
            {
                ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
            }

            modelo.ModificarDatos(
                ficha,
                solicitante,
                await _reservas.ContarConfirmadasAsync(modelo.Id),
                await _reservas.ContarActivasAsync(modelo.Id));
            await _modelos.ActualizarAsync(
                modelo, Actividad.ModeloModificado(ficha.Nombre), solicitante.NombreUsuario);
        }
        finally
        {
            _candado.Liberar();
        }
    }

    // Se valida la baja, se borra el archivo y recién entonces se borra el modelo de la base.
    // Si el archivo no se puede borrar la baja falla y el modelo queda como estaba.
    // La imagen se aparta antes de borrar la fila y se descarta recién con la baja confirmada. Si
    // no se puede apartar, la baja se rechaza (SR3); si la base falla, la imagen vuelve a su lugar
    // y el modelo sigue completo.
    public async Task EliminarModeloAsync(string nombreModelo, Usuario solicitante)
    {
        await _candado.TomarAsync();
        try
        {
            VehiculoModelo modelo = await _modelos.ObtenerAsync(nombreModelo);
            string nombreArchivoImagen =
                modelo.ValidarBaja(solicitante, await _reservas.ContarActivasAsync(modelo.Id));
            _imagenes.Apartar(nombreArchivoImagen);
            await EliminarConImagenApartadaAsync(modelo, nombreArchivoImagen, solicitante);
            _imagenes.Descartar(nombreArchivoImagen);
        }
        finally
        {
            _candado.Liberar();
        }
    }

    private async Task EliminarConImagenApartadaAsync(
        VehiculoModelo modelo, string nombreArchivoImagen, Usuario solicitante)
    {
        try
        {
            await _modelos.EliminarAsync(
                modelo, Actividad.ModeloEliminado(modelo.Nombre), solicitante.NombreUsuario);
        }
        catch
        {
            _imagenes.Restaurar(nombreArchivoImagen);
            throw;
        }
    }

    // Devuelve el nombre de la imagen reemplazada para que la capa de arriba borre el archivo viejo.
    public async Task<string> CambiarImagenDeModeloAsync(
        string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante)
    {
        await _candado.TomarAsync();
        try
        {
            VehiculoModelo modelo = await _modelos.ObtenerAsync(nombreModelo);
            string nombreArchivoAnterior = modelo.CambiarImagen(
                solicitante,
                nuevoNombreArchivoImagen,
                await _reservas.ContarConfirmadasAsync(modelo.Id));
            await _modelos.ActualizarAsync(
                modelo, Actividad.ModeloModificado(modelo.Nombre), solicitante.NombreUsuario);
            return nombreArchivoAnterior;
        }
        finally
        {
            _candado.Liberar();
        }
    }

    // Al arrancar: la base dice qué imágenes están en uso y la carpeta se ajusta a eso. Cubre lo que
    // pudo quedar desparejo tras un cierre forzado o un commit de resultado incierto.
    public async Task ReconciliarImagenesAsync()
    {
        await _candado.TomarAsync();
        try
        {
            HashSet<string> enUso = (await _modelos.ListarAsync())
                .Select(item => item.Modelo.Datos.NombreArchivoImagen)
                .Where(nombre => nombre.Length > 0)
                .ToHashSet();
            _imagenes.Reconciliar(enUso);
        }
        finally
        {
            _candado.Liberar();
        }
    }

    public async Task<IReadOnlyList<LecturaDeModelo>> BuscarAsync(FiltroDeModelos filtro)
    {
        await _candado.TomarAsync();
        try
        {
            return (await _modelos.ListarAsync())
                .Select(item => item.Modelo.TomarLectura(item.Confirmadas))
                .Where(modelo => modelo.TieneUnidadDisponible)
                .Where(modelo => Cumple(modelo, filtro))
                .ToList();
        }
        finally
        {
            _candado.Liberar();
        }
    }

    public async Task<LecturaDeModelo> ConsultarLecturaAsync(string nombreModelo)
    {
        await _candado.TomarAsync();
        try
        {
            VehiculoModelo modelo = await _modelos.ObtenerAsync(nombreModelo);
            return modelo.TomarLectura(await _reservas.ContarConfirmadasAsync(modelo.Id));
        }
        finally
        {
            _candado.Liberar();
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
