namespace Common.Protocolo;

// El estado de una reserva viaja como texto. Estos son los nombres que publica el dominio
// (ver Dominio.Estados), y el Cliente necesita reconocerlos sin repetirlos campo a campo.
public static class EstadosDeReserva
{
    public const string Pendiente = "Pendiente";
    public const string Confirmada = "Confirmada";
    public const string Cancelada = "Cancelada";
    public const string Rechazada = "Rechazada";

    public static bool SePuedeCancelar(string estado)
        => estado is Pendiente or Confirmada;
}
