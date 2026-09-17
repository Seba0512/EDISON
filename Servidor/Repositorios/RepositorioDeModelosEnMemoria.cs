using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Servidor.Repositorios;

internal sealed class RepositorioDeModelosEnMemoria : IRepositorioDeModelos
{
    private readonly object _candado = new();
    private readonly Dictionary<string, Modelo> _modelos = new(StringComparer.OrdinalIgnoreCase);

    public void Agregar(Modelo modelo)
    {
        string nombre = modelo.Nombre;
        lock (_candado)
        {
            if (_modelos.ContainsKey(nombre))
                throw new DominioException($"Ya existe un modelo llamado {nombre}");

            _modelos.Add(nombre, modelo);
        }
    }

    public Modelo ObtenerPorNombre(string nombreModelo)
    {
        lock (_candado)
        {
            if (!_modelos.TryGetValue(nombreModelo.Trim(), out Modelo? modelo))
                throw new ModeloInexistenteException(nombreModelo);

            return modelo;
        }
    }

    public void Eliminar(Modelo modelo)
    {
        string nombre = modelo.Nombre;
        lock (_candado)
        {
            if (!_modelos.TryGetValue(nombre, out Modelo? registrado)
                || !ReferenceEquals(registrado, modelo))
                throw new ModeloInexistenteException(nombre);

            _modelos.Remove(nombre);
        }
    }

    public IReadOnlyList<Modelo> Todos()
    {
        lock (_candado)
            return _modelos.Values.ToList();
    }
}
