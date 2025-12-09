using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MiddlewareNexi.Data;
using MiddlewareNexi.Services;
using MiddlewareNexi.Utils;
using System.Text;

namespace NexiPagamentoCleaner
{
    public class PagamentoCleanerWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public PagamentoCleanerWorker(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int scadenzaMinuti = _configuration.GetValue<int>("PagamentoCleaner:ScadenzaMinuti");
            int delayMinuti = _configuration.GetValue<int>("PagamentoCleaner:DelayLoopMinuti");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await LogHelper.ScriviLogDettagliatoAsync("cleaner", "⏳ Avvio ciclo PagamentoCleaner");

                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<PagamentoDbContext>();
                    var dmService = scope.ServiceProvider.GetRequiredService<DocsMarshalService>();

                    var cutoff = DateTime.Now.AddMinutes(-scadenzaMinuti);

                    var scaduti = await dbContext.Pagamenti
                        .Where(p => p.Stato == "IN ATTESA" && p.DataRichiesta <= cutoff)
                        .ToListAsync(stoppingToken);

                    if (scaduti.Any())
                    {
                        var objectIdsBuilder = new StringBuilder();

                        foreach (var pagamento in scaduti)
                        {
                            pagamento.Stato = "ANNULLATO";
                            objectIdsBuilder.Append($";{pagamento.ObjectId}");

                            await LogHelper.ScriviLogDettagliatoAsync("cleaner",
                                LogHelper.FormatLog(pagamento.ObjectId, pagamento.Id, "ANNULLAMENTO_AUTO_DB", true));
                        }

                        var objectIds = objectIdsBuilder.ToString();

                        var response = await dmService.UpdateAnnullatoAsync("ANNULLATO", objectIds);

                        //await LogHelper.ScriviLogDettagliatoAsync("cleaner",
                          //  LogHelper.FormatLog(objectIds, null, "UPDATE_ANNULLATI_DM", !response.HasError, response.Error));

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                    else
                    {
                        await LogHelper.ScriviLogDettagliatoAsync("cleaner", "🔍 Nessun pagamento da annullare.");
                    }
                }
                catch (Exception ex)
                {
                    await LogHelper.ScriviLogDettagliatoAsync("cleaner",
                        $"❌ Errore nel worker PagamentoCleaner: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(delayMinuti), stoppingToken);
            }
        }
    }
}
