using Server.Domain.Entities;
using Server.Domain.Repositories;
using Server.Domain.Services;

namespace Server.Domain.Facade;

// Único punto de entrada al dominio. No decide nada: delega cada caso de uso en su servicio.
public class Edison
{
    private readonly ServicioDeUsuarios _usuarios;
    private readonly ServicioDeModelos _modelos;
    private readonly ServicioDeReservas _reservas;

    public Edison(RepositoriosDelDominio repositorios, IReloj reloj, int segundosDeAprobacion)
    {
        _usuarios = new ServicioDeUsuarios(repositorios.Usuarios, reloj);
        _modelos = new ServicioDeModelos(repositorios.Modelos, reloj);
        _reservas = new ServicioDeReservas(repositorios, reloj, segundosDeAprobacion);
    }

    public Usuario Registrar(string nombreUsuario, string clave)
    {
        return _usuarios.Registrar(nombreUsuario, clave);
    }

    public Usuario IniciarSesion(string nombreUsuario, string clave)
    {
        return _usuarios.IniciarSesion(nombreUsuario, clave);
    }

    public void CerrarSesion(Usuario usuario)
    {
        _usuarios.CerrarSesion(usuario);
    }

    public IReadOnlyList<Actividad> HistorialDe(Usuario usuario)
    {
        return usuario.Actividades();
    }

    public void AltaDeModelo(FichaDeModelo ficha, Usuario creador)
    {
        _modelos.Alta(ficha, creador);
    }

    public void ModificarModelo(FichaDeModelo ficha, Usuario solicitante)
    {
        _modelos.Modificar(ficha, solicitante);
    }

    public string EliminarModelo(string nombreModelo, Usuario solicitante)
    {
        return _modelos.Eliminar(nombreModelo, solicitante);
    }

    public string CambiarImagenDeModelo(CambioDeImagen cambio, Usuario solicitante)
    {
        return _modelos.CambiarImagen(cambio, solicitante);
    }

    public IReadOnlyList<LecturaDeModelo> Buscar(FiltroDeModelos filtro)
    {
        return _modelos.Buscar(filtro);
    }

    public LecturaDeModelo ConsultarLectura(string nombreModelo)
    {
        return _modelos.Consultar(nombreModelo);
    }

    public Guid SolicitarReserva(string nombreModelo, Usuario solicitante)
    {
        return _reservas.Solicitar(nombreModelo, solicitante);
    }

    public void CancelarReserva(Guid idReserva, Usuario solicitante)
    {
        _reservas.Cancelar(idReserva, solicitante);
    }

    public IReadOnlyList<LecturaDeReserva> ConsultarReservasDe(Usuario usuario)
    {
        return _reservas.ConsultarDe(usuario);
    }

    public void ResolverVencidas()
    {
        _reservas.ResolverVencidas();
    }
}
