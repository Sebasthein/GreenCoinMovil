using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.DTO; // Asegúrate de tener tus DTOs aquí
using GreenCoinMovil.Models;
using System.Collections.ObjectModel;

namespace GreenCoinMovil.ViewModels
{
    public partial class AdminValidationViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<ReciclajeDTO> _pendientes;

        public AdminValidationViewModel(ApiService apiService)
        {
            _apiService = apiService;
            _pendientes = new ObservableCollection<ReciclajeDTO>();
            CargarPendientesCommand.Execute(null);
        }

        [RelayCommand]
        private async Task CargarPendientes()
        {
            IsBusy = true;
            try
            {
                var lista = await _apiService.ObtenerPendientesAsync();
                Pendientes.Clear();
                foreach (var item in lista)
                {
                    // TRUCO: Si la URL viene como ruta de archivo local (C:\...), 
                    // necesitamos convertirla a URL http para que el celular la vea.
                    // Si tu backend ya devuelve URL http, borra esta línea.
                    item.ImagenUrl = ConvertirRutaAUrl(item.ImagenUrl);

                    Pendientes.Add(item);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Aprobar(ReciclajeDTO reciclaje)
        {
            if (reciclaje == null) return;

            bool confirm = await Shell.Current.DisplayAlert("Validar",
                $"¿Aprobar reciclaje de {reciclaje.MaterialNombre}?", "Sí", "Cancelar");

            if (confirm)
            {
                bool exito = await _apiService.ValidarReciclajeAsync(reciclaje.Id);
                if (exito)
                {
                    Pendientes.Remove(reciclaje); // Lo quitamos de la lista
                    await Shell.Current.DisplayAlert("Éxito", "Reciclaje validado y puntos asignados.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "No se pudo validar.", "OK");
                }
            }
        }

        // Método auxiliar para ver imágenes en el emulador
        private string ConvertirRutaAUrl(string path)
        {
            if (string.IsNullOrEmpty(path)) return "no_image.png";

            // Si la base de datos guardó "uploads/reciclajes/foto.jpg"
            // y queremos verla desde el emulador Android:
            if (!path.StartsWith("http"))
            {
                // Extraer solo el nombre del archivo si viene con ruta completa
                var fileName = Path.GetFileName(path);
                return $"http://192.168.1.8:8080/uploads/{fileName}";
            }
            return path;
        }
    }
}