using Cliente.Excepciones;
using Cliente.Operaciones;
using Cliente.Validaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionAltaDeModelo : IOpcionDeMenu
{
    private readonly AltaDeModeloRemota _altaDeModeloRemota;
    private readonly ConsultaDeModeloRemota _consulta;
    private readonly OfertaDeSubidaDeImagen _ofertaDeSubidaDeImagen;

    public OpcionAltaDeModelo(ConexionConServidor conexion)
    {
        _altaDeModeloRemota = new AltaDeModeloRemota(conexion);
        _consulta = new ConsultaDeModeloRemota(conexion);
        _ofertaDeSubidaDeImagen = new OfertaDeSubidaDeImagen(conexion);
    }

    public string Titulo
        => "Alta de modelo";

    public void Ejecutar()
    {
        string? nombre = LectorDeConsola.LeerTexto("Nombre del modelo:", ValidacionesDeModelo.Nombre);
        if (nombre is null)
            return;

        if (ExisteModelo(nombre))
        {
            Consola.Error($"Ya existe un modelo llamado '{nombre}'.");
            return;
        }

        if (!LectorDeConsola.LeerTextoOpcional(
                "Descripción (Enter para omitir):", out string? descripcion, ValidacionesDeModelo.Descripcion))
            return;

        decimal? precio = LectorDeConsola.LeerDecimal("Precio:", ValidacionesDeModelo.Precio);
        if (precio is null)
            return;

        DateTime? fechaEntregaPrometida = LectorDeConsola.LeerFecha(
            "Fecha de entrega prometida (dd-MM-yyyy):", ValidacionesDeModelo.FechaFutura);
        if (fechaEntregaPrometida is null)
            return;

        int? stock = LectorDeConsola.LeerEntero("Stock:", ValidacionesDeModelo.Stock);
        if (stock is null)
            return;

        string? rutaImagen = _ofertaDeSubidaDeImagen.PreguntarRuta();

        DatosDeAltaDeModelo datos = new(
            nombre, descripcion ?? string.Empty, precio.Value, fechaEntregaPrometida.Value, stock.Value);
        Consola.Exito(_altaDeModeloRemota.Ejecutar(datos));

        if (rutaImagen is not null)
            _ofertaDeSubidaDeImagen.Subir(datos.Nombre, rutaImagen);
    }

    // La única causa realista de rechazo de una consulta por nombre es que el modelo no existe.
    private bool ExisteModelo(string nombre)
    {
        try
        {
            _consulta.Ejecutar(nombre);
            return true;
        }
        catch (OperacionRechazadaException)
        {
            return false;
        }
    }
}
