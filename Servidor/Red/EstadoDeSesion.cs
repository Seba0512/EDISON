using Dominio;
using Dominio.Excepciones;

namespace Servidor.Red;

/// <summary>
/// Sesión de dominio de una conexión TCP (CR1): cero o un <see cref="Usuario"/> autenticado.
/// No es un registro global de sesiones activas, así que la misma cuenta puede autenticarse desde
/// varias conexiones a la vez; esa decisión vive en <c>protocolo-base.md</c>, no acá.
/// </summary>
internal sealed class EstadoDeSesion
{
    private Usuario? _usuario;

    public bool TieneSesion
        => _usuario is not null;

    public Usuario UsuarioAutenticado
        => _usuario ?? throw new DominioException("La conexión no tiene una sesión iniciada");

    public void Autenticar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        if (TieneSesion)
            throw new DominioException("La conexión ya tiene una sesión iniciada");

        _usuario = usuario;
    }

    public void Cerrar()
        => _usuario = null;
}
