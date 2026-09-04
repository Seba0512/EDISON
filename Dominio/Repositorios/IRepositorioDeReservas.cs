namespace Dominio.Repositorios;

/// <summary>
/// Indice de reservas por id y por usuario. No es el dueño de las reservas: cada una vive dentro de
/// su <see cref="Modelo"/>, que es quien decide sus transiciones. Este repositorio existe para poder
/// encontrarlas sin recorrer todo el catalogo, que es lo que piden CR6, CR9 y la resolucion diferida
/// de CR5.
/// </summary>
public interface IRepositorioDeReservas
{
    void Agregar(Reserva reserva);

    /// <exception cref="Excepciones.DominioException">Si no hay una reserva con ese id.</exception>
    Reserva ObtenerPorId(Guid idReserva);

    /// <summary>Reservas de un usuario, para CR9. Coleccion vacia si no tiene ninguna.</summary>
    IReadOnlyList<Reserva> DeUsuario(string nombreUsuario);

    /// <summary>Solicitudes todavia sin resolver, para el proceso diferido de CR5.</summary>
    IReadOnlyList<Reserva> Pendientes();
}
