using MySqlConnector;
using Servidor.Dominio.Entidades;
using Servidor.Dominio.Repositorios;

namespace Servidor.Persistencia;

internal sealed class MysqlRepositorioDeActividades : IRepositorioDeActividades
{
    private readonly FabricaDeConexionesMysql _conexiones;

    public MysqlRepositorioDeActividades(FabricaDeConexionesMysql conexiones)
    {
        _conexiones = conexiones;
    }

    public async Task RegistrarAsync(string nombreUsuario, Actividad actividad)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await InsertarAsync(conexion, null, nombreUsuario, actividad);
    }

    public async Task<IReadOnlyList<Actividad>> ListarDeAsync(string nombreUsuario)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlCommand consulta = new(
            "SELECT tipo, detalle, fecha_hora FROM actividades WHERE usuario = @usuario ORDER BY id",
            conexion);
        consulta.Parameters.AddWithValue("@usuario", nombreUsuario);
        List<Actividad> actividades = new();
        await using MySqlDataReader lector = await consulta.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            actividades.Add(Actividad.Restaurar(
                Enum.Parse<TipoActividad>(lector.GetString(0)),
                lector.GetString(1),
                lector.GetDateTime(2)));
        }

        return actividades;
    }

    // Lo usan también los demás repositorios para guardar la actividad dentro de su transacción.
    internal static async Task InsertarAsync(
        MySqlConnection conexion, MySqlTransaction? transaccion, string nombreUsuario, Actividad actividad)
    {
        await using MySqlCommand comando = new(
            "INSERT INTO actividades (usuario, tipo, detalle, fecha_hora) " +
            "VALUES (@usuario, @tipo, @detalle, @fechaHora)",
            conexion,
            transaccion);
        comando.Parameters.AddWithValue("@usuario", nombreUsuario);
        comando.Parameters.AddWithValue("@tipo", actividad.Tipo.ToString());
        comando.Parameters.AddWithValue("@detalle", actividad.Detalle);
        comando.Parameters.AddWithValue("@fechaHora", actividad.FechaHora);
        await comando.ExecuteNonQueryAsync();
    }
}
