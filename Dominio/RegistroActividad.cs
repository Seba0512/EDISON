using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Entrada del historial de un usuario (CR10). Es inmutable y de solo agregado: una vez registrada
/// una actividad no se modifica ni se borra.
///
/// Se construye con metodos de fabricacion nombrados en lugar de un constructor publico que reciba
/// el <see cref="TipoActividad"/>: el llamador dice que paso en vez de pasar un enum suelto, y cada
/// fabrica arma el detalle con el formato correcto.
/// </summary>
public class RegistroActividad
{
    public Guid Id { get; }

    public string NombreUsuario { get; }

    public TipoActividad Tipo { get; }

    public DateTime FechaHora { get; }

    public string Detalle { get; }

    private RegistroActividad(string nombreUsuario, TipoActividad tipo, string detalle)
    {
        Id = Guid.NewGuid();
        NombreUsuario = NombreDeUsuarioValidado(nombreUsuario);
        Tipo = tipo;
        Detalle = detalle;
        FechaHora = DateTime.UtcNow;
    }

    public static RegistroActividad InicioSesion(string nombreUsuario)
    {
        return new RegistroActividad(nombreUsuario, TipoActividad.InicioSesion, "Inicio de sesion");
    }

    public static RegistroActividad CierreSesion(string nombreUsuario)
    {
        return new RegistroActividad(nombreUsuario, TipoActividad.CierreSesion, "Cierre de sesion");
    }

    public static RegistroActividad ModeloCreado(string nombreUsuario, string nombreModelo)
    {
        return new RegistroActividad(
            nombreUsuario, TipoActividad.ModeloCreado, $"Creo el modelo {nombreModelo}");
    }

    public static RegistroActividad ModeloModificado(string nombreUsuario, string nombreModelo)
    {
        return new RegistroActividad(
            nombreUsuario, TipoActividad.ModeloModificado, $"Modifico el modelo {nombreModelo}");
    }

    public static RegistroActividad ModeloEliminado(string nombreUsuario, string nombreModelo)
    {
        return new RegistroActividad(
            nombreUsuario, TipoActividad.ModeloEliminado, $"Elimino el modelo {nombreModelo}");
    }

    public static RegistroActividad ReservaCreada(string nombreUsuario, string nombreModelo)
    {
        return new RegistroActividad(
            nombreUsuario, TipoActividad.ReservaCreada, $"Solicito una reserva de {nombreModelo}");
    }

    public static RegistroActividad ReservaCancelada(string nombreUsuario, string nombreModelo)
    {
        return new RegistroActividad(
            nombreUsuario, TipoActividad.ReservaCancelada, $"Cancelo su reserva de {nombreModelo}");
    }

    /// <summary>
    /// Una reserva confirmada es la compra: la letra pide "compras realizadas" en CR10 pero ningun
    /// requerimiento define una compra aparte.
    /// </summary>
    public static RegistroActividad Compra(string nombreUsuario, string nombreModelo)
    {
        return new RegistroActividad(
            nombreUsuario, TipoActividad.Compra, $"Se confirmo su reserva de {nombreModelo}");
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
