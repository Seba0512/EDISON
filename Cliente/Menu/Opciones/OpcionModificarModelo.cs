using Cliente.Operaciones;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

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

        decimal? precio = LectorDeConsola.LeerDecimal("Nuevo precio:");
        if (precio is null)
            return;

        DateTime? fechaEntregaPrometida =
            LectorDeConsola.LeerFecha("Nueva fecha de entrega prometida (yyyyMMdd):");
        if (fechaEntregaPrometida is null)
            return;

        int? stock = LectorDeConsola.LeerEntero("Nuevo stock:");
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
}
