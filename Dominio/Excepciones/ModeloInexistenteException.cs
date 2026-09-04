namespace Dominio.Excepciones;

/// <summary>
/// Referencia a un modelo que no esta en el catalogo (CR3, CR4, CR5, CR8). La lanza el repositorio
/// en lugar de devolver null, para que ningun llamador tenga que preguntar antes de usar el modelo.
/// </summary>
public class ModeloInexistenteException : DominioException
{
    public ModeloInexistenteException(string nombreModelo)
        : base($"No existe el modelo {nombreModelo}")
    {
    }
}
