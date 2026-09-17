namespace Dominio.Repositorios;

public interface IRepositorioDeModelos
{
    void Agregar(Modelo modelo);

    Modelo ObtenerPorNombre(string nombreModelo);

    void Eliminar(Modelo modelo);

    IReadOnlyList<Modelo> Todos();
}
