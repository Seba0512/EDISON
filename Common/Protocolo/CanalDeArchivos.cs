using System.Buffers.Binary;
using Common.Excepciones;

namespace Common.Protocolo;

/// <summary>
/// Transferencia binaria fuera de frame de CMD 15/16: <c>[Int64 big-endian][N bytes exactos]</c>,
/// copiados por bloques de un tamaño configurable en vez de en una sola alocación, para no reservar
/// de golpe un buffer del tamaño completo del archivo.
///
/// Vive en <c>Common</c> porque tanto el Cliente (que sube y descarga) como el Servidor (que recibe y
/// devuelve) hablan exactamente este mismo formato de cable.
/// </summary>
public sealed class CanalDeArchivos
{
    private readonly int _tamanioDeBloque;

    public CanalDeArchivos(int tamanioDeBloque)
    {
        if (tamanioDeBloque <= 0)
            throw new ArgumentOutOfRangeException(nameof(tamanioDeBloque));

        _tamanioDeBloque = tamanioDeBloque;
    }

    /// <summary>Anuncia <paramref name="tamanio"/> y copia exactamente esa cantidad de bytes de <paramref name="origen"/> a <paramref name="destino"/>.</summary>
    public void Enviar(Stream destino, Stream origen, long tamanio)
    {
        EscribirTamanio(destino, tamanio);
        Copiar(origen, destino, tamanio);
    }

    /// <summary>
    /// Lee el tamaño anunciado, lo valida contra <paramref name="maxBytes"/> y copia exactamente esa
    /// cantidad de bytes de <paramref name="origen"/> a <paramref name="destino"/>.
    /// </summary>
    /// <returns>La cantidad de bytes recibidos.</returns>
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
