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
        // Solo se exige futura si la fecha efectivamente cambió: así no se rompe la subida de imagen
        // (que reusa la fecha almacenada) ni la edición tardía de otros campos de un modelo ya vencido.
        if (ficha.Datos.FechaEntregaPrometida.Date != modelo.FechaEntregaPrometida.Date)
            ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);

        modelo.Modificar(ficha, solicitante);

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

    public IReadOnlyList<Modelo> Buscar(ICriterioDeBusqueda criterio)
    {
        return modelos.Todos().Where(criterio.Cumple).ToList();
    }

    public Modelo Consultar(string nombreModelo)
    {
        return modelos.ObtenerPorNombre(nombreModelo);
    }

    private void BorrarImagen(string nombreArchivoImagen)
    {
        if (nombreArchivoImagen.Length > 0)
        {
            imagenes.Eliminar(nombreArchivoImagen);
        }
    }
}
