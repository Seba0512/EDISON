namespace Dominio.Fachada;

/// <summary>
/// Resolucion diferida de las solicitudes vencidas (CR5). Va separada de
/// <see cref="IGestionDeReservas"/> porque el proceso periodico de aprobaciones no necesita conocer
/// las operaciones interactivas de solicitud, cancelacion y consulta (ISP).
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
