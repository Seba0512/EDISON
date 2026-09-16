using System.Buffers.Binary;
using Common.Excepciones;

namespace Common.Protocolo;

// El archivo viaja fuera de la trama. Primero el tamaño en 8 bytes y después sus bytes exactos.
public sealed class CanalDeArchivos
{
    private readonly int _tamanioDeBloque;

    public CanalDeArchivos(int tamanioDeBloque)
    {
        if (tamanioDeBloque <= 0)
            throw new ArgumentOutOfRangeException(nameof(tamanioDeBloque));

        _tamanioDeBloque = tamanioDeBloque;
    }

    public void Enviar(Stream destino, Stream origen, long tamanio)
    {
        EscribirTamanio(destino, tamanio);
        Copiar(origen, destino, tamanio);
    }

    public long RecibirEn(Stream origen, Stream destino, long maxBytes)
    {
        long tamanio = LeerTamanio(origen);
        ExigirTamanioAdmitido(tamanio, maxBytes);
        Copiar(origen, destino, tamanio);
        return tamanio;
    }

    private static void EscribirTamanio(Stream destino, long tamanio)
    {
        byte[] bytes = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(bytes, tamanio);
        destino.Write(bytes);
    }

    private static long LeerTamanio(Stream origen)
    {
        byte[] bytes = LeerExactamente(origen, sizeof(long));
        return BinaryPrimitives.ReadInt64BigEndian(bytes);
    }

    private static void ExigirTamanioAdmitido(long tamanio, long maxBytes)
    {
        if (tamanio < 0 || tamanio > maxBytes)
            throw new TransferenciaDeArchivoException(
                $"El tamaño de archivo anunciado ({tamanio}) no es válido (máximo {maxBytes})");
    }

    private void Copiar(Stream origen, Stream destino, long tamanio)
    {
        byte[] buffer = new byte[_tamanioDeBloque];
        long restante = tamanio;
        // Un bloque puede llegar repartido en varios Read. Un archivo de 0 bytes es válido y no entra al bucle.
        while (restante > 0)
        {
            int aLeer = (int)Math.Min(buffer.Length, restante);
            int leidos = origen.Read(buffer, 0, aLeer);
            if (leidos == 0)
                throw new TransferenciaDeArchivoException(
                    "La conexión se cerró antes de completar la transferencia del archivo");

            destino.Write(buffer, 0, leidos);
            restante -= leidos;
        }
    }

    private static byte[] LeerExactamente(Stream origen, int cantidad)
    {
        byte[] buffer = new byte[cantidad];
        int offset = 0;
        while (offset < cantidad)
        {
            int leidos = origen.Read(buffer, offset, cantidad - offset);
            if (leidos == 0)
                throw new TransferenciaDeArchivoException(
                    "La conexión se cerró antes de completar la transferencia del archivo");

            offset += leidos;
        }

        return buffer;
    }
}
