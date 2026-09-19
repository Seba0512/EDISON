using Server.Domain.Entities;
using Server.Domain.Exceptions;

namespace Server.Commands;

internal sealed class EstadoDeSesion
{
    private Usuario? _usuario;

    public bool TieneSesion => _usuario is not null;

    public Usuario UsuarioAutenticado
        => _usuario ?? throw new DominioException("La conexión no tiene una sesión iniciada");

    public void Autenticar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        if (TieneSesion)
        {
            throw new DominioException("La conexión ya tiene una sesión iniciada");
        }

        _usuario = usuario;
    }

    public void Cerrar()
    {
        _usuario = null;
    }
}
