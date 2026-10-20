-- Inicialización de la base de Edison. Se monta en /docker-entrypoint-initdb.d/ y MySQL la ejecuta
-- una sola vez, cuando el volumen de datos está vacío.
--
-- Todo en utf8mb4 para que las tildes y la ñ se guarden bien. Las fechas y horas son DATETIME en
-- UTC: la conversión a hora local se hace solo al mostrarlas, en el cliente.
--
-- Los nombres de usuario y de modelo usan utf8mb4_0900_as_ci: no distinguen mayúsculas (igual que
-- la comparación OrdinalIgnoreCase del dominio) pero sí tildes, así "Peña" y "Pena" son distintos.

CREATE DATABASE IF NOT EXISTS edison
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE edison;

CREATE TABLE IF NOT EXISTS usuarios (
    nombre  VARCHAR(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci NOT NULL,
    salt    VARBINARY(16) NOT NULL,
    resumen VARBINARY(32) NOT NULL,
    PRIMARY KEY (nombre)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS modelos (
    id            INT NOT NULL AUTO_INCREMENT,
    nombre        VARCHAR(60) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci NOT NULL,
    descripcion   VARCHAR(500) NOT NULL DEFAULT '',
    precio        DECIMAL(12, 2) NOT NULL,
    fecha_entrega DATE NOT NULL,
    stock         INT NOT NULL,
    imagen        VARCHAR(255) NOT NULL DEFAULT '',
    creador       VARCHAR(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_modelos_nombre (nombre),
    CONSTRAINT fk_modelos_creador FOREIGN KEY (creador) REFERENCES usuarios (nombre)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- nombre_modelo se guarda aparte para que el listado de reservas (CR9) siga mostrando el modelo
-- después de una baja: al borrarse el modelo, modelo_id queda en NULL.
CREATE TABLE IF NOT EXISTS reservas (
    id                   CHAR(36) NOT NULL,
    usuario              VARCHAR(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci NOT NULL,
    modelo_id            INT NULL,
    nombre_modelo        VARCHAR(60) NOT NULL,
    estado               VARCHAR(12) NOT NULL,
    fecha_creacion       DATETIME NOT NULL,
    ultima_actualizacion DATETIME NOT NULL,
    PRIMARY KEY (id),
    KEY ix_reservas_modelo_estado (modelo_id, estado),
    KEY ix_reservas_usuario (usuario),
    KEY ix_reservas_estado (estado),
    CONSTRAINT fk_reservas_usuario FOREIGN KEY (usuario) REFERENCES usuarios (nombre),
    CONSTRAINT fk_reservas_modelo FOREIGN KEY (modelo_id) REFERENCES modelos (id) ON DELETE SET NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS actividades (
    id         BIGINT NOT NULL AUTO_INCREMENT,
    usuario    VARCHAR(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci NOT NULL,
    tipo       VARCHAR(30) NOT NULL,
    detalle    VARCHAR(200) NOT NULL,
    fecha_hora DATETIME NOT NULL,
    PRIMARY KEY (id),
    KEY ix_actividades_usuario (usuario),
    CONSTRAINT fk_actividades_usuario FOREIGN KEY (usuario) REFERENCES usuarios (nombre)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
