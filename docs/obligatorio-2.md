# Obligatorio 2: Upgrade al sistema Edison

**Programación de Redes**, Ingeniería en Sistemas, Universidad ORT Uruguay
Grupo M6CD, octubre de 2026

**Repositorio:** https://github.com/ArqSis-PDR-2026-2/M6CD-ID-274754-328344-302420

| Integrante | Número de estudiante |
|---|---|
| Sebastián Luciardi | 274754 |
| Juan *(completar apellido)* | 328344 |
| Andrés Bustamante | 302420 |

---

## Índice

1. Alcance de la entrega
2. Correcciones de la entrega 1 (requerimiento 4)
3. Modificaciones al código y su motivación
4. Docker
5. Supuestos
6. Errores conocidos
7. Declaración de uso de IA

Anexos en el repositorio:

- `README.md`: guía de ejecución y variables de entorno.
- `docs/protocolo.md`: especificación del protocolo.
- `docs/uml/`: diagramas en PlantUML y PNG.

## 1. Alcance de la entrega

Los requerimientos funcionales son los de la entrega 1 (CR1 a CR10 y SR1 a SR4), más el
requerimiento 5. Sobre ese sistema, esta entrega incorpora:

| Punto de la letra | Estado | Dónde |
|---|---|---|
| 1. Migrar de Threads a Tasks con async/await | Completo: el código no usa la clase `Thread`, y todo el I/O usa las APIs `*Async` reales | Sección 3.1 |
| 2. Dockerizar servidor y cliente; configuración apta para contenedores; acceso desde el host a imágenes y comprobantes | Configuración por variables de entorno: completa. Dockerfiles y guía: sección 4 | Secciones 3.3 y 4 |
| 3. Persistir en MySQL (en Docker) todo salvo las imágenes | Completo: usuarios, modelos, reservas e historial | Sección 3.4 |
| 4. Mejoras pedidas en la devolución de la entrega 1 | Completo | Sección 2 |
| 5. Cancelar el mecanismo de una reserva pendiente (CancellationToken) y generar un comprobante por cada reserva aprobada | Completo | Sección 3.5 |

Las restricciones de la evaluación siguen cumpliéndose: no se usan `Thread` ni BinaryFormatter, ni
ningún parser existente (JSON u otro) para serializar. Toda la información viaja por streams, con el
protocolo propio descrito en `docs/protocolo.md`.

## 2. Correcciones de la entrega 1 (requerimiento 4)

### 2.1 Observaciones de la devolución

Se resolvieron en la rama `fix/feedback-primera-entrega`, integrada a `develop` en el merge `8d55c32`.

| Observación de los docentes | Cómo se resolvió | Commits |
|---|---|---|
| Uso de `while (true)` en los bucles | El servidor (aceptador, sesión, consola) y el cliente (menús y lecturas) cortan sus bucles con una variable o una condición explícita. Ningún bucle depende de un `break` dentro de un `while (true)` | `d6dd64b`, `68289d8` |
| El App.config tenía valores que no dependen del entorno | Solo quedaron como configuración IP, puerto y carpetas. Los límites del protocolo pasaron a `Common/Limites`, porque cliente y servidor tienen que coincidir. El backlog y la espera máxima del cierre pasaron a ser constantes con nombre. En esta entrega, además, el App.config se reemplazó por variables de entorno (sección 3.3) | `709ab25`, `e768f8c`, `95f6143` |
| Las imágenes se cargaban enteras en memoria y viajaban dentro de una sola trama | `Common/TransferenciaPorPartes`: la trama anuncia nombre y largo, y el archivo sigue crudo por el stream en partes de 32 KiB, escritas o leídas directo del disco. CMD 15 pasó a dos pasos (anuncio → OK → partes → respuesta final) y CMD 16 responde con el anuncio seguido de las partes. Como DATOS ya no transporta archivos, su máximo bajó de ~10 MB a 1 MiB | `fbdc2f8` |

### 2.2 Problemas detectados en la revisión de esta entrega

Al revisar el sistema migrado contra la letra y probarlo de punta a punta con MySQL en Docker,
aparecieron estos problemas. Cada uno se corrigió en su propia rama:

