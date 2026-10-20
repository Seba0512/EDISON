using MySqlConnector;
using Servidor.Dominio.Entidades;
using Servidor.Dominio.Repositorios;

namespace Servidor.Persistencia;

internal sealed class MysqlRepositorioDeReservas : IRepositorioDeReservas
{
    private const string Columnas =
        "id, usuario, modelo_id, nombre_modelo, estado, fecha_creacion, ultima_actualizacion";

    private readonly FabricaDeConexionesMysql _conexiones;

    public MysqlRepositorioDeReservas(FabricaDeConexionesMysql conexiones)
    {
        _conexiones = conexiones;
    }

    public async Task AgregarAsync(Reserva reserva, Actividad actividad)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlTransaction transaccion = await conexion.BeginTransactionAsync();
        await using (MySqlCommand comando = new(
            $"INSERT INTO reservas ({Columnas}) " +
            "VALUES (@id, @usuario, @modeloId, @nombreModelo, @estado, @fechaCreacion, @ultimaActualizacion)",
            conexion,
            transaccion))
        {
            comando.Parameters.AddWithValue("@id", reserva.Id.ToString());
            comando.Parameters.AddWithValue("@usuario", reserva.NombreSolicitante);
            comando.Parameters.AddWithValue("@modeloId", reserva.ModeloId);
            comando.Parameters.AddWithValue("@nombreModelo", reserva.NombreModelo);
            comando.Parameters.AddWithValue("@estado", reserva.Estado.ToString());
            comando.Parameters.AddWithValue("@fechaCreacion", reserva.FechaCreacion);
            comando.Parameters.AddWithValue("@ultimaActualizacion", reserva.UltimaActualizacion);
            await comando.ExecuteNonQueryAsync();
        }

        await MysqlRepositorioDeActividades.InsertarAsync(
            conexion, transaccion, reserva.NombreSolicitante, actividad);
        await transaccion.CommitAsync();
    }

    public async Task<Reserva?> BuscarAsync(Guid idReserva)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlCommand consulta = new(
            $"SELECT {Columnas} FROM reservas WHERE id = @id", conexion);
        consulta.Parameters.AddWithValue("@id", idReserva.ToString());
        await using MySqlDataReader lector = await consulta.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Leer(lector) : null;
    }

    public async Task ActualizarEstadoAsync(Reserva reserva, Actividad? actividad)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlTransaction transaccion = await conexion.BeginTransactionAsync();
        await using (MySqlCommand comando = new(
            "UPDATE reservas SET estado = @estado, ultima_actualizacion = @ultimaActualizacion " +
            "WHERE id = @id",
            conexion,
            transaccion))
        {
            comando.Parameters.AddWithValue("@estado", reserva.Estado.ToString());
            comando.Parameters.AddWithValue("@ultimaActualizacion", reserva.UltimaActualizacion);
            comando.Parameters.AddWithValue("@id", reserva.Id.ToString());
            await comando.ExecuteNonQueryAsync();
        }

        if (actividad is not null)
        {
            await MysqlRepositorioDeActividades.InsertarAsync(
                conexion, transaccion, reserva.NombreSolicitante, actividad);
        }

        await transaccion.CommitAsync();
    }

    public Task<IReadOnlyList<Reserva>> ListarDeAsync(string nombreUsuario)
        => ListarAsync("WHERE usuario = @valor ORDER BY fecha_creacion", nombreUsuario);

    public Task<IReadOnlyList<Reserva>> ListarPendientesAsync()
        => ListarAsync("WHERE estado = @valor ORDER BY fecha_creacion", nameof(EstadoDeReserva.Pendiente));

    public Task<int> ContarConfirmadasAsync(int idModelo)
        => ContarAsync("estado = 'Confirmada'", idModelo);

    public Task<int> ContarActivasAsync(int idModelo)
        => ContarAsync("estado IN ('Pendiente', 'Confirmada')", idModelo);

    private async Task<int> ContarAsync(string condicionDeEstado, int idModelo)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlCommand consulta = new(
            $"SELECT COUNT(*) FROM reservas WHERE modelo_id = @modeloId AND {condicionDeEstado}", conexion);
        consulta.Parameters.AddWithValue("@modeloId", idModelo);
        return Convert.ToInt32(await consulta.ExecuteScalarAsync());
    }

    private async Task<IReadOnlyList<Reserva>> ListarAsync(string filtro, object valor)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlCommand consulta = new($"SELECT {Columnas} FROM reservas {filtro}", conexion);
        consulta.Parameters.AddWithValue("@valor", valor);
        List<Reserva> reservas = new();
        await using MySqlDataReader lector = await consulta.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            reservas.Add(Leer(lector));
        }

        return reservas;
    }

    private static Reserva Leer(MySqlDataReader lector)
    {
        return Reserva.Restaurar(
            Guid.Parse(lector.GetString(0)),
            lector.GetString(1),
            lector.IsDBNull(2) ? null : lector.GetInt32(2),
            lector.GetString(3),
            Enum.Parse<EstadoDeReserva>(lector.GetString(4)),
            lector.GetDateTime(5),
            lector.GetDateTime(6));
    }
}
