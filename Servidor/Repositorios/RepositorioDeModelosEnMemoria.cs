using Dominio;
using Dominio.Excepciones;
using Dominio.Repositorios;

namespace Servidor.Repositorios;

/// <summary>
/// Implementación productiva de <see cref="IRepositorioDeModelos"/> (CR2-CR4, CR7, CR8): modelos en
/// memoria, vivos mientras el proceso del servidor esté en pie. Portado tal cual desde el doble ya
/// validado por <c>RepositoriosConcurrentesTests</c> en <c>Dominio.Tests</c>.
/// </summary>
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

    public bool Existe(string nombreModelo)
    {
        lock (_candado)
            return _modelos.ContainsKey(nombreModelo.Trim());
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
