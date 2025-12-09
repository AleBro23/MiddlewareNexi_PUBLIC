using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MiddlewareNexi.Utils
{
    public static class LogHelper
    {
        public static async Task ScriviLogDettagliatoAsync(string tipo, string contenuto)
        {
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            var oggi = DateTime.Now;
            var dirMese = Path.Combine(basePath, oggi.ToString("yyyy-MM"));
            Directory.CreateDirectory(dirMese);

            var fileName = Path.Combine(dirMese, $"{oggi:dd-MM-yyyy}_{tipo.ToLower()}.log");
            var lineaLog = $"{DateTime.Now:HH:mm:ss} | {contenuto}";

            await File.AppendAllTextAsync(fileName, lineaLog + Environment.NewLine);
        }

        public static string FormatLog(string objectId, int? id, string tipoOperazione, bool successo, string? errore = null)
        {
            var esito = successo ? "SUCCESSO" : "ERRORE";

            var log = new StringBuilder();

            if (id != null)
                log.Append($"Id={id} | ");

            if (!string.IsNullOrWhiteSpace(objectId))
                log.Append($"ObjectId={objectId} | ");

            log.Append($"TipoOperazione={tipoOperazione} | Esito={esito}");

            if (!string.IsNullOrWhiteSpace(errore))
                log.Append($" | Errore={errore}");

            return log.ToString();
        }
    }
}
