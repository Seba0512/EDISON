using System.Configuration;

namespace Common;

public class ConfigManager
{
        static string? Buscar(string clave)
            => ConfigurationManager.AppSettings[clave];

        public static string Leer(string clave)
        {
            string? valor = Buscar(clave);

            if (string.IsNullOrEmpty(valor))
                throw new ConfigurationErrorsException(
                    $"Falta la clave '{clave}' en App.config");

            return valor;
        }

        public static string Leer(string clave, string defecto)
        {
            string? valor = Buscar(clave);
            return string.IsNullOrEmpty(valor) ? defecto : valor;
        }
}