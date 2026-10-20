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
| Un error de MySQL durante una solicitud terminaba en `IN`: el servidor cortaba la conexión y el cliente se cerraba | `EjecutorDeComandos` atrapa `DbException`, responde `ER` y la sesión sigue. Si la base no registra una imagen subida, el archivo recién recibido se borra | `d7dd926`, `7c8ed2d` |
| Al cancelar, la espera de la reserva se cortaba antes de guardar el estado: si el UPDATE fallaba, la reserva quedaba Pendiente y sin nada que la resolviera | La espera se cancela recién después de persistir la cancelación | `5f77130` |
| El cierre del servidor no esperaba a las reservas que se estaban confirmando, y podía cortar un comprobante a medias | `ResolutorDeReservas` sigue sus Tasks y el cierre las espera, con un tope de 3 s | `4f2cca1`, `5017b87` |
| El cliente usaba solo la primera dirección del host: con `localhost` en Windows elegía `::1` y el servidor, que escucha en IPv4, rechazaba la conexión | Socket dual y `ConnectAsync(host, puerto)`, que prueba todas las direcciones. También se ofrece reintentar cuando el nombre todavía no resuelve (contenedor del servidor aún no creado) | `e59949a`, `adc688b` |
| El log del servidor mostraba `Task -` en todas las líneas, porque `Task.CurrentId` es nulo en métodos async, y no se distinguía a cada cliente | Cada línea identifica la conexión y el usuario: `[Cliente 2 \| ana]` | `e7f1435` |
| Un nombre de usuario como `a/b` se podía registrar, pero sus reservas aprobadas nunca generaban comprobante | El registro solo acepta letras, números, espacios, `.`, `-` y `_`, empezando y terminando con letra o número (validado en dominio y cliente) | `1dc8401`, `90005b1`, `728a2ea` |
| El historial se guardaba sin tildes, y la base no tomaba la colación del script cuando la creaba la imagen de MySQL | Textos con tildes y `ALTER DATABASE` en `init.sql` | `18941e1`, `9c9e0f7` |
