using System.Data.Common;
using Common;
using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;
using Servidor.Dominio.Servicios;

namespace Servidor;

internal sealed class EjecutorDeComandos
{
    public const string SesionRequerida = "Debe iniciar sesión para realizar esta operación";
    private const string BaseNoDisponible =
        "No se pudo acceder a los datos del sistema. Intente nuevamente en unos instantes.";

    private readonly ServicioDeUsuarios _usuarios;
    private readonly ServicioDeModelos _modelos;
    private readonly ServicioDeReservas _reservas;
    private readonly ImagenesEnDisco _imagenes;

    public EjecutorDeComandos(
        ServicioDeUsuarios usuarios,
        ServicioDeModelos modelos,
        ServicioDeReservas reservas,
        ImagenesEnDisco imagenes)
    {
        _usuarios = usuarios;
        _modelos = modelos;
        _reservas = reservas;
        _imagenes = imagenes;
    }

    public async Task<RespuestaDelServidor> EjecutarAsync(
        Trama solicitud, SesionDeCliente sesion, CancellationToken token)
    {
        try
        {
            return await ResponderAsync(solicitud, sesion, token);
        }
        catch (DominioException excepcion)
        {
            return RespuestaDelServidor.Rechazo(excepcion.Message);
        }
        catch (ProtocoloException excepcion)
        {
            // La trama ya se consumió entera, así que un payload inválido no desincroniza la conexión.
            return RespuestaDelServidor.Rechazo(excepcion.Message);
        }
        catch (AlmacenDeImagenesException excepcion)
        {
            // Un problema de disco no desincroniza el stream, así que se rechaza y la conexión sigue.
            // El detalle técnico, con rutas del servidor, queda en la consola del servidor.
            Console.Error.WriteLine($"{excepcion.Message} Detalle: {excepcion.InnerException?.Message}");
            return RespuestaDelServidor.Rechazo(excepcion.Message);
        }
        catch (DbException excepcion)
        {
            // Que la base no responda no es un error del servidor ni desincroniza el stream: se
            // rechaza el pedido y la sesión sigue. El detalle técnico queda en la consola del servidor.
            Console.Error.WriteLine(
                $"Error de base de datos en CMD {solicitud.Comando:D2}: {excepcion.Message}");
            return RespuestaDelServidor.Rechazo(BaseNoDisponible);
        }
    }

