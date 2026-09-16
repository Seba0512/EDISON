namespace Common.Protocolo;

public static class CodigosDeEstado
{
    public const string Ok = "OK";
    public const string Error = "ER";
    // Después de un PR la conexión sigue andando. Después de un IN la conexión se cierra.
    public const string Protocolo = "PR";
    public const string Interno = "IN";
    public const int LargoEstado = 2;
}
