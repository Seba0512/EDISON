using Common.Excepciones;
using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

internal sealed class LectorDeFiltrosDeBusqueda
{
    public FiltrosDeBusqueda? Leer()
    {
        if (!LectorDeConsola.LeerTextoOpcional(
                "Texto a buscar en el nombre (Enter para omitir):", out string? texto))
            return null;

        if (!TryLeerDecimalOpcional("Precio mínimo (Enter para omitir):", out decimal? precioMinimo))
            return null;

        if (!TryLeerDecimalOpcional("Precio máximo (Enter para omitir):", out decimal? precioMaximo))
            return null;

        if (!TryLeerFechaOpcional(
                "Fecha de entrega máxima, formato aaaammdd (Enter para omitir):", out DateTime? fechaEntregaMaxima))
            return null;

        if (!TryLeerEnteroOpcional("Mínimo de unidades disponibles (Enter para omitir):", out int? minimoDisponibles))
            return null;

        return new FiltrosDeBusqueda(texto, precioMinimo, precioMaximo, fechaEntregaMaxima, minimoDisponibles);
    }

    private static bool TryLeerDecimalOpcional(string mensaje, out decimal? valor)
    {
        while (true)
        {
            if (!LectorDeConsola.LeerTextoOpcional(mensaje, out string? texto))
            {
                valor = null;
                return false;
            }

            if (texto is null)
            {
                valor = null;
                return true;
            }

            try
            {
                valor = FormatoDeCampos.ADecimal(texto);
                return true;
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }

    private static bool TryLeerEnteroOpcional(string mensaje, out int? valor)
    {
        while (true)
        {
            if (!LectorDeConsola.LeerTextoOpcional(mensaje, out string? texto))
            {
                valor = null;
                return false;
            }

            if (texto is null)
            {
                valor = null;
                return true;
            }

            try
            {
                valor = FormatoDeCampos.AEntero(texto);
                return true;
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }

    private static bool TryLeerFechaOpcional(string mensaje, out DateTime? valor)
    {
        while (true)
        {
            if (!LectorDeConsola.LeerTextoOpcional(mensaje, out string? texto))
            {
                valor = null;
                return false;
            }

            if (texto is null)
            {
                valor = null;
                return true;
            }

            try
            {
                valor = FormatoDeCampos.AFecha(texto);
                return true;
            }
            catch (ProtocoloException excepcion)
            {
                Console.WriteLine(excepcion.Message);
            }
        }
    }
}
