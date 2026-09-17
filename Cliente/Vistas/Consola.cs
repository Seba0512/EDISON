namespace Cliente.Vistas;

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

    // Variante para los errores fatales que ya se escribían por stderr: mantiene ese flujo,
    // solo agrega color.
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
