using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionConsultarModelo : IOpcionDeMenu
{
    private readonly ConsultaDeModeloRemota _consultaDeModeloRemota;
    private readonly DescargaDeImagenRemota _descargaDeImagenRemota;
    private readonly EstadoDelMenu _estado;

    public OpcionConsultarModelo(
        ConexionConServidor conexion,
        EstadoDelMenu estado,
        DirectorioDeDescargasDeInstancia descargas)
    {
        _consultaDeModeloRemota = new ConsultaDeModeloRemota(conexion);
        _descargaDeImagenRemota = new DescargaDeImagenRemota(conexion, descargas);
        _estado = estado;
    }

    public string Titulo
        => "Consultar modelo";

    public void Ejecutar()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a consultar:");
        if (nombreModelo is null)
            return;

        DetalleDeModelo detalle = _consultaDeModeloRemota.Ejecutar(nombreModelo);
        PresentadorDeDetalleDeModelo.Mostrar(detalle);

        OfrecerDescargaDeImagen(nombreModelo, detalle);
    }

    private void OfrecerDescargaDeImagen(string nombreModelo, DetalleDeModelo detalle)
    {
        if (detalle.NombreArchivoImagen.Length == 0)
            return;

        if (!LectorDeConsola.Confirmar("¿Desea descargar la imagen? (s/n):"))
            return;

        Consola.Exito(_descargaDeImagenRemota.Ejecutar(
            nombreModelo, _estado.NombreUsuarioAutenticado));
    }
}
