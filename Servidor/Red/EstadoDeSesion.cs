using Dominio;

namespace Servidor.Red;

internal sealed class EstadoDeSesion
{
    private Usuario? _usuario;

    public bool TieneUsuario => _usuario != null;

    public void Iniciar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        if (TieneUsuario)
            throw new InvalidOperationException("La conexión ya tiene un usuario autenticado");

        _usuario = usuario;
    }

    public Usuario ObtenerUsuario()
        => _usuario ?? throw new InvalidOperationException(
            "La conexión no tiene un usuario autenticado");

    public void Cerrar()
        => _usuario = null;
}
