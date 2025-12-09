using System;

namespace MiddlewareNexi.Models
{
    public class Pagamento
    {
        public int Id { get; set; } 
        public string ObjectId { get; set; }      // ObjectId richiesta DocsMarshal
        public string? CodiceOrdine { get; set; }  // ID restituito da Nexi
        public string? Stato { get; set; }         // Es. "In attesa", "Pagato", "Errore"
        public DateTime DataRichiesta { get; set; }
        public DateTime? DataPagamento { get; set; }
        public decimal Prezzo { get; set; } //ricevuto dal JSON della richiesta
        public decimal Importo { get; set; } // Importo in centesimi
    }
}
