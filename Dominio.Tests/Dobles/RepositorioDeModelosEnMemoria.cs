using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Dominio.Tests.Dobles;

/// <summary>Catalogo en memoria para las pruebas: lanza en lugar de devolver null, como el contrato.</summary>
public class RepositorioDeModelosEnMemoria : IRepositorioDeModelos
{
    private readonly Dictionary<string, Modelo> modelos = new(StringComparer.OrdinalIgnoreCase);

    public void Agregar(Modelo modelo)
    {
        modelos[modelo.Nombre] = modelo;
    }

    public Modelo ObtenerPorNombre(string nombreModelo)
    {
        if (!modelos.TryGetValue(nombreModelo.Trim(), out Modelo? modelo))
        {
            throw new ModeloInexistenteException(nombreModelo);
        }

        return modelo;
    }

    public bool Existe(string nombreModelo)
    {
        return modelos.ContainsKey(nombreModelo.Trim());
    }

    public void Eliminar(string nombreModelo)
    {
        modelos.Remove(nombreModelo.Trim());
    }

    public IReadOnlyList<Modelo> Todos()
    {
        return modelos.Values.ToList();
    }
}
