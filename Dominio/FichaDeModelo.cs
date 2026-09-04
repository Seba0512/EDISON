using Dominio.Excepciones;

namespace Dominio;

/// <summary>
/// Datos completos de un modelo tal como llegan del cliente en el alta (CR2) y en la modificacion
/// (CR3): los descriptivos mas el stock. Existe como objeto parametro para que dar de alta y
/// modificar reciban dos argumentos en lugar de seis.
///
/// Agrupa <see cref="DatosDeModelo"/> con la cantidad en vez de sumarle un campo, porque puertas
/// adentro el stock no es un dato descriptivo sino la cantidad de unidades fisicas que existen: el
/// modelo lo aplica sobre su pool, no lo guarda.
/// </summary>
public class FichaDeModelo
{
    public DatosDeModelo Datos { get; }

    public int Stock { get; }

    public FichaDeModelo(DatosDeModelo datos, int stock)
    {
        if (stock <= 0)
        {
            throw new DominioException("El stock debe ser mayor a cero");
        }

        Datos = datos;
        Stock = stock;
    }

    public string Nombre => Datos.Nombre;
}
