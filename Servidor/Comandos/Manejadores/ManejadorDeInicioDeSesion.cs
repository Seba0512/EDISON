using Common.Protocolo;
using Dominio;
using Dominio.Fachada;
using Servidor.Red;

namespace Servidor.Comandos.Manejadores;

internal sealed class ManejadorDeInicioDeSesion : IManejadorDeComando
{
    private readonly IAutenticacion _autenticacion;

    public ManejadorDeInicioDeSesion(IAutenticacion autenticacion)
        => _autenticacion = autenticacion;

    public int Comando => Common.Protocolo.Comandos.IniciarSesion;

    public RespuestaDeOperacion Ejecutar(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion)
    {
        PayloadDeCredenciales credenciales = PayloadDeCredenciales.Decodificar(datos.Span);
        if (sesion.TieneUsuario)
            return RespuestaDeOperacion.ErrorDeNegocio("Ya hay una sesión iniciada");

        Usuario usuario = _autenticacion.IniciarSesion(
            credenciales.NombreUsuario, credenciales.Clave);
        sesion.Iniciar(usuario);
        return RespuestaDeOperacion.Exitosa("Sesión iniciada");
    }
}
