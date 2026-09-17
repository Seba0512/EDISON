using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionModificarModelo : IOpcionDeMenu
{
    private readonly ConsultaDeModeloRemota _consultaDeModeloRemota;
    private readonly ModificacionDeModeloRemota _modificacionDeModeloRemota;
    private readonly OfertaDeSubidaDeImagen _ofertaDeSubidaDeImagen;

    public OpcionModificarModelo(ConexionConServidor conexion)
    {
        _consultaDeModeloRemota = new ConsultaDeModeloRemota(conexion);
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
        // Además, el detalle ya trae la fecha y el stock actuales, que se usan para validar
        // los nuevos valores sin tener que esperar el rechazo del servidor.
        DetalleDeModelo detalleActual = _consultaDeModeloRemota.Ejecutar(nombre);

        DatosDeAltaDeModelo? datos = FormularioDeModelo.ParaModificacion(detalleActual).Completar(nombre);
        if (datos is null)
            return;

        string? rutaImagen = _ofertaDeSubidaDeImagen.PreguntarRuta();
        Consola.Exito(_modificacionDeModeloRemota.Ejecutar(datos.Value));
        _ofertaDeSubidaDeImagen.SubirSiHayRuta(datos.Value.Nombre, rutaImagen);
    }
}
