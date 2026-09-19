using Common;
using Server.Commands;

namespace Server.Connections;

// Objeto parámetro: lo que todas las sesiones comparten y el servidor crea una sola vez.
internal sealed record RecursosDeSesion(
    CodecDeTramas Codec,
    CanalDeArchivos CanalDeArchivos,
    DespachadorDeComandos Despachador,
    long MaxFileBytes);
