using Dominio.Busqueda;
using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

public class CatalogoDeModelos : ICatalogoDeModelos
{
    private readonly IRepositorioDeModelos modelos;
    private readonly IAlmacenDeImagenes imagenes;
    private readonly IReloj reloj;

    public CatalogoDeModelos(
        IRepositorioDeModelos modelos, IAlmacenDeImagenes imagenes, IReloj reloj)
    {
        this.modelos = modelos;
        this.imagenes = imagenes;
        this.reloj = reloj;
    }

    public void AltaDeModelo(FichaDeModelo ficha, Usuario creador)
    {
        ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);

        Modelo modelo = new Modelo(ficha.Datos, creador);
        modelo.AgregarUnidades(ficha.Stock);
        modelos.Agregar(modelo);

        creador.Registrar(Actividad.ModeloCreado(ficha.Nombre, reloj));
    }

    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        Modelo modelo = modelos.ObtenerPorNombre(ficha.Nombre);
        // Solo se exige futura si la fecha efectivamente cambió, para permitir la edición tardía
        // de otros campos de un modelo cuya fecha ya venció.
        if (ficha.Datos.FechaEntregaPrometida.Date != modelo.FechaEntregaPrometida.Date)
            ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);

        modelo.ModificarDatos(ficha, solicitante);

        solicitante.Registrar(Actividad.ModeloModificado(ficha.Nombre, reloj));
    }

    public void EliminarModelo(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = modelos.ObtenerPorNombre(nombreModelo);
        string nombreArchivoImagen = modelo.MarcarComoEliminado(solicitante);
        modelos.Eliminar(modelo);
        BorrarImagen(nombreArchivoImagen);

        solicitante.Registrar(Actividad.ModeloEliminado(modelo.Nombre, reloj));
    }

    public string CambiarImagenDeModelo(string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante)
    {
        Modelo modelo = modelos.ObtenerPorNombre(nombreModelo);
        string nombreArchivoAnterior = modelo.CambiarImagen(solicitante, nuevoNombreArchivoImagen);

        solicitante.Registrar(Actividad.ModeloModificado(modelo.Nombre, reloj));

        return nombreArchivoAnterior;
    }

    private void ExigirFechaDeEntregaFutura(DateTime fechaEntregaPrometida)
    {
        if (fechaEntregaPrometida.Date <= reloj.Ahora.Date)
        {
            throw new DominioException("La fecha de entrega debe ser futura");
        }
    }

    public IReadOnlyList<LecturaDeModelo> Buscar(ICriterioDeBusqueda criterio)
    {
        return modelos.Todos()
            .Select(modelo => modelo.TomarLectura())
            .Where(modelo => modelo.TieneUnidadDisponible)
            .Where(criterio.Cumple)
            .ToList();
    }

    public Modelo Consultar(string nombreModelo)
    {
        return modelos.ObtenerPorNombre(nombreModelo);
    }

    public LecturaDeModelo ConsultarLectura(string nombreModelo)
    {
        return modelos.ObtenerPorNombre(nombreModelo).TomarLectura();
    }

    private void BorrarImagen(string nombreArchivoImagen)
    {
        if (nombreArchivoImagen.Length > 0)
        {
            imagenes.Eliminar(nombreArchivoImagen);
        }
    }
}
