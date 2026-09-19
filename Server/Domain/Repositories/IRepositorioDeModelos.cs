using Server.Domain.Entities;

namespace Server.Domain.Repositories;

public interface IRepositorioDeModelos
{
    void Agregar(Modelo modelo);

    bool Existe(string nombreModelo);

    Modelo Obtener(string nombreModelo);

    IReadOnlyList<Modelo> Todos();

    void Quitar(string nombreModelo);
}
