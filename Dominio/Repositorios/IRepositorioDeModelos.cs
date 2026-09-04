namespace Dominio.Repositorios;

/// <summary>
/// Catalogo permanente de modelos (CR2, CR3, CR4, CR7, CR8). Nunca devuelve null:
/// <see cref="ObtenerPorNombre"/> lanza <see cref="Excepciones.ModeloInexistenteException"/> y
/// <see cref="Todos"/> devuelve una coleccion vacia si no hay ninguno.
/// </summary>
public interface IRepositorioDeModelos
{
    void Agregar(Modelo modelo);

    /// <exception cref="Excepciones.ModeloInexistenteException">Si no hay un modelo con ese nombre.</exception>
    Modelo ObtenerPorNombre(string nombreModelo);

    bool Existe(string nombreModelo);

    void Eliminar(string nombreModelo);

    IReadOnlyList<Modelo> Todos();
}
