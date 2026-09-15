using Common.Protocolo;
using Servidor.Red;

namespace Servidor.Comandos.Manejadores;

internal sealed class ManejadorDeDiagnostico : IManejadorDeComando
{
    public int Comando => Common.Protocolo.Comandos.Diagnostico;

    public RespuestaDeOperacion Ejecutar(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion)
        => RespuestaDeOperacion.Exitosa(TextoUtf8.Decodificar(datos.Span));
}
