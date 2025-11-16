using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.Models;
using GreenCoinMovil.Views;
using Microsoft.Maui.Controls;



namespace GreenCoinMovil.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private bool isBusy;

        private readonly AuthService _authService;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private async Task NavigateToRegister()
        {
            await Shell.Current.GoToAsync("RegisterPage");
        }

        [RelayCommand]
        private async Task Login()
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // ✅ Validaciones básicas
                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Por favor, completa todos los campos";
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔐 Iniciando login para: {Email}");

                // 1. Crear la solicitud
                var request = new LoginRequest { Email = Email, Password = Password };

                // 2. Llamar al servicio
                var response = await _authService.AttemptLoginAsync(request);

                if (response.Success)
                {
                    System.Diagnostics.Debug.WriteLine("✅✅✅ LOGIN EXITOSO - Navegando al Dashboard...");

                    // ✅ Limpiar campos
                    Email = string.Empty;
                    Password = string.Empty;

                    // ✅ Navegación ABSOLUTA al Dashboard
                    await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
                }
                else
                {
                    // 4. Fallo: Mostrar el error
                    ErrorMessage = response.Error;
                    System.Diagnostics.Debug.WriteLine($"❌❌❌ LOGIN FALLIDO: {response.Error}");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error de conexión con el servidor";
                System.Diagnostics.Debug.WriteLine($"💥💥💥 ERROR EN LOGIN: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            await Application.Current.MainPage.DisplayAlert(
                "Recuperar Contraseña",
                "Función en desarrollo",
                "OK");
        }
    }
}
