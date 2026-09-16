using Cliente.Operaciones;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionModificarModelo : IOpcionDeMenu
{
    private readonly ModificacionDeModeloRemota _modificacionDeModeloRemota;
    private readonly OfertaDeSubidaDeImagen _ofertaDeSubidaDeImagen;

    public OpcionModificarModelo(ConexionConServidor conexion)
    {
        _modificacionDeModeloRemota = new ModificacionDeModeloRemota(conexion);
        _ofertaDeSubidaDeImagen = new OfertaDeSubidaDeImagen(conexion);
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

        _ofertaDeSubidaDeImagen.Ofrecer(datos.Nombre);
    }
}
