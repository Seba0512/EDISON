namespace Servidor.Red;

internal sealed class RegistroDeConexiones
{
    // El candado coordina admisión y registro. Con él tomado nunca se hace Join ni I/O bloqueante.
    private readonly object _candado = new();
    private readonly HashSet<ConexionActiva> _conexiones = [];
    private bool _admiteNuevasConexiones;

    public void AbrirAdmision()
    {
        lock (_candado)
        {
            DescartarTerminadasSinLock();
            _admiteNuevasConexiones = true;
        }
    }

    public bool IntentarAgregarEIniciar(ConexionActiva conexion)
    {
        ArgumentNullException.ThrowIfNull(conexion);
        lock (_candado)
        {
            if (!_admiteNuevasConexiones)
                return false;

            DescartarTerminadasSinLock();
            _conexiones.Add(conexion);
            IniciarRegistrada(conexion);
            return true;
        }
    }

    public ConexionActiva[] CerrarAdmisionYTomarSnapshot()
    {
        lock (_candado)
        {
            _admiteNuevasConexiones = false;
            return _conexiones.ToArray();
        }
    }

    public void Quitar(ConexionActiva conexion)
    {
        ArgumentNullException.ThrowIfNull(conexion);
        lock (_candado)
            _conexiones.Remove(conexion);
    }

    public ConexionActiva[] TomarSnapshot()
    {
        lock (_candado)
            return _conexiones.ToArray();
    }

    public void DescartarTerminadas()
    {
        lock (_candado)
            DescartarTerminadasSinLock();
    }

    public bool TieneConexiones
    {
        get
        {
            lock (_candado)
                return _conexiones.Count > 0;
        }
    }

    private void IniciarRegistrada(ConexionActiva conexion)
    {
        try
        {
            conexion.Iniciar();
        }
        catch
        {
            _conexiones.Remove(conexion);
            throw;
        }
    }

    private void DescartarTerminadasSinLock()
        => _conexiones.RemoveWhere(conexion => !conexion.TieneThreadVivo);
}
