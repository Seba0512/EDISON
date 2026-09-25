using System.Globalization;
using System.Text;
using Common;

namespace Cliente;

// Centraliza el estilo de la consola (colores, encabezados, separadores) para no repetir
// el manejo de ForegroundColor en cada punto donde se imprime algo.
internal static class Consola
{
    public static void Titulo(string texto)
    {
        Console.WriteLine();
        ConColor(ConsoleColor.Cyan, () => Console.WriteLine(texto));
        Separador();
    }

    public static void Exito(string mensaje)
        => ConColor(ConsoleColor.Green, () => Console.WriteLine(mensaje));

    public static void Error(string mensaje)
        => ConColor(ConsoleColor.Red, () => Console.WriteLine(mensaje));

    // Variante para los errores fatales que ya se escribían por stderr. Mantiene ese flujo
    // y solo agrega color.
    public static void ErrorFatal(string mensaje)
        => ConColor(ConsoleColor.Red, () => Console.Error.WriteLine(mensaje));

    public static void Info(string mensaje)
        => ConColor(ConsoleColor.Cyan, () => Console.WriteLine(mensaje));

    public static void Sutil(string mensaje)
        => ConColor(ConsoleColor.DarkGray, () => Console.WriteLine(mensaje));

    public static void Prompt(string mensaje)
        => ConColor(ConsoleColor.Cyan, () => Console.Write(mensaje));

    public static void Campo(string etiqueta, string valor)
    {
        ConColor(ConsoleColor.DarkCyan, () => Console.Write($"{etiqueta}: "));
        Console.WriteLine(valor);
    }

    public static void Separador(int ancho = 60)
        => ConColor(ConsoleColor.DarkGray, () => Console.WriteLine(new string('─', ancho)));

    public static void ConColor(ConsoleColor color, Action accion)
    {
        ConsoleColor original = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            accion();
        }
        finally
        {
            Console.ForegroundColor = original;
        }
    }
}

// Dibuja tablas con bordes a partir de encabezados y filas ya formateadas como texto.
// El ancho de cada columna se calcula con el contenido más largo para no truncar nunca.
internal static class TablaDeConsola
{
    private static readonly BordeDeTabla BordeSuperior = new('┌', '┬', '┐');
    private static readonly BordeDeTabla BordeIntermedio = new('├', '┼', '┤');
    private static readonly BordeDeTabla BordeInferior = new('└', '┴', '┘');

    public static void Mostrar(
        IReadOnlyList<string> encabezados,
        IReadOnlyList<string[]> filas,
        IReadOnlyList<bool>? alinearADerecha = null)
    {
        int[] anchos = CalcularAnchos(encabezados, filas);
        Alineacion alineacion = new(alinearADerecha, encabezados.Count);

        EscribirBorde(anchos, BordeSuperior);
        EscribirFila(encabezados, anchos, alineacion, ConsoleColor.White);
        EscribirBorde(anchos, BordeIntermedio);
        foreach (string[] fila in filas)
            EscribirFila(fila, anchos, alineacion, Console.ForegroundColor);
        EscribirBorde(anchos, BordeInferior);
    }

    private static int[] CalcularAnchos(
        IReadOnlyList<string> encabezados, IReadOnlyList<string[]> filas)
    {
        int[] anchos = encabezados.Select(encabezado => encabezado.Length).ToArray();
        foreach (string[] fila in filas)
            for (int columna = 0; columna < anchos.Length; columna++)
                anchos[columna] = Math.Max(anchos[columna], fila[columna].Length);

        return anchos;
    }

    private static void EscribirFila(
        IReadOnlyList<string> celdas, int[] anchos, Alineacion alineacion, ConsoleColor color)
    {
        EscribirSeparadorVertical();
        for (int columna = 0; columna < celdas.Count; columna++)
        {
            string celda = alineacion.Encuadrar(celdas[columna], anchos[columna], columna);
            Console.Write(' ');
            Consola.ConColor(color, () => Console.Write(celda));
            Console.Write(' ');
            EscribirSeparadorVertical();
        }

        Console.WriteLine();
    }

    private static void EscribirBorde(int[] anchos, BordeDeTabla borde)
    {
        Consola.ConColor(ConsoleColor.DarkGray, () =>
        {
            Console.Write(borde.Inicio);
            for (int columna = 0; columna < anchos.Length; columna++)
            {
                Console.Write(new string('─', anchos[columna] + 2));
                Console.Write(columna < anchos.Length - 1 ? borde.Medio : borde.Fin);
            }

            Console.WriteLine();
        });
    }

    private static void EscribirSeparadorVertical()
        => Consola.ConColor(ConsoleColor.DarkGray, () => Console.Write('│'));

