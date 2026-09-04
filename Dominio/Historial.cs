namespace Dominio;

/// <summary>
/// Actividades de un usuario en el orden en que ocurrieron (CR10). Es una Fabricacion Pura: acumular
/// el historial no es responsabilidad de <see cref="Usuario"/>, que quedaria a cargo de autenticar
/// y ademas de registrar; se extrae a una clase cohesiva que solo hace esto.
///
/// Es de solo agregado: las actividades no se modifican ni se borran, el historial es evidencia de
/// lo que paso.
///
/// No valida a que usuario pertenece cada actividad porque no puede recibir una ajena: vive por
/// composicion dentro de un <see cref="Usuario"/>, que nunca lo expone. La regla se cumple por
/// construccion en lugar de por un chequeo que habria que acordarse de mantener.
///
/// Tiene candado propio porque un mismo usuario puede estar operando desde varias conexiones, y
/// cada conexion es un thread distinto (SR2). <see cref="Actividades"/> devuelve una copia y no la
/// lista viva: quien recorre el historial para responder CR10 no puede quedar a merced de otro
/// thread que registre una actividad en el medio del recorrido.
/// </summary>
public class Historial
{
    private readonly object candado = new();
    private readonly List<Actividad> actividades = new();

    public IReadOnlyList<Actividad> Actividades
    {
        get
        {
            lock (candado)
            {
                return actividades.ToList();
            }
        }
    }

    public void Registrar(Actividad actividad)
    {
        lock (candado)
        {
            actividades.Add(actividad);
        }
    }
}
