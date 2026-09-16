using Common.Protocolo;
using Dominio.Fachada;

namespace Servidor.Comandos;

internal sealed class ManejadorDeRegistro : IManejadorDeComando
{
    private readonly IAutenticacion _autenticacion;

    public ManejadorDeRegistro(IAutenticacion autenticacion)
    {
        _autenticacion = autenticacion;
    }

    public RespuestaDelServidor Responder(SolicitudRecibida solicitud)
    {
        Credenciales credenciales = CodecDeCredenciales.Decodificar(solicitud.Datos.Span);
        _autenticacion.Registrar(credenciales.NombreUsuario, credenciales.Clave);
        return RespuestaDelServidor.Ok($"Usuario '{credenciales.NombreUsuario}' registrado");
    }
}
