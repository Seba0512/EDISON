using System.Globalization;

namespace Common;

// Siempre InvariantCulture para que los datos no cambien según el idioma configurado en la máquina.
public static class FormatoDeCampos
{
    private const string FormatoFecha = "dd-MM-yyyy";
    private const string FormatoFechaHora = "yyyyMMddHHmmss";
    private const NumberStyles EstiloDecimal = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static string De(int valor)
    {
        return valor.ToString(CultureInfo.InvariantCulture);
    }

    public static string De(long valor)
    {
        return valor.ToString(CultureInfo.InvariantCulture);
    }

    public static string De(decimal valor)
    {
        return valor.ToString(CultureInfo.InvariantCulture);
    }

    public static string DeFecha(DateTime fecha)
    {
        return fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture);
    }

    public static string DeFechaHora(DateTime fechaHora)
    {
        return fechaHora.ToUniversalTime().ToString(FormatoFechaHora, CultureInfo.InvariantCulture);
    }

    public static string De(Guid valor)
    {
        return valor.ToString("D");
    }

    public static int AEntero(string texto)
    {
        if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor))
        {
            return valor;
        }

        throw new ProtocoloException($"'{texto}' no es un entero válido");
    }

    public static long AEnteroLargo(string texto)
    {
        if (long.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out long valor))
        {
            return valor;
        }

        throw new ProtocoloException($"'{texto}' no es un entero válido");
    }

    public static decimal ADecimal(string texto)
    {
        // Sin separador de miles: con NumberStyles.Number, "1,5" se leería como 15 sin error.
        if (decimal.TryParse(texto, EstiloDecimal, CultureInfo.InvariantCulture, out decimal valor))
        {
            return valor;
        }

        throw new ProtocoloException($"'{texto}' no es un decimal válido");
    }

    public static DateTime AFecha(string texto)
    {
        if (DateTime.TryParseExact(
                texto, FormatoFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime valor))
        {
            return valor;
        }

        throw new ProtocoloException($"'{texto}' no es una fecha válida (formato {FormatoFecha})");
    }

    public static DateTime AFechaHora(string texto)
    {
        if (DateTime.TryParseExact(
                texto,
                FormatoFechaHora,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime valor))
        {
            return valor;
        }

        throw new ProtocoloException(
            $"'{texto}' no es una fecha/hora válida (formato {FormatoFechaHora})");
    }

    public static Guid AGuid(string texto)
    {
        if (Guid.TryParseExact(texto, "D", out Guid valor))
        {
            return valor;
        }

        throw new ProtocoloException($"'{texto}' no es un Guid válido");
    }
}

// Los DATOS de cada comando son texto UTF-8 con campos separados por '#', como en los ejemplos
// de clase. Las listas viajan como la cantidad seguida de los campos de cada elemento, en orden.
public static class Mensajes
{
    public const char Separador = '#';

    public static string Unir(params string[] campos)
    {
        foreach (string campo in campos)
        {
            if (campo is null)
            {
                throw new ProtocoloException("Un campo del mensaje no puede ser null");
            }

            if (campo.Contains(Separador))
            {
                throw new ProtocoloException($"Un campo no puede contener el carácter '{Separador}'");
            }
        }

        return string.Join(Separador, campos);
    }

    public static string[] Separar(string mensaje, int cantidadDeCampos)
    {
        string[] campos = mensaje.Split(Separador);
        if (campos.Length != cantidadDeCampos)
        {
            throw new ProtocoloException(
                $"Se esperaban {cantidadDeCampos} campos y llegaron {campos.Length}");
        }

        return campos;
    }

    public static string DeCredenciales(Credenciales credenciales)
    {
        return Unir(credenciales.NombreUsuario, credenciales.Clave);
    }

    public static Credenciales ACredenciales(string mensaje)
    {
        string[] campos = Separar(mensaje, 2);
        return new Credenciales(campos[0], campos[1]);
    }

    public static string DeDatosDeModelo(DatosDeAltaDeModelo datos)
    {
        return Unir(
            datos.Nombre,
            datos.Descripcion,
            FormatoDeCampos.De(datos.Precio),
            FormatoDeCampos.DeFecha(datos.FechaEntregaPrometida),
            FormatoDeCampos.De(datos.Stock));
    }

