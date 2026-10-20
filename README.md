# Edison: modelos y reservas de vehículos

Obligatorio de Programación de Redes (Universidad ORT Uruguay, 2026). Es un sistema cliente-servidor
sobre TCP con un protocolo propio. El servidor atiende a varios clientes a la vez con Tasks y
`async`/`await`. Guarda usuarios, modelos, reservas e historial en MySQL. Cuando se aprueba una
reserva, genera un comprobante en disco.

## Estructura de la solución

| Proyecto / carpeta | Contenido |
|---|---|
| `Common/` | Protocolo compartido: tramas, mensajes, transferencia de archivos por partes, configuración por variables de entorno |
| `Servidor/` | Red (`ServidorTcp`, `SesionDeCliente`), comandos, dominio, persistencia MySQL (`Persistencia/`), imágenes y comprobantes en disco |
| `Cliente/` | Aplicación de consola: menú, lectura validada de datos y conexión con el servidor |
| `db/init.sql` | Script que crea la base `edison` y sus tablas en utf8mb4 |
| `docs/` | Protocolo, diagramas PlantUML y documentación de la entrega |

## Requisitos

- .NET SDK 8.0
- Docker: la base de datos corre en un contenedor MySQL 8.4

## Configuración

Las dos aplicaciones se configuran solo con variables de entorno. Si una variable no está definida,
se usa su valor por defecto, y con esos valores todo corre en una misma máquina sin configurar nada.

**Servidor**

| Variable | Por defecto | Uso |
|---|---|---|
| `SERVIDOR_IP_ESCUCHA` | `0.0.0.0` | IP donde escucha (todas las interfaces) |
| `SERVIDOR_PUERTO` | `8080` | Puerto TCP del servidor |
| `DIRECTORIO_IMAGENES` | `imagenes` | Carpeta de las imágenes de los modelos |
| `DIRECTORIO_COMPROBANTES` | `comprobantes` | Carpeta de los comprobantes: `<usuario>/<idReserva>.txt` |
| `PLAZO_RESERVA_SEGUNDOS` | `30` | Tiempo hasta que una reserva pendiente se confirma o se rechaza |
| `MYSQL_CADENA_CONEXION` | `Server=127.0.0.1;Port=3306;Database=edison;User=edison;Password=edison;CharSet=utf8mb4` | Conexión con MySQL |

**Cliente**

| Variable | Por defecto | Uso |
|---|---|---|
| `SERVIDOR_HOST` | `127.0.0.1` | IP o nombre del servidor (en Docker, el nombre del contenedor) |
| `SERVIDOR_PUERTO` | `8080` | Puerto TCP del servidor |
| `DIRECTORIO_DESCARGAS` | `descargas` | Carpeta donde se guardan las imágenes descargadas |

Las rutas relativas se resuelven desde la carpeta donde se ejecuta cada aplicación.

## Ejecución local (aplicaciones con dotnet, base en Docker)

1. Levantar MySQL. El script `db/init.sql` crea las tablas la primera vez, cuando el volumen está vacío:

   ```powershell
   docker run -d --name edison-mysql -p 3306:3306 `
     -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=edison `
     -e MYSQL_USER=edison -e MYSQL_PASSWORD=edison `
     -v edison-mysql:/var/lib/mysql `
     -v "${PWD}/db/init.sql:/docker-entrypoint-initdb.d/init.sql:ro" `
     mysql:8.4
   ```

   En bash, cambiar los acentos graves del final de línea por `\` y `${PWD}` por `$(pwd)`.

2. Levantar el servidor desde la raíz del repositorio:

   ```powershell
   dotnet run --project Servidor
   ```

   Si MySQL todavía está iniciando, el servidor reintenta la conexión hasta 15 veces, con 2 s entre
   cada intento. Cuando se conecta muestra `Servidor escuchando en 0.0.0.0:8080`.

3. Levantar uno o más clientes, cada uno en su propia terminal:

   ```powershell
   dotnet run --project Cliente
   ```

Para cambiar un valor, se define la variable antes de ejecutar. Por ejemplo, para que las reservas
se resuelvan a los 10 segundos:

```powershell
$env:PLAZO_RESERVA_SEGUNDOS = "10"; dotnet run --project Servidor
```

```bash
PLAZO_RESERVA_SEGUNDOS=10 dotnet run --project Servidor
```

## Cierre

- **Servidor:** escribir `salir` en su consola, o enviarle Ctrl+C o SIGTERM (`docker stop`). En
  cualquiera de los casos, primero deja de aceptar conexiones, después cierra las de los clientes y
  por último espera a que terminen las reservas que se estaban resolviendo. Las reservas que siguen
  pendientes quedan así en la base y se reprograman en el próximo arranque, con el tiempo que les
  quedaba.
- **Cliente:** opción *Salir* del menú.
- **Base:** `docker stop edison-mysql`. Para borrar todos los datos: `docker rm -f edison-mysql` y
  `docker volume rm edison-mysql`.

## Archivos que genera el sistema

| Qué | Dónde (relativo a la carpeta de ejecución) |
|---|---|
| Imágenes de los modelos (servidor) | `imagenes/` |
| Comprobantes de reservas aprobadas (servidor) | `comprobantes/<usuario>/<idReserva>.txt` |
| Imágenes descargadas (cliente) | `descargas/` |

Las imágenes y los comprobantes no se guardan en la base. Los datos de los modelos, incluido el
nombre de su imagen, sí están en MySQL, y por eso las imágenes se conservan entre reinicios.
