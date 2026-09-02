using System.Globalization;
using Dominio.Excepciones;

namespace Dominio.Busqueda;

/// <summary>
/// Factory Method de los criterios de CR7: resuelve codigo -> constructor por diccionario, sin
/// ningun switch por tipo. Agregar un criterio nuevo es una clase nueva y una linea de registro
/// (OCP), y quien busca no se acopla a ninguna clase concreta de criterio.
///
/// Los valores llegan como texto porque vienen de la trama del protocolo. Se interpretan con
/// cultura invariante y fecha yyyyMMdd: sin esto, un cliente con configuracion regional distinta
/// mandaria 1.234,50 y el filtro compararia cualquier cosa.
/// </summary>
public class FabricaDeCriterios
{
    public const string PorNombre = "NOMBRE";
    public const string PorPrecioMinimo = "PRECIOMIN";
    public const string PorPrecioMaximo = "PRECIOMAX";
    public const string PorUnidadesDisponibles = "UNIDADES";
    public const string PorFechaDeEntregaMaxima = "FECHAHASTA";

    private const string FormatoDeFecha = "yyyyMMdd";

    private readonly Dictionary<string, Func<string, ICriterioDeBusqueda>> constructores;

    public FabricaDeCriterios()
    {
        constructores = new Dictionary<string, Func<string, ICriterioDeBusqueda>>(
            StringComparer.OrdinalIgnoreCase)
        {
            [PorNombre] = valor => new CriterioPorNombre(valor),
            [PorPrecioMinimo] = valor => new CriterioPorPrecioMinimo(DecimalDe(valor)),
            [PorPrecioMaximo] = valor => new CriterioPorPrecioMaximo(DecimalDe(valor)),
            [PorUnidadesDisponibles] = valor => new CriterioPorUnidadesDisponibles(EnteroDe(valor)),
            [PorFechaDeEntregaMaxima] = valor => new CriterioPorFechaDeEntregaMaxima(FechaDe(valor)),
        };
    }

    public ICriterioDeBusqueda Crear(string codigo, string valor)
    {
        string codigoBuscado = codigo?.Trim() ?? string.Empty;
        if (!constructores.TryGetValue(codigoBuscado, out Func<string, ICriterioDeBusqueda>? constructor))
        {
            throw new DominioException($"No existe el criterio de busqueda {codigoBuscado}");
        }

        return constructor(valor);
    }

    private static decimal DecimalDe(string valor)
    {
        if (!decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal numero))
        {
            throw new DominioException($"El valor {valor} no es un numero valido");
        }

        return numero;
    }

    private static int EnteroDe(string valor)
    {
        if (!int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numero))
        {
            throw new DominioException($"El valor {valor} no es un numero entero valido");
        }

        return numero;
    }

    private static DateTime FechaDe(string valor)
    {
        bool esValida = DateTime.TryParseExact(
            valor, FormatoDeFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha);
        if (!esValida)
        {
            throw new DominioException($"La fecha {valor} no tiene el formato {FormatoDeFecha}");
        }

        return fecha;
    }
}
