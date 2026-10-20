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
