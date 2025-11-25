using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GreenCoinMovil.UsuarioDTO;

namespace GreenCoinMovil.Service
{
    public class UsuarioService
    {
        // ⚠️ Importante: Usa la IP de tu PC para el emulador de Android (10.0.2.2)
        private readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://10.2.14.179:8080") };
        private const string PerfilEndpoint = "/api/usuarios/perfil"; // Tu endpoint GET protegido

        // Opcional: Constructor para inyección de dependencias (si lo usas)
        public UsuarioService()
        {
            // El HttpClient se inicializa una vez con la dirección base.
        }

        /// <summary>
        /// Obtiene el perfil del usuario autenticado desde la API de Java.
        /// Esta petición requiere un token JWT en el encabezado.
        /// </summary>
        /// <returns>El objeto UsuarioDto con los datos del perfil.</returns>
        /// <exception cref="UnauthorizedAccessException">Se lanza si el token no existe o es inválido/expirado (401).</exception>
        public async Task<UsuarioDto> ObtenerMiPerfilAsync()
        {
            // 1. Obtener el token de autenticación del almacenamiento seguro
            var token = await SecureStorage.GetAsync("jwt_token");

            if (string.IsNullOrEmpty(token))
            {
                // Si no hay token, el usuario no está logueado.
                throw new UnauthorizedAccessException("Usuario no autenticado. Inicie sesión nuevamente.");
            }

            try
            {
                // 2. Configurar el encabezado de autorización Bearer
                // Esto es crucial para que Spring Security acepte la petición.
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // 3. Realizar la solicitud GET al endpoint protegido
                HttpResponseMessage response = await _httpClient.GetAsync(PerfilEndpoint);

                // 4. Limpiar el encabezado inmediatamente (Buena práctica)
                _httpClient.DefaultRequestHeaders.Authorization = null;

                if (response.IsSuccessStatusCode)
                {
                    // Código 200 OK: Deserializar el UsuarioDto
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var perfil = JsonSerializer.Deserialize<UsuarioDto>(jsonResponse, options);
                    return perfil;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) // 401
                {
                    // El token existe pero es inválido, expiró, o el backend lo rechazó.
                    throw new UnauthorizedAccessException("La sesión ha expirado. Por favor, vuelva a iniciar sesión.");
                }
                else
                {
                    // Manejo de otros errores HTTP (404, 500, etc.)
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al obtener perfil: {response.StatusCode}. Detalles: {errorContent}");
                    throw new Exception($"Error del servidor al obtener el perfil: {response.StatusCode}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Propagar el error 401 para que el ViewModel fuerce el Logout
                throw;
            }
            catch (Exception ex)
            {
                // Manejo de errores de red o cualquier otra excepción
                Console.WriteLine($"Excepción al consumir la API de perfil: {ex.Message}");
                throw new Exception("Fallo de conexión al servidor: " + ex.Message);
            }
        }
    }
}
