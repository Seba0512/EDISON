using Dominio.Fachada;
using Servidor.Repositorios;

namespace Servidor.Composicion;

/// <summary>
/// Objeto parámetro con los colaboradores que necesitan los manejadores de CR2-CR10 (ISP): cada uno
/// depende de la interfaz chica que le corresponde en vez de recibir <c>Edison</c> entero. Las cuatro
/// primeras propiedades apuntan a la misma instancia de <c>Edison</c>, armada en
/// <see cref="NucleoDelServidor"/>.
/// </summary>
internal sealed class ServiciosDelServidor
{
    public IAutenticacion Autenticacion { get; }
    public ICatalogoDeModelos CatalogoDeModelos { get; }
    public IGestionDeReservas GestionDeReservas { get; }
    public IHistorialDeUsuario HistorialDeUsuario { get; }
    public IRepositorioDeImagenes RepositorioDeImagenes { get; }
    public ConfiguracionServidor Configuracion { get; }

    public ServiciosDelServidor(
        IAutenticacion autenticacion,
        ICatalogoDeModelos catalogoDeModelos,
        IGestionDeReservas gestionDeReservas,
        IHistorialDeUsuario historialDeUsuario,
        IRepositorioDeImagenes repositorioDeImagenes,
        ConfiguracionServidor configuracion)
    {
        Autenticacion = autenticacion;
        CatalogoDeModelos = catalogoDeModelos;
        GestionDeReservas = gestionDeReservas;
        HistorialDeUsuario = historialDeUsuario;
        RepositorioDeImagenes = repositorioDeImagenes;
        Configuracion = configuracion;
    }
}
