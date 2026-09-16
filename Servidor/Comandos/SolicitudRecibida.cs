using Servidor.Red;

namespace Servidor.Comandos;

internal readonly record struct SolicitudRecibida(
    ReadOnlyMemory<byte> Datos, EstadoDeSesion Sesion, ArchivosDeSesion Archivos);
