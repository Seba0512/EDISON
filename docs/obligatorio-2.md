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

### 3.3 De App.config a variables de entorno

**Antes.** Cada aplicación leía su `App.config` con `ConfigurationManager`, y el cliente solo
aceptaba una IP (`IPAddress.TryParse`).

**Problema.**

- En un contenedor, el App.config queda adentro de la imagen: cambiar un valor obliga a reconstruirla
  o a montar un archivo.
- Docker entrega la configuración con `-e` / `environment:`.
- El cliente tiene que llegar al servidor por el *nombre* de su contenedor, no por una IP fija.

**Alternativas.**

1. `appsettings.json` con Microsoft.Extensions.Configuration. Igual habría que montar el archivo o
   sobrescribirlo con variables, y suma una dependencia.
2. Un archivo montado como volumen.
3. Variables de entorno.

**Decisión: la alternativa 3** (`Common/ConfiguracionDeLaAplicacion.cs`).

- Todo valor tiene un default razonable, así las aplicaciones corren sin Docker sin configurar nada.
- Un valor mal formado lanza `ConfiguracionInvalidaException` y el proceso termina con un mensaje
  claro.
- `LeerHost` devuelve el texto tal cual, IP o nombre: el cliente lo resuelve al conectar.
- Se eliminaron los dos App.config y el paquete System.Configuration.ConfigurationManager.
- Las variables están en el `README.md`.

**El plazo de resolución de reservas** (`PLAZO_RESERVA_SEGUNDOS`, 30 s por defecto) se volvió
configurable a propósito: es un parámetro de operación, no una regla del protocolo. Permite probar y
demostrar el requerimiento 5 con plazos cortos sin recompilar. Los límites del protocolo, en cambio,
siguen siendo constantes de `Common`, por la devolución de la entrega 1.

**Beneficio.** La misma imagen sirve en cualquier entorno, la configuración queda visible en el
comando o en el compose, y el cliente puede usar el DNS interno de Docker.

### 3.4 De memoria a MySQL

**Antes.** El estado vivía en memoria (`EstadoEnMemoria`, con colecciones protegidas por el `lock`
global). Al reiniciar el servidor se perdía todo, y por eso también se borraban las imágenes al
arrancar y al apagar.

**Alternativas de acceso a datos.**

1. Entity Framework Core: un ORM con migraciones, que esconde el SQL.
2. ADO.NET con el driver oficial MySql.Data, cuyos métodos async fueron por mucho tiempo sincrónicos
   por dentro.
3. ADO.NET con MySqlConnector: async real y SQL explícito.

**Decisión: la alternativa 3**, con estas reglas:

- **Puertos en el dominio** (DIP / Variaciones Protegidas). `IRepositorioDeUsuarios`, `…DeModelos`,
  `…DeReservas` e `…DeActividades` están en `Servidor/Dominio/Repositorios/`. Las implementaciones
  `MysqlRepositorioDe*` están en `Servidor/Persistencia/`. `Program` hace el wiring, y el dominio no
  referencia MySQL.
- **SQL siempre con parámetros** (`@nombre`), nunca concatenando datos, contra la inyección SQL.
- **Transacciones** (`MySqlTransaction`) cuando una operación toca dos tablas: un alta de modelo o una
  reserva se guarda junto con su actividad del historial, o no se guarda ninguna de las dos.
- **Una conexión por operación**, abierta por `FabricaDeConexionesMysql`. El driver mantiene un pool,
  y así nunca se comparte una conexión entre clientes concurrentes.
- **Rehidratación** con fábricas `internal` (`Usuario.Restaurar`, `Reserva.Restaurar`, …). Las
  validaciones de alta siguen en los constructores públicos.
- **Conteos** con `SELECT COUNT(*)`. Por ejemplo, las reservas confirmadas de un modelo se cuentan
  para validar el stock.
