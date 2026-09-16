using Common.Excepciones;
using Common.Protocolo;
using Common.Protocolo.Vistas;

namespace Cliente.Menu.Opciones;

/// <summary>
/// CR7: lectura de consola propia para los cinco filtros opcionales de búsqueda de modelos. Enter
/// vacío en cualquiera de ellos significa "sin filtro"; sólo el fin de entrada (EOF, igual que
/// <see cref="LectorDeConsola"/>) cancela toda la operación. Un valor con formato inválido se
/// reintenta en el momento en vez de viajar al servidor, para no gastar una ida y vuelta de red en un
/// error que ya se puede detectar acá.
/// </summary>
internal sealed class LectorDeFiltrosDeBusqueda
{
    private const char Bom = '﻿';

    public FiltrosDeBusqueda? Leer()
    {
        if (!TryLeerCampoOpcional("Texto a buscar en el nombre (Enter para omitir):", out string? texto))
            return null;

        if (!TryLeerDecimalOpcional("Precio mínimo (Enter para omitir):", out decimal? precioMinimo))
            return null;

        if (!TryLeerDecimalOpcional("Precio máximo (Enter para omitir):", out decimal? precioMaximo))
            return null;

        if (!TryLeerFechaOpcional(
                "Fecha de entrega máxima, formato dd-MM-yyyy (Enter para omitir):", out DateTime? fechaEntregaMaxima))
            return null;

        if (!TryLeerEnteroOpcional("Mínimo de unidades disponibles (Enter para omitir):", out int? minimoDisponibles))
            return null;

        return new FiltrosDeBusqueda(texto, precioMinimo, precioMaximo, fechaEntregaMaxima, minimoDisponibles);
    }

    private static bool TryLeerDecimalOpcional(string mensaje, out decimal? valor)
    {
        while (true)
        {
            if (!TryLeerCampoOpcional(mensaje, out string? texto))
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
            if (!TryLeerCampoOpcional(mensaje, out string? texto))
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
            if (!TryLeerCampoOpcional(mensaje, out string? texto))
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

    private static bool TryLeerCampoOpcional(string mensaje, out string? valor)
    {
        Console.WriteLine(mensaje);
        string? entrada = Console.ReadLine()?.TrimStart(Bom);
        if (entrada is null)
        {
            valor = null;
            return false;
        }

        string texto = entrada.Trim();
        valor = texto.Length == 0 ? null : texto;
        return true;
    }
}
