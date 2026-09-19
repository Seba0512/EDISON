namespace Server.Domain.Repositories;

// Objeto parámetro: agrupa los tres repositorios para no pasarlos sueltos a cada servicio.
public sealed record RepositoriosDelDominio(
    IRepositorioDeUsuarios Usuarios,
    IRepositorioDeModelos Modelos,
    IRepositorioDeReservas Reservas);
