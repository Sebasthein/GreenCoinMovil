using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.DTO
{
    public class ReciclajeDTO
    {
        // Asegúrate de que se llame "Id" (si antes se llamaba "IdReciclaje", cámbialo a "Id")
        public long Id { get; set; }

        public string MaterialNombre { get; set; }
        public string UsuarioNombre { get; set; }
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; }
        public bool Validado { get; set; }

        // Esta propiedad es CRÍTICA para tu error de la imagen 1
        public string ImagenUrl { get; set; }

        public string Estado { get; set; } // "Pendiente", "Aprobado", etc.
        public int PuntosGanados { get; set; }
        public string Observaciones { get; set; }
        public string ObservacionesAdmin { get; set; }

        public string CantidadFormateada { get; set; }
        public string FechaFormateada { get; set; }

    }
}
