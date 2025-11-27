using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.DTO;
using GreenCoinMovil.Models;
using Microsoft.Maui.Storage;
#if MACCATALYST
using Foundation;
#endif

namespace GreenCoinMovil.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly HttpClient _httpClient;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<ReciclajeDisplayItem> _reciclajesFiltrados;

        [ObservableProperty]
        private ObservableCollection<ReciclajeDTO> _todosLosReciclajes;

        [ObservableProperty]
        private bool _hasRecyclingHistory;

        [ObservableProperty]
        private bool _isEmptyList;

        [ObservableProperty]
        private int _totalReciclajes;

        [ObservableProperty]
        private int _reciclajesAprobados;

        [ObservableProperty]
        private int _totalPuntos;

        private string _currentFilter = "todos";

        public HistoryViewModel(ApiService apiService, HttpClient httpClient)
        {
            _apiService = apiService;
            _httpClient = httpClient;
            ReciclajesFiltrados = new ObservableCollection<ReciclajeDisplayItem>();
            TodosLosReciclajes = new ObservableCollection<ReciclajeDTO>();

            // Cargar datos al inicializar
            _ = LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                Console.WriteLine($"📊 HistoryViewModel: Cargando historial completo del usuario desde /api/reciclajes/mis-reciclajes");

                // Asegurar que tenemos el token de autenticación
                await EnsureTokenAsync();

                // Usar el endpoint correcto para obtener el historial completo
                var response = await _httpClient.GetAsync("api/reciclajes/mis-reciclajes");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var historial = JsonSerializer.Deserialize<List<ReciclajeDTO>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Console.WriteLine($"📊 HistoryViewModel: Tipo de respuesta: {historial?.GetType()?.Name ?? "null"}");

                    if (historial != null && historial.Any())
                    {
                        Console.WriteLine($"📊 HistoryViewModel: Primer reciclaje - Material: '{historial[0].MaterialNombre}', Fecha: {historial[0].Fecha}, Estado: '{historial[0].Estado}'");

                        TodosLosReciclajes.Clear();
                        foreach (var item in historial)
                        {
                            TodosLosReciclajes.Add(item);
                        }

                        HasRecyclingHistory = true;
                        IsEmptyList = false;

                        // Calcular estadísticas
                        CalcularEstadisticas();

                        // Aplicar filtro actual
                        AplicarFiltro(_currentFilter);

                        Console.WriteLine($"✅ HistoryViewModel: {historial.Count} reciclajes cargados");
                    }
                    else
                    {
                        HasRecyclingHistory = false;
                        IsEmptyList = true;
                        TotalReciclajes = 0;
                        ReciclajesAprobados = 0;
                        TotalPuntos = 0;
                        ReciclajesFiltrados.Clear();

                        Console.WriteLine("⚠️ HistoryViewModel: No se encontraron reciclajes");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ HistoryViewModel: Error HTTP {response.StatusCode}");
                    var errorMessage = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                        ? "Acceso denegado. Verifica tu sesión."
                        : $"Error al cargar el historial: {response.StatusCode}";
                    await Shell.Current.DisplayAlert("Error", errorMessage, "OK");
                    HasRecyclingHistory = false;
                    IsEmptyList = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 HistoryViewModel: Error cargando historial: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", $"Error al cargar el historial: {ex.Message}", "OK");
                HasRecyclingHistory = false;
                IsEmptyList = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

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
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", savedToken);
                    Console.WriteLine($"✅ Token JWT configurado en headers: {savedToken.Substring(0, Math.Min(20, savedToken.Length))}...");
                }
                else
                {
                    Console.WriteLine("⚠️ Advertencia: No hay token guardado.");
                }
            }
        }

        private void CalcularEstadisticas()
        {
            TotalReciclajes = TodosLosReciclajes.Count;
            ReciclajesAprobados = TodosLosReciclajes.Count(r => r.Estado == "VALIDADO");
            TotalPuntos = TodosLosReciclajes.Where(r => r.Estado == "VALIDADO").Sum(r => r.PuntosGanados);
        }

        private void AplicarFiltro(string filtro)
        {
            _currentFilter = filtro;
            IEnumerable<ReciclajeDTO> filtrados;

            switch (filtro)
            {
                case "pendientes":
                    filtrados = TodosLosReciclajes.Where(r => r.Estado == "PENDIENTE");
                    break;
                case "rechazados":
                    filtrados = TodosLosReciclajes.Where(r => r.Estado == "RECHAZADO");
                    break;
                case "aprobados":
                    filtrados = TodosLosReciclajes.Where(r => r.Estado == "VALIDADO");
                    break;
                default: // "todos"
                    filtrados = TodosLosReciclajes;
                    break;
            }

            // Convertir a items de display
            ReciclajesFiltrados.Clear();
            foreach (var reciclaje in filtrados.OrderByDescending(r => r.Fecha))
            {
                ReciclajesFiltrados.Add(new ReciclajeDisplayItem(reciclaje));
            }

            IsEmptyList = !ReciclajesFiltrados.Any();
        }

        [RelayCommand]
        private void FilterAll() => AplicarFiltro("todos");

        [RelayCommand]
        private void FilterPending() => AplicarFiltro("pendientes");

        [RelayCommand]
        private void FilterRejected() => AplicarFiltro("rechazados");

        [RelayCommand]
        private async Task Refresh() => await LoadHistoryAsync();
    }

    // Clase auxiliar para mostrar los reciclajes en la UI
    public class ReciclajeDisplayItem : ObservableObject
    {
        private readonly ReciclajeDTO _reciclaje;

        public ReciclajeDisplayItem(ReciclajeDTO reciclaje)
        {
            _reciclaje = reciclaje;
        }

        public string MaterialNombre => _reciclaje.MaterialNombre ?? "Material desconocido";
        public string FechaFormateada => _reciclaje.Fecha.ToString("dd/MM/yyyy HH:mm");
        public string EstadoTexto => _reciclaje.Estado ?? "Desconocido";
        public string PuntosTexto => _reciclaje.Estado == "Aprobado" ? $"+{_reciclaje.PuntosGanados}" : "-";
        public string CantidadTexto => $"Cant: {_reciclaje.Cantidad}";

        public string StatusIcon => _reciclaje.Estado switch
        {
            "VALIDADO" => "✅",
            "PENDIENTE" => "⏳",
            "RECHAZADO" => "❌",
            _ => "📝"
        };

        public string StatusColor => _reciclaje.Estado switch
        {
            "VALIDADO" => "#00b894",
            "PENDIENTE" => "#74b9ff",
            "RECHAZADO" => "#d63031",
            _ => "#636e72"
        };

        public string BackgroundColor => _reciclaje.Estado switch
        {
            "VALIDADO" => "#F8FFFE",
            "PENDIENTE" => "#F8FBFF",
            "RECHAZADO" => "#FFF8F8",
            _ => "#FAFAFA"
        };

        public string PuntosColor => _reciclaje.Estado == "VALIDADO" ? "#00b894" : "#636e72";
    }
}