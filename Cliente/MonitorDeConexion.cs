using System.Net.Sockets;

namespace Cliente;

// El hilo principal queda bloqueado esperando entrada por consola entre operaciones,
// así que sin este monitor una caída del servidor recién se notaría en la próxima operación.
internal sealed class MonitorDeConexion : IDisposable
{
    private static readonly TimeSpan IntervaloDeChequeo = TimeSpan.FromMilliseconds(300);

    private readonly Socket _socket;
    private readonly object _candadoDeSocket;
    private readonly Action _alDetectarCaida;
    private readonly Thread _hilo;
    private volatile bool _detenido;

    // El candado es el mismo que usa ConexionConServidor durante sus operaciones. Sin compartirlo,
    // Poll+Available podría correr en paralelo con un read real: el monitor puede ver el socket
    // "listo para leer" justo cuando llega una respuesta y, para cuando consulta Available, el hilo
    // principal ya la consumió, dando un falso positivo de conexión caída.
    public MonitorDeConexion(Socket socket, object candadoDeSocket, Action alDetectarCaida)
    {
        _socket = socket;
        _candadoDeSocket = candadoDeSocket;
        _alDetectarCaida = alDetectarCaida;
        _hilo = new Thread(Vigilar) { IsBackground = true };
        _hilo.Start();
    }

    private void Vigilar()
    {
        while (!_detenido)
        {
            if (!ConexionCaida())
            {
                Thread.Sleep(IntervaloDeChequeo);
                continue;
            }

            if (!_detenido)
                _alDetectarCaida();
            return;
        }
    }

    private bool ConexionCaida()
    {
        // Si el candado está tomado, hay una operación real en curso: se salta este chequeo.
        if (!Monitor.TryEnter(_candadoDeSocket, IntervaloDeChequeo))
            return false;

        try
        {
            // Poll(0, ...) no bloquea: solo consulta el estado actual del socket.
            // "Listo para leer" sin datos disponibles significa que el otro lado cerró la conexión.
            return _socket.Poll(0, SelectMode.SelectRead) && _socket.Available == 0;
        }
        catch (SocketException)
        {
            return true;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
        finally
        {
            Monitor.Exit(_candadoDeSocket);
        }
    }

    public void Dispose()
    {
        _detenido = true;
        if (_hilo.IsAlive)
            _hilo.Join();
    }
}
