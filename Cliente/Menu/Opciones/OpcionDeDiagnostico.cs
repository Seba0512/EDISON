using Cliente.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class OpcionDeDiagnostico : IOpcionDeMenu
{
    private readonly ConexionConServidor _conexion;

    public OpcionDeDiagnostico(ConexionConServidor conexion)
    {
        _conexion = conexion;
    }

    public string Titulo
        => "Diagnóstico";

    public void Ejecutar()
    {
        string? texto = LectorDeConsola.LeerTexto("Texto a enviar:");
        if (texto is null)
            return;

        Consola.Exito(_conexion.EnviarDiagnostico(texto));
    }
}
