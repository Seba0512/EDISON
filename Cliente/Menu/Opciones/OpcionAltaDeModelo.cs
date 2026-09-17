using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionAltaDeModelo : IOpcionDeMenu
{
    private readonly AltaDeModeloRemota _altaDeModeloRemota;
    private readonly OfertaDeSubidaDeImagen _ofertaDeSubidaDeImagen;

    public OpcionAltaDeModelo(ConexionConServidor conexion)
    {
        _altaDeModeloRemota = new AltaDeModeloRemota(conexion);
        _ofertaDeSubidaDeImagen = new OfertaDeSubidaDeImagen(conexion);
    }

    public string Titulo
        => "Alta de modelo";

    public void Ejecutar()
    {
        string? nombre = LectorDeConsola.LeerTexto("Nombre del modelo:");
        if (nombre is null)
            return;

        if (!LectorDeConsola.LeerTextoOpcional("Descripción (Enter para omitir):", out string? descripcion))
            return;

        decimal? precio = LectorDeConsola.LeerDecimal("Precio:");
        if (precio is null)
            return;

        DateTime? fechaEntregaPrometida = LectorDeConsola.LeerFecha("Fecha de entrega prometida (dd-MM-yyyy):");
        if (fechaEntregaPrometida is null)
            return;

        int? stock = LectorDeConsola.LeerEntero("Stock:");
        if (stock is null)
            return;

        string? rutaImagen = _ofertaDeSubidaDeImagen.PreguntarRuta();

        DatosDeAltaDeModelo datos = new(
            nombre, descripcion ?? string.Empty, precio.Value, fechaEntregaPrometida.Value, stock.Value);
        Consola.Exito(_altaDeModeloRemota.Ejecutar(datos));

        if (rutaImagen is not null)
            _ofertaDeSubidaDeImagen.Subir(datos.Nombre, rutaImagen);
    }
}
