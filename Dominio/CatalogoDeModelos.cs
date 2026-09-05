using Dominio.Busqueda;
using Dominio.Excepciones;
using Dominio.Fachada;
using Dominio.Repositorios;
using Dominio.Tiempo;

namespace Dominio;

/// <summary>
/// Alta, modificacion, baja, busqueda y consulta de modelos (CR2, CR3, CR4, CR7, CR8).
///
/// Responsabilidad en una frase, sin "y": administrar el catalogo de modelos. Orquesta, no decide:
/// si un modelo puede modificarse, cuanto stock admite o si puede eliminarse lo resuelve el propio
/// <see cref="Modelo"/>, que es quien tiene las unidades y las reservas a la vista.
///
/// El orden de los candados es repositorio y despues modelo: cada operacion busca primero en el
/// repositorio, y recien con el modelo en la mano lo opera. Nunca se llama al repositorio teniendo
/// tomado el candado de un modelo.
/// </summary>
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

    /// <summary>Alta de CR2: el nombre del modelo es unico en todo el catalogo.</summary>
    public void AltaDeModelo(FichaDeModelo ficha, Usuario creador)
    {
        Modelo modelo = new Modelo(ficha.Datos, creador);
        modelo.AgregarUnidades(ficha.Stock);
        modelos.Agregar(modelo);

        creador.Registrar(Actividad.ModeloCreado(ficha.Nombre, reloj));
    }

    /// <summary>
    /// Modificacion de CR3. El catalogo solo localiza el agregado; el modelo valida autorizacion,
    /// datos y stock y aplica la modificacion completa bajo su propio candado. El historial se
    /// registra despues, fuera de la region critica del agregado.
    /// </summary>
    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        Modelo modelo = modelos.ObtenerPorNombre(ficha.Nombre);
        modelo.Modificar(ficha, solicitante);

        solicitante.Registrar(Actividad.ModeloModificado(ficha.Nombre, reloj));
    }

    /// <summary>
    /// Baja de CR4. Borrar la imagen es parte de la baja y no un paso aparte: asi SR3 se cumple aca
    /// mismo y no depende de que alguien se acuerde de llamarlo despues.
    /// </summary>
    public void EliminarModelo(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = modelos.ObtenerPorNombre(nombreModelo);
        modelo.ExigirPropietario(solicitante);
        if (!modelo.PuedeEliminarse())
        {
            throw new DominioException(
                $"No se puede eliminar el modelo {modelo.Nombre} porque tiene reservas activas");
        }

        BorrarImagenDe(modelo);
        modelos.Eliminar(modelo.Nombre);

        solicitante.Registrar(Actividad.ModeloEliminado(modelo.Nombre, reloj));
    }

    public IReadOnlyList<Modelo> Buscar(ICriterioDeBusqueda criterio)
    {
        return modelos.Todos().Where(criterio.Cumple).ToList();
    }

    public Modelo Consultar(string nombreModelo)
    {
        return modelos.ObtenerPorNombre(nombreModelo);
    }

    private void BorrarImagenDe(Modelo modelo)
    {
        if (modelo.TieneImagen)
        {
            imagenes.Eliminar(modelo.NombreArchivoImagen);
        }
    }
}
