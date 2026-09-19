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
            new("Registrarse", Registrarse, RequierePausa: true),
            // Sin pausa porque enseguida se muestra el menú de la sesión nueva.
            new("Iniciar sesión", IniciarSesion, RequierePausa: false),
            new("Salir", Salir, RequierePausa: true)
        };
        _opcionesAutenticadas = new List<Opcion>
        {
            new("Alta de modelo", AltaDeModelo, RequierePausa: true),
            new("Modificar modelo", ModificarModelo, RequierePausa: true),
            new("Eliminar modelo", EliminarModelo, RequierePausa: true),
            new("Buscar modelos", BuscarModelos, RequierePausa: true),
            new("Consultar modelo", ConsultarModelo, RequierePausa: true),
            new("Solicitar reserva", SolicitarReserva, RequierePausa: true),
            new("Cancelar reserva", CancelarReserva, RequierePausa: true),
            new("Mis reservas", MisReservas, RequierePausa: true),
            new("Historial", Historial, RequierePausa: true),
            new("Cerrar sesión", CerrarSesion, RequierePausa: true)
        };
        _opcionesActuales = _opcionesAnonimas;
    }

    public void Ejecutar()
    {
        while (_seguirEjecutando)
        {
            Mostrar();
            int? indice = LectorDeConsola.LeerOpcion(_opcionesActuales.Count);
            // Se acabó la entrada y eso es una salida normal, no un error.
            if (indice is null)
                return;

            Console.WriteLine();
            bool requierePausa = EjecutarOpcion(_opcionesActuales[indice.Value]);
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
    private bool EjecutarOpcion(Opcion opcion)
    {
        try
        {
            opcion.Ejecutar();
            return opcion.RequierePausa;
        }
        catch (OperacionRechazadaException excepcion)
        {
            Consola.Error(excepcion.Message);
            return true;
        }
    }

    private void Registrarse()
    {
        Credenciales? credenciales = LectorDeConsola.LeerCredenciales(
            Validaciones.NombreUsuario, Validaciones.Clave);
        if (credenciales is null)
            return;

        Consola.Exito(_conexion.Registrar(credenciales.Value));
    }

    // Al iniciar sesión solo se exige que las credenciales no rompan el protocolo. Las reglas
    // de largo quedan para el registro y acá decide el servidor.
    private void IniciarSesion()
    {
        Credenciales? credenciales = LectorDeConsola.LeerCredenciales(
            Validaciones.SinSeparador, Validaciones.SinSeparador);
        if (credenciales is null)
            return;

        Consola.Exito(_conexion.IniciarSesion(credenciales.Value));
        _nombreUsuario = credenciales.Value.NombreUsuario;
        _opcionesActuales = _opcionesAutenticadas;
    }

    private void CerrarSesion()
    {
        try
        {
            Consola.Exito(_conexion.CerrarSesion());
        }
        catch (OperacionRechazadaException excepcion)
        {
            // Si el servidor dice que no hay sesión, quedarse en el menú autenticado sería mentir.
            Consola.Error(excepcion.Message);
        }

        _nombreUsuario = string.Empty;
        _opcionesActuales = _opcionesAnonimas;
    }

    private void Salir()
        => _seguirEjecutando = false;

    private void AltaDeModelo()
    {
        string? nombre = LectorDeConsola.LeerTexto("Nombre del modelo:", Validaciones.NombreDeModelo);
        if (nombre is null)
            return;

        if (ExisteModelo(nombre))
        {
            Consola.Error($"Ya existe un modelo llamado '{nombre}'.");
            return;
        }

        DatosDeAltaDeModelo? datos = LeerDatosParaAlta(nombre);
        if (datos is null)
            return;

        string? rutaImagen = LectorDeConsola.PreguntarRutaDeImagen();
        Consola.Exito(_conexion.AltaDeModelo(datos.Value));
        LectorDeConsola.SubirImagenSiHayRuta(_conexion, datos.Value.Nombre, rutaImagen);
    }

    private void ModificarModelo()
    {
        string? nombre = LectorDeConsola.LeerTexto("Nombre del modelo a modificar:");
        if (nombre is null)
            return;

        // Se consulta antes de pedir el resto de los datos. Si el modelo no existe, lanza y no
        // sigue. Además el detalle trae la fecha y el stock actuales para validar los nuevos.
        DetalleDeModelo detalleActual = _conexion.Consultar(nombre);

        DatosDeAltaDeModelo? datos = LeerDatosParaModificacion(nombre, detalleActual);
        if (datos is null)
            return;

        string? rutaImagen = LectorDeConsola.PreguntarRutaDeImagen();
        Consola.Exito(_conexion.ModificarModelo(datos.Value));
        LectorDeConsola.SubirImagenSiHayRuta(_conexion, datos.Value.Nombre, rutaImagen);
    }

    private void EliminarModelo()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a eliminar:");
        if (nombreModelo is null)
            return;

        Consola.Exito(_conexion.EliminarModelo(nombreModelo));
    }

    private void BuscarModelos()
    {
        FiltrosDeBusqueda? filtros = LectorDeConsola.LeerFiltros();
        if (filtros is null)
            return;

        Presentadores.MostrarModelos(_conexion.Buscar(filtros.Value));
    }

    private void ConsultarModelo()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a consultar:");
        if (nombreModelo is null)
            return;

        DetalleDeModelo detalle = _conexion.Consultar(nombreModelo);
        Presentadores.MostrarDetalle(detalle);
        OfrecerDescargaDeImagen(nombreModelo, detalle);
    }

    private void OfrecerDescargaDeImagen(string nombreModelo, DetalleDeModelo detalle)
    {
        if (detalle.NombreArchivoImagen.Length == 0)
            return;

        if (!LectorDeConsola.Confirmar("¿Desea descargar la imagen? (s/n):"))
            return;

        Consola.Exito(_conexion.DescargarImagen(nombreModelo));
    }

    private void SolicitarReserva()
    {
        string? nombreModelo = LectorDeConsola.LeerTexto("Nombre del modelo a reservar:");
        if (nombreModelo is null)
            return;

        Consola.Exito(_conexion.SolicitarReserva(nombreModelo));
    }

    private void CancelarReserva()
    {
        IReadOnlyList<ReservaListada> reservas = _conexion.MisReservas();
        Presentadores.MostrarReservas(reservas);
        if (reservas.Count == 0)
            return;

        Guid? idReserva = LeerIdDeReservaCancelable(reservas);
        if (idReserva is null)
            return;

        Consola.Exito(_conexion.CancelarReserva(idReserva.Value));
    }

    private void MisReservas()
        => Presentadores.MostrarReservas(_conexion.MisReservas());

    private void Historial()
        => Presentadores.MostrarHistorial(_conexion.Historial());

    // La única causa realista de rechazo de una consulta por nombre es que el modelo no existe.
    private bool ExisteModelo(string nombre)
    {
        try
        {
            _conexion.Consultar(nombre);
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

        int? stock = LectorDeConsola.LeerEntero(
            "Nuevo stock:",
            nuevoStock => Validaciones.StockDeModificacion(nuevoStock, detalleActual));
        if (stock is null)
            return null;

        return new DatosDeAltaDeModelo(
            nombreModelo, descripcion ?? string.Empty, precio.Value, fechaEntrega.Value, stock.Value);
    }

    // Valida contra las reservas ya mostradas. Evita mandar al servidor un id que no está en la
    // lista, o el de una reserva que ya no se puede cancelar, y perder la elección por eso.
    private static Guid? LeerIdDeReservaCancelable(IReadOnlyList<ReservaListada> reservas)
    {
        while (true)
        {
            Guid? candidato = LectorDeConsola.LeerGuid("Id de la reserva a cancelar:");
            if (candidato is null)
                return null;

            ReservaListada? reserva = ReservaConId(reservas, candidato.Value);
            if (reserva is null)
            {
                Consola.Error("Ese id no corresponde a ninguna de tus reservas.");
                continue;
            }

            if (!EstadosDeReserva.SePuedeCancelar(reserva.Value.Estado))
            {
                Consola.Error($"No se puede cancelar una reserva {reserva.Value.Estado}.");
                continue;
            }

            return candidato;
        }
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

    private sealed record Opcion(string Titulo, Action Ejecutar, bool RequierePausa);
}