| Problema | Corrección | Commits |
|---|---|---|
| Al pasar a MySQL volvió a aparecer un bucle sin condición de corte (`for (int intento = 1; ; intento++)`, en los reintentos de conexión), justo lo que había marcado la devolución | El bucle sigue mientras la base no responda. El último intento deja pasar la excepción, así que siempre termina | `24f6adf` |
| Un error de MySQL durante una solicitud terminaba en `IN`: el servidor cortaba la conexión y el cliente se cerraba | `EjecutorDeComandos` atrapa `DbException`, responde `ER` y la sesión sigue. Si la base no registra una imagen subida, el archivo recién recibido se borra | `d7dd926`, `7c8ed2d` |
| Al cancelar, la espera de la reserva se cortaba antes de guardar el estado: si el UPDATE fallaba, la reserva quedaba Pendiente y sin nada que la resolviera | La espera se cancela recién después de persistir la cancelación | `5f77130` |
| El cierre del servidor no esperaba a las reservas que se estaban confirmando, y podía cortar un comprobante a medias | `ResolutorDeReservas` sigue sus Tasks y el cierre las espera, con un tope de 3 s | `4f2cca1`, `5017b87` |
| El cliente usaba solo la primera dirección del host: con `localhost` en Windows elegía `::1` y el servidor, que escucha en IPv4, rechazaba la conexión | Socket dual y `ConnectAsync(host, puerto)`, que prueba todas las direcciones. También se ofrece reintentar cuando el nombre todavía no resuelve (contenedor del servidor aún no creado) | `e59949a`, `adc688b` |
| El log del servidor mostraba `Task -` en todas las líneas, porque `Task.CurrentId` es nulo en métodos async, y no se distinguía a cada cliente | Cada línea identifica la conexión y el usuario: `[Cliente 2 \| ana]` | `e7f1435` |
| Un nombre de usuario como `a/b` se podía registrar, pero sus reservas aprobadas nunca generaban comprobante | El registro solo acepta letras, números, espacios, `.`, `-` y `_`, empezando y terminando con letra o número (validado en dominio y cliente) | `1dc8401`, `90005b1`, `728a2ea` |
| El historial se guardaba sin tildes, y la base no tomaba la colación del script cuando la creaba la imagen de MySQL | Textos con tildes y `ALTER DATABASE` en `init.sql` | `18941e1`, `9c9e0f7` |

## 3. Modificaciones al código y su motivación

Cada modificación se presenta como problema → alternativas → decisión → beneficio frente al código
de la entrega 1. Los diagramas están en `docs/uml/`:

- componentes: `componentes.png`;
- clases del dominio: `clases-dominio.png`;
- aceptación y cierre: `secuencia-aceptacion.png`;
- reserva: `secuencia-reserva.png`.

### 3.1 De Threads a Tasks con async/await

**Antes.**

- El servidor creaba un `Thread` aceptador, que se bloqueaba en `Accept()`, y un `Thread` por cada
  cliente, que se bloqueaba en `Read()` sobre su socket.
- Cada reserva tenía su propio `Thread`, que dormía el plazo con `Thread.Sleep`.
- El cierre usaba un `volatile bool`, cerraba los sockets para despertar a los hilos bloqueados y los
  esperaba con `Join` y un timeout.
- Las conexiones activas estaban en una `List` protegida con `lock`.

**Problema.**

- Cada `Thread` reserva su propia pila y pasa casi todo el tiempo bloqueado esperando la red, así que
  el modelo no escala con la cantidad de clientes.
- `Thread.Sleep` no se puede cancelar.
- La letra prohíbe la clase `Thread`.

**Alternativas.**

1. Envolver el código sincrónico en `Task.Run`. Desaparece la palabra `Thread`, pero cada espera
   sigue bloqueando un hilo, ahora del pool, lo que es peor porque lo comparte todo el proceso.
2. Usar `async`/`await` con las APIs asíncronas reales de .NET.

**Decisión: la alternativa 2.**

- **Red:** `AcceptAsync`, `ReadAsync` / `ReadExactlyAsync`, `WriteAsync` y `ConnectAsync`.
- **Disco:** `FileStream` con `useAsync: true`.
- **Base:** MySqlConnector, con API asíncrona real.

Dos reglas para toda la migración:

