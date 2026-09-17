using Common.Protocolo.Vistas;

namespace Common.Protocolo;

public static class CodecDeBusquedaDeModelos
{
    private const int CamposPorModelo = 4;

    public static byte[] Codificar(FiltrosDeBusqueda filtros)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirCampo(filtros.Texto ?? "");
        escritor.EscribirCampo(ADeCampoOpcional(filtros.PrecioMinimo));
        escritor.EscribirCampo(ADeCampoOpcional(filtros.PrecioMaximo));
        escritor.EscribirCampo(filtros.FechaEntregaMaxima.HasValue
            ? FormatoDeCampos.DeFecha(filtros.FechaEntregaMaxima.Value)
            : "");
        escritor.EscribirCampo(ADeCampoOpcional(filtros.MinimoDisponibles));
        return escritor.ObtenerBytes();
    }

    public static FiltrosDeBusqueda DecodificarFiltros(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        string? texto = lector.LeerCampoOpcional();
        string? precioMinimo = lector.LeerCampoOpcional();
        string? precioMaximo = lector.LeerCampoOpcional();
        string? fechaEntregaMaxima = lector.LeerCampoOpcional();
        string? minimoDisponibles = lector.LeerCampoOpcional();
        lector.ExigirFinDePayload();

        return new FiltrosDeBusqueda(
            texto,
            precioMinimo is null ? null : FormatoDeCampos.ADecimal(precioMinimo),
            precioMaximo is null ? null : FormatoDeCampos.ADecimal(precioMaximo),
            fechaEntregaMaxima is null ? null : FormatoDeCampos.AFecha(fechaEntregaMaxima),
            minimoDisponibles is null ? null : FormatoDeCampos.AEntero(minimoDisponibles));
    }

    public static byte[] CodificarResultado(IReadOnlyList<ModeloListado> modelos)
    {
        EscritorDeCampos escritor = new();
        escritor.EscribirEntero(modelos.Count);
        foreach (ModeloListado modelo in modelos)
        {
            escritor.EscribirCampo(modelo.Nombre);
            escritor.EscribirDecimal(modelo.Precio);
            escritor.EscribirFecha(modelo.FechaEntregaPrometida);
            escritor.EscribirEntero(modelo.UnidadesDisponibles);
        }

        return escritor.ObtenerBytes();
    }

    public static IReadOnlyList<ModeloListado> DecodificarResultado(ReadOnlySpan<byte> datos)
    {
        LectorDeCampos lector = new(datos);
        int cantidad = lector.LeerCantidadDeElementos(CamposPorModelo);
        List<ModeloListado> modelos = new(cantidad);
        for (int i = 0; i < cantidad; i++)
        {
            string nombre = lector.LeerCampo();
            decimal precio = lector.LeerDecimal();
            DateTime fechaEntregaPrometida = lector.LeerFecha();
            int unidadesDisponibles = lector.LeerEntero();
            modelos.Add(new ModeloListado(nombre, precio, fechaEntregaPrometida, unidadesDisponibles));
        }

        lector.ExigirFinDePayload();
        return modelos;
    }

    private static string ADeCampoOpcional(decimal? valor)
        => valor.HasValue ? FormatoDeCampos.De(valor.Value) : "";

    private static string ADeCampoOpcional(int? valor)
        => valor.HasValue ? FormatoDeCampos.De(valor.Value) : "";
}
