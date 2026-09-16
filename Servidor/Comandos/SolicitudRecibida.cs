using Servidor.Red;

namespace Servidor.Comandos;

/// <summary>
/// Objeto parámetro que reciben los manejadores (CR1-CR10): el payload ya separado del framing, la
/// sesión de dominio de la conexión y el acceso a transferencia binaria fuera de frame. Se introduce
/// en la base de CR2-CR10 para que agregar CMD 15/16 no requiera cambiar <see cref="IManejadorDeComando"/>
/// ni ninguno de los manejadores que no suben o bajan archivos.
/// </summary>
internal readonly record struct SolicitudRecibida(
    ReadOnlyMemory<byte> Datos, EstadoDeSesion Sesion, ArchivosDeSesion Archivos);
