using Cliente.Operaciones;
using Common.Excepciones;
using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

/// <summary>
/// CMD 11: pide por consola los mismos cinco datos que el alta (CR3, la modificacion manda la ficha
/// completa, no un delta) y los delega en <see cref="ModificacionDeModeloRemota"/>. Nunca ve bytes ni
/// sockets. La lectura con reintento replica el patron de <see cref="OpcionAltaDeModelo"/>, pero con
/// helpers propios: esa clase no se toca para no acoplar dos comandos distintos entre si.
/// </summary>
internal sealed class OpcionModificarModelo : IOpcionDeMenu
{
    private readonly ModificacionDeModeloRemota _modificacionDeModeloRemota;
    private readonly SubidaDeImagenRemota _subidaDeImagenRemota;

    public OpcionModificarModelo(ConexionConServidor conexion)
    {
        _modificacionDeModeloRemota = new ModificacionDeModeloRemota(conexion);
        _subidaDeImagenRemota = new SubidaDeImagenRemota(conexion);
    }

    public string Titulo
        => "Modificar modelo";

    public void Ejecutar()
    {
        string? nombre = LectorDeConsola.LeerTexto("Nombre del modelo a modificar:");
        if (nombre is null)
            return;

        string? descripcion = LectorDeConsola.LeerTexto("Nueva descripción:");
        if (descripcion is null)
            return;

        decimal? precio = LeerDecimal("Nuevo precio:");
        if (precio is null)
            return;

        DateTime? fechaEntregaPrometida = LeerFecha("Nueva fecha de entrega prometida (yyyyMMdd):");
        if (fechaEntregaPrometida is null)
            return;

        int? stock = LeerEntero("Nuevo stock:");
        if (stock is null)
            return;

        DatosDeAltaDeModelo datos = new(
            nombre, descripcion, precio.Value, fechaEntregaPrometida.Value, stock.Value);
        Console.WriteLine(_modificacionDeModeloRemota.Ejecutar(datos));

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
