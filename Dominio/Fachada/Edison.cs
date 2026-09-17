using Dominio.Busqueda;

namespace Dominio.Fachada;

public class Edison : IAutenticacion, ICatalogoDeModelos, IGestionDeReservas, IResolucionDeReservas,
    IHistorialDeUsuario
{
    private readonly RegistroDeUsuarios _registroDeUsuarios;
    private readonly CatalogoDeModelos _catalogoDeModelos;
    private readonly CentralDeReservas _centralDeReservas;

    public Edison(
        RegistroDeUsuarios registroDeUsuarios,
        CatalogoDeModelos catalogoDeModelos,
        CentralDeReservas centralDeReservas)
    {
        _registroDeUsuarios = registroDeUsuarios;
        _catalogoDeModelos = catalogoDeModelos;
        _centralDeReservas = centralDeReservas;
    }

    public Usuario Registrar(string nombreUsuario, string clave)
    {
        return _registroDeUsuarios.Registrar(nombreUsuario, clave);
    }

    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
        return _registroDeUsuarios.IniciarSesion(nombreUsuario, clave);
    }

    public void CerrarSesion(Usuario usuario)
    {
        _registroDeUsuarios.CerrarSesion(usuario);
    }

    public IReadOnlyList<Actividad> HistorialDe(Usuario usuario)
    {
        return _registroDeUsuarios.HistorialDe(usuario);
    }

    public void AltaDeModelo(FichaDeModelo ficha, Usuario creador)
    {
        _catalogoDeModelos.AltaDeModelo(ficha, creador);
    }

    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        _catalogoDeModelos.ModificarModelo(ficha, solicitante);
    }

    public void EliminarModelo(string nombreModelo, Usuario solicitante)
    {
        _catalogoDeModelos.EliminarModelo(nombreModelo, solicitante);
    }

    public string CambiarImagenDeModelo(string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante)
    {
        return _catalogoDeModelos.CambiarImagenDeModelo(nombreModelo, nuevoNombreArchivoImagen, solicitante);
    }

    public IReadOnlyList<LecturaDeModelo> Buscar(ICriterioDeBusqueda criterio)
    {
        return _catalogoDeModelos.Buscar(criterio);
    }

    public Modelo Consultar(string nombreModelo)
    {
        return _catalogoDeModelos.Consultar(nombreModelo);
    }

    public LecturaDeModelo ConsultarLectura(string nombreModelo)
    {
        return _catalogoDeModelos.ConsultarLectura(nombreModelo);
    }

    public Guid SolicitarReserva(string nombreModelo, Usuario solicitante)
    {
        return _centralDeReservas.SolicitarReserva(nombreModelo, solicitante);
    }

    public void CancelarReserva(Guid idReserva, Usuario solicitante)
    {
        _centralDeReservas.CancelarReserva(idReserva, solicitante);
    }

    public IReadOnlyList<Reserva> ReservasDe(Usuario usuario)
    {
        return _centralDeReservas.ReservasDe(usuario);
    }

    public IReadOnlyList<LecturaDeReserva> ConsultarReservasDe(Usuario usuario)
    {
        return _centralDeReservas.ConsultarReservasDe(usuario);
    }

    public void ResolverVencidas()
    {
        _centralDeReservas.ResolverVencidas();
    }
}
