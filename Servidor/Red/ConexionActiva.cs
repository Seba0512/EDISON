using System.Net.Sockets;

namespace Servidor.Red;

internal sealed class ConexionActiva
{
    private const int Abierta = 0;
    private const int Cerrada = 1;

    private readonly Socket _socket;
    private Thread? _thread;
    private int _cerrada;

    public ConexionActiva(int numero, Socket socket)
    {
        Numero = numero;
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
    }

    public int Numero { get; }
    public Socket Socket => _socket;
    public bool EstaAbierta => Volatile.Read(ref _cerrada) == Abierta;

    public void AsignarThread(Thread thread)
    {
        ArgumentNullException.ThrowIfNull(thread);
        if (Interlocked.CompareExchange(ref _thread, thread, null) is not null)
            throw new InvalidOperationException("La conexión ya tiene un Thread asignado");
    }

    public void Iniciar()
        => ObtenerThread().Start();

    public bool Unir(TimeSpan timeout)
        => ObtenerThread().Join(timeout);

    public void Cerrar()
    {
        if (Interlocked.Exchange(ref _cerrada, Cerrada) != Abierta)
            return;

        IntentarShutdown();
        _socket.Dispose();
    }

    private void IntentarShutdown()
    {
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private Thread ObtenerThread()
        => _thread ?? throw new InvalidOperationException(
            "La conexión todavía no tiene un Thread asignado");
}
