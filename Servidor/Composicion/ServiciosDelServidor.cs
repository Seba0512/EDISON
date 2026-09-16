using Dominio.Fachada;
using Servidor.Repositorios;

namespace Servidor.Composicion;

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
