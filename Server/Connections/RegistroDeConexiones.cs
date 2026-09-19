namespace Server.Connections;

internal sealed class RegistroDeConexiones
{
    // El candado coordina admisión y registro. Con él tomado nunca se hace Join ni I/O bloqueante.
    private readonly object _candado = new();
    private readonly HashSet<ConexionActiva> _conexiones = [];
    private bool _admiteNuevasConexiones;

    public bool TieneConexiones
    {
        get
        {
            lock (_candado)
            {
                return _conexiones.Count > 0;
            }
        }
    }

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
            {
                return false;
            }

            DescartarTerminadasSinLock();
            _conexiones.Add(conexion);
            IniciarRegistrada(conexion);
            return true;
        }
    }

    public void CerrarAdmision()
    {
        lock (_candado)
        {
            _admiteNuevasConexiones = false;
        }
    }

    public void Quitar(ConexionActiva conexion)
    {
        ArgumentNullException.ThrowIfNull(conexion);
        lock (_candado)
        {
            _conexiones.Remove(conexion);
        }
    }

    // Cierra los sockets, espera a cada Thread y devuelve lo que no se pudo cerrar.
    // Se llama con la admisión ya cerrada: ninguna conexión nueva aparece a mitad del cierre.
    public List<string> CerrarTodas(TimeSpan timeout)
    {
        List<string> problemas = [];
        ConexionActiva[] conexiones = TomarSnapshot();
        IntentarCerrar(conexiones, problemas);
        foreach (ConexionActiva conexion in conexiones)
        {
            IntentarUnir(conexion, timeout, problemas);
        }

        DescartarTerminadas();
        AgregarThreadsVivos(problemas);
        return problemas;
    }

    private ConexionActiva[] TomarSnapshot()
    {
        lock (_candado)
        {
            return _conexiones.ToArray();
        }
    }

    private void DescartarTerminadas()
    {
        lock (_candado)
        {
            DescartarTerminadasSinLock();
        }
    }

    private static void IntentarCerrar(
        IEnumerable<ConexionActiva> conexiones, List<string> problemas)
    {
        foreach (ConexionActiva conexion in conexiones)
        {
            try
            {
                conexion.Cerrar();
            }
            catch (Exception exception)
            {
                problemas.Add(DescribirProblema(
                    $"No se pudo cerrar el cliente {conexion.Numero}", exception));
            }
        }
    }

    private static void IntentarUnir(
        ConexionActiva conexion, TimeSpan timeout, List<string> problemas)
    {
        try
        {
            if (!conexion.Unir(timeout))
            {
                problemas.Add(
                    $"El Thread del cliente {conexion.Numero} no terminó dentro del timeout");
            }
        }
        catch (Exception exception)
        {
            problemas.Add(DescribirProblema(
                $"No se pudo esperar al cliente {conexion.Numero}", exception));
        }
    }

    private void AgregarThreadsVivos(List<string> problemas)
    {
        foreach (ConexionActiva conexion in TomarSnapshot())
        {
            if (conexion.TieneThreadVivo)
            {
                problemas.Add($"El Thread del cliente {conexion.Numero} continúa vivo");
            }
        }
    }

    private static string DescribirProblema(string descripcion, Exception exception)
    {
        return $"{descripcion}: {exception.GetType().Name}: {exception.Message}";
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
    {
        _conexiones.RemoveWhere(conexion => !conexion.TieneThreadVivo);
    }
}