- nunca bloquear sobre una Task (no hay `.Result`, `.Wait()` ni `GetAwaiter().GetResult()`);
- no usar `Task.Run` para disfrazar código sincrónico. El único `Task.Run` es el que aísla
  `Console.ReadLine` en la consola del servidor, porque no tiene versión asíncrona.

| Pieza | Entrega 1 | Entrega 2 |
|---|---|---|
| Aceptador | `Thread` + `Accept()` | Task `AceptarConexionesAsync` + `await AcceptAsync(token)`. El método async devuelve su Task al primer `await`, sin `Task.Run` |
| Atención de cada cliente | `Thread` por cliente | Task `AtenderAsync` por cliente |
| Conexiones activas | `List` + `lock` | `ConcurrentDictionary<int, ConexionDeCliente>` |
| Señal de ejecución | `volatile bool` | `CancellationTokenSource` |
| Espera del cierre | `Join(timeout)` por hilo | `await Task.WhenAll(...).WaitAsync(5 s)` |
| Espera de una reserva | `Thread.Sleep(plazo)` | `await Task.Delay(plazo, token)`, cancelable (sección 3.5) |
| Códec y transferencia de archivos | `Read`/`Write` bloqueantes | `LeerAsync`/`EscribirAsync`, `EnviarAsync`/`RecibirAsync` con `CancellationToken` |

**Beneficio.**

- Mientras una conexión espera bytes no ocupa ningún hilo: el `await` devuelve el hilo al pool, y un
  cliente lento no frena a los demás ni consume memoria de pila.
- La cancelación es cooperativa, con `CancellationToken`, en vez de cerrar sockets para provocar
  excepciones.
- El cierre es más simple y tiene un tope de tiempo explícito.
- Se mantuvieron la distinción entre `ConexionCerradaException` (el otro lado cerró entre dos tramas)
  y `TramaTruncadaException` (cerró a mitad de una), y la lectura exacta de cada campo.

**Cierre en Docker.** `docker stop` envía SIGTERM, y el servidor lo atiende con
`PosixSignalRegistration` (también Ctrl+C / SIGINT). La orden `salir` se lee solo si la consola es
interactiva (`!Console.IsInputRedirected`). Sin esa condición, en un contenedor sin `-i`,
`ReadLine()` devuelve `null` enseguida y el servidor se cerraría al arrancar.

### 3.2 De `lock` a un candado asíncrono (`CandadoGlobal`)

**Antes.** Un `lock` global (`EstadoEnMemoria.Candado`). Cada operación lo tomaba una vez y adentro
hacía leer → validar → modificar de forma atómica: por ejemplo, contar las reservas confirmadas y
confirmar una más sin pasarse del stock.

**Problema.** Ahora los datos están en MySQL y su acceso es asíncrono. Un bloque `lock` no puede
contener un `await`: el compilador lo prohíbe, porque el código después del `await` puede seguir en
otro hilo, y un monitor tiene que liberarlo el mismo hilo que lo tomó.

**Alternativas.**

1. `SemaphoreSlim(1, 1)` en memoria, con `await WaitAsync()` y `Release()` en un `finally`. Mantiene
   la semántica que se defendió en la entrega 1.
2. Sin candado en memoria: transacciones con `SELECT … FOR UPDATE` o un nivel de aislamiento
   serializable, y que MySQL coordine la concurrencia.
3. Un candado por modelo.

**Decisión: la alternativa 1** (`Servidor/Dominio/Servicios/CandadoGlobal.cs`).

- Es el cambio mínimo que preserva la corrección de la entrega 1: cada servicio sigue tomando el
  candado una sola vez, y las reglas de stock y estado se validan con los datos que se van a
  modificar.
- La alternativa 2 permitiría varias instancias del servidor, pero agrega complejidad (bloqueos de
  filas, deadlocks de la base, reintentos) sin beneficio para el alcance del obligatorio.
- La alternativa 3 complica el razonamiento cuando una operación toca varios modelos.

**Supuesto:** hay una sola instancia del servidor (sección 5).

**Costo:** las operaciones que toman el candado se ejecutan de a una. Es aceptable para la carga del
obligatorio, y como el candado se espera con `await`, mientras tanto no se bloquea ningún hilo.
