using Common.Protocolo;
using Dominio.Fachada;
using Servidor.Red;

namespace Servidor.Comandos.Manejadores;

internal sealed class ManejadorDeRegistro : IManejadorDeComando
{
    private readonly IAutenticacion _autenticacion;

    public ManejadorDeRegistro(IAutenticacion autenticacion)
        => _autenticacion = autenticacion;

    public int Comando => Common.Protocolo.Comandos.Registrar;

    public RespuestaDeOperacion Ejecutar(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion)
    {
        PayloadDeCredenciales credenciales = PayloadDeCredenciales.Decodificar(datos.Span);
        _autenticacion.Registrar(credenciales.NombreUsuario, credenciales.Clave);
        return RespuestaDeOperacion.Exitosa("Cuenta creada");
    }
}
