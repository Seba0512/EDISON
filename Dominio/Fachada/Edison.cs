using Dominio.Busqueda;

namespace Dominio.Fachada;

/// <summary>
/// Facade del dominio: un unico punto de entrada que evita que sus consumidores deban conocer los
/// tres colaboradores ni el orden en que se los arma.
///
/// Cada metodo delega en una linea y no decide nada, por eso no se convierte en una clase gorda:
/// toda la logica sigue viviendo en <see cref="RegistroDeUsuarios"/>, <see cref="CatalogoDeModelos"/>
/// y <see cref="CentralDeReservas"/>.
///
/// Implementa cinco interfaces chicas en vez de exponer una sola grande (ISP): un consumidor puede
/// depender de <see cref="IAutenticacion"/> sin ver reservas, o solo de
/// <see cref="IHistorialDeUsuario"/>. La fachada existe para la comodidad del composition root, no
/// para que los consumidores dependan de ella entera.
///
/// Recibe los tres colaboradores concretos y no sus interfaces a proposito: son partes del mismo
/// dominio y cada uno implementa mas de una de estas interfaces, asi que pedirlas sueltas serian
/// cinco parametros que ademas se podrian pasar inconsistentes entre si. La inversion de dependencia
/// importa en el limite exterior, donde los consumidores dependen de estas interfaces.
/// </summary>
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

    public IReadOnlyList<Modelo> Buscar(ICriterioDeBusqueda criterio)
    {
        return catalogoDeModelos.Buscar(criterio);
    }

    public Modelo Consultar(string nombreModelo)
    {
        return catalogoDeModelos.Consultar(nombreModelo);
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

    public void ResolverVencidas()
    {
        centralDeReservas.ResolverVencidas();
    }
}