    public static DatosDeAltaDeModelo ADatosDeModelo(string mensaje)
    {
        string[] campos = Separar(mensaje, 5);
        return new DatosDeAltaDeModelo(
            campos[0],
            campos[1],
            FormatoDeCampos.ADecimal(campos[2]),
            FormatoDeCampos.AFecha(campos[3]),
            FormatoDeCampos.AEntero(campos[4]));
    }

    // Los filtros son opcionales: un campo vacío significa que ese filtro no se aplica.
    public static string DeFiltros(FiltrosDeBusqueda filtros)
    {
        return Unir(
            filtros.Texto ?? string.Empty,
            filtros.PrecioMinimo is decimal minimo ? FormatoDeCampos.De(minimo) : string.Empty,
            filtros.PrecioMaximo is decimal maximo ? FormatoDeCampos.De(maximo) : string.Empty,
            filtros.FechaEntregaMaxima is DateTime fecha ? FormatoDeCampos.DeFecha(fecha) : string.Empty,
            filtros.MinimoDisponibles is int disponibles ? FormatoDeCampos.De(disponibles) : string.Empty);
    }

    public static FiltrosDeBusqueda AFiltros(string mensaje)
    {
        string[] campos = Separar(mensaje, 5);
        return new FiltrosDeBusqueda(
            campos[0].Length == 0 ? null : campos[0],
            campos[1].Length == 0 ? null : FormatoDeCampos.ADecimal(campos[1]),
            campos[2].Length == 0 ? null : FormatoDeCampos.ADecimal(campos[2]),
            campos[3].Length == 0 ? null : FormatoDeCampos.AFecha(campos[3]),
            campos[4].Length == 0 ? null : FormatoDeCampos.AEntero(campos[4]));
    }

    public static string DeModelosListados(IReadOnlyList<ModeloListado> modelos)
    {
        List<string> campos = new() { FormatoDeCampos.De(modelos.Count) };
        foreach (ModeloListado modelo in modelos)
        {
            campos.Add(modelo.Nombre);
            campos.Add(FormatoDeCampos.De(modelo.Precio));
            campos.Add(FormatoDeCampos.DeFecha(modelo.FechaEntregaPrometida));
            campos.Add(FormatoDeCampos.De(modelo.UnidadesDisponibles));
        }

        return Unir(campos.ToArray());
    }

    public static IReadOnlyList<ModeloListado> AModelosListados(string mensaje)
    {
        LectorDeLista lector = new(mensaje, camposPorElemento: 4);
        List<ModeloListado> modelos = new();
        while (lector.HayElementos)
        {
            string[] campos = lector.LeerElemento();
            modelos.Add(new ModeloListado(
                campos[0],
                FormatoDeCampos.ADecimal(campos[1]),
                FormatoDeCampos.AFecha(campos[2]),
                FormatoDeCampos.AEntero(campos[3])));
        }

        return modelos;
    }

    public static string DeDetalleDeModelo(DetalleDeModelo detalle)
    {
        return Unir(
            detalle.Nombre,
            detalle.Descripcion,
            FormatoDeCampos.De(detalle.Precio),
            FormatoDeCampos.DeFecha(detalle.FechaEntregaPrometida),
            FormatoDeCampos.De(detalle.UnidadesDisponibles),
            FormatoDeCampos.De(detalle.UnidadesReservadas),
            detalle.NombreArchivoImagen);
    }

    public static DetalleDeModelo ADetalleDeModelo(string mensaje)
    {
        string[] campos = Separar(mensaje, 7);
        return new DetalleDeModelo(
            campos[0],
            campos[1],
            FormatoDeCampos.ADecimal(campos[2]),
            FormatoDeCampos.AFecha(campos[3]),
            FormatoDeCampos.AEntero(campos[4]),
            FormatoDeCampos.AEntero(campos[5]),
            campos[6]);
    }

    public static string DeReservasListadas(IReadOnlyList<ReservaListada> reservas)
    {
        List<string> campos = new() { FormatoDeCampos.De(reservas.Count) };
        foreach (ReservaListada reserva in reservas)
        {
            campos.Add(FormatoDeCampos.De(reserva.Id));
            campos.Add(reserva.NombreModelo);
            campos.Add(reserva.Estado);
            campos.Add(FormatoDeCampos.DeFechaHora(reserva.FechaCreacion));
            campos.Add(FormatoDeCampos.DeFechaHora(reserva.UltimaActualizacion));
        }

        return Unir(campos.ToArray());
    }

