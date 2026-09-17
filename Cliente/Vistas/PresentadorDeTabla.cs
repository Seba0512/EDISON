namespace Cliente.Vistas;

// Template Method: el esqueleto de "lista vacía o tabla" es siempre el mismo y vive acá.
// Cada presentador concreto solo aporta sus encabezados, cómo se ve una fila y qué decir
// cuando no hay nada que mostrar.
internal abstract class PresentadorDeTabla<T>
{
    public void Mostrar(IReadOnlyList<T> elementos)
    {
        MostrarTitulo();
        if (elementos.Count == 0)
        {
            Consola.Sutil(MensajeSinElementos);
            return;
        }

        List<string[]> filas = elementos.Select(AFila).ToList();
        TablaDeConsola.Mostrar(Encabezados, filas, AlinearADerecha);
    }

    protected abstract IReadOnlyList<string> Encabezados { get; }

    protected abstract string MensajeSinElementos { get; }

    protected abstract string[] AFila(T elemento);

    // Hooks: por defecto no hay título y todas las columnas van alineadas a la izquierda.
    protected virtual void MostrarTitulo()
    {
    }

    protected virtual IReadOnlyList<bool>? AlinearADerecha => null;
}
