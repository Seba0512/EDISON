using Cliente.Operaciones;
using Common.Excepciones;
using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionAltaDeModelo : IOpcionDeMenu
{
    private readonly AltaDeModeloRemota _altaDeModeloRemota;
    private readonly SubidaDeImagenRemota _subidaDeImagenRemota;

    public OpcionAltaDeModelo(ConexionConServidor conexion)
    {
        _altaDeModeloRemota = new AltaDeModeloRemota(conexion);
        _subidaDeImagenRemota = new SubidaDeImagenRemota(conexion);
    }

    public string Titulo
        => "Alta de modelo";

    public void Ejecutar()
    {
        string? nombre = LectorDeConsola.LeerTexto("Nombre del modelo:");
        if (nombre is null)
            return;

        string? descripcion = LectorDeConsola.LeerTexto("Descripción:");
        if (descripcion is null)
            return;

        decimal? precio = LeerDecimal("Precio:");
        if (precio is null)
            return;

        DateTime? fechaEntregaPrometida = LeerFecha("Fecha de entrega prometida (yyyyMMdd):");
        if (fechaEntregaPrometida is null)
            return;

        int? stock = LeerEntero("Stock:");
        if (stock is null)
            return;

        DatosDeAltaDeModelo datos = new(
            nombre, descripcion, precio.Value, fechaEntregaPrometida.Value, stock.Value);
        Console.WriteLine(_altaDeModeloRemota.Ejecutar(datos));

        OfrecerSubidaDeImagen(datos.Nombre);
    }

    private void OfrecerSubidaDeImagen(string nombreModelo)
    {
        string? respuesta = LectorDeConsola.LeerTexto("¿Desea subir una imagen para el modelo? (s/n):");
        if (respuesta is null || !respuesta.Trim().Equals("s", StringComparison.OrdinalIgnoreCase))
            return;

        string? rutaArchivo = LectorDeConsola.LeerTexto("Ruta local del archivo de imagen:");
        if (rutaArchivo is null)
            return;

        if (!File.Exists(rutaArchivo))
        {
            Console.WriteLine($"No se encontró el archivo '{rutaArchivo}'.");
            return;
        }

        Console.WriteLine(_subidaDeImagenRemota.Ejecutar(nombreModelo, rutaArchivo));
    }

    private static decimal? LeerDecimal(string mensaje)
    {
        while (true)
        {
            string? texto = LectorDeConsola.LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                return FormatoDeCampos.ADecimal(texto);
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }

    private static DateTime? LeerFecha(string mensaje)
    {
        while (true)
        {
            string? texto = LectorDeConsola.LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                return FormatoDeCampos.AFecha(texto);
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }

    private static int? LeerEntero(string mensaje)
    {
        while (true)
        {
            string? texto = LectorDeConsola.LeerTexto(mensaje);
            if (texto is null)
                return null;

            try
            {
                return FormatoDeCampos.AEntero(texto);
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }
}
