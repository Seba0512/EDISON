using Dominio.Busqueda;

namespace Dominio.Fachada;

public class Edison : IAutenticacion, ICatalogoDeModelos, IGestionDeReservas, IResolucionDeReservas,
    IHistorialDeUsuario
{
    private readonly RegistroDeUsuarios registroDeUsuarios;
    private readonly CatalogoDeModelos catalogoDeModelos;
    private readonly CentralDeReservas centralDeReservas;

    public Edison(
        RegistroDeUsuarios registroDeUsuarios,
        CatalogoDeModelos catalogoDeModelos,
        CentralDeReservas centralDeReservas)
    {
        this.registroDeUsuarios = registroDeUsuarios;
        this.catalogoDeModelos = catalogoDeModelos;
        this.centralDeReservas = centralDeReservas;
    }

    public Usuario Registrar(string nombreUsuario, string clave)
    {
        return registroDeUsuarios.Registrar(nombreUsuario, clave);
    }

    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
        return registroDeUsuarios.IniciarSesion(nombreUsuario, clave);
    }

    public void CerrarSesion(Usuario usuario)
    {
        registroDeUsuarios.CerrarSesion(usuario);
    }

    public IReadOnlyList<Actividad> HistorialDe(Usuario usuario)
    {
        return registroDeUsuarios.HistorialDe(usuario);
    }

    public void AltaDeModelo(FichaDeModelo ficha, Usuario creador)
    {
        catalogoDeModelos.AltaDeModelo(ficha, creador);
    }

    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        catalogoDeModelos.ModificarModelo(ficha, solicitante);
    }

    public void EliminarModelo(string nombreModelo, Usuario solicitante)
    {
        catalogoDeModelos.EliminarModelo(nombreModelo, solicitante);
    }

    public string CambiarImagenDeModelo(string nombreModelo, string nuevoNombreArchivoImagen, Usuario solicitante)
    {
        return catalogoDeModelos.CambiarImagenDeModelo(nombreModelo, nuevoNombreArchivoImagen, solicitante);
    }

    public IReadOnlyList<LecturaDeModelo> Buscar(ICriterioDeBusqueda criterio)
    {
        return catalogoDeModelos.Buscar(criterio);
    }

    public Modelo Consultar(string nombreModelo)
    {
        return catalogoDeModelos.Consultar(nombreModelo);
    }

    public LecturaDeModelo ConsultarLectura(string nombreModelo)
    {
        return catalogoDeModelos.ConsultarLectura(nombreModelo);
    }

    public Guid SolicitarReserva(string nombreModelo, Usuario solicitante)
    {
        return centralDeReservas.SolicitarReserva(nombreModelo, solicitante);
    }

    public void CancelarReserva(Guid idReserva, Usuario solicitante)
    {
        centralDeReservas.CancelarReserva(idReserva, solicitante);
    }

    public IReadOnlyList<Reserva> ReservasDe(Usuario usuario)
    {
        return centralDeReservas.ReservasDe(usuario);
    }

    public IReadOnlyList<LecturaDeReserva> ConsultarReservasDe(Usuario usuario)
    {
        return centralDeReservas.ConsultarReservasDe(usuario);
    }

    public void ResolverVencidas()
    {
        centralDeReservas.ResolverVencidas();
    }
}
