namespace Dominio.Excepciones;

/// <summary>
/// Raiz de todos los errores de regla de negocio del dominio.
///
/// Permite que cualquier capa exterior distinga con un solo catch una regla de negocio incumplida
/// de un fallo tecnico. Las subclases nombran el caso concreto para que el llamador que necesite
/// distinguirlo no tenga que interpretar el texto del mensaje.
///
/// Se lanza directamente solo cuando el caso no justifica un tipo propio; las reglas que algun
/// llamador necesita tratar distinto tienen su subclase.
/// </summary>
public class DominioException : Exception
{
    public DominioException(string mensaje) : base(mensaje)
    {
    }

    public DominioException(string mensaje, Exception causaOriginal) : base(mensaje, causaOriginal)
    {
    }
}
