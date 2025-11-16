using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.Models;
using GreenCoinMovil.UsuarioDTO;
using GreenCoinMovil.Views;

namespace GreenCoinMovil.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private UsuarioDto _usuario;

        [ObservableProperty]
        private string _nivelActual = "Semilla Verde";

        [ObservableProperty]
        private int _puntosTotales;

        [ObservableProperty]
        private int _logrosDesbloqueados = 1;

        [ObservableProperty]
        private int _totalLogros = 15;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private List<ActividadDTO> _actividadesRecientes;

        public DashboardViewModel(AuthService authService)
        {
            _authService = authService;
            LoadDashboardDataCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadDashboardData()
        {
            IsBusy = true;
            try
            {
                // Obtener datos del usuario
                Usuario = AuthService.CurrentUsuario;

                if (Usuario == null)
                {
                    await Logout();
                    return;
                }

                // Simular datos del dashboard (debes reemplazar con llamadas a tu API)
                await SimularCargaDeDatos();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Error al cargar el dashboard", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SimularCargaDeDatos()
        {
            // Simular carga de datos desde API
            await Task.Delay(1000);

            PuntosTotales = Usuario?.Puntos?? 0;

            // Simular actividades recientes
            ActividadesRecientes = new List<ActividadDTO>
            {
                new ActividadDTO { Titulo = "Reciclaje de Plástico", Puntos = 50, Fecha = DateTime.Now.AddDays(-1) },
                new ActividadDTO { Titulo = "Reciclaje de Vidrio", Puntos = 30, Fecha = DateTime.Now.AddDays(-2) },
                new ActividadDTO { Titulo = "Reciclaje de Papel", Puntos = 20, Fecha = DateTime.Now.AddDays(-3) }
            };
        }

        [RelayCommand]
        private async Task NavigateToProfile()
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage));
        }

        [RelayCommand]
        private async Task NavigateToRecycle()
        {
            await Shell.Current.GoToAsync(nameof(RecyclePage));
        }

        [RelayCommand]
        private async Task NavigateToAchievements()
        {
            await Shell.Current.GoToAsync(nameof(AchievementsPage));
        }

        [RelayCommand]
        private async Task NavigateToHistory()
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }

        [RelayCommand]
        private async Task ShowQRCode()
        {
            await Shell.Current.DisplayAlert("Código QR", "Aquí se mostrará tu código QR único", "OK");
        }

        [RelayCommand]
        private async Task RegisterRecycling()
        {
            await Shell.Current.DisplayAlert("Registrar Reciclaje", "Función para registrar nuevo reciclaje", "OK");
        }

        [RelayCommand]
        private async Task ViewAllAchievements()
        {
            await NavigateToAchievements();
        }

        [RelayCommand]
        private async Task ViewAllHistory()
        {
            await NavigateToHistory();
        }

        [RelayCommand]
        private async Task Logout()
        {
            IsBusy = true;
            try
            {
                _authService.Logout();
                await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo cerrar la sesión", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class ActividadDTO
    {
        public string Titulo { get; set; }
        public int Puntos { get; set; }
        public DateTime Fecha { get; set; }

        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        public string PuntosFormateados => $"+{Puntos} pts";
    }
}
