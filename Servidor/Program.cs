namespace Servidor;
using Common;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        ConfigManager.Leer("serverIp");
    }
}