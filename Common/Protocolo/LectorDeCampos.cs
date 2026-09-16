using System.Buffers.Binary;
using Common.Excepciones;

namespace Common.Protocolo;

public ref struct LectorDeCampos
{
    private readonly ReadOnlySpan<byte> _datos;
    private int _posicion;

    public LectorDeCampos(ReadOnlySpan<byte> datos)
    {
        _datos = datos;
        _posicion = 0;
    }

    public string LeerCampo()
    {
        int largo = LeerLargo();
        ExigirDisponibles(largo);
        string texto = TextoUtf8.Decodificar(_datos.Slice(_posicion, largo));
        _posicion += largo;
        return texto;
    }

    public string? LeerCampoOpcional()
    {
        string campo = LeerCampo();
        return campo.Length == 0 ? null : campo;
    }

    public int LeerEntero()
        => FormatoDeCampos.AEntero(LeerCampo());

    public decimal LeerDecimal()
        => FormatoDeCampos.ADecimal(LeerCampo());

    public DateTime LeerFecha()
        => FormatoDeCampos.AFecha(LeerCampo());

    public DateTime LeerFechaHora()
        => FormatoDeCampos.AFechaHora(LeerCampo());

    public Guid LeerGuid()
        => FormatoDeCampos.AGuid(LeerCampo());

    public int LeerCantidad()
        => LeerEntero();

    public readonly void ExigirFinDePayload()
    {
        if (_posicion != _datos.Length)
            throw new ProtocoloException("El payload contiene bytes sobrantes");
    }

    private int LeerLargo()
    {
        ExigirDisponibles(ConstantesDelProtocolo.LargoCampoLargo);
        int largo = BinaryPrimitives.ReadInt32BigEndian(
            _datos.Slice(_posicion, ConstantesDelProtocolo.LargoCampoLargo));
        _posicion += ConstantesDelProtocolo.LargoCampoLargo;
        ExigirLargoValido(largo);
        return largo;
    }

    private readonly void ExigirDisponibles(int cantidad)
    {
        if (_posicion + cantidad > _datos.Length)
            throw new ProtocoloException("El payload terminó antes de lo esperado");
    }

    private static void ExigirLargoValido(int largo)
    {
        if (largo < 0)
            throw new ProtocoloException("Un campo no puede tener LARGO negativo");
    }
}
