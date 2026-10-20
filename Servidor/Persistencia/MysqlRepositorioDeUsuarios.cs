using MySqlConnector;
using Servidor.Dominio.Entidades;
using Servidor.Dominio.Repositorios;

namespace Servidor.Persistencia;

internal sealed class MysqlRepositorioDeUsuarios : IRepositorioDeUsuarios
{
    private readonly FabricaDeConexionesMysql _conexiones;

    public MysqlRepositorioDeUsuarios(FabricaDeConexionesMysql conexiones)
    {
        _conexiones = conexiones;
    }

    public async Task<Usuario?> BuscarAsync(string nombreUsuario)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlCommand consulta = new(
            "SELECT nombre, salt, resumen FROM usuarios WHERE nombre = @nombre", conexion);
        consulta.Parameters.AddWithValue("@nombre", nombreUsuario);
        await using MySqlDataReader lector = await consulta.ExecuteReaderAsync();
        if (!await lector.ReadAsync())
        {
            return null;
        }

        Credencial credencial = Credencial.Restaurar(
            lector.GetFieldValue<byte[]>(1), lector.GetFieldValue<byte[]>(2));
        return Usuario.Restaurar(lector.GetString(0), credencial);
    }

    public async Task AgregarAsync(Usuario usuario)
    {
        await using MySqlConnection conexion = await _conexiones.AbrirAsync();
        await using MySqlCommand comando = new(
            "INSERT INTO usuarios (nombre, salt, resumen) VALUES (@nombre, @salt, @resumen)", conexion);
        comando.Parameters.AddWithValue("@nombre", usuario.NombreUsuario);
        comando.Parameters.AddWithValue("@salt", usuario.Credencial.Salt);
        comando.Parameters.AddWithValue("@resumen", usuario.Credencial.Resumen);
        await comando.ExecuteNonQueryAsync();
    }
}
