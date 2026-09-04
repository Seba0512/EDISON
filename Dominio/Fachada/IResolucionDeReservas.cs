namespace Dominio.Fachada;

/// <summary>
/// Resolucion diferida de las solicitudes vencidas (CR5). Va separada de
/// <see cref="IGestionDeReservas"/> porque no la usa ningun manejador de comando: la llama el thread
/// de la mesa de aprobaciones del servidor, y ese thread no tiene por que ver los metodos que
/// atienden a un cliente (ISP).
/// </summary>
public interface IResolucionDeReservas
{
    /// <summary>
    /// Resuelve en orden de creacion las solicitudes cuyo plazo ya vencio. El plazo y el reloj son
    /// estado del colaborador, asi que quien la llama no necesita saber ni la hora ni la espera
    /// configurada: solo golpea cada tanto.
    /// </summary>
    void ResolverVencidas();
}
