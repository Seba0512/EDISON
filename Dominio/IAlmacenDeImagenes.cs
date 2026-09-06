namespace Dominio;

/// <summary>
/// Borrado de la imagen de un modelo (SR3). Se declara en el dominio y se implementa afuera (DIP):
/// asi <c>CatalogoDeModelos.EliminarModelo</c> cumple SR3 sin enterarse de que existe un sistema de
/// archivos, y la baja borra la imagen en el mismo lugar donde borra el modelo, no en un paso aparte
/// que alguien se pueda olvidar de llamar (Indireccion y Variaciones Protegidas).
///
/// Tiene un solo metodo a proposito (ISP): este caso de uso solamente necesita eliminar. Agregar
/// operaciones que el catalogo no utiliza lo obligaria a depender de un contrato mas amplio.
/// </summary>
public interface IAlmacenDeImagenes
{
    /// <summary>No falla si el archivo no esta: el objetivo es que despues no exista.</summary>
    void Eliminar(string nombreArchivo);
}
