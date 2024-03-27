using System;
using System.Collections.Generic;

namespace API_REST_evaluacion_aperez.Models
{
    public partial class TicketSP
    {
        public string IdTienda { get; set; }
        public string IdRegistradora { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Ticket { get; set; }
        public string Impuesto { get; set; }
        public string Total { get; set; }
    }
}
