namespace Cliente.Vistas;

// Dibuja tablas con bordes a partir de encabezados y filas ya formateadas como texto.
// El ancho de cada columna se calcula en base al contenido más largo (encabezado o fila)
// para no truncar nunca información.
internal static class TablaDeConsola
{
    public static void Mostrar(
        IReadOnlyList<string> encabezados,
        IReadOnlyList<string[]> filas,
        IReadOnlyList<bool>? alinearADerecha = null)
    {
        int columnas = encabezados.Count;
        bool[] derecha = new bool[columnas];
        if (alinearADerecha is not null)
            for (int c = 0; c < columnas; c++)
                derecha[c] = alinearADerecha[c];

        int[] anchos = new int[columnas];
        for (int c = 0; c < columnas; c++)
            anchos[c] = encabezados[c].Length;

        foreach (string[] fila in filas)
            for (int c = 0; c < columnas; c++)
                anchos[c] = Math.Max(anchos[c], fila[c].Length);

        EscribirBorde(anchos, '┌', '┬', '┐');
        EscribirFila(encabezados, anchos, derecha, esEncabezado: true);
        EscribirBorde(anchos, '├', '┼', '┤');
        foreach (string[] fila in filas)
            EscribirFila(fila, anchos, derecha, esEncabezado: false);
        EscribirBorde(anchos, '└', '┴', '┘');
    }

    private static void EscribirBorde(int[] anchos, char inicio, char medio, char fin)
    {
        Consola.ConColor(ConsoleColor.DarkGray, () =>
        {
            Console.Write(inicio);
            for (int c = 0; c < anchos.Length; c++)
            {
                Console.Write(new string('─', anchos[c] + 2));
                Console.Write(c < anchos.Length - 1 ? medio : fin);
            }

            Console.WriteLine();
        });
    }

    private static void EscribirFila(
        IReadOnlyList<string> celdas, int[] anchos, bool[] derecha, bool esEncabezado)
    {
        Consola.ConColor(ConsoleColor.DarkGray, () => Console.Write('│'));
        for (int c = 0; c < celdas.Count; c++)
        {
            string texto = derecha[c] ? celdas[c].PadLeft(anchos[c]) : celdas[c].PadRight(anchos[c]);
            ConsoleColor color = esEncabezado ? ConsoleColor.White : Console.ForegroundColor;
            Console.Write(' ');
            Consola.ConColor(color, () => Console.Write(texto));
            Console.Write(' ');
            Consola.ConColor(ConsoleColor.DarkGray, () => Console.Write('│'));
        }

        Console.WriteLine();
    }
}