    public static IReadOnlyList<ReservaListada> AReservasListadas(string mensaje)
    {
        LectorDeLista lector = new(mensaje, camposPorElemento: 5);
        List<ReservaListada> reservas = new();
        while (lector.HayElementos)
        {
            string[] campos = lector.LeerElemento();
            reservas.Add(new ReservaListada(
                FormatoDeCampos.AGuid(campos[0]),
                campos[1],
                campos[2],
                FormatoDeCampos.AFechaHora(campos[3]),
                FormatoDeCampos.AFechaHora(campos[4])));
        }

        return reservas;
    }

    public static string DeActividades(IReadOnlyList<ActividadListada> actividades)
    {
        List<string> campos = new() { FormatoDeCampos.De(actividades.Count) };
        foreach (ActividadListada actividad in actividades)
        {
            campos.Add(actividad.Tipo);
            campos.Add(actividad.Detalle);
            campos.Add(FormatoDeCampos.DeFechaHora(actividad.FechaHora));
        }

        return Unir(campos.ToArray());
    }

    public static IReadOnlyList<ActividadListada> AActividades(string mensaje)
    {
        LectorDeLista lector = new(mensaje, camposPorElemento: 3);
        List<ActividadListada> actividades = new();
        while (lector.HayElementos)
        {
            string[] campos = lector.LeerElemento();
            actividades.Add(new ActividadListada(
                campos[0], campos[1], FormatoDeCampos.AFechaHora(campos[2])));
        }

        return actividades;
    }

    // Las imágenes no viajan en DATOS: el anuncio dice cuántos bytes van a seguir por el stream.
    public static string DeSubidaDeImagen(SubidaDeImagen subida)
    {
        return Unir(subida.NombreModelo, subida.NombreArchivo, FormatoDeCampos.De(subida.Largo));
    }

    public static SubidaDeImagen ASubidaDeImagen(string mensaje)
    {
        string[] campos = Separar(mensaje, 3);
        return new SubidaDeImagen(campos[0], campos[1], ALargoDeArchivo(campos[2]));
    }

    public static string DeArchivoAnunciado(ArchivoAnunciado archivo)
    {
        return Unir(archivo.Nombre, FormatoDeCampos.De(archivo.Largo));
    }

    public static ArchivoAnunciado AArchivoAnunciado(string mensaje)
    {
        string[] campos = Separar(mensaje, 2);
        return new ArchivoAnunciado(campos[0], ALargoDeArchivo(campos[1]));
    }

    // El largo se valida al leer el anuncio, antes de leer un solo byte del archivo.
    private static long ALargoDeArchivo(string texto)
    {
        long largo = FormatoDeCampos.AEnteroLargo(texto);
        if (largo < 0)
        {
            throw new ProtocoloException($"'{texto}' no es un largo de archivo válido");
        }

        if (largo > Limites.LargoMaximoDeArchivo)
        {
            throw new ProtocoloException("El archivo supera el tamaño máximo permitido " +
                $"({Limites.LargoMaximoDeArchivo} bytes)");
        }

        return largo;
    }

    // Recorre una lista plana: primero la cantidad y después los campos de cada elemento en orden.
    private sealed class LectorDeLista
    {
        private readonly string[] _campos;
        private readonly int _camposPorElemento;
        private int _posicion;
        private int _elementosRestantes;

        public LectorDeLista(string mensaje, int camposPorElemento)
        {
            _campos = mensaje.Split(Separador);
            _camposPorElemento = camposPorElemento;
            _elementosRestantes = FormatoDeCampos.AEntero(_campos[0]);
            _posicion = 1;
            // En long: una cantidad enorme desbordaría el int y podría coincidir por casualidad con
            // los campos recibidos, y la lectura terminaría fuera de rango.
            long camposEsperados = 1L + (long)_elementosRestantes * camposPorElemento;
            bool listaVaciaBienFormada = _elementosRestantes == 0 && _campos.Length == 2
                && _campos[1].Length == 0;
            if (_elementosRestantes < 0
                || (_campos.Length != camposEsperados && !listaVaciaBienFormada))
            {
                throw new ProtocoloException("La cantidad de la lista no coincide con sus campos");
            }
        }

        public bool HayElementos => _elementosRestantes > 0;

        public string[] LeerElemento()
        {
            string[] campos = _campos[_posicion..(_posicion + _camposPorElemento)];
            _posicion += _camposPorElemento;
            _elementosRestantes--;
            return campos;
        }
    }
}
