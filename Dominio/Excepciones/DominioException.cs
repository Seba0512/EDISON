namespace Dominio.Excepciones;

/// <summary>
/// Raiz de todos los errores de regla de negocio del dominio.
///
/// Existe para que el borde del servidor pueda distinguir con un solo catch "el cliente pidio algo
/// invalido" de un fallo tecnico: lo primero se responde al cliente y la conexion sigue, lo segundo
/// se registra en la bitacora. Las subclases nombran el caso concreto para que el llamador que si
/// quiere distinguirlo no tenga que leer el texto del mensaje.
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
