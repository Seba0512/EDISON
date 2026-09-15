using Common.Protocolo;
using Servidor.Red;

namespace Servidor.Comandos;

/// <summary>
/// Comportamiento de un comando del protocolo (CR1-CR10): decodifica su payload, ejecuta la regla
/// de dominio correspondiente y arma la respuesta. Cada CMD tiene su propia implementación
/// (polimorfismo) en lugar de un switch en <see cref="DespachadorDeComandos"/>.
/// </summary>
internal interface IManejadorDeComando
{
    RespuestaDelServidor Responder(ReadOnlyMemory<byte> datos, EstadoDeSesion sesion);
}
