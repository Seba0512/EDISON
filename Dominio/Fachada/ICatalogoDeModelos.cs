using Dominio.Busqueda;

namespace Dominio.Fachada;

public interface ICatalogoDeModelos
{
    void AltaDeModelo(FichaDeModelo ficha, Usuario creador);

    void ModificarModelo(FichaDeModelo ficha, Usuario solicitante);

    void EliminarModelo(string nombreModelo, Usuario solicitante);

    string CambiarImagenDeModelo(string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante);

    IReadOnlyList<LecturaDeModelo> Buscar(ICriterioDeBusqueda criterio);

    LecturaDeModelo ConsultarLectura(string nombreModelo);
}
