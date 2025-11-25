using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class ActividadDTO
    {
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("puntos")]
        public int Puntos { get; set; }

        [JsonPropertyName("fecha")]
        public DateTime Fecha => DateTime.TryParse(FechaRaw, out var date) ? date : DateTime.Now;

        // Formateo para la vista
        public string FechaFormateada => Fecha.ToString("dd/MM");
        public string PuntosTexto => $"+{Puntos} pts";

        [JsonPropertyName("fecha")]
        public string FechaRaw { get; set; }
    }
}
