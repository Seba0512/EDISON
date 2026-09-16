namespace Dominio.Excepciones;

public class ModeloInexistenteException : DominioException
{
    public ModeloInexistenteException(string nombreModelo)
        : base($"No existe el modelo {nombreModelo}")
    {
    }
}
