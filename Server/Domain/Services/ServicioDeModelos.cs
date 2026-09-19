using Server.Domain.Entities;
using Server.Domain.Exceptions;
using Server.Domain.Repositories;

namespace Server.Domain.Services;

public class ServicioDeModelos
{
    private readonly IRepositorioDeModelos _modelos;
    private readonly IReloj _reloj;
    // Hace atómico el par existe-agregar para que dos altas simultáneas no dupliquen el nombre.
    private readonly object _candadoDeUnicidad = new();

    public ServicioDeModelos(IRepositorioDeModelos modelos, IReloj reloj)
    {
        _modelos = modelos;
        _reloj = reloj;
    }

    public void Alta(FichaDeModelo ficha, Usuario creador)
    {
        ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
        Modelo modelo = new Modelo(ficha.Datos, creador);
        modelo.AgregarUnidades(ficha.Stock);
        lock (_candadoDeUnicidad)
        {
            if (_modelos.Existe(modelo.Nombre))
            {
                throw new DominioException($"Ya existe un modelo llamado {modelo.Nombre}");
            }

            _modelos.Agregar(modelo);
        }

        creador.Registrar(Actividad.ModeloCreado(ficha.Nombre, _reloj));
    }

    public void Modificar(FichaDeModelo ficha, Usuario solicitante)
    {
        Modelo modelo = _modelos.Obtener(ficha.Nombre);
        // Solo se exige futura si la fecha efectivamente cambió, para permitir la edición tardía
        // de otros campos de un modelo cuya fecha ya venció.
        DateTime fechaVigente = modelo.TomarLectura().FechaEntregaPrometida;
        if (ficha.Datos.FechaEntregaPrometida.Date != fechaVigente.Date)
        {
            ExigirFechaDeEntregaFutura(ficha.Datos.FechaEntregaPrometida);
        }

        modelo.ModificarDatos(ficha, solicitante);
        solicitante.Registrar(Actividad.ModeloModificado(ficha.Nombre, _reloj));
    }

    // Devuelve el nombre del archivo de imagen para que la capa de arriba borre el archivo físico.
    public string Eliminar(string nombreModelo, Usuario solicitante)
    {
        Modelo modelo = _modelos.Obtener(nombreModelo);
        string nombreArchivoImagen = modelo.MarcarComoEliminado(solicitante);
        _modelos.Quitar(modelo.Nombre);
        solicitante.Registrar(Actividad.ModeloEliminado(modelo.Nombre, _reloj));
        return nombreArchivoImagen;
    }

    // Devuelve el nombre de la imagen reemplazada para que la capa de arriba borre el archivo viejo.
    public string CambiarImagen(CambioDeImagen cambio, Usuario solicitante)
    {
        Modelo modelo = _modelos.Obtener(cambio.NombreModelo);
        string nombreArchivoAnterior =
            modelo.CambiarImagen(solicitante, cambio.NuevoNombreArchivoImagen);
        solicitante.Registrar(Actividad.ModeloModificado(modelo.Nombre, _reloj));
        return nombreArchivoAnterior;
    }

    public IReadOnlyList<LecturaDeModelo> Buscar(FiltroDeModelos filtro)
    {
        return _modelos.Todos()
            .Select(modelo => modelo.TomarLectura())
            .Where(modelo => modelo.TieneUnidadDisponible)
            .Where(filtro.Admite)
            .ToList();
    }

    public LecturaDeModelo Consultar(string nombreModelo)
    {
        return _modelos.Obtener(nombreModelo).TomarLectura();
    }

    private void ExigirFechaDeEntregaFutura(DateTime fechaEntregaPrometida)
    {
        if (fechaEntregaPrometida.Date <= _reloj.Ahora.Date)
        {
            throw new DominioException("La fecha de entrega debe ser futura");
        }
    }
}
