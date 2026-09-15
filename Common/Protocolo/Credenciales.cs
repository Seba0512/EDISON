namespace Common.Protocolo;

/// <summary>
/// Par nombre de usuario/clave que cruza la red (CR1). Es una estructura de datos del protocolo,
/// sin comportamiento: no debe confundirse con <c>Dominio.Credencial</c>, que sí lo tiene.
/// </summary>
public readonly record struct Credenciales(string NombreUsuario, string Clave);
