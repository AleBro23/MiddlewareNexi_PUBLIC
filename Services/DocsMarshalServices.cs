using Microsoft.Extensions.Configuration;
using MiddlewareNexi.Utils;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace MiddlewareNexi.Services
{
    public class DocsMarshalService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public DocsMarshalService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public class UpdateStatoResponse
        {
            public bool HasError { get; set; }
            public string Error { get; set; } = string.Empty;
        }

        public async Task<UpdateStatoResponse> UpdateStatoAsync(string objectId, string stato, string codiceOrdine, string id)
        {
            var sessionId = _config["DocsMarshal:SessionId"];
            var endpoint = _config["DocsMarshal:Endpoint"];
            var idWorkflowEvent = _config["DocsMarshal:IdWorkflowEventMiddleware"];

            var payload = new
            {
                sessionID = sessionId,
                idWorkFlowEvent = idWorkflowEvent,
                parameters = new[]
                {
                    new { Name = "ObjectId", Value = objectId, ValueType = "Guid" },
                    new { Name = "Stato", Value = stato, ValueType = "String" },
                    new { Name = "CodiceOrdine", Value = codiceOrdine, ValueType = "String" },
                    new { Name = "IdPagamento", Value = id, ValueType = "String" }
                }
            };

            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            //await LogHelper.ScriviLogDettagliatoAsync("middleware", $"📤 Payload inviato a DocsMarshal:\n{payloadJson}");

            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadFromJsonAsync<DocsMarshalResponse>();

                var result = new UpdateStatoResponse
                {
                    HasError = content?.result?.HasError ?? true,
                    Error = content?.result?.Error ?? "Errore sconosciuto"
                };

                int? idInt = int.TryParse(id, out var tempId) ? tempId : null;

                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(objectId, idInt, "UPDATE_STATO_DM", !result.HasError, result.Error));

                return result;
            }
            catch (Exception ex)
            {
                int? idInt = int.TryParse(id, out var tempId) ? tempId : null;

                await LogHelper.ScriviLogDettagliatoAsync("middleware",
                    LogHelper.FormatLog(objectId, idInt, "UPDATE_STATO_DM", false, ex.Message));

                return new UpdateStatoResponse
                {
                    HasError = true,
                    Error = ex.Message
                };
            }
        }

        public async Task<UpdateStatoResponse> UpdateAnnullatoAsync(string stato, string objectIds)
        {
            var sessionId = _config["DocsMarshal:SessionId"];
            var endpoint = _config["DocsMarshal:Endpoint"];
            var idWorkflowEvent = _config["DocsMarshal:IdWorkflowEventCleaner"];

            var payload = new
            {
                sessionID = sessionId,
                idWorkFlowEvent = idWorkflowEvent,
                parameters = new[]
                {
                    new { Name = "Stato", Value = stato, ValueType = "String" },
                    new { Name = "ObjectIds", Value = objectIds, ValueType = "String" }
                }
            };

            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            //await LogHelper.ScriviLogDettagliatoAsync("cleaner", $"📤 Payload UpdateAnnullatoAsync inviato a DocsMarshal:\n{payloadJson}");

            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadFromJsonAsync<DocsMarshalResponse>();

                var result = new UpdateStatoResponse
                {
                    HasError = content?.result?.HasError ?? true,
                    Error = content?.result?.Error ?? "Errore sconosciuto"
                };

                await LogHelper.ScriviLogDettagliatoAsync("cleaner",
                    LogHelper.FormatLog(objectIds, null, "UPDATE_ANNULLATI_DM", !result.HasError, result.Error));

                return result;
            }
            catch (Exception ex)
            {
                await LogHelper.ScriviLogDettagliatoAsync("cleaner",
                    LogHelper.FormatLog(objectIds, null, "UPDATE_ANNULLATI_DM", false, ex.Message));

                return new UpdateStatoResponse
                {
                    HasError = true,
                    Error = ex.Message
                };
            }
        }

        private class DocsMarshalResponse
        {
            public Result result { get; set; } = new();

            public class Result
            {
                public bool HasError { get; set; }
                public string Error { get; set; } = string.Empty;
            }
        }
    }
}
