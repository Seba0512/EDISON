namespace Cliente.Menu;

internal sealed class EstadoDelMenu
{
    public bool EstaAutenticado { get; private set; }
    public bool SalidaSolicitada { get; private set; }

    public void RegistrarInicioDeSesion()
        => EstaAutenticado = true;

    public void RegistrarCierreDeSesion()
        => EstaAutenticado = false;

    public void SolicitarSalida()
        => SalidaSolicitada = true;
}
