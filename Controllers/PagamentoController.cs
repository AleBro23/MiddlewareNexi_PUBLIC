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
    [Route("midapi/[controller]")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class PagamentoController : ControllerBase
    {
        private readonly PagamentoDbContext _context;
        private readonly IConfiguration _config;
        private readonly DocsMarshalService _dmService;

        public PagamentoController(PagamentoDbContext context, IConfiguration config, DocsMarshalService dmService)
        {
            _context = context;
            _config = config;
            _dmService = dmService;

        }

        // POST: midapi/pagamento/crea
        [HttpPost("crea")]
        public async Task<IActionResult> CreaPagamento([FromBody] Pagamento nuovo)
        {
            try
            {
                nuovo.Stato = "IN ATTESA";
                nuovo.DataRichiesta = DateTime.Now;
                nuovo.CodiceOrdine = Guid.NewGuid().ToString("N").Substring(0, 15).ToUpper();
                nuovo.Importo = Math.Round(nuovo.Prezzo * 100, 0);
                
                _context.Pagamenti.Add(nuovo);
                await _context.SaveChangesAsync();

                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(nuovo.ObjectId.ToString(), nuovo.Id, "CREAZIONE_PAGAMENTO", true));

                // Lettura parametri Nexi da appsettings.json
                var alias = _config["Nexi:Alias"];
                var chiaveMac = _config["Nexi:ChiaveMac"];
                var divisa = _config["Nexi:Divisa"];
                var urlEsito = _config["Nexi:UrlEsito"];
                var urlAnnulla = _config["Nexi:UrlAnnulla"];
                var baseUrl = _config["Nexi:BaseUrl"];
                var urlpost = _config["Nexi:UrlPost"];

                var codTrans = nuovo.CodiceOrdine;
                var importo = ((int)nuovo.Importo).ToString();

                string stringaMac = $"codTrans={codTrans}divisa={divisa}importo={importo}{chiaveMac}";
                string mac = SecuritySha1.CalcolaSha1(stringaMac);

                var queryParams = new Dictionary<string, string>
                {
                    {"alias", alias},
                    {"importo", importo},
                    {"divisa", divisa},
                    {"codTrans", codTrans},
                    {"url", urlEsito},
                    {"url_back", urlAnnulla},
                    {"mac", mac},
                    {"urlpost", urlpost},
                    {"descrizione", "Pagamento Cartella Clinica" }
                     // --- CUSTOM PARAMS x UI ---
                    //{ "objectId", nuovo.ObjectId.ToString() }
                };

                var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
                var redirectUrl = $"{baseUrl}?{queryString}";

                return Ok(new
                {
                    ordineId = nuovo.Id,
                    redirectUrl
                });
            }
            catch (Exception ex)
            {
                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog("-", 0, "CREAZIONE_PAGAMENTO", false, ex.Message));

                return StatusCode(500, "Errore nella creazione del pagamento");
            }
        }

        // GET: midapi/pagamento/stato?objectId=...
        [HttpGet("stato")]
        public async Task<IActionResult> StatoPagamento([FromQuery] string objectId)
        {
            try
            {
                var pagamento = await _context.Pagamenti.FirstOrDefaultAsync(p => p.ObjectId == objectId);

                if (pagamento == null)
                {
                    await LogHelper.ScriviLogDettagliatoAsync("middleware",
                        LogHelper.FormatLog(objectId, 0, "GET_STATO", false, "Pagamento non trovato"));
                    return NotFound();
                }

                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(objectId, pagamento.Id, "GET_STATO", true));

                return Ok(new
                {
                    pagamento.Id,
                    pagamento.ObjectId,
                    pagamento.Stato,
                    pagamento.DataPagamento,
                    pagamento.Importo,
                    pagamento.CodiceOrdine
                });
            }
            catch (Exception ex)
            {
                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(objectId, 0, "GET_STATO", false, ex.Message));

                return StatusCode(500, "Errore durante il recupero dello stato");
            }
        }

        [HttpGet("statoCT")]
        public async Task<IActionResult> StatoPagamentoCT([FromQuery] string codTrans)
        {
            try
            {
                var pagamento = await _context.Pagamenti.FirstOrDefaultAsync(p => p.CodiceOrdine == codTrans);

                if (pagamento == null)
                {
                    await LogHelper.ScriviLogDettagliatoAsync("middleware",
                        LogHelper.FormatLog(codTrans, 0, "GET_STATO_CT", false, "Pagamento non trovato"));
                    return NotFound();
                }

                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(codTrans, pagamento.Id, "GET_STATO_CT", true));

                return Ok(new
                {
                    pagamento.Id,
                    pagamento.ObjectId,
                    pagamento.Stato,
                    pagamento.DataPagamento,
                    pagamento.Importo,
                    pagamento.CodiceOrdine
                });
            }
            catch (Exception ex)
            {
                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(codTrans, 0, "GET_STATO_CT", false, ex.Message));

                return StatusCode(500, "Errore durante il recupero dello stato");
            }
        }


    }
}
