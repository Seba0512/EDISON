using MySqlConnector;

namespace Servidor.Persistencia;

// Punto único para abrir conexiones a MySQL. MySqlConnector mantiene un pool, así que abrir una
// conexión por operación es barato y evita compartir una conexión entre clientes concurrentes.
internal sealed class FabricaDeConexionesMysql
{
    private readonly string _cadenaDeConexion;

    public FabricaDeConexionesMysql(string cadenaDeConexion)
    {
        _cadenaDeConexion = cadenaDeConexion;
    }

    public async Task<MySqlConnection> AbrirAsync(CancellationToken token = default)
    {
        MySqlConnection conexion = new(_cadenaDeConexion);
        try
        {
            await conexion.OpenAsync(token);
            return conexion;
        }
        catch
        {
            await conexion.DisposeAsync();
            throw;
        }
    }

    // Al arrancar la base puede estar todavía iniciando (en Docker el contenedor de MySQL tarda
    // más que el del servidor): se reintenta con Task.Delay antes de rendirse.
    public async Task EsperarDisponibilidadAsync(
        int intentos, TimeSpan demoraEntreIntentos, CancellationToken token)
    {
        int intento = 1;
        while (!await BaseRespondeAsync(intento, intentos, token))
        {
            await Task.Delay(demoraEntreIntentos, token);
            intento++;
        }

        Console.WriteLine("Conexión con MySQL establecida.");
    }

    // Devuelve false si la base no respondió y quedan intentos. En el último intento la excepción
    // sigue de largo, así que el bucle de arriba siempre termina.
    private async Task<bool> BaseRespondeAsync(int intento, int intentos, CancellationToken token)
    {
        try
        {
            await using MySqlConnection conexion = await AbrirAsync(token);
            await using MySqlCommand consulta = new("SELECT 1", conexion);
            await consulta.ExecuteScalarAsync(token);
            return true;
        }
        catch (MySqlException excepcion) when (intento < intentos)
        {
            Console.WriteLine(
                $"MySQL no está disponible todavía (intento {intento} de {intentos}): {excepcion.Message}");
            return false;
        }
    }
}
