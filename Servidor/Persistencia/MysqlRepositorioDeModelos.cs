using MySqlConnector;
using Servidor.Dominio.Entidades;
using Servidor.Dominio.Repositorios;

namespace Servidor.Persistencia;

internal sealed class MysqlRepositorioDeModelos : IRepositorioDeModelos
{
    private const string Columnas =
        "id, nombre, descripcion, precio, fecha_entrega, stock, imagen, creador";

    private readonly FabricaDeConexionesMysql _conexiones;

    public MysqlRepositorioDeModelos(FabricaDeConexionesMysql conexiones)
    {
        _conexiones = conexiones;
    }

    public Task<VehiculoModelo?> BuscarPorNombreAsync(string nombreModelo)
        => BuscarAsync("nombre = @valor", nombreModelo);

    public Task<VehiculoModelo?> BuscarPorIdAsync(int idModelo)
        => BuscarAsync("id = @valor", idModelo);

    public async Task<IReadOnlyList<ModeloConConfirmadas>> ListarAsync()
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlCommand consulta = new(
            $"SELECT {Columnas}, (SELECT COUNT(*) FROM reservas r " +
            "WHERE r.modelo_id = modelos.id AND r.estado = 'Confirmada') AS confirmadas " +
            "FROM modelos ORDER BY nombre",
            conexion);
        List<ModeloConConfirmadas> modelos = new();
        await using MySqlDataReader lector = await consulta.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            modelos.Add(new ModeloConConfirmadas(Leer(lector), lector.GetInt32(8)));
        }

        return modelos;
    }

    public async Task AgregarAsync(VehiculoModelo modelo, Actividad actividad, string nombreUsuario)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlTransaction transaccion = await conexion.BeginTransactionAsync();
        await using (MySqlCommand comando = new(
            "INSERT INTO modelos (nombre, descripcion, precio, fecha_entrega, stock, imagen, creador) " +
            "VALUES (@nombre, @descripcion, @precio, @fechaEntrega, @stock, @imagen, @creador)",
            conexion,
            transaccion))
        {
            comando.Parameters.AddWithValue("@nombre", modelo.Nombre);
            comando.Parameters.AddWithValue("@descripcion", modelo.Datos.Descripcion);
            comando.Parameters.AddWithValue("@precio", modelo.Datos.Precio);
            comando.Parameters.AddWithValue("@fechaEntrega", modelo.Datos.FechaEntregaPrometida.Date);
            comando.Parameters.AddWithValue("@stock", modelo.Stock);
            comando.Parameters.AddWithValue("@imagen", modelo.Datos.NombreArchivoImagen);
            comando.Parameters.AddWithValue("@creador", modelo.NombreCreador);
            await comando.ExecuteNonQueryAsync();
            modelo.Id = (int)comando.LastInsertedId;
        }

        await MysqlRepositorioDeActividades.InsertarAsync(conexion, transaccion, nombreUsuario, actividad);
        await transaccion.CommitAsync();
    }

    public async Task ActualizarAsync(VehiculoModelo modelo, Actividad actividad, string nombreUsuario)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlTransaction transaccion = await conexion.BeginTransactionAsync();
        await using (MySqlCommand comando = new(
            "UPDATE modelos SET descripcion = @descripcion, precio = @precio, " +
            "fecha_entrega = @fechaEntrega, stock = @stock, imagen = @imagen WHERE id = @id",
            conexion,
            transaccion))
        {
            comando.Parameters.AddWithValue("@descripcion", modelo.Datos.Descripcion);
            comando.Parameters.AddWithValue("@precio", modelo.Datos.Precio);
            comando.Parameters.AddWithValue("@fechaEntrega", modelo.Datos.FechaEntregaPrometida.Date);
            comando.Parameters.AddWithValue("@stock", modelo.Stock);
            comando.Parameters.AddWithValue("@imagen", modelo.Datos.NombreArchivoImagen);
            comando.Parameters.AddWithValue("@id", modelo.Id);
            await comando.ExecuteNonQueryAsync();
        }

        await MysqlRepositorioDeActividades.InsertarAsync(conexion, transaccion, nombreUsuario, actividad);
        await transaccion.CommitAsync();
    }

    // Las reservas que ya no están activas conservan su nombre_modelo y quedan con modelo_id en NULL.
    public async Task EliminarAsync(VehiculoModelo modelo, Actividad actividad, string nombreUsuario)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlTransaction transaccion = await conexion.BeginTransactionAsync();
        await using (MySqlCommand comando = new(
            "DELETE FROM modelos WHERE id = @id", conexion, transaccion))
        {
            comando.Parameters.AddWithValue("@id", modelo.Id);
            await comando.ExecuteNonQueryAsync();
        }

        await MysqlRepositorioDeActividades.InsertarAsync(conexion, transaccion, nombreUsuario, actividad);
        await transaccion.CommitAsync();
    }

    private async Task<VehiculoModelo?> BuscarAsync(string condicion, object valor)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlCommand consulta = new(
            $"SELECT {Columnas} FROM modelos WHERE {condicion}", conexion);
        consulta.Parameters.AddWithValue("@valor", valor);
        await using MySqlDataReader lector = await consulta.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Leer(lector) : null;
    }

    private static VehiculoModelo Leer(MySqlDataReader lector)
    {
        DatosDeModelo datos = new(
            lector.GetString(1),
            lector.GetString(2),
            lector.GetDecimal(3),
            lector.GetDateTime(4),
            lector.GetString(6));
        return VehiculoModelo.Restaurar(lector.GetInt32(0), datos, lector.GetInt32(5), lector.GetString(7));
    }
}
