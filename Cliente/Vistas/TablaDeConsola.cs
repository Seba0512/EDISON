namespace Cliente.Vistas;

// Dibuja tablas con bordes a partir de encabezados y filas ya formateadas como texto.
// El ancho de cada columna se calcula en base al contenido más largo (encabezado o fila)
// para no truncar nunca información.
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
        EscribirEncabezado(encabezados, anchos, alineacion);
        EscribirBorde(anchos, BordeIntermedio);
        foreach (string[] fila in filas)
            EscribirFilaDeDatos(fila, anchos, alineacion);
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

    private static void EscribirEncabezado(
        IReadOnlyList<string> celdas, int[] anchos, Alineacion alineacion)
        => EscribirFila(celdas, anchos, alineacion, ConsoleColor.White);

    private static void EscribirFilaDeDatos(
        IReadOnlyList<string> celdas, int[] anchos, Alineacion alineacion)
        => EscribirFila(celdas, anchos, alineacion, Console.ForegroundColor);

    private static void EscribirFila(
        IReadOnlyList<string> celdas, int[] anchos, Alineacion alineacion, ConsoleColor color)
    {
        EscribirSeparadorVertical();
        for (int columna = 0; columna < celdas.Count; columna++)
        {
            Console.Write(' ');
            Consola.ConColor(color, () => Console.Write(alineacion.Encuadrar(celdas[columna], anchos[columna], columna)));
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
