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
/// No guarda a que usuario pertenece. El <see cref="Historial"/> que la contiene ya es el de un
/// unico usuario, asi que repetir el dato aca solo abriria la posibilidad de que los dos no
/// coincidan.
///
/// La marca de tiempo sale del <see cref="IReloj"/> recibido y no de <c>DateTime.UtcNow</c> (DIP):
/// el historial es la unica fuente del orden en que ocurrieron las cosas, y con el reloj inyectado
/// ese orden se puede armar y verificar de forma determinista.
///
/// El constructor privado toma tres parametros, uno mas de los que admite el criterio de diseño del
/// curso: la excepcion esta acotada a construir un objeto de valor inmutable desde sus propias
/// fabricas, que son las que se usan de afuera y toman uno o dos.
/// </summary>
public class Actividad
{
    public TipoActividad Tipo { get; }

    public DateTime FechaHora { get; }

    public string Detalle { get; }

    private Actividad(TipoActividad tipo, string detalle, IReloj reloj)
    {
        Tipo = tipo;
        Detalle = detalle;
        FechaHora = reloj.Ahora;
    }

    public static Actividad InicioSesion(IReloj reloj)
    {
        return new Actividad(TipoActividad.InicioSesion, "Inicio de sesion", reloj);
    }

    public static Actividad CierreSesion(IReloj reloj)
    {
        return new Actividad(TipoActividad.CierreSesion, "Cierre de sesion", reloj);
    }

    public static Actividad ModeloCreado(string nombreModelo, IReloj reloj)
    {
        return new Actividad(TipoActividad.ModeloCreado, $"Creo el modelo {nombreModelo}", reloj);
    }

    public static Actividad ModeloModificado(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.ModeloModificado, $"Modifico el modelo {nombreModelo}", reloj);
    }

    public static Actividad ModeloEliminado(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.ModeloEliminado, $"Elimino el modelo {nombreModelo}", reloj);
    }

    public static Actividad ReservaCreada(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.ReservaCreada, $"Solicito una reserva de {nombreModelo}", reloj);
    }

    public static Actividad ReservaCancelada(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.ReservaCancelada, $"Cancelo su reserva de {nombreModelo}", reloj);
    }

    /// <summary>
    /// Una reserva confirmada es la compra: la letra pide "compras realizadas" en CR10 pero ningun
    /// requerimiento define una compra aparte.
    /// </summary>
    public static Actividad Compra(string nombreModelo, IReloj reloj)
    {
        return new Actividad(
            TipoActividad.Compra, $"Se confirmo su reserva de {nombreModelo}", reloj);
    }
}
