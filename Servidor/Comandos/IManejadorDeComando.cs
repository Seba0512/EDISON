using Common.Protocolo;
using Servidor.Red;

namespace Servidor.Comandos;

internal interface IManejadorDeComando
{
    int Comando { get; }
    RespuestaDeOperacion Ejecutar(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion);
}
