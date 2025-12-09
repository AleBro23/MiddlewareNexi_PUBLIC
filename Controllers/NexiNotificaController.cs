using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiddlewareNexi.Data;
using MiddlewareNexi.Filters;
using MiddlewareNexi.Models;
using MiddlewareNexi.Services;
using MiddlewareNexi.Utils;
using System.Security.Cryptography;
using System.Text;

namespace MiddlewareNexi.Controllers
{
    [ApiController]
    [Route("notifica/nexi")]
    public class NexiNotificaController : ControllerBase
    {
        private readonly PagamentoDbContext _context;
        private readonly DocsMarshalService _dmService;
        private readonly IConfiguration _config;

        public NexiNotificaController(PagamentoDbContext context, IConfiguration config, DocsMarshalService dmService)
        {
            _context = context;
            _dmService = dmService;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> RiceviNotifica([FromForm] IFormCollection form)
        {
            // Lettura parametri obbligatori
            string? esito = form["esito"];
            string? codTrans = form["codTrans"];
            string? macDaVerificare = form["mac"];
            string? data = form["data"];
            string? orario = form["orario"];
            string? codAut = form["codAut"];
            string? divisa = form["divisa"];
            string? importo = form["importo"];

            await LogHelper.ScriviLogDettagliatoAsync("middleware",
                $"📩 Form ricevuto da Nexi: esito={esito}, codTrans={codTrans}, data={data}, orario={orario}, codAut={codAut}, mac={macDaVerificare}");

            var pagamento = await _context.Pagamenti.FirstOrDefaultAsync(p => p.CodiceOrdine == codTrans);
            if (pagamento == null)
            {
                var msg = $"❌ Tentativo sospetto: pagamento non trovato per codTrans={codTrans}";
                await LogHelper.ScriviLogDettagliatoAsync("middleware", msg);
                return BadRequest("Pagamento non trovato");
            }

            string chiaveMac = _config["Nexi:ChiaveMac"] ?? "";
            
            //string divisa = _config["Nexi:Divisa"] ?? "EUR";
            //string importo = Convert.ToInt32(Math.Round(pagamento.Importo, 0)).ToString();

            // Costruzione della stringa MAC 
            var sb = new StringBuilder();
            sb.Append($"codTrans={codTrans}");
            sb.Append($"esito={esito}");
            sb.Append($"importo={importo}");
            sb.Append($"divisa={divisa}");
            sb.Append($"data={data}");
            sb.Append($"orario={orario}");
            sb.Append($"codAut={codAut}");
            sb.Append(chiaveMac);

            string stringaMac = sb.ToString();
            string macRicalcolato = SecuritySha1.CalcolaSha1(stringaMac);

            //await LogHelper.ScriviLogDettagliatoAsync("middleware", $"🔐 Stringa MAC usata per calcolo: {stringaMac}");
            await LogHelper.ScriviLogDettagliatoAsync("middleware", $"🔄 MAC ricevuto={macDaVerificare} | MAC calcolato={macRicalcolato}");

            if (!string.Equals(macRicalcolato, macDaVerificare, StringComparison.OrdinalIgnoreCase))
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var msg = $"❌ MAC NON VALIDO da IP={ip} | pagamento={pagamento.Id} | ricevuto={macDaVerificare} | atteso={macRicalcolato}";
                await LogHelper.ScriviLogDettagliatoAsync("middleware", msg);
                return BadRequest("MAC non valido");
            }

            try
            {
                string stato = esito == "OK" ? "PAGATO" : "FALLITO";

                pagamento.Stato = stato;
                if (stato == "PAGATO")
                    pagamento.DataPagamento = DateTime.Now;

                await _dmService.UpdateStatoAsync(pagamento.ObjectId, stato, pagamento.CodiceOrdine, pagamento.Id.ToString());
                await _context.SaveChangesAsync();

                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(pagamento.ObjectId.ToString(), pagamento.Id, $"NOTIFICA_NEXI_{stato}", true));

                return Ok();
            }
            catch (Exception ex)
            {
                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(pagamento.ObjectId.ToString(), pagamento.Id, "NOTIFICA_NEXI_ERRORE", false, ex.Message));

                return StatusCode(500, "Errore durante l'elaborazione della notifica Nexi");
            }
        }
    }

}
