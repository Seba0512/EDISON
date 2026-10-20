# Protocolo de comunicación

Este es el protocolo vigente en la entrega 2. Respecto de la entrega 1, la trama es la misma. Lo que
cambió:

- la forma de transferir imágenes (CMD 15 y 16), por la devolución de los docentes;
- los límites, que pasaron a ser constantes del protocolo;
- el manejo de las fallas de la base de datos.

Todo el código del protocolo está en `Common/` y lo comparten cliente y servidor:

- `Protocolo.cs`: tramas, estados y códec.
- `Mensajes.cs`: campos de cada comando.
- `TransferenciaPorPartes.cs`: archivos.

## Trama

```
+--------+--------+--------+--------+--------+--------+---------------------+
| CMD[0] | CMD[1] |          LARGO (int32)            |        DATOS        |
+--------+--------+--------+--------+--------+--------+---------------------+
 1 byte   1 byte               4 bytes                    LARGO bytes
```

| Campo | Formato |
|---|---|
| CMD | Dos dígitos ASCII, de `00` a `99` |
| LARGO | Entero de 32 bits **big-endian**: cantidad de bytes de DATOS, no de caracteres |
| DATOS | Texto UTF-8 estricto. Un byte inválido da error de protocolo y nunca se reemplaza en silencio |

- El encabezado tiene tamaño fijo, 6 bytes. Cada lado lee exactamente 6 bytes y después exactamente
  LARGO bytes, y así sabe siempre dónde termina una trama aunque TCP entregue el flujo partido o
  junto.
- No hay campo REQ/RES: el cliente solo envía solicitudes y el servidor solo envía respuestas. La
  respuesta repite el CMD de la solicitud, y el cliente verifica que coincida.
- El protocolo es estrictamente pedido-respuesta: el cliente no envía una solicitud nueva hasta
  recibir la respuesta de la anterior.

### Límites (`Common/Limites`)

| Límite | Valor | Para qué |
|---|---|---|
| `LargoMaximoDeDatos` | 1 MiB | Se rechaza un LARGO mayor antes de reservar memoria para leerlo |
| `LargoMaximoDeArchivo` | 10 MiB | Tamaño máximo de una imagen |
| `TransferenciaPorPartes.LargoDeParte` | 32 KiB | Tamaño de cada parte de un archivo |

En la entrega 1 estos valores estaban en el App.config. Ahora son constantes del protocolo: no
dependen del entorno, y cliente y servidor tienen que usar los mismos.

## Campos dentro de DATOS

- Los campos van separados por `#`. Ningún campo puede contener `#`: `Mensajes.Unir` lo rechaza antes
  de enviar.
- **Listas:** primero la cantidad de elementos y después los campos de cada elemento, en orden. Una
  lista vacía es `0`. Si la cantidad no coincide con los campos, el mensaje se rechaza como mal
  formado.
- **Decimales y enteros:** cultura invariante, con `.` como separador decimal.
- **Fechas:** `dd-MM-yyyy`.
- **Fecha y hora:** `yyyyMMddHHmmss` en **UTC**. El cliente las convierte a su hora local recién al
  mostrarlas.
- **Ids de reserva:** Guid en formato `D` (`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`).

No se usa ningún serializador (ni BinaryFormatter ni JSON): cada mensaje se arma y se lee a mano en
`Mensajes`.

## Respuestas: estado + contenido

El DATOS de toda respuesta empieza con dos caracteres de estado:

| Estado | Significado | Qué pasa con la conexión |
|---|---|---|
| `OK` | La operación salió bien; sigue el resultado | Sigue |
| `ER` | Pedido rechazado: regla de negocio, datos inválidos, falla de disco o base no disponible; sigue el mensaje para el usuario | Sigue: el cliente muestra el mensaje y vuelve al menú |
| `IN` | Error interno inesperado (un bug) | El servidor la cierra después de enviarla |

## Comandos

Todos los comandos, salvo 01 y 02, exigen haber iniciado sesión en esa conexión. Si no, la
respuesta es `ER`.