- **Baja de un modelo:** `reservas.modelo_id` queda en `NULL` (`ON DELETE SET NULL`) y la reserva
  conserva `nombre_modelo`, así CR9 sigue mostrando el modelo después de la baja.

**Esquema** (`db/init.sql`):

- La base y las tablas usan `utf8mb4`, para guardar bien tildes y eñes.
- Los nombres de usuario y de modelo usan la colación `utf8mb4_0900_as_ci`: no distinguen mayúsculas
  (como la comparación `OrdinalIgnoreCase` del dominio) pero sí tildes, así "Peña" y "Pena" son
  distintos.
- El script se monta en `/docker-entrypoint-initdb.d/` y MySQL lo ejecuta una sola vez, con el
  volumen vacío.

**Fechas en UTC.** El dominio usa `DateTime.UtcNow` y la base guarda `DATETIME` en UTC. El protocolo
transmite las fechas y horas en UTC, y solo el cliente (y el comprobante) las convierte a hora local
para mostrarlas. Así, el plazo de una reserva se calcula igual aunque el contenedor del servidor
corra en otra zona horaria, y un cliente en otra zona ve su propia hora.

**Arranque.**

- El servidor reintenta la conexión con MySQL hasta 15 veces, con `Task.Delay` de 2 s, porque el
  contenedor de la base tarda en estar listo.
- Las imágenes ya no se borran al arrancar ni al apagar: pertenecen a modelos que ahora persisten.
  SR3 sigue cumplido, porque la imagen se borra con la baja del modelo.

**Fallas de la base durante la operación.** Responden `ER` sin cortar la sesión (sección 2.2).

**Beneficio.** Los datos sobreviven a reinicios y a la recreación de contenedores. El dominio sigue
sin depender de la tecnología de persistencia, y cada operación compuesta es atómica también en la
base.

### 3.5 Reservas cancelables y comprobantes (requerimiento 5)

**Antes.** Cada reserva lanzaba un `Thread` que dormía el plazo y después la resolvía. Cancelar una
reserva pendiente solo cambiaba su estado: el hilo seguía vivo hasta despertar y recién ahí veía que
ya no tenía nada que hacer.

**Decisión.** `Servidor/Dominio/Servicios/ResolutorDeReservas.cs` es una fabricación pura que lleva
la cuenta de las esperas:

- Cada reserva pendiente tiene su propio `CancellationTokenSource`, en un
  `ConcurrentDictionary<Guid, CancellationTokenSource>`, vinculado
  (`CreateLinkedTokenSource`) al token de apagado del servidor.
- `Programar(id, demora)` lanza `await Task.Delay(demora, token)` y después resuelve la reserva:
  Confirmada si hay stock, Rechazada si no. La espera no ocupa ningún hilo.
- `Cancelar(id)` hace `Cancel()` sobre el CTS de esa reserva: el `Task.Delay` termina enseguida con
  `OperationCanceledException`. Así se cancela *el mecanismo*, no solo el estado.
- Por cualquier camino, al terminar, el CTS sale del diccionario y se dispone (`finally`): las
  reservas resueltas no quedan en memoria.
- La `OperationCanceledException` tiene dos causas y se tratan distinto:
  - **Cancelación del usuario:** es el final esperado. `ServicioDeReservas` ya guardó el estado
    Cancelada, y la espera se cancela recién después de guardarlo.
  - **Apagado del servidor:** la reserva no se toca y queda Pendiente en la base. Al arrancar,
    `ReprogramarPendientesAsync` la vuelve a programar con la demora restante,
    `max(0, creación + plazo − ahora)`, en UTC. Con persistencia, un reinicio no puede dejar
    reservas pendientes para siempre. Esto se probó matando el servidor con una reserva pendiente:
    al volver, se confirmó a los 20 s exactos de creada.
- **Carrera entre cancelar y resolver:** las dos operaciones toman el `CandadoGlobal`. La resolución
  relee la reserva dentro del candado y, si ya no está Pendiente, no hace nada.
