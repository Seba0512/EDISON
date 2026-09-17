using Common.Excepciones;
using Servidor.Red;

namespace Servidor.Comandos;

internal readonly record struct SolicitudRecibida(
    ReadOnlyMemory<byte> Datos, EstadoDeSesion Sesion, ArchivosDeSesion Archivos)
{
    public void ExigirDatosVacios()
    {
        if (!Datos.IsEmpty)
            throw new ProtocoloException("DATA debe estar vacío para este comando");
    }
}