    private async Task<RespuestaDelServidor> ResponderAsync(
        Trama solicitud, SesionDeCliente sesion, CancellationToken token)
    {
        if (ExigeSesion(solicitud.Comando) && sesion.UsuarioAutenticado is null)
        {
            return RespuestaDelServidor.Rechazo(SesionRequerida);
        }

        switch (solicitud.Comando)
        {
            case Comandos.Registrar:
                return await RegistrarAsync(solicitud);
            case Comandos.IniciarSesion:
                return await IniciarSesionAsync(solicitud, sesion);
            case Comandos.CerrarSesion:
                return await CerrarSesionAsync(solicitud, sesion);
            case Comandos.AltaDeModelo:
                return await AltaDeModeloAsync(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.ModificarModelo:
                return await ModificarModeloAsync(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.BajaDeModelo:
                return await BajaDeModeloAsync(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.BuscarModelos:
                return await BuscarModelosAsync(solicitud);
            case Comandos.ConsultarModelo:
                return await ConsultarModeloAsync(solicitud);
            case Comandos.SubirImagen:
                return await SubirImagenAsync(solicitud, sesion, token);
            case Comandos.DescargarImagen:
                return await DescargarImagenAsync(solicitud);
            case Comandos.SolicitarReserva:
                return await SolicitarReservaAsync(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.CancelarReserva:
                return await CancelarReservaAsync(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.ConsultarMisReservas:
                return await ConsultarMisReservasAsync(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.ConsultarHistorial:
                return await ConsultarHistorialAsync(solicitud, sesion.UsuarioAutenticado!);
            default:
                return RespuestaDelServidor.Rechazo("CMD no soportado");
        }
    }

    private static bool ExigeSesion(int comando)
    {
        return comando is Comandos.CerrarSesion
            or Comandos.AltaDeModelo or Comandos.ModificarModelo or Comandos.BajaDeModelo
            or Comandos.BuscarModelos or Comandos.ConsultarModelo
            or Comandos.SubirImagen or Comandos.DescargarImagen
            or Comandos.SolicitarReserva or Comandos.CancelarReserva or Comandos.ConsultarMisReservas
            or Comandos.ConsultarHistorial;
    }

    private async Task<RespuestaDelServidor> RegistrarAsync(Trama solicitud)
    {
        Credenciales credenciales = Mensajes.ACredenciales(solicitud.DatosComoTexto());
        Usuario usuario = await _usuarios.RegistrarAsync(credenciales.NombreUsuario, credenciales.Clave);
        return RespuestaDelServidor.Ok($"Usuario '{usuario.NombreUsuario}' registrado");
    }

    private async Task<RespuestaDelServidor> IniciarSesionAsync(Trama solicitud, SesionDeCliente sesion)
    {
        if (sesion.UsuarioAutenticado is not null)
        {
            return RespuestaDelServidor.Rechazo("La conexión ya tiene una sesión iniciada");
        }

        Credenciales credenciales = Mensajes.ACredenciales(solicitud.DatosComoTexto());
        Usuario usuario = await _usuarios.IniciarSesionAsync(credenciales.NombreUsuario, credenciales.Clave);
        sesion.UsuarioAutenticado = usuario;
        return RespuestaDelServidor.Ok($"Bienvenido, {usuario.NombreUsuario}");
    }

    private async Task<RespuestaDelServidor> CerrarSesionAsync(Trama solicitud, SesionDeCliente sesion)
    {
        ExigirDatosVacios(solicitud);
        try
        {
            await _usuarios.CerrarSesionAsync(sesion.UsuarioAutenticado!);
        }
        finally
        {
            // La sesión se cierra aunque no se haya podido registrar en el historial: si quedara
            // abierta, el cliente (que ya volvió al menú anónimo) no podría volver a iniciar sesión.
            sesion.UsuarioAutenticado = null;
        }

        return RespuestaDelServidor.Ok("Sesión cerrada");
    }

    private async Task<RespuestaDelServidor> AltaDeModeloAsync(Trama solicitud, Usuario usuario)
    {
        await _modelos.AltaDeModeloAsync(FichaRecibida(solicitud), usuario);
        return RespuestaDelServidor.Ok("Modelo creado correctamente.");
    }

    private async Task<RespuestaDelServidor> ModificarModeloAsync(Trama solicitud, Usuario usuario)
    {
        await _modelos.ModificarModeloAsync(FichaRecibida(solicitud), usuario);
        return RespuestaDelServidor.Ok("Modelo modificado correctamente.");
    }

    private async Task<RespuestaDelServidor> BajaDeModeloAsync(Trama solicitud, Usuario usuario)
    {
        await _modelos.EliminarModeloAsync(solicitud.DatosComoTexto(), usuario);
        return RespuestaDelServidor.Ok("Modelo eliminado correctamente.");
    }

    private async Task<RespuestaDelServidor> BuscarModelosAsync(Trama solicitud)
    {
        FiltrosDeBusqueda filtros = Mensajes.AFiltros(solicitud.DatosComoTexto());
        FiltroDeModelos filtro = new(
            filtros.Texto,
            filtros.PrecioMinimo,
            filtros.PrecioMaximo,
            filtros.FechaEntregaMaxima,
            filtros.MinimoDisponibles);

        List<ModeloListado> listado = new();
        foreach (LecturaDeModelo modelo in await _modelos.BuscarAsync(filtro))
        {
            listado.Add(new ModeloListado(
                modelo.Nombre, modelo.Precio, modelo.FechaEntregaPrometida, modelo.UnidadesDisponibles));
        }

        return RespuestaDelServidor.Ok(Mensajes.DeModelosListados(listado));
    }

    private async Task<RespuestaDelServidor> ConsultarModeloAsync(Trama solicitud)
    {
        LecturaDeModelo modelo = await _modelos.ConsultarLecturaAsync(solicitud.DatosComoTexto());
        DetalleDeModelo detalle = new(
            modelo.Nombre,
            modelo.Descripcion,
            modelo.Precio,
            modelo.FechaEntregaPrometida,
            modelo.UnidadesDisponibles,
            modelo.UnidadesReservadas,
            modelo.NombreArchivoImagen);

        return RespuestaDelServidor.Ok(Mensajes.DeDetalleDeModelo(detalle));
    }

    // La subida va en dos pasos: validado el anuncio, la sesión confirma que puede recibir y
    // recién entonces el cliente manda las partes, que se escriben directo al archivo temporal.
    private async Task<RespuestaDelServidor> SubirImagenAsync(
        Trama solicitud, SesionDeCliente sesion, CancellationToken token)
    {
        SubidaDeImagen subida = Mensajes.ASubidaDeImagen(solicitud.DatosComoTexto());
        string extension = Path.GetExtension(ImagenesEnDisco.ValidarNombre(subida.NombreArchivo));
        string nombreArchivoNuevo = $"{Guid.NewGuid():N}{extension}";
        await _imagenes.GuardarAsync(
            nombreArchivoNuevo,
            destino => sesion.RecibirArchivoAsync(solicitud.Comando, destino, subida.Largo, token));

        string nombreArchivoAnterior =
            await CambiarImagenAsync(subida.NombreModelo, nombreArchivoNuevo, sesion.UsuarioAutenticado!);
        IntentarEliminarImagen(nombreArchivoAnterior, "No se pudo limpiar la imagen reemplazada");
        return RespuestaDelServidor.Ok("Imagen actualizada correctamente.");
    }

    // La respuesta solo anuncia nombre y largo: la sesión manda las partes después de la trama,
    // leyéndolas del disco de a una. La subida ya garantizó que la imagen no supera el máximo.
    private async Task<RespuestaDelServidor> DescargarImagenAsync(Trama solicitud)
    {
        LecturaDeModelo modelo = await _modelos.ConsultarLecturaAsync(solicitud.DatosComoTexto());
        if (!modelo.TieneImagen)
        {
            return RespuestaDelServidor.Rechazo($"El modelo {modelo.Nombre} no tiene imagen");
        }

        string nombreSugerido = $"{modelo.Nombre}{Path.GetExtension(modelo.NombreArchivoImagen)}";
        return RespuestaDelServidor.OkConArchivo(
            nombreSugerido, _imagenes.AbrirLectura(modelo.NombreArchivoImagen));
    }

    private async Task<RespuestaDelServidor> SolicitarReservaAsync(Trama solicitud, Usuario usuario)
    {
        Guid idReserva = await _reservas.SolicitarReservaAsync(solicitud.DatosComoTexto(), usuario);
        return RespuestaDelServidor.Ok(
            $"Reserva {FormatoDeCampos.De(idReserva)} creada, pendiente de aprobación");
    }

    private async Task<RespuestaDelServidor> CancelarReservaAsync(Trama solicitud, Usuario usuario)
    {
        Guid idReserva = FormatoDeCampos.AGuid(solicitud.DatosComoTexto());
        await _reservas.CancelarReservaAsync(idReserva, usuario);
        return RespuestaDelServidor.Ok($"Reserva '{idReserva}' cancelada");
    }

    private async Task<RespuestaDelServidor> ConsultarMisReservasAsync(Trama solicitud, Usuario usuario)
    {
        ExigirDatosVacios(solicitud);
        List<ReservaListada> listado = new();
        foreach (LecturaDeReserva reserva in await _reservas.ConsultarReservasDeAsync(usuario))
        {
            listado.Add(new ReservaListada(
                reserva.Id,
                reserva.NombreModelo,
                reserva.Estado.ToString(),
                reserva.FechaCreacion,
                reserva.UltimaActualizacion));
        }

        return RespuestaDelServidor.Ok(Mensajes.DeReservasListadas(listado));
    }

    private async Task<RespuestaDelServidor> ConsultarHistorialAsync(Trama solicitud, Usuario usuario)
    {
        ExigirDatosVacios(solicitud);
        List<ActividadListada> listado = new();
        foreach (Actividad actividad in await _usuarios.HistorialDeAsync(usuario))
        {
            listado.Add(new ActividadListada(
                actividad.Tipo.ToString(), actividad.Detalle, actividad.FechaHora));
        }

        return RespuestaDelServidor.Ok(Mensajes.DeActividades(listado));
    }

    // El alta y la modificación arman la misma ficha a partir del mismo payload.
    private static FichaDeModelo FichaRecibida(Trama solicitud)
    {
        DatosDeAltaDeModelo datos = Mensajes.ADatosDeModelo(solicitud.DatosComoTexto());
        // La imagen no viaja en este comando: se asigna aparte, con CMD 15.
        DatosDeModelo datosDeModelo = new(
            datos.Nombre, datos.Descripcion, datos.Precio, datos.FechaEntregaPrometida, string.Empty);

        return new FichaDeModelo(datosDeModelo, datos.Stock);
    }

    private async Task<string> CambiarImagenAsync(string nombreModelo, string nombreArchivoNuevo, Usuario usuario)
    {
        try
        {
            return await _modelos.CambiarImagenDeModeloAsync(nombreModelo, nombreArchivoNuevo, usuario);
        }
        catch
        {
            // Si el cambio no se registró, por el motivo que sea (el dominio lo rechaza, la base no
            // lo guarda o un error inesperado), la imagen recién guardada no debe quedar huérfana:
            // ningún modelo apunta a ella.
            IntentarEliminarImagen(
                nombreArchivoNuevo, "No se pudo compensar la imagen nueva tras rechazarse el cambio");
            throw;
        }
    }

    // Una limpieza que falla nunca tapa el error original ni voltea una operación ya confirmada.
    private void IntentarEliminarImagen(string nombreArchivo, string mensaje)
    {
        if (nombreArchivo.Length == 0)
        {
            return;
        }

        try
        {
            _imagenes.Eliminar(nombreArchivo);
        }
        catch (AlmacenDeImagenesException excepcion)
        {
            Console.Error.WriteLine($"{mensaje} '{nombreArchivo}': {excepcion}");
        }
    }

    private static void ExigirDatosVacios(Trama solicitud)
    {
        if (!solicitud.Datos.IsEmpty)
        {
            throw new ProtocoloException("DATOS debe estar vacío para este comando");
        }
    }
}
