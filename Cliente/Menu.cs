using Common;

namespace Cliente;

internal sealed class Menu
{
    private readonly ConexionConServidor _conexion;
    private readonly IReadOnlyList<Opcion> _opcionesAnonimas;
    private readonly IReadOnlyList<Opcion> _opcionesAutenticadas;
    private IReadOnlyList<Opcion> _opcionesActuales;
    private string _nombreUsuario = string.Empty;
    private bool _seguirEjecutando = true;

    public Menu(ConexionConServidor conexion)
    {
        _conexion = conexion;
        _opcionesAnonimas = new List<Opcion>
        {
            new("Registrarse", RegistrarseAsync, RequierePausa: true),
            // Sin pausa porque enseguida se muestra el menú de la sesión nueva.
            new("Iniciar sesión", IniciarSesionAsync, RequierePausa: false),
            new("Salir", SalirAsync, RequierePausa: true)
        };
        _opcionesAutenticadas = new List<Opcion>
        {
            new("Alta de modelo", AltaDeModeloAsync, RequierePausa: true),
            new("Modificar modelo", ModificarModeloAsync, RequierePausa: true),
            new("Eliminar modelo", EliminarModeloAsync, RequierePausa: true),
            new("Buscar modelos", BuscarModelosAsync, RequierePausa: true),
            new("Consultar modelo", ConsultarModeloAsync, RequierePausa: true),
            new("Solicitar reserva", SolicitarReservaAsync, RequierePausa: true),
            new("Cancelar reserva", CancelarReservaAsync, RequierePausa: true),
            new("Mis reservas", MisReservasAsync, RequierePausa: true),
            new("Historial", HistorialAsync, RequierePausa: true),
            new("Cerrar sesión", CerrarSesionAsync, RequierePausa: true)
        };
        _opcionesActuales = _opcionesAnonimas;
    }

    public async Task EjecutarAsync()
    {
        while (_seguirEjecutando)
        {
            Mostrar();
            int? indice = LectorDeConsola.LeerOpcion(_opcionesActuales.Count);
            // Se acabó la entrada y eso es una salida normal, no un error.
            if (indice is null)
                return;

            Console.WriteLine();
            bool requierePausa = await EjecutarOpcionAsync(_opcionesActuales[indice.Value]);
            if (_seguirEjecutando && requierePausa)
                LectorDeConsola.EsperarEnter();
        }
    }

    private void Mostrar()
    {
        string titulo = _nombreUsuario.Length > 0 ? $"Sesión de {_nombreUsuario}" : "Menú principal";
        Consola.Titulo(titulo);
        for (int numero = 1; numero <= _opcionesActuales.Count; numero++)
            Console.WriteLine($"{numero}. {_opcionesActuales[numero - 1].Titulo}");

        Console.WriteLine();
    }

    // Ante un rechazo se pausa igual para que el error alcance a leerse antes de redibujar el menú.
    private async Task<bool> EjecutarOpcionAsync(Opcion opcion)
    {
        try
        {
            await opcion.Ejecutar();
            return opcion.RequierePausa;
        }
        catch (OperacionRechazadaException excepcion)
        {
            Consola.Error(excepcion.Message);
            return true;
        }
    }

    private async Task RegistrarseAsync()
    {
        Credenciales? credenciales = LectorDeConsola.LeerCredenciales(
            Validaciones.NombreUsuario, Validaciones.Clave);
        if (credenciales is null)
            return;

        Consola.Exito(await _conexion.RegistrarAsync(credenciales.Value));
    }

    // Al iniciar sesión solo se exige que las credenciales no rompan el protocolo. Las reglas
    // de largo quedan para el registro y acá decide el servidor.
    private async Task IniciarSesionAsync()
    {
        Credenciales? credenciales = LectorDeConsola.LeerCredenciales(
            Validaciones.SinSeparador, Validaciones.SinSeparador);
        if (credenciales is null)
            return;

        Consola.Exito(await _conexion.IniciarSesionAsync(credenciales.Value));
        _nombreUsuario = credenciales.Value.NombreUsuario;
        _opcionesActuales = _opcionesAutenticadas;
    }

