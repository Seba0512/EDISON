using Common;
using Server.Domain.Entities;
using Server.Domain.Facade;

namespace Server.Commands;

internal sealed class ManejadorRegistrar : ManejadorDeComando
{
    private readonly Edison _edison;

    public ManejadorRegistrar(Edison edison)
    {
        _edison = edison;
    }

    public override int Comando => Comandos.Registrar;

    protected override bool ExigeSesion => false;

    protected override RespuestaDelServidor Atender(Trama solicitud, ContextoDeConexion conexion)
    {
        Credenciales credenciales = Mensajes.ACredenciales(solicitud.DatosComoTexto());
        Usuario usuario = _edison.Registrar(credenciales.NombreUsuario, credenciales.Clave);
        return RespuestaDelServidor.Ok($"Usuario '{usuario.NombreUsuario}' registrado");
    }
}