    private readonly record struct BordeDeTabla(char Inicio, char Medio, char Fin);

    private readonly struct Alineacion
    {
        private readonly bool[] _aLaDerecha;

        public Alineacion(IReadOnlyList<bool>? aLaDerecha, int columnas)
        {
            _aLaDerecha = new bool[columnas];
            if (aLaDerecha is null)
                return;

            for (int columna = 0; columna < columnas; columna++)
                _aLaDerecha[columna] = aLaDerecha[columna];
        }

        public string Encuadrar(string celda, int ancho, int columna)
            => _aLaDerecha[columna] ? celda.PadLeft(ancho) : celda.PadRight(ancho);
    }
}

internal static class Presentadores
{
    // Todas las fechas se muestran igual que como se ingresan. InvariantCulture para que el
    // separador de la hora no dependa del idioma de la máquina.
    private const string FormatoFecha = "dd-MM-yyyy";
    private const string FormatoFechaHora = "dd-MM-yyyy HH:mm:ss";

    public static void MostrarModelos(IReadOnlyList<ModeloListado> modelos)
    {
        if (modelos.Count == 0)
        {
            Consola.Sutil("No se encontraron modelos.");
            return;
        }

        List<string[]> filas = new();
        foreach (ModeloListado modelo in modelos)
        {
            filas.Add(
            [
                modelo.Nombre,
                modelo.Precio.ToString("0.00"),
                Fecha(modelo.FechaEntregaPrometida),
                modelo.UnidadesDisponibles.ToString()
            ]);
        }

        TablaDeConsola.Mostrar(
            ["Nombre", "Precio", "Entrega", "Disponibles"],
            filas,
            [false, true, false, true]);
    }

    public static void MostrarReservas(IReadOnlyList<ReservaListada> reservas)
    {
        Consola.Info("Mis reservas:");
        if (reservas.Count == 0)
        {
            Consola.Sutil("No tenés reservas.");
            return;
        }

        List<string[]> filas = new();
        foreach (ReservaListada reserva in reservas)
        {
            filas.Add(
            [
                reserva.Id.ToString(),
                reserva.NombreModelo,
                reserva.Estado,
                FechaHora(reserva.FechaCreacion),
                FechaHora(reserva.UltimaActualizacion)
            ]);
        }

        TablaDeConsola.Mostrar(["ID", "MODELO", "ESTADO", "CREACION", "ACTUALIZACION"], filas);
    }

    public static void MostrarHistorial(IReadOnlyList<ActividadListada> actividades)
    {
        if (actividades.Count == 0)
        {
            Consola.Sutil("No hay actividades registradas.");
            return;
        }

        List<string[]> filas = new();
        foreach (ActividadListada actividad in actividades)
        {
            filas.Add(
            [
                FechaHora(actividad.FechaHora),
                ConEspacios(actividad.Tipo),
                actividad.Detalle
            ]);
        }

        TablaDeConsola.Mostrar(["Fecha", "Tipo", "Detalle"], filas);
    }

    public static void MostrarDetalle(DetalleDeModelo detalle)
    {
        Consola.Titulo("Detalle del modelo");
        Consola.Campo("Nombre", detalle.Nombre);
        Consola.Campo(
            "Descripción",
            detalle.Descripcion.Length > 0 ? detalle.Descripcion : "(sin descripción)");
        Consola.Campo("Precio", detalle.Precio.ToString());
        Consola.Campo(
            "Fecha de entrega prometida", Fecha(detalle.FechaEntregaPrometida));
        Consola.Campo("Unidades disponibles", detalle.UnidadesDisponibles.ToString());
        Consola.Campo("Unidades reservadas", detalle.UnidadesReservadas.ToString());
        Consola.Campo(
            "Imagen",
            detalle.NombreArchivoImagen.Length > 0 ? detalle.NombreArchivoImagen : "(sin imagen)");
    }

    private static string Fecha(DateTime fecha)
        => fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture);

    // Los momentos viajan en UTC por el protocolo; cada cliente los muestra en su hora local.
    private static string FechaHora(DateTime fechaHora)
        => fechaHora.ToLocalTime().ToString(FormatoFechaHora, CultureInfo.InvariantCulture);

    // El tipo llega como el nombre del enum en PascalCase (ej. "InicioSesion") y se separa
    // en palabras para mostrarlo.
    private static string ConEspacios(string tipo)
    {
        StringBuilder resultado = new(tipo.Length + 4);
        for (int indice = 0; indice < tipo.Length; indice++)
        {
            if (indice > 0 && char.IsUpper(tipo[indice]))
                resultado.Append(' ');
            resultado.Append(tipo[indice]);
        }

        return resultado.ToString();
    }
}
