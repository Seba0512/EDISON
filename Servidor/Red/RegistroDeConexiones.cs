namespace Servidor.Red;

internal sealed class RegistroDeConexiones
{
    // El candado solo cuida la lista. Con él tomado no se usa la red ni se espera a otro hilo.
    private readonly object _candado = new();
    private readonly HashSet<ConexionActiva> _conexiones = [];

    public void Agregar(ConexionActiva conexion)
    {
        ArgumentNullException.ThrowIfNull(conexion);
        lock (_candado)
            _conexiones.Add(conexion);
    }

    public void Quitar(ConexionActiva conexion)
    {
        lock (_candado)
            _conexiones.Remove(conexion);
    }

    public ConexionActiva[] TomarSnapshot()
    {
        lock (_candado)
            return _conexiones.ToArray();
    }
}
