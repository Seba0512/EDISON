namespace Common.Protocolo;

public static class Comandos
{
    public const int Diagnostico = 0;
    public const int Registrar = 1;
    public const int IniciarSesion = 2;
    public const int CerrarSesion = 3;

    public const int AltaDeModelo = 10;
    public const int ModificarModelo = 11;
    public const int BajaDeModelo = 12;
    public const int BuscarModelos = 13;
    public const int ConsultarModelo = 14;
    public const int SubirImagen = 15;
    public const int DescargarImagen = 16;

    public const int SolicitarReserva = 20;
    public const int CancelarReserva = 21;
    public const int ConsultarMisReservas = 22;

    public const int ConsultarHistorial = 30;
}
