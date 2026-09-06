using Dominio.Busqueda;

namespace Dominio.Fachada;

/// <summary>Alta, modificacion, baja, busqueda y consulta de modelos (CR2, CR3, CR4, CR7, CR8).</summary>
public interface ICatalogoDeModelos
{
    void AltaDeModelo(FichaDeModelo ficha, Usuario creador);

    void ModificarModelo(FichaDeModelo ficha, Usuario solicitante);

    void EliminarModelo(string nombreModelo, Usuario solicitante);

    /// <summary>Filtra el catalogo con el criterio recibido (CR7). Coleccion vacia si no hay coincidencias.</summary>
    IReadOnlyList<Modelo> Buscar(ICriterioDeBusqueda criterio);

    /// <exception cref="Excepciones.ModeloInexistenteException">Si no existe ese modelo.</exception>
    Modelo Consultar(string nombreModelo);
}
