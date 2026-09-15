using System.Buffers.Binary;
using Common.Excepciones;

namespace Common.Protocolo;

public sealed class PayloadDeCredenciales
{
    private const int LargoDeEntero = sizeof(int);
    private const int LargoMinimo = LargoDeEntero * 2;

    private PayloadDeCredenciales(string nombreUsuario, string clave)
    {
        NombreUsuario = nombreUsuario;
        Clave = clave;
    }

    public string NombreUsuario { get; }
    public string Clave { get; }

    public static byte[] Codificar(string nombreUsuario, string clave)
    {
        byte[] nombre = TextoUtf8.Codificar(nombreUsuario);
        byte[] claveCodificada = TextoUtf8.Codificar(clave);
        byte[] payload = new byte[LargoMinimo + nombre.Length + claveCodificada.Length];

        BinaryPrimitives.WriteInt32BigEndian(payload, nombre.Length);
        nombre.CopyTo(payload, LargoDeEntero);
        int offsetClave = LargoDeEntero + nombre.Length;
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(offsetClave), claveCodificada.Length);
        claveCodificada.CopyTo(payload, offsetClave + LargoDeEntero);
        return payload;
    }

    public static PayloadDeCredenciales Decodificar(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < LargoMinimo)
            throw new ProtocoloException("El payload de credenciales debe tener al menos 8 bytes");

        int largoNombre = BinaryPrimitives.ReadInt32BigEndian(payload);
        ExigirLargoNoNegativo(largoNombre, "nombre de usuario");
        if (largoNombre > payload.Length - LargoMinimo)
            throw new ProtocoloException("El largo del nombre de usuario excede DATA");

        int offsetLargoClave = LargoDeEntero + largoNombre;
        int largoClave = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offsetLargoClave));
        ExigirLargoNoNegativo(largoClave, "clave");
        int offsetClave = offsetLargoClave + LargoDeEntero;
        if (largoClave > payload.Length - offsetClave)
            throw new ProtocoloException("El largo de la clave excede DATA");
        if (largoClave < payload.Length - offsetClave)
            throw new ProtocoloException("El payload de credenciales contiene bytes sobrantes");

        string nombre = TextoUtf8.Decodificar(payload.Slice(LargoDeEntero, largoNombre));
        string clave = TextoUtf8.Decodificar(payload.Slice(offsetClave, largoClave));
        return new PayloadDeCredenciales(nombre, clave);
    }

    private static void ExigirLargoNoNegativo(int largo, string campo)
    {
        if (largo < 0)
            throw new ProtocoloException($"El largo de {campo} no puede ser negativo");
    }
}
