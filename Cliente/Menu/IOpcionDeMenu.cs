namespace Cliente.Menu;

internal interface IOpcionDeMenu
{
    string Titulo { get; }

    // Se salta tras cambiar de menu (ej. iniciar sesion): la pausa no aporta nada antes de mostrar el nuevo.
    bool RequierePausa
        => true;

    void Ejecutar();
}