- **Cierre:** el resolutor sigue sus Tasks y el cierre espera, con un tope de 3 s, a las reservas que
  se estaban confirmando.

**Comprobante** (`Servidor/ComprobantesEnDisco.cs`, detrás del puerto `IGeneradorDeComprobantes`):

- Cada reserva confirmada genera `DIRECTORIO_COMPROBANTES/<usuario>/<idReserva>.txt`, con id,
  usuario, modelo, precio y fechas de solicitud y de confirmación, en hora local y con su zona.
- El texto se arma a mano: no pasa por ningún serializador.
- Se escribe en un `.tmp` y después se mueve, para no dejar un comprobante a medias.
- El nombre de usuario se usa como carpeta, con dos barreras:
  - el registro solo acepta nombres válidos como carpeta, y `ComprobantesEnDisco` los vuelve a
    validar (sin separadores, sin rutas absolutas, sin `.` ni `..`);
  - se verifica que la ruta final (`Path.GetFullPath`) quede dentro de `DIRECTORIO_COMPROBANTES`.
- Se genera después de confirmar en la base y fuera del candado, para no retener a los demás clientes
  mientras se escribe el disco. Si falla, se registra en la consola y la confirmación no se revierte
  (sección 5).

**Beneficio frente a la entrega 1.**

- Una cancelación libera de inmediato el recurso de la espera.
- El apagado no pierde reservas.
- Cada reserva aprobada deja un comprobante que se puede ver desde el host (sección 4).

## 4. Docker

> **Pendiente:** esta sección la completa quien implementa los Dockerfile y la guía (fase 5).
> Tiene que cubrir lo que pide la letra:

- `Servidor/Dockerfile` y `Cliente/Dockerfile` de una sola etapa (sin multi-stage builds), con la
  raíz del repositorio como contexto de build, porque los dos dependen de `Common/`.
- MySQL 8.4 en su propio contenedor, con `db/init.sql` montado en `/docker-entrypoint-initdb.d/` y
  un volumen nombrado para los datos.
- Acceso desde el host a las imágenes de los modelos y a los comprobantes, con bind mounts de
  `/app/imagenes` y `/app/comprobantes` a carpetas del host. Hay que documentar cómo verlos.
- `TZ=America/Montevideo` en los contenedores del cliente y del servidor: las fechas viajan en UTC y
  cada uno las muestra en su hora local, que dentro de un contenedor es UTC salvo que se configure.
- El cliente usa `SERVIDOR_HOST=<nombre del contenedor del servidor>` y se ejecuta con `-it`.
- Guía paso a paso sin docker-compose, y opcionalmente con compose.

## 5. Supuestos

1. **Una sola instancia del servidor.** El `CandadoGlobal` vive en memoria y alcanza para serializar
   las operaciones sobre la base mientras haya un único proceso servidor (sección 3.2).
2. **Reservas pendientes durante un apagado.** No se cancelan ni se rechazan: quedan Pendiente y, al
   volver el servidor, se reprograman con el tiempo que les quedaba. Si el plazo venció mientras el
   servidor estaba apagado, se resuelven apenas arranca.
3. **El comprobante no revierte la confirmación.** Se genera después de confirmar la reserva en la
   base. Si escribirlo falla (por ejemplo, disco lleno), el error se registra en la consola del
   servidor y la reserva sigue Confirmada.
4. **Cancelar una reserva ya confirmada no borra su comprobante.** Como en la entrega 1, una reserva
   confirmada se puede cancelar. El comprobante documenta que la aprobación ocurrió, y el estado
   vigente se consulta en *Mis reservas* (CR9).
5. **Nombres de usuario.** Tienen entre 3 y 20 caracteres: letras (con tildes y ñ), números,
   espacios, `.`, `-` y `_`, y empiezan y terminan con letra o número. No distinguen mayúsculas pero
   sí tildes. La restricción de caracteres es nueva en esta entrega: el nombre también es la carpeta
   de los comprobantes del usuario.
