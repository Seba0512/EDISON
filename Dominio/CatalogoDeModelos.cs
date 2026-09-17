using Dominio.Busqueda;
using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

public class CatalogoDeModelos : ICatalogoDeModelos
{
    private readonly IRepositorioDeModelos _modelos;
    private readonly IAlmacenDeImagenes _imagenes;
    private readonly IReloj _reloj;

    public CatalogoDeModelos(
        IRepositorioDeModelos modelos, IAlmacenDeImagenes imagenes, IReloj reloj)
    {
        _modelos = modelos;
        _imagenes = imagenes;
        _reloj = reloj;
    }

    public void AltaDeModelo(FichaDeModelo ficha, Usuario creador)
    {
        ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);

        Modelo modelo = new Modelo(ficha.Datos, creador);
        modelo.AgregarUnidades(ficha.Stock);
        _modelos.Agregar(modelo);

        creador.Registrar(Actividad.ModeloCreado(ficha.Nombre, _reloj));
    }

    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        Modelo modelo = _modelos.ObtenerPorNombre(ficha.Nombre);
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

    public void EliminarModelo(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = _modelos.ObtenerPorNombre(nombreModelo);
        string nombreArchivoImagen = modelo.MarcarComoEliminado(solicitante);
        // Se conserva en el repositorio hasta borrar la imagen: así se reserva el nombre
        // y una falla de almacenamiento se compensa reactivando la misma entidad.
        try
        {
            BorrarImagen(nombreArchivoImagen);
        }
        catch (ErrorDeAlmacenamientoDeImagenException)
        {
            modelo.RestaurarTrasBajaFallida();
            throw;
        }

        _modelos.Eliminar(modelo);
        solicitante.Registrar(Actividad.ModeloEliminado(modelo.Nombre, _reloj));
    }

    public string CambiarImagenDeModelo(string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante)
    {
        Modelo modelo = _modelos.ObtenerPorNombre(nombreModelo);
        string nombreArchivoAnterior = modelo.CambiarImagen(solicitante, nuevoNombreArchivoImagen);

        solicitante.Registrar(Actividad.ModeloModificado(modelo.Nombre, _reloj));

        return nombreArchivoAnterior;
    }

    public IReadOnlyList<LecturaDeModelo> Buscar(ICriterioDeBusqueda criterio)
    {
        return _modelos.Todos()
            .Select(modelo => modelo.TomarLectura())
            .Where(modelo => modelo.TieneUnidadDisponible)
            .Where(criterio.Cumple)
            .ToList();
    }

    public LecturaDeModelo ConsultarLectura(string nombreModelo)
    {
        return _modelos.ObtenerPorNombre(nombreModelo).TomarLectura();
    }

    private void ExigirFechaDeEntregaFutura(DateTime fechaEntregaPrometida)
    {
        if (fechaEntregaPrometida.Date <= _reloj.Ahora.Date)
        {
            throw new DominioException("La fecha de entrega debe ser futura");
        }
    }

    private void BorrarImagen(string nombreArchivoImagen)
    {
        if (nombreArchivoImagen.Length > 0)
        {
            _imagenes.Eliminar(nombreArchivoImagen);
        }
    }
}
