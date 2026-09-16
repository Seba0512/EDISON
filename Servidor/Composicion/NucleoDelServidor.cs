using Dominio;
using Dominio.Fachada;
using Dominio.Tiempo;
using Servidor.Comandos;
using Servidor.Comandos.Registro;
using Servidor.Repositorios;
using Servidor.Reservas;

namespace Servidor.Composicion;

/// <summary>
/// Composition root del servidor: arma los repositorios, el dominio, el despachador de comandos y la
/// mesa de aprobaciones a partir de la <see cref="ConfiguracionServidor"/> cargada. Vive separado de
/// <see cref="ServidorTcp"/> para que agregar CMD 10-30 no requiera abrir esa clase.
/// </summary>
internal sealed class NucleoDelServidor
{
    public DespachadorDeComandos Despachador { get; }

    public MesaDeAprobaciones MesaDeAprobaciones { get; }

    private NucleoDelServidor(DespachadorDeComandos despachador, MesaDeAprobaciones mesaDeAprobaciones)
    {
        Despachador = despachador;
        MesaDeAprobaciones = mesaDeAprobaciones;
    }

    public static NucleoDelServidor Crear(ConfiguracionServidor configuracion)
    {
        IReloj reloj = new RelojDelSistema();
        RepositorioDeUsuariosEnMemoria repositorioDeUsuarios = new();
        RepositorioDeModelosEnMemoria repositorioDeModelos = new();
        RepositorioDeReservasEnMemoria repositorioDeReservas = new();
        AlmacenDeImagenesEnDisco almacenDeImagenes = new(configuracion.ImagesDirectory);

        RegistroDeUsuarios registroDeUsuarios = new(repositorioDeUsuarios, reloj);
        CatalogoDeModelos catalogoDeModelos = new(repositorioDeModelos, almacenDeImagenes, reloj);
        CentralDeReservas centralDeReservas = new(
            repositorioDeReservas,
            repositorioDeModelos,
            repositorioDeUsuarios,
            reloj,
            configuracion.ReservationDelaySeconds);

        Edison edison = new(registroDeUsuarios, catalogoDeModelos, centralDeReservas);

        ServiciosDelServidor servicios = new(
            autenticacion: edison,
            catalogoDeModelos: edison,
            gestionDeReservas: edison,
            historialDeUsuario: edison,
            repositorioDeImagenes: almacenDeImagenes,
            configuracion: configuracion);

        DespachadorDeComandos despachador = new(RegistroDeManejadores.Crear(servicios));
        MesaDeAprobaciones mesa = new(edison, TimeSpan.FromMilliseconds(configuracion.ApprovalIntervalMilliseconds));

        return new NucleoDelServidor(despachador, mesa);
    }
}
