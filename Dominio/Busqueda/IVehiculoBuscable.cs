namespace Dominio.Busqueda;

/// <summary>
/// Lo unico que un criterio necesita saber de un vehiculo para decidir si entra en un resultado de
/// CR7: los cuatro datos por los que se filtra, de solo lectura.
///
/// ISP: los criterios ya no reciben el <see cref="Modelo"/> completo, que ademas sabe asignar
/// unidades, ajustar stock y cambiar su ficha. Reciben una interfaz chica con lo que de verdad usan,
/// y asi un criterio no puede modificar el catalogo mientras lo filtra.
/// DIP: la busqueda y sus criterios dependen de esta abstraccion estable y no de la clase concreta.
/// </summary>
public interface IVehiculoBuscable
{
    string Nombre { get; }

    decimal Precio { get; }

    DateTime FechaEntregaPrometida { get; }

    int UnidadesDisponibles { get; }
}
