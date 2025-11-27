using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GreenCoinMovil.DTO;
using GreenCoinMovil.Models;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace GreenCoinMovil.Models
{
    public class ApiService
    {

            private readonly HttpClient _httpClient;
            private static string BaseUrl => Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://10.0.2.2:8080"; // Variable de entorno con fallback

            // Método público para obtener la URL base actual (útil para debugging)
            public static string GetBaseUrl() => BaseUrl;

            public ApiService()
            {
                var handler = new HttpClientHandler();

                // SOLO PARA DESARROLLO - Deshabilitar SSL
#if DEBUG
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif

                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(BaseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };

                // Log para debugging
                Console.WriteLine($"🚀 ApiService inicializado con BaseUrl: {BaseUrl}");
            }

            // 🔐 MÉTODO CRÍTICO: Configurar el token después del login
            public void SetAuthToken(string token)
            {
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }

        public async Task<DashboardResponseDTO> ObtenerDatosDashboardAsync()
        {
            try
            {
                await EnsureTokenAsync();
                Console.WriteLine("📊 Pidiendo datos del Dashboard...");

                // Usamos la ruta correcta con /api para consistencia
                var response = await _httpClient.GetAsync("/api/dashboard/datos");

                var jsonString = await response.Content.ReadAsStringAsync();

                // 👇 ESTO TE SALVARÁ LA VIDA: Verás el JSON exacto en la consola
                
                Console.WriteLine($"📦 JSON DASHBOARD RECIBIDO:\n{jsonString}");

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<DashboardResponseDTO>(jsonString, options);
                }
                else
                {
                    Console.WriteLine($"❌ Error Servidor: {response.StatusCode}");
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"💥 ERROR DE FORMATO JSON: {jsonEx.Message}");
                Console.WriteLine($"   Ruta del error: {jsonEx.Path}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error General: {ex.Message}");
                return null;
            }
        }


        // 📱 ESCANEAR QR
        public async Task<Material> ScanQRAsync(string qrData)
            {
                try
                {
                    await EnsureTokenAsync();

                    var requestData = new { qrData };
                    var response = await _httpClient.PostAsJsonAsync("/api/materiales/crear-desde-qr", requestData);

                    Console.WriteLine($"📡 QR Scan Response: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Material>>();
                        Console.WriteLine($"✅ QR Scan Success: {result.Message}");
                        return result.Data;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ QR Scan Error: {errorContent}");
                        throw new Exception($"Error: {response.StatusCode} - {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 QR Scan Exception: {ex.Message}");
                    throw;
                }
            }

        // 📸 NUEVO MÉTODO: Registrar Reciclaje con FOTO (Multipart)
        public async Task<bool> RegistrarReciclajeConFotoAsync(long materialId, byte[] fotoBytes, double cantidad = 1.0)
        {
            try
            {
                Console.WriteLine("🚀 INICIANDO SUBIDA DE FOTO DESDE APISERVICE...");
                Console.WriteLine($"📦 Datos a enviar - MaterialId: {materialId}, Cantidad: {cantidad}");

                // 1. Obtener el token fresco del almacenamiento
                string token = null;

                #if MACCATALYST
                token = Preferences.Get("auth_token", string.Empty);
                #else
                token = await SecureStorage.GetAsync("auth_token");
                #endif

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ ERROR: No hay token guardado. El usuario debe loguearse.");
                    return false;
                }

                Console.WriteLine($"✅ Token obtenido: {token.Substring(0, Math.Min(20, token.Length))}...");

                // 2. Crear la petición
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{BaseUrl}/api/reciclajes/registrar-con-foto"
                );

                // 3. Agregar el token de autorización
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 4. Construir el contenido multipart
                var content = new MultipartFormDataContent();

                // ✅ Enviar datos como strings
                content.Add(new StringContent(materialId.ToString()), "materialId");
                content.Add(new StringContent(cantidad.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)), "cantidad");

                // ✅ La imagen - usar el nombre correcto "foto" que espera tu backend
                var imageContent = new ByteArrayContent(fotoBytes);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "foto", "reciclaje.jpg");

                request.Content = content;

                // 5. Enviar la petición
                Console.WriteLine("📤 Enviando petición al servidor...");
                var response = await _httpClient.SendAsync(request);

                // 6. Procesar respuesta
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"✅ ¡RECICLAJE REGISTRADO CON ÉXITO! Respuesta: {responseContent}");

                    // Parsear la respuesta para verificar éxito
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("success", out JsonElement successElement) &&
                            successElement.ValueKind == JsonValueKind.True)
                        {
                            Console.WriteLine("🎉 Reciclaje guardado en base de datos correctamente");
                            return true;
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"⚠️ Respuesta exitosa pero no pudo parsearse: {jsonEx.Message}");
                        // Si no se puede parsear pero el status es exitoso, asumimos éxito
                        return true;
                    }

                    return true;
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ ERROR DEL SERVIDOR ({response.StatusCode}): {errorResponse}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("⚠️ El token venció o es inválido.");
                        // Limpiar token expirado
                        #if MACCATALYST
                        Preferences.Remove("auth_token");
                        #else
                        SecureStorage.Remove("auth_token");
                        #endif

                        // Mostrar alerta en el hilo principal
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Sesión Expirada",
                                "Por favor, inicia sesión nuevamente",
                                "OK");
                        });
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Error",
                                "Datos inválidos enviados al servidor",
                                "OK");
                        });
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 EXCEPCIÓN CRÍTICA: {ex.Message}");
                Console.WriteLine($"💥 StackTrace: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error de Conexión",
                        "No se pudo conectar con el servidor. Verifica tu conexión a internet.",
                        "OK");
                });

                return false;
            }
        }


        // 📋 OBTENER MATERIALES
        public async Task<List<Material>> GetMaterialsAsync()
            {
                try
                {
                    await EnsureTokenAsync();

                    var response = await _httpClient.GetAsync("/api/materiales");

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<List<Material>>();
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error getting materials: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 GetMaterials error: {ex.Message}");
                    throw;
                }
            }

            // 🔄 REGISTRAR RECICLAJE (Solo Datos, sin foto - Método antiguo si lo necesitas)
            public async Task<MaterialScanResponse> RegisterRecyclingAsync(string qrData, int quantity = 1)
            {
                try
                {
                    await EnsureTokenAsync();

                    var request = new { qrData, quantity };
                    var response = await _httpClient.PostAsJsonAsync("/api/reciclajes/crear-desde-qr", request);

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<MaterialScanResponse>();
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error registering recycling: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 RegisterRecycling error: {ex.Message}");
                    throw;
                }
            }

            // 🔍 MÉTODO PRIVADO: Asegurar que tenemos token
            private async Task EnsureTokenAsync()
            {
                // Si no hay token en los headers, intentar recuperarlo
                if (_httpClient.DefaultRequestHeaders.Authorization == null)
                {
                    string savedToken = null;

                    #if MACCATALYST
                    // Para Mac Catalyst usar Preferences
                    savedToken = Preferences.Get("auth_token", string.Empty);
                    Console.WriteLine($"🔄 [MACCATALYST] Recuperando token desde Preferences");
                    #else
                    // Para otras plataformas usar SecureStorage
                    savedToken = await SecureStorage.GetAsync("auth_token");
                    Console.WriteLine($"🔄 Recuperando token desde SecureStorage");
                    #endif

                    if (!string.IsNullOrEmpty(savedToken))
                    {
                        SetAuthToken(savedToken);
                        Console.WriteLine($"✅ Token JWT configurado en headers: {savedToken.Substring(0, Math.Min(20, savedToken.Length))}...");
                    }
                    else
                    {
                        // Opcional: Lanzar excepción o manejar logout
                        // throw new Exception("Usuario no autenticado.");
                        Console.WriteLine("⚠️ Advertencia: No hay token guardado.");
                    }
                }
            }

        // 1. Obtener lista de pendientes
        public async Task<List<ReciclajeDTO>> ObtenerPendientesAsync()
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync("/api/reciclajes/pendientes");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ReciclajeDTO>>();
                }
                return new List<ReciclajeDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo pendientes: {ex.Message}");
                return new List<ReciclajeDTO>();
            }
        }

        // 2. Validar (Aprobar) un reciclaje
        public async Task<bool> ValidarReciclajeAsync(long id)
        {
            try
            {
                await EnsureTokenAsync();
                // Tu backend tiene este endpoint PUT: /validar/{id}
                var response = await _httpClient.PutAsync($"/api/reciclajes/validar/{id}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validando: {ex.Message}");
                return false;
            }
        }
        // 🧪 TEST DE CONEXIÓN
        public async Task<bool> TestConnectionAsync()
            {
                try
                {
                    var response = await _httpClient.GetAsync("/api/materiales");
                    Console.WriteLine($"🔗 Connection test: {response.StatusCode}");
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔗 Connection test failed: {ex.Message}");
                    return false;
                }
            }

        // 📋 MÉTODOS PARA ADMINISTRADOR
        public async Task<List<ReciclajeDTO>> ObtenerReciclajesPendientesAsync()
        {
            try
            {
                await EnsureTokenAsync();
                // Este endpoint debe coincidir con tu ReciclajeController en Java (@GetMapping("/pendientes"))
                var response = await _httpClient.GetAsync("/api/reciclajes/pendientes");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ReciclajeDTO>>();
                }
                else
                {
                    Console.WriteLine($"Error API: {response.StatusCode}");
                    return new List<ReciclajeDTO>(); // Retorna lista vacía si falla
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción obteniendo pendientes: {ex.Message}");
                return new List<ReciclajeDTO>();
            }
        }

        public async Task<bool> AprobarReciclajeAsync(long id)
        {
            try
            {
                await EnsureTokenAsync();
                // Endpoint: @PutMapping("/validar/{id}")
                var response = await _httpClient.PutAsync($"/api/reciclajes/validar/{id}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error aprobando: {ex.Message}");
                return false;
            }
        }

        // 3. Rechazar Reciclaje (Opcional, si tu backend lo soporta)
        public async Task<bool> RechazarReciclajeAsync(long id, string motivo)
        {
            try
            {
                await EnsureTokenAsync();
                // Si no tienes endpoint de rechazar, puedes usar un Delete o crear uno nuevo
                // Por ahora, asumamos que borrar es rechazar:
                var response = await _httpClient.DeleteAsync($"/api/reciclajes/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rechazando: {ex.Message}");
                return false;
            }
        }

        // Verificar si el usuario actual es admin
        public async Task<bool> EsAdministradorAsync()
        {
            try
            {
                string email = null;

                #if MACCATALYST
                email = Preferences.Get("user_email", string.Empty);
                #else
                email = await SecureStorage.GetAsync("user_email");
                #endif

                return email == "admin@gmail.com";
            }
            catch
            {
                return false;
            }
        }

        // 📝 ACTUALIZAR MATERIAL
        public async Task<Material> ActualizarMaterialAsync(long id, Material material)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.PutAsJsonAsync($"/api/materiales/{id}", material);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Material>();
                }
                else
                {
                    throw new Exception($"Error updating material: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ActualizarMaterial error: {ex.Message}");
                throw;
            }
        }

        // ➕ CREAR MATERIAL
        public async Task<Material> CrearMaterialAsync(Material material)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.PostAsJsonAsync("/api/materiales", material);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Material>();
                }
                else
                {
                    throw new Exception($"Error creating material: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 CrearMaterial error: {ex.Message}");
                throw;
            }
        }

        // 🔍 BUSCAR MATERIAL POR CÓDIGO
        public async Task<object> BuscarMaterialPorCodigoAsync(object request)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.PostAsJsonAsync("/api/materiales/buscar-por-codigo", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error searching material by code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 BuscarMaterialPorCodigo error: {ex.Message}");
                throw;
            }
        }

        // 👤 OBTENER PERFIL DE USUARIO POR ID
        public async Task<Usuario> ObtenerPerfilUsuarioAsync(long id)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync($"/api/usuarios/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Usuario>();
                }
                else
                {
                    throw new Exception($"Error getting user profile: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ObtenerPerfilUsuario error: {ex.Message}");
                throw;
            }
        }

        // 📋 RECICLAJES POR USUARIO
        public async Task<object> ObtenerReciclajesPorUsuarioAsync(long usuarioId)
        {
            try
            {
                Console.WriteLine($"🔍 ApiService: Obteniendo reciclajes para usuario {usuarioId}");
                await EnsureTokenAsync();

                var url = $"/api/reciclajes/usuario/{usuarioId}";
                Console.WriteLine($"🌐 ApiService: Llamando a {url}");

                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"📡 ApiService: Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"📦 ApiService: JSON recibido ({jsonString.Length} chars): {jsonString}");

                    // Intentar deserializar como List<ReciclajeDTO>
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var result = JsonSerializer.Deserialize<List<ReciclajeDTO>>(jsonString, options);
                        Console.WriteLine($"✅ ApiService: Deserializado correctamente - {result?.Count ?? 0} items");
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"💥 ApiService: Error de deserialización JSON: {jsonEx.Message}");
                        // Retornar el JSON crudo como fallback
                        return jsonString;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ ApiService: Error HTTP {response.StatusCode}: {errorContent}");
                    throw new Exception($"Error getting recyclings by user: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ObtenerReciclajesPorUsuario error: {ex.Message}");
                throw;
            }
        }

        // 📋 TODOS LOS RECICLAJES
        public async Task<object> ObtenerTodosReciclajesAsync()
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync("/api/reciclajes/todos");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error getting all recyclings: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ObtenerTodosReciclajes error: {ex.Message}");
                throw;
            }
        }

        // 🔍 BUSCAR MATERIALES POR TIPO
        public async Task<List<Material>> BuscarMaterialesPorTipoAsync(string tipo)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync($"/api/materiales/tipo/{tipo}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Material>>();
                }
                else
                {
                    throw new Exception($"Error searching materials by type: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 BuscarMaterialesPorTipo error: {ex.Message}");
                throw;
            }
        }

        // 👤 REGISTRAR USUARIO API
        public async Task<object> RegistrarUsuarioApiAsync(RegistroRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/usuarios/api/registro", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error registering user: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 RegistrarUsuarioApi error: {ex.Message}");
                throw;
            }
        }

        // 📝 REGISTRAR RECICLAJE SIMPLE (sin foto)
        public async Task<object> RegistrarReciclajeSimpleAsync(long materialId, int cantidad = 1)
        {
            try
            {
                await EnsureTokenAsync();
                var request = new
                {
                    materialId = materialId,
                    cantidad = cantidad
                };
                var response = await _httpClient.PostAsJsonAsync("/api/reciclajes/registrar", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error registering recycling: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 RegistrarReciclajeSimple error: {ex.Message}");
                throw;
            }
        }

        // 📱 MIS RECICLAJES
        public async Task<object> ObtenerMisReciclajesAsync()
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync("/api/reciclajes/mis-reciclajes");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error getting my recyclings: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ObtenerMisReciclajes error: {ex.Message}");
                throw;
            }
        }

        // 🏆 OBTENER LOGROS
        public async Task<List<Logro>> ObtenerLogrosAsync()
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync("/api/logros");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Logro>>();
                }
                else
                {
                    throw new Exception($"Error getting achievements: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ObtenerLogros error: {ex.Message}");
                throw;
            }
        }

        // 📊 OBTENER NIVELES
        public async Task<List<Nivel>> ObtenerNivelesAsync()
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync("/api/niveles");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Nivel>>();
                }
                else
                {
                    throw new Exception($"Error getting levels: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ObtenerNiveles error: {ex.Message}");
                throw;
            }
        }

    }
    }