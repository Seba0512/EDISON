using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionModificarModelo : IOpcionDeMenu
{
    private readonly ConsultaDeModeloRemota _consulta;
    private readonly ModificacionDeModeloRemota _modificacionDeModeloRemota;
    private readonly OfertaDeSubidaDeImagen _ofertaDeSubidaDeImagen;

    public OpcionModificarModelo(ConexionConServidor conexion)
    {
        _consulta = new ConsultaDeModeloRemota(conexion);
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

        // Se consulta antes de pedir el resto de los datos: si el modelo no existe, lanza y no sigue.
        _consulta.Ejecutar(nombre);

        if (!LectorDeConsola.LeerTextoOpcional("Nueva descripción (Enter para omitir):", out string? descripcion))
            return;

        decimal? precio = LectorDeConsola.LeerDecimal("Nuevo precio:");
        if (precio is null)
            return;

        DateTime? fechaEntregaPrometida =
            LectorDeConsola.LeerFecha("Nueva fecha de entrega prometida (dd-MM-yyyy):");
        if (fechaEntregaPrometida is null)
            return;

        int? stock = LectorDeConsola.LeerEntero("Nuevo stock:");
        if (stock is null)
            return;

        string? rutaImagen = _ofertaDeSubidaDeImagen.PreguntarRuta();

        DatosDeAltaDeModelo datos = new(
            nombre, descripcion ?? string.Empty, precio.Value, fechaEntregaPrometida.Value, stock.Value);
        Consola.Exito(_modificacionDeModeloRemota.Ejecutar(datos));

        if (rutaImagen is not null)
            _ofertaDeSubidaDeImagen.Subir(datos.Nombre, rutaImagen);
    }
}
