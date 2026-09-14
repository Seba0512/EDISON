namespace Common.Protocolo;

public static class ConstantesDelProtocolo
{
    public const string Solicitud = "REQ";
    public const string Respuesta = "RES";
    public const int LargoHeader = 3;
    public const int LargoComando = 2;
    public const int LargoCampoLargo = 4;
    public const int OffsetHeader = 0;
    public const int OffsetComando = OffsetHeader + LargoHeader;
    public const int OffsetDecenasComando = OffsetComando;
    public const int OffsetUnidadesComando = OffsetComando + 1;
    public const int OffsetLargo = OffsetComando + LargoComando;
    public const int LargoPrefijo = OffsetLargo + LargoCampoLargo;
}
