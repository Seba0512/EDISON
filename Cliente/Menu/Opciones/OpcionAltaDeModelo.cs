using Cliente.Excepciones;
using Cliente.Operaciones;
using Cliente.Validaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionAltaDeModelo : IOpcionDeMenu
{
    private readonly AltaDeModeloRemota _altaDeModeloRemota;
    private readonly ConsultaDeModeloRemota _consultaDeModeloRemota;
    private readonly OfertaDeSubidaDeImagen _ofertaDeSubidaDeImagen;

    public OpcionAltaDeModelo(ConexionConServidor conexion)
    {
        _altaDeModeloRemota = new AltaDeModeloRemota(conexion);
        _consultaDeModeloRemota = new ConsultaDeModeloRemota(conexion);
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

        DatosDeAltaDeModelo? datos = FormularioDeModelo.ParaAlta().Completar(nombre);
        if (datos is null)
            return;

        string? rutaImagen = _ofertaDeSubidaDeImagen.PreguntarRuta();
        Consola.Exito(_altaDeModeloRemota.Ejecutar(datos.Value));
        _ofertaDeSubidaDeImagen.SubirSiHayRuta(datos.Value.Nombre, rutaImagen);
    }

    // La única causa realista de rechazo de una consulta por nombre es que el modelo no existe.
    private bool ExisteModelo(string nombre)
    {
        try
        {
            _consultaDeModeloRemota.Ejecutar(nombre);
            return true;
        }
        catch (OperacionRechazadaException)
        {
            return false;
        }
    }
}
