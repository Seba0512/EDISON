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

    public OpcionConsultarModelo(ConexionConServidor conexion)
    {
        _consulta = new ConsultaDeModeloRemota(conexion);
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
    }
}