| CMD | Operación | DATOS de la solicitud | Contenido del `OK` |
|---|---|---|---|
| 01 | Registrar usuario | `usuario#clave` | Mensaje de confirmación |
| 02 | Iniciar sesión | `usuario#clave` | Mensaje de bienvenida |
| 03 | Cerrar sesión | vacío | Mensaje de confirmación |
| 10 | Alta de modelo | `nombre#descripcion#precio#fechaEntrega#stock` | Mensaje de confirmación |
| 11 | Modificar modelo | `nombre#descripcion#precio#fechaEntrega#stock` | Mensaje de confirmación |
| 12 | Baja de modelo | `nombre` | Mensaje de confirmación |
| 13 | Buscar modelos | `texto#precioMin#precioMax#fechaEntregaMax#minDisponibles` (vacío = sin filtro) | Lista: `nombre#precio#fechaEntrega#disponibles` |
| 14 | Consultar modelo | `nombre` | `nombre#descripcion#precio#fechaEntrega#disponibles#reservadas#archivoImagen` |
| 15 | Subir imagen | `nombreModelo#nombreArchivo#largo`, y después las partes | Ver flujo abajo |
| 16 | Descargar imagen | `nombreModelo` | `nombreArchivo#largo`, y después las partes |
| 20 | Solicitar reserva | `nombreModelo` | `Reserva <id> creada, pendiente de aprobación` |
| 21 | Cancelar reserva | `idReserva` | Mensaje de confirmación |
| 22 | Mis reservas | vacío | Lista: `id#modelo#estado#creacion#ultimaActualizacion` |
| 30 | Historial | vacío | Lista: `tipo#detalle#fechaHora` |

Los estados de reserva viajan como texto: `Pendiente`, `Confirmada`, `Rechazada` o `Cancelada`.

## Transferencia de imágenes por partes (CMD 15 y 16)

En la entrega 1, la imagen viajaba entera dentro de DATOS: se cargaba completa en memoria y obligaba
a un `maxPayloadBytes` de más de 10 MB. Por la devolución, ahora el archivo **no viaja en DATOS**. La
trama solo anuncia el nombre y el largo. Los bytes del archivo siguen crudos por el stream, en partes
de 32 KiB, y cada lado escribe o lee una parte por vez directo del archivo. En memoria nunca hay más
de una parte, sea cual sea el tamaño de la imagen.

### CMD 15: subida, en dos pasos

```
Cliente                                          Servidor
   | CMD 15  "modelo#foto.png#123456"  --------->  |  valida nombre, extensión y largo
   | <---------  CMD 15  OK "Listo para recibir"   |
   | partes crudas (123456 bytes en total) ------> |  escribe en un .tmp mientras llegan
   |                                               |  mueve el .tmp y asigna la imagen al modelo
   | <---------  CMD 15  OK / ER (respuesta final) |
```

- Si el anuncio se rechaza (extensión no permitida, archivo vacío o de más de 10 MiB), el servidor
  responde `ER` en el primer paso y el cliente no envía ningún byte del archivo.
- Si el disco falla a mitad de la recepción, el servidor igual consume las partes restantes. Así el
  stream queda sincronizado, responde `ER` y la conexión sigue.
- Si el modelo no existe, no es del usuario o la base no registra el cambio, la imagen recién
  recibida se borra (no queda huérfana) y la respuesta final es `ER`.

### CMD 16: descarga

```
Cliente                                          Servidor
   | CMD 16  "modelo"  ------------------------->  |
   | <---------  CMD 16  OK "Fiat Uno.png#123456"  |
   | <---------  partes crudas (123456 bytes)      |
```

- El cliente valida el nombre anunciado (extensión permitida, sin rutas, nombres reservados de
  Windows), recibe en un `.tmp` y recién al completar lo mueve a la carpeta de descargas.
- Si el modelo no tiene imagen, la respuesta es `ER` y no se envían partes.

## Manejo de errores

| Situación | Dónde se detecta | Resultado |
|---|---|---|
| DATOS mal formado (campos, números, fechas, UTF-8) | `Mensajes` / `TextoUtf8` → `ProtocoloException` | `ER`, la sesión sigue: la trama ya se consumió entera |
| Regla de negocio violada | Dominio → `DominioException` | `ER`, la sesión sigue |
| Falla del disco del servidor | `ImagenesEnDisco` → `AlmacenDeImagenesException` | `ER` con un mensaje para el usuario; el detalle técnico queda en la consola del servidor |
| MySQL no disponible o rechaza la operación | Repositorios → `DbException` | `ER` "No se pudo acceder a los datos del sistema…", la sesión sigue |
| Respuesta mayor a 1 MiB (por ejemplo, una búsqueda enorme) | `CodecDeTramas.EscribirAsync`, antes de escribir | `ER` "refine la búsqueda", la sesión sigue |
| Excepción inesperada | `SesionDeCliente` | `IN` y cierre de la conexión |
| El otro lado cierra entre dos tramas | Primer byte del CMD | Fin normal de la sesión (`ConexionCerradaException`) |
| El otro lado cierra a mitad de una trama o de un archivo | Cualquier otro campo, o las partes | `TramaTruncadaException` (es un corte de conexión, no un dato inválido) y cierre de la sesión |
| Encabezado inválido: CMD no numérico, LARGO negativo o mayor a 1 MiB | `CodecDeTramas.LeerAsync` | Cierre de la conexión: ya no se puede saber dónde empieza la trama siguiente |
