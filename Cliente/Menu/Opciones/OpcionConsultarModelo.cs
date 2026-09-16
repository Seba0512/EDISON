using Cliente.Operaciones;
using Cliente.Vistas;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

/// <summary>
/// CMD 14 en el menú autenticado (CR8): pide el nombre del modelo, delega la consulta en
/// <see cref="ConsultaDeModeloRemota"/> y muestra el resultado con
/// <see cref="PresentadorDeDetalleDeModelo"/>. No maneja bytes ni sockets.
/// </summary>
internal sealed class OpcionConsultarModelo : IOpcionDeMenu
{
    private readonly ConsultaDeModeloRemota _consulta;
    private readonly DescargaDeImagenRemota _descargaDeImagenRemota;

    public OpcionConsultarModelo(ConexionConServidor conexion)
    {
        _consulta = new ConsultaDeModeloRemota(conexion);
        _descargaDeImagenRemota = new DescargaDeImagenRemota(conexion);
    }

    public string Titulo
        => "Consultar modelo";

    public void Ejecutar()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a consultar:");
        if (nombreModelo is null)
            return;

        DetalleDeModelo detalle = _consulta.Ejecutar(nombreModelo);
        PresentadorDeDetalleDeModelo.Mostrar(detalle);

        OfrecerDescargaDeImagen(nombreModelo, detalle);
    }

    private void OfrecerDescargaDeImagen(string nombreModelo, DetalleDeModelo detalle)
    {
        if (detalle.NombreArchivoImagen.Length == 0)
            return;

        string? respuesta = LectorDeConsola.LeerTexto("¿Desea descargar la imagen? (s/n):");
        if (respuesta is null || !respuesta.Trim().Equals("s", StringComparison.OrdinalIgnoreCase))
            return;

        Console.WriteLine(_descargaDeImagenRemota.Ejecutar(nombreModelo));
    }
}