    private async Task CerrarSesionAsync()
    {
        try
        {
            Consola.Exito(await _conexion.CerrarSesionAsync());
        }
        catch (OperacionRechazadaException excepcion)
        {
            // Si el servidor dice que no hay sesión, quedarse en el menú autenticado sería mentir.
            Consola.Error(excepcion.Message);
        }

        _nombreUsuario = string.Empty;
        _opcionesActuales = _opcionesAnonimas;
    }

    private Task SalirAsync()
    {
        _seguirEjecutando = false;
        return Task.CompletedTask;
    }

    private async Task AltaDeModeloAsync()
    {
        string? nombre = LectorDeConsola.LeerTexto("Nombre del modelo:", Validaciones.NombreDeModelo);
        if (nombre is null)
            return;

        if (await ExisteModeloAsync(nombre))
        {
            Consola.Error($"Ya existe un modelo llamado '{nombre}'.");
            return;
        }

        DatosDeAltaDeModelo? datos = LeerDatosParaAlta(nombre);
        if (datos is null)
            return;

        string? rutaImagen = LectorDeConsola.PreguntarRutaDeImagen();
        Consola.Exito(await _conexion.AltaDeModeloAsync(datos.Value));
        await LectorDeConsola.SubirImagenSiHayRutaAsync(_conexion, datos.Value.Nombre, rutaImagen);
    }

    private async Task ModificarModeloAsync()
    {
        string? nombre = LectorDeConsola.LeerTexto("Nombre del modelo a modificar:");
        if (nombre is null)
            return;

        // Se consulta antes de pedir el resto de los datos. Si el modelo no existe, lanza y no
        // sigue. Además el detalle trae la fecha actual para validar la nueva.
        DetalleDeModelo detalleActual = await _conexion.ConsultarAsync(nombre);

        DatosDeAltaDeModelo? datos = LeerDatosParaModificacion(nombre, detalleActual);
        if (datos is null)
            return;

        string? rutaImagen = LectorDeConsola.PreguntarRutaDeImagen();
        Consola.Exito(await _conexion.ModificarModeloAsync(datos.Value));
        await LectorDeConsola.SubirImagenSiHayRutaAsync(_conexion, datos.Value.Nombre, rutaImagen);
    }

    private async Task EliminarModeloAsync()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a eliminar:");
        if (nombreModelo is null)
            return;

