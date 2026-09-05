using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Dominio.Tests.Dobles;

/// <summary>
/// Catalogo en memoria para las pruebas. Un candado privado protege el indice; las consultas de
/// conjunto toman un snapshot para no exponer la coleccion mutable ni ejecutar filtros bajo lock.
/// </summary>
public class RepositorioDeModelosEnMemoria : IRepositorioDeModelos
{
    private readonly object candado = new();
    private readonly Dictionary<string, Modelo> modelos = new(StringComparer.OrdinalIgnoreCase);

    public void Agregar(Modelo modelo)
    {
        string nombre = modelo.Nombre;
        lock (candado)
        {
            if (modelos.ContainsKey(nombre))
            {
                throw new DominioException($"Ya existe un modelo llamado {nombre}");
            }

            modelos.Add(nombre, modelo);
        }
    }

    public Modelo ObtenerPorNombre(string nombreModelo)
    {
        lock (candado)
        {
            if (!modelos.TryGetValue(nombreModelo.Trim(), out Modelo? modelo))
            {
                throw new ModeloInexistenteException(nombreModelo);
            }

            return modelo;
        }
    }

    public bool Existe(string nombreModelo)
    {
        lock (candado)
        {
            return modelos.ContainsKey(nombreModelo.Trim());
        }
    }

    public void Eliminar(string nombreModelo)
    {
        lock (candado)
        {
            modelos.Remove(nombreModelo.Trim());
        }
    }

    public IReadOnlyList<Modelo> Todos()
    {
        lock (candado)
        {
            return modelos.Values.ToList();
        }
    }
}
