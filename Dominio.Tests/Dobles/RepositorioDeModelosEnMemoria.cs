using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Dominio.Tests.Dobles;

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

    // Helper solo de pruebas: IRepositorioDeModelos ya no expone esta consulta.
    public bool Existe(string nombreModelo)
    {
        lock (candado)
        {
            return modelos.ContainsKey(nombreModelo.Trim());
        }
    }

    public void Eliminar(Modelo modelo)
    {
        string nombre = modelo.Nombre;
        lock (candado)
        {
            if (!modelos.TryGetValue(nombre, out Modelo? registrado)
                || !ReferenceEquals(registrado, modelo))
            {
                throw new ModeloInexistenteException(nombre);
            }

            modelos.Remove(nombre);
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
