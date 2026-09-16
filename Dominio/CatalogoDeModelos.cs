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
        Modelo modelo = new Modelo(ficha.Datos, creador);
        modelo.AgregarUnidades(ficha.Stock);
        modelos.Agregar(modelo);

        creador.Registrar(Actividad.ModeloCreado(ficha.Nombre, reloj));
    }

    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        Modelo modelo = modelos.ObtenerPorNombre(ficha.Nombre);
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
