namespace Dominio.Excepciones;

public class SinUnidadesDisponiblesException : DominioException
{
    public SinUnidadesDisponiblesException(string nombreModelo)
        : base($"El modelo {nombreModelo} no tiene unidades disponibles")
    {
    }
}