        Consola.Exito(await _conexion.EliminarModeloAsync(nombreModelo));
    }

    private async Task BuscarModelosAsync()
    {
        FiltrosDeBusqueda? filtros = LectorDeConsola.LeerFiltros();
        if (filtros is null)
            return;

        Presentadores.MostrarModelos(await _conexion.BuscarAsync(filtros.Value));
    }

    private async Task ConsultarModeloAsync()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a consultar:");
        if (nombreModelo is null)
            return;

        DetalleDeModelo detalle = await _conexion.ConsultarAsync(nombreModelo);
        Presentadores.MostrarDetalle(detalle);
        await OfrecerDescargaDeImagenAsync(nombreModelo, detalle);
    }

    private async Task OfrecerDescargaDeImagenAsync(string nombreModelo, DetalleDeModelo detalle)
    {
        if (detalle.NombreArchivoImagen.Length == 0)
            return;

        if (!LectorDeConsola.Confirmar("¿Desea descargar la imagen? (s/n):"))
            return;

        Consola.Exito(await _conexion.DescargarImagenAsync(nombreModelo));
    }

    private async Task SolicitarReservaAsync()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a reservar:");
        if (nombreModelo is null)
            return;

        Consola.Exito(await _conexion.SolicitarReservaAsync(nombreModelo));
    }

    private async Task CancelarReservaAsync()
    {
        IReadOnlyList<ReservaListada> reservas = await _conexion.MisReservasAsync();
        Presentadores.MostrarReservas(reservas);
        if (reservas.Count == 0)
            return;

        // Sin esto, con todas las reservas Rechazadas o Canceladas, el id se pediría para siempre.
        if (!reservas.Any(reserva => EstadosDeReserva.SePuedeCancelar(reserva.Estado)))
        {
            Consola.Info("No tenés reservas que se puedan cancelar.");
            return;
        }

        Guid? idReserva = LeerIdDeReservaCancelable(reservas);
        if (idReserva is null)
            return;

        Consola.Exito(await _conexion.CancelarReservaAsync(idReserva.Value));
    }

    private async Task MisReservasAsync()
        => Presentadores.MostrarReservas(await _conexion.MisReservasAsync());

    private async Task HistorialAsync()
        => Presentadores.MostrarHistorial(await _conexion.HistorialAsync());

    // La única causa realista de rechazo de una consulta por nombre es que el modelo no existe.
    private async Task<bool> ExisteModeloAsync(string nombre)
    {
        try
        {
            await _conexion.ConsultarAsync(nombre);
            return true;
        }
        catch (OperacionRechazadaException)
        {
            return false;
        }
    }

    private static DatosDeAltaDeModelo? LeerDatosParaAlta(string nombreModelo)
    {
        (bool sinEntrada, string? descripcion) = LectorDeConsola.LeerTextoOpcional(
            "Descripción (Enter para omitir):", Validaciones.Descripcion);
        if (sinEntrada)
            return null;

        decimal? precio = LectorDeConsola.LeerDecimal("Precio:", Validaciones.Precio);
        if (precio is null)
            return null;

        DateTime? fechaEntrega = LectorDeConsola.LeerFecha(
            "Fecha de entrega prometida (dd-MM-yyyy):", Validaciones.FechaFutura);
        if (fechaEntrega is null)
            return null;

        int? stock = LectorDeConsola.LeerEntero("Stock:", Validaciones.Stock);
        if (stock is null)
            return null;

        return new DatosDeAltaDeModelo(
            nombreModelo, descripcion ?? string.Empty, precio.Value, fechaEntrega.Value, stock.Value);
    }

    private static DatosDeAltaDeModelo? LeerDatosParaModificacion(
        string nombreModelo, DetalleDeModelo detalleActual)
    {
        (bool sinEntrada, string? descripcion) = LectorDeConsola.LeerTextoOpcional(
            "Nueva descripción (Enter para omitir):", Validaciones.Descripcion);
        if (sinEntrada)
            return null;

        decimal? precio = LectorDeConsola.LeerDecimal("Nuevo precio:", Validaciones.Precio);
        if (precio is null)
            return null;

        DateTime? fechaEntrega = LectorDeConsola.LeerFecha(
            "Nueva fecha de entrega prometida (dd-MM-yyyy):",
            fecha => Validaciones.FechaFuturaSiCambio(fecha, detalleActual.FechaEntregaPrometida));
        if (fechaEntrega is null)
            return null;

        int? stock = LectorDeConsola.LeerEntero("Nuevo stock:", Validaciones.Stock);
        if (stock is null)
            return null;

        return new DatosDeAltaDeModelo(
            nombreModelo, descripcion ?? string.Empty, precio.Value, fechaEntrega.Value, stock.Value);
    }

    // Valida contra las reservas ya mostradas. Evita mandar al servidor un id que no está en la
    // lista, o el de una reserva que ya no se puede cancelar, y perder la elección por eso.
    private static Guid? LeerIdDeReservaCancelable(IReadOnlyList<ReservaListada> reservas)
    {
        Guid? candidato;
        do
        {
            candidato = LectorDeConsola.LeerGuid("Id de la reserva a cancelar:");
        } while (candidato is not null && !EsCancelable(reservas, candidato.Value));

        return candidato;
    }

    private static bool EsCancelable(IReadOnlyList<ReservaListada> reservas, Guid id)
    {
        ReservaListada? reserva = ReservaConId(reservas, id);
        if (reserva is null)
        {
            Consola.Error("Ese id no corresponde a ninguna de tus reservas.");
            return false;
        }

        if (!EstadosDeReserva.SePuedeCancelar(reserva.Value.Estado))
        {
            Consola.Error($"No se puede cancelar una reserva {reserva.Value.Estado}.");
            return false;
        }

        return true;
    }

    // ReservaListada es un record struct. Buscar con FirstOrDefault devolvería una reserva
    // vacía en vez de null y "ese id no existe" nunca se detectaría.
    private static ReservaListada? ReservaConId(IReadOnlyList<ReservaListada> reservas, Guid id)
    {
        foreach (ReservaListada reserva in reservas)
        {
            if (reserva.Id == id)
                return reserva;
        }

        return null;
    }

    private sealed record Opcion(string Titulo, Func<Task> Ejecutar, bool RequierePausa);
}
