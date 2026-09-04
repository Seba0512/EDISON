using Dominio.Excepciones;
using Dominio.Tiempo;

namespace Dominio;

/// <summary>
/// Entrada del historial de un usuario (CR10). Es inmutable y de solo agregado: una vez registrada
/// una actividad no se modifica ni se borra.
///
/// Se construye con metodos de fabricacion nombrados en lugar de un constructor publico que reciba
/// el <see cref="TipoActividad"/>: el llamador dice que paso en vez de pasar un enum suelto, y cada
/// fabrica arma el detalle con el formato correcto.
///
/// La marca de tiempo sale del <see cref="IReloj"/> recibido y no de <c>DateTime.UtcNow</c> (DIP):
/// el historial es la unica fuente del orden en que ocurrieron las cosas, y con el reloj inyectado
/// ese orden se puede armar y verificar de forma determinista. El reloj es un colaborador, no un
/// dato mas de la actividad; por eso suma un parametro a las fabricas y aun asi conviene.
/// </summary>
public class Actividad
{
    public Guid Id { get; }

    public string NombreUsuario { get; }

    public TipoActividad Tipo { get; }

    public DateTime FechaHora { get; }

    public string Detalle { get; }

    private Actividad(string nombreUsuario, TipoActividad tipo, string detalle, IReloj reloj)
    {
        Id = Guid.NewGuid();
        NombreUsuario = NombreDeUsuarioValidado(nombreUsuario);
        Tipo = tipo;
        Detalle = detalle;
        FechaHora = reloj.Ahora;
    }

    public static Actividad InicioSesion(string nombreUsuario, IReloj reloj)
    {
        return new Actividad(
            nombreUsuario, TipoActividad.InicioSesion, "Inicio de sesion", reloj);
    }

    public static Actividad CierreSesion(string nombreUsuario, IReloj reloj)
    {
        return new Actividad(
            nombreUsuario, TipoActividad.CierreSesion, "Cierre de sesion", reloj);
    }

    public static Actividad ModeloCreado(string nombreUsuario, string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            nombreUsuario, TipoActividad.ModeloCreado, $"Creo el modelo {nombreModelo}", reloj);
    }

    public static Actividad ModeloModificado(string nombreUsuario, string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            nombreUsuario, TipoActividad.ModeloModificado, $"Modifico el modelo {nombreModelo}", reloj);
    }

    public static Actividad ModeloEliminado(string nombreUsuario, string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            nombreUsuario, TipoActividad.ModeloEliminado, $"Elimino el modelo {nombreModelo}", reloj);
    }

    public static Actividad ReservaCreada(string nombreUsuario, string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            nombreUsuario, TipoActividad.ReservaCreada, $"Solicito una reserva de {nombreModelo}", reloj);
    }

    public static Actividad ReservaCancelada(string nombreUsuario, string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            nombreUsuario, TipoActividad.ReservaCancelada, $"Cancelo su reserva de {nombreModelo}", reloj);
    }

    /// <summary>
    /// Una reserva confirmada es la compra: la letra pide "compras realizadas" en CR10 pero ningun
    /// requerimiento define una compra aparte.
    /// </summary>
    public static Actividad Compra(string nombreUsuario, string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            nombreUsuario, TipoActividad.Compra, $"Se confirmo su reserva de {nombreModelo}", reloj);
    }

    public bool EsDe(string nombreUsuario)
    {
        return NombreUsuario.Equals(nombreUsuario?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string NombreDeUsuarioValidado(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new DominioException("La actividad debe pertenecer a un usuario");
        }

        return nombreUsuario.Trim();
    }
}
