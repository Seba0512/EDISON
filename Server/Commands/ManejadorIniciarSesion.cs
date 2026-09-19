using Common;
using Server.Domain.Entities;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorIniciarSesion : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorIniciarSesion(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.IniciarSesion;

    protected override bool ExigeSesion => false;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        if (conexion.Sesion.TieneSesion)
        {
            return RespuestaDelServidor.Rechazo("La conexión ya tiene una sesión iniciada");
        }

        Credenciales credenciales = Mensajes.ACredenciales(solicitud.DatosComoTexto());
        Usuario usuario = _edison.IniciarSesion(credenciales.NombreUsuario, credenciales.Clave);
        conexion.Sesion.Autenticar(usuario);
        return RespuestaDelServidor.Ok($"Bienvenido, {usuario.NombreUsuario}");
    }
}
