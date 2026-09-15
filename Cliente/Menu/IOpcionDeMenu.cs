namespace Cliente.Menu;

/// <summary>Un ítem ejecutable del menú (polimorfismo): agregar una opción no toca el menú que la lista.</summary>
internal interface IOpcionDeMenu
{
    string Titulo { get; }
    void Ejecutar();
}
