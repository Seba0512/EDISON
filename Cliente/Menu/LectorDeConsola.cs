namespace Cliente.Menu;

internal static class LectorDeConsola
{
    public static string LeerTextoObligatorio(string etiqueta)
    {
        while (true)
        {
            Console.Write($"{etiqueta}: ");
            string? texto = Console.ReadLine();
            if (texto is null)
                throw new EndOfStreamException("La entrada de consola fue cerrada");
            if (!string.IsNullOrWhiteSpace(texto))
                return texto;

            Console.WriteLine("El valor no puede estar vacío.");
        }
    }

    public static int LeerOpcion(int cantidad)
    {
        while (true)
        {
            Console.Write("Opción: ");
            string? entrada = Console.ReadLine();
            if (entrada is null)
                throw new EndOfStreamException("La entrada de consola fue cerrada");
            if (int.TryParse(entrada, out int opcion) && opcion >= 1 && opcion <= cantidad)
                return opcion;

            Console.WriteLine($"Ingrese un número entre 1 y {cantidad}.");
        }
    }
}
