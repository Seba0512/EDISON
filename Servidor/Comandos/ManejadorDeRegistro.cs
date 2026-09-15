using Common.Protocolo;
using Dominio.Fachada;
using Servidor.Red;

namespace Servidor.Comandos;

/// <summary>CMD 01: alta de usuario (CR1). Delega en <see cref="IAutenticacion"/>, no en Edison entero.</summary>
internal sealed class ManejadorDeRegistro : IManejadorDeComando
{
    private readonly IAutenticacion _autenticacion;

    public ManejadorDeRegistro(IAutenticacion autenticacion)
    {
        _autenticacion = autenticacion;
    }

    public RespuestaDelServidor Responder(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion)
    {
        Credenciales credenciales = CodecDeCredenciales.Decodificar(datos.Span);
        _autenticacion.Registrar(credenciales.NombreUsuario, credenciales.Clave);
        return RespuestaDelServidor.Ok($"Usuario '{credenciales.NombreUsuario}' registrado");
    }
}
