using Common.Excepciones;
using Common.Protocolo;
using Dominio;
using Dominio.Fachada;
using Servidor.Red;

namespace Servidor.Comandos.Manejadores;

internal sealed class ManejadorDeCierreDeSesion : IManejadorDeComando
{
    private readonly IAutenticacion _autenticacion;

    public ManejadorDeCierreDeSesion(IAutenticacion autenticacion)
        => _autenticacion = autenticacion;

    public int Comando => Common.Protocolo.Comandos.CerrarSesion;

    public RespuestaDeOperacion Ejecutar(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion)
    {
        if (!datos.IsEmpty)
            throw new ProtocoloException("Cerrar sesión requiere DATA vacío");
        if (!sesion.TieneUsuario)
            return RespuestaDeOperacion.ErrorDeNegocio("No hay una sesión iniciada");

        Usuario usuario = sesion.ObtenerUsuario();
        _autenticacion.CerrarSesion(usuario);
        sesion.Cerrar();
        return RespuestaDeOperacion.Exitosa("Sesión cerrada");
    }
}
