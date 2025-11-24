using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GreenCoinMovil.Models;
using GreenCoinMovil.UsuarioDTO;
using Microsoft.Maui.Storage; 

namespace GreenCoinMovil.Models
{
    public class AuthService
    {
        // ✅ CORRECCIÓN: Usar la IP correcta para Android/emulador
        private readonly HttpClient _httpClient;

        // Para Android Emulator usa: http://10.0.2.2:8080/api/
        // Para dispositivo físico usa la IP de tu PC: http://10.2.14.235:8080/api/
        private const string BaseUrl = "http://192.168.3.39:8080/api"; // ✅ CAMBIA ESTO

        private const string LoginEndpoint = "auth/login";
        private const string RegisterEndpoint = "registro";

        // CLAVES PARA SECURESTORAGE
        private const string UserDataKey = "currentUserData";
        private const string AuthTokenKey = "authToken";

        // Propiedad estática para el usuario actual
        public static UsuarioDto CurrentUsuario { get; private set; }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AuthService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            // ✅ Inicializar sesión si existe
            _ = InitializeSessionAsync();
        }

        public async Task<bool> EsAdministradorAsync()
        {
            try
            {
                var email = await SecureStorage.GetAsync("user_email");
                return email == "admin@gmail.com";
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> ObtenerEmailUsuarioAsync()
        {
            return await SecureStorage.GetAsync("user_email");
        }

        public async Task<AuthResponse> AttemptLoginAsync(LoginRequest request)
        {
            try
            {
                var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"🔐 Enviando login a: {BaseUrl}{LoginEndpoint}");
                System.Diagnostics.Debug.WriteLine($"📧 Email: {request.Email}");

                HttpResponseMessage response = await _httpClient.PostAsync(LoginEndpoint, content);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"📥 Respuesta HTTP: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📦 JSON recibido: {jsonResponse}");

                var authResult = JsonSerializer.Deserialize<AuthResponse>(jsonResponse, _jsonOptions);

                if (response.IsSuccessStatusCode && authResult.Success)
                {
                    // ✅ GUARDAR SESIÓN COMPLETA
                    await SaveSessionAsync(authResult.Usuario, authResult.Token);

                    System.Diagnostics.Debug.WriteLine($"✅ LOGIN EXITOSO - Usuario: {authResult.Usuario?.Email}");
                    System.Diagnostics.Debug.WriteLine($"✅ Token guardado: {!string.IsNullOrEmpty(authResult.Token)}");

                    return authResult;
                }

                return new AuthResponse
                {
                    Success = false,
                    Error = authResult?.Error ?? $"Error {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR DE CONEXIÓN: {ex.Message}");
                return new AuthResponse
                {
                    Success = false,
                    Error = $"Fallo al conectar con el servidor: {ex.Message}"
                };
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string name, string email, string password, string telefono, string direccion)
        {
            try
            {
                var request = new RegisterRequest
                {
                    Nombre = name,
                    Email = email,
                    Password = password,
                    Telefono = telefono,
                    Direccion = direccion,
                    AvatarId = "12345678-abcd-efgh-ijkl-0123456789ab"
                };

                var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"📤 Enviando registro a: {BaseUrl}{RegisterEndpoint}");

                var response = await _httpClient.PostAsync(RegisterEndpoint, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"📥 Respuesta HTTP: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(responseJson, _jsonOptions);

                    if (registerResponse != null && registerResponse.Success)
                    {
                        return (true, registerResponse.Message ?? "Usuario registrado exitosamente");
                    }
                    else
                    {
                        return (false, registerResponse?.Error ?? "Error desconocido del servidor");
                    }
                }
                else
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<RegisterResponse>(responseJson, _jsonOptions);
                        return (false, errorResponse?.Error ?? $"Error HTTP: {response.StatusCode}");
                    }
                    catch
                    {
                        return (false, $"Error HTTP: {response.StatusCode} - {responseJson}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión: {ex.Message}");
                return (false, "Error de conexión con el servidor. Verifica que la API esté ejecutándose.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado: {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
        }

        public void Logout()
        {
            SecureStorage.Remove(UserDataKey);
            SecureStorage.Remove(AuthTokenKey);
            CurrentUsuario = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;

            System.Diagnostics.Debug.WriteLine("🚪 Sesión cerrada");
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("test");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Test conexión fallido: {ex.Message}");
                return false;
            }
        }

        // ✅ MÉTODO CLAVE: Inicializar sesión al abrir la app
        public async Task<bool> InitializeSessionAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync(AuthTokenKey);
                var userDataJson = await SecureStorage.GetAsync(UserDataKey);

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userDataJson))
                {
                    CurrentUsuario = JsonSerializer.Deserialize<UsuarioDto>(userDataJson, _jsonOptions);
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    System.Diagnostics.Debug.WriteLine($"🔄 Sesión inicializada: {CurrentUsuario?.Email}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al inicializar sesión: {ex.Message}");
            }

            return false;
        }

        // ✅ MÉTODO CLAVE: Guardar sesión completa
        private async Task SaveSessionAsync(UsuarioDto usuario, string token)
        {
            try
            {
                // Guardar en SecureStorage
                await SecureStorage.SetAsync("auth_token", token);
                await SecureStorage.SetAsync("user_email", usuario?.Email ?? string.Empty);
                await SecureStorage.SetAsync("user_id", usuario?.Id.ToString() ?? string.Empty);

                // También guardar en Preferences como backup
                Preferences.Set("auth_token", token);
                Preferences.Set("user_email", usuario?.Email ?? string.Empty);

                System.Diagnostics.Debug.WriteLine($"💾 Sesión guardada - Token: {!string.IsNullOrEmpty(token)}, Email: {usuario?.Email}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error guardando sesión: {ex.Message}");
            }
        }
    }
}