6. **Plazo de resolución configurable** con `PLAZO_RESERVA_SEGUNDOS`, 30 s por defecto (sección 3.3).
7. **Cierre por consola.** La orden `salir` solo se acepta con una consola interactiva. En Docker, el
   servidor se cierra con `docker stop` (SIGTERM), como habilita la letra.
8. **Tope del cierre.** El cierre espera hasta 5 s al aceptador, 5 s a las conexiones de los clientes
   y 3 s a las reservas que se estaban confirmando. Normalmente todo termina en milisegundos, y los
   topes solo evitan que el cierre quede colgado. En el peor caso suman 13 s, más que los 10 s que
   `docker stop` espera por defecto antes de forzar la terminación: si hiciera falta, se amplía con
   `docker stop -t 15`.
9. **Precio.** Como en la entrega 1, el precio de un modelo se redondea a un valor entero.

## 6. Errores conocidos

1. **MySQL caído al vencer el plazo de una reserva.** La resolución falla, el error se registra en
   la consola y la reserva queda Pendiente hasta el próximo arranque del servidor, que la reprograma.
   No se reintenta mientras el servidor sigue corriendo.
2. **Terminación forzada entre la confirmación y el comprobante.** Si el proceso muere de golpe
   (SIGKILL o corte de energía) justo después de confirmar una reserva en la base, el comprobante
   puede no escribirse, y no se regenera.
3. **Subida de imagen a un modelo ajeno o inexistente.** El modelo y el permiso se validan cuando
   termina la recepción del archivo, no con el anuncio. Un usuario puede transferir hasta 10 MiB que
   después se descartan con un `ER`: el archivo se borra y no queda huérfano.
4. **Nombres reservados en Windows.** Si el servidor corre en Windows, un usuario llamado `CON`,
   `NUL`, `COM1`, etc. no obtiene comprobantes (se registra en la consola). En el contenedor Linux no
   ocurre.

## 7. Declaración de uso de IA

Según las pautas de la letra, para cada uso se indica (a) la herramienta y (b) el contexto. Todo el
contenido generado con IA fue revisado y verificado por el equipo, que es responsable de él.

### Andrés Bustamante

**Herramienta:** Claude Code (Anthropic), modelo Claude Opus 5.5, el 20 de octubre de 2026.

| Contexto de uso | Sobre qué parte | Verificación |
|---|---|---|
| Análisis | Revisión del repositorio contra la letra y el plan de trabajo, para detectar faltantes y errores en lo ya integrado | Se discutió cada hallazgo antes de corregirlo. Uno se descartó: invertir el orden de la baja de imagen rompía la garantía de SR3 |
| Asistencia de git | Resolución de los conflictos de un `git pull` entre los cambios locales y las fases 1 a 4 | Se verificó que el código resultante quedara idéntico al de `develop` |
| Implementación asistida | Las correcciones de la sección 2.2 | Cada cambio se compiló en Release sin advertencias y se probó contra el servidor real con MySQL en Docker: registro con ñ, reservas confirmadas, rechazadas y canceladas, comprobante, base caída con la sesión viva, reinicio con una reserva pendiente, nombres de usuario inválidos |
| Herramienta de prueba | Un cliente de prueba descartable que habla el protocolo usando `Common` (no forma parte de la entrega) | Se usó solo para ejecutar los escenarios anteriores |
| Borrador de documentación | `README.md`, `docs/protocolo.md`, los diagramas PlantUML de `docs/uml/` y este documento | Se revisaron contra el código; los diagramas se exportaron y se controlaron visualmente |

### Juan *(completar)*

*Herramientas y contexto de uso en las fases 1 a 4: configuración por entorno, migración async,
persistencia MySQL y cancelación de reservas con comprobantes.*

### Sebastián Luciardi *(completar)*

*Herramientas y contexto de uso en Docker (fase 5) y en la regresión (fase 6).*
