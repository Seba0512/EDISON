using Common;
using Servidor.Dominio.Entidades;
using Servidor.Dominio.Excepciones;
using Servidor.Dominio.Servicios;

namespace Servidor;

internal sealed class EjecutorDeComandos
{
    public const string SesionRequerida = "Debe iniciar sesión para realizar esta operación";

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

    public RespuestaDelServidor Ejecutar(Trama solicitud, SesionDeCliente sesion)
    {
        try
        {
            return Responder(solicitud, sesion);
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
    }

    private RespuestaDelServidor Responder(Trama solicitud, SesionDeCliente sesion)
    {
        if (ExigeSesion(solicitud.Comando) && sesion.UsuarioAutenticado is null)
        {
            return RespuestaDelServidor.Rechazo(SesionRequerida);
        }

        switch (solicitud.Comando)
        {
            case Comandos.Registrar:
                return Registrar(solicitud);
            case Comandos.IniciarSesion:
                return IniciarSesion(solicitud, sesion);
            case Comandos.CerrarSesion:
                return CerrarSesion(solicitud, sesion);
            case Comandos.AltaDeModelo:
                return AltaDeModelo(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.ModificarModelo:
                return ModificarModelo(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.BajaDeModelo:
                return BajaDeModelo(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.BuscarModelos:
                return BuscarModelos(solicitud);
            case Comandos.ConsultarModelo:
                return ConsultarModelo(solicitud);
            case Comandos.SubirImagen:
                return SubirImagen(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.DescargarImagen:
                return DescargarImagen(solicitud);
            case Comandos.SolicitarReserva:
                return SolicitarReserva(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.CancelarReserva:
                return CancelarReserva(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.ConsultarMisReservas:
                return ConsultarMisReservas(solicitud, sesion.UsuarioAutenticado!);
            case Comandos.ConsultarHistorial:
                return ConsultarHistorial(solicitud, sesion.UsuarioAutenticado!);
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

    private RespuestaDelServidor Registrar(Trama solicitud)
    {
        Credenciales credenciales = Mensajes.ACredenciales(solicitud.DatosComoTexto());
        Usuario usuario = _usuarios.Registrar(credenciales.NombreUsuario, credenciales.Clave);
        return RespuestaDelServidor.Ok($"Usuario '{usuario.NombreUsuario}' registrado");
    }

    private RespuestaDelServidor IniciarSesion(Trama solicitud, SesionDeCliente sesion)
    {
        if (sesion.UsuarioAutenticado is not null)
        {
            return RespuestaDelServidor.Rechazo("La conexión ya tiene una sesión iniciada");
        }

        Credenciales credenciales = Mensajes.ACredenciales(solicitud.DatosComoTexto());
        Usuario usuario = _usuarios.IniciarSesion(credenciales.NombreUsuario, credenciales.Clave);
        sesion.UsuarioAutenticado = usuario;
        return RespuestaDelServidor.Ok($"Bienvenido, {usuario.NombreUsuario}");
    }

    private RespuestaDelServidor CerrarSesion(Trama solicitud, SesionDeCliente sesion)
    {
        ExigirDatosVacios(solicitud);
        _usuarios.CerrarSesion(sesion.UsuarioAutenticado!);
        sesion.UsuarioAutenticado = null;
        return RespuestaDelServidor.Ok("Sesión cerrada");
    }

    private RespuestaDelServidor AltaDeModelo(Trama solicitud, Usuario usuario)
    {
        _modelos.AltaDeModelo(FichaRecibida(solicitud), usuario);
        return RespuestaDelServidor.Ok("Modelo creado correctamente.");
    }

    private RespuestaDelServidor ModificarModelo(Trama solicitud, Usuario usuario)
    {
        _modelos.ModificarModelo(FichaRecibida(solicitud), usuario);
        return RespuestaDelServidor.Ok("Modelo modificado correctamente.");
    }

    private RespuestaDelServidor BajaDeModelo(Trama solicitud, Usuario usuario)
    {
        _modelos.EliminarModelo(solicitud.DatosComoTexto(), usuario, _imagenes.Eliminar);
        return RespuestaDelServidor.Ok("Modelo eliminado correctamente.");
    }

    private RespuestaDelServidor BuscarModelos(Trama solicitud)
    {
        FiltrosDeBusqueda filtros = Mensajes.AFiltros(solicitud.DatosComoTexto());
        FiltroDeModelos filtro = new(
            filtros.Texto,
            filtros.PrecioMinimo,
            filtros.PrecioMaximo,
            filtros.FechaEntregaMaxima,
            filtros.MinimoDisponibles);

        List<ModeloListado> listado = new();
        foreach (LecturaDeModelo modelo in _modelos.Buscar(filtro))
        {
            listado.Add(new ModeloListado(
                modelo.Nombre, modelo.Precio, modelo.FechaEntregaPrometida, modelo.UnidadesDisponibles));
        }

        return RespuestaDelServidor.Ok(Mensajes.DeModelosListados(listado));
    }

    private RespuestaDelServidor ConsultarModelo(Trama solicitud)
    {
        LecturaDeModelo modelo = _modelos.ConsultarLectura(solicitud.DatosComoTexto());
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

    // La imagen viaja dentro de DATOS: primero el texto "modelo#archivo" y después sus bytes.
    private RespuestaDelServidor SubirImagen(Trama solicitud, Usuario usuario)
    {
        (string mensaje, byte[] imagen) = Mensajes.ATextoYBinario(solicitud.Datos.Span);
        if (imagen.LongLength > Limites.LargoMaximoDeArchivo)
        {
            return RespuestaDelServidor.Rechazo(
                $"El archivo supera el tamaño máximo permitido ({Limites.LargoMaximoDeArchivo} bytes)");
        }

        (string nombreModelo, string nombreArchivoCliente) = Mensajes.ASubidaDeImagen(mensaje);
        string extension = Path.GetExtension(ImagenesEnDisco.ValidarNombre(nombreArchivoCliente));
        string nombreArchivoNuevo = $"{Guid.NewGuid():N}{extension}";
        using MemoryStream archivo = new(imagen);
        _imagenes.Guardar(nombreArchivoNuevo, archivo);

        string nombreArchivoAnterior = CambiarImagen(nombreModelo, nombreArchivoNuevo, usuario);
        IntentarEliminarImagen(nombreArchivoAnterior, "No se pudo limpiar la imagen reemplazada");
        return RespuestaDelServidor.Ok("Imagen actualizada correctamente.");
    }

    private RespuestaDelServidor DescargarImagen(Trama solicitud)
    {
        LecturaDeModelo modelo = _modelos.ConsultarLectura(solicitud.DatosComoTexto());
        if (!modelo.TieneImagen)
        {
            return RespuestaDelServidor.Rechazo($"El modelo {modelo.Nombre} no tiene imagen");
        }

        string nombreSugerido = $"{modelo.Nombre}{Path.GetExtension(modelo.NombreArchivoImagen)}";
        using Stream origen = _imagenes.AbrirLectura(modelo.NombreArchivoImagen);
        if (origen.Length > Limites.LargoMaximoDeArchivo)
        {
            return RespuestaDelServidor.Rechazo(
                $"La imagen supera el tamaño máximo de transferencia ({Limites.LargoMaximoDeArchivo} bytes)");
        }

        try
        {
            using MemoryStream memoria = new();
            origen.CopyTo(memoria);
            return RespuestaDelServidor.OkConArchivo(nombreSugerido, memoria.ToArray());
        }
        catch (IOException excepcion)
        {
            // Todavía no se escribió nada en la red: se puede rechazar y la conexión sigue.
            Console.Error.WriteLine($"No se pudo leer la imagen del modelo {modelo.Nombre}: {excepcion.Message}");
            return RespuestaDelServidor.Rechazo("No se pudo leer la imagen en el servidor. Intente nuevamente.");
        }
    }

    private RespuestaDelServidor SolicitarReserva(Trama solicitud, Usuario usuario)
    {
        Guid idReserva = _reservas.SolicitarReserva(solicitud.DatosComoTexto(), usuario);
        return RespuestaDelServidor.Ok(
            $"Reserva {FormatoDeCampos.De(idReserva)} creada, pendiente de aprobación");
    }

    private RespuestaDelServidor CancelarReserva(Trama solicitud, Usuario usuario)
    {
        Guid idReserva = FormatoDeCampos.AGuid(solicitud.DatosComoTexto());
        _reservas.CancelarReserva(idReserva, usuario);
        return RespuestaDelServidor.Ok($"Reserva '{idReserva}' cancelada");
    }

    private RespuestaDelServidor ConsultarMisReservas(Trama solicitud, Usuario usuario)
    {
        ExigirDatosVacios(solicitud);
        List<ReservaListada> listado = new();
        foreach (LecturaDeReserva reserva in _reservas.ConsultarReservasDe(usuario))
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

    private RespuestaDelServidor ConsultarHistorial(Trama solicitud, Usuario usuario)
    {
        ExigirDatosVacios(solicitud);
        List<ActividadListada> listado = new();
        foreach (Actividad actividad in _usuarios.HistorialDe(usuario))
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

    private string CambiarImagen(string nombreModelo, string nombreArchivoNuevo, Usuario usuario)
    {
        try
        {
            return _modelos.CambiarImagenDeModelo(nombreModelo, nombreArchivoNuevo, usuario);
        }
        catch (DominioException)
        {
            // Si el dominio rechaza el cambio, la imagen recién guardada no debe quedar huérfana.
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
