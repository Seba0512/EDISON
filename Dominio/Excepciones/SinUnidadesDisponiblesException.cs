namespace Dominio.Excepciones;

/// <summary>
/// Intento de tomar una unidad de un modelo que no tiene ninguna libre (CR5). En el camino normal la
/// solicitud vencida se rechaza sin llegar aca: esta excepcion protege el invariante del pool contra
/// cualquier otro camino que intente tomar una unidad inexistente.
/// </summary>
public class SinUnidadesDisponiblesException : DominioException
{
    public SinUnidadesDisponiblesException(string nombreModelo)
        : base($"El modelo {nombreModelo} no tiene unidades disponibles")
    {
    }
}
