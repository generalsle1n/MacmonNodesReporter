using MacmonNodesReporter.Model;
using Quartz;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;

namespace MacmonNodesReporter
{
    public class Worker : IJob
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpHandler;
        private const string _serverSection = "Server";
        private const string _defaultApiVersion = "v1.2";
        private readonly string[] _fallbackApiVersion = { "v1.1" };
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpHandler = new HttpClientHandler();
            _httpHandler.ServerCertificateCustomValidationCallback = (request, cert, certChain, policyErrors) => true;
            _httpClient = new HttpClient(_httpHandler);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            List<MacmonServer> AllServer = _configuration.GetSection(_serverSection).Get<List<MacmonServer>>();
            foreach (MacmonServer SingleServer in AllServer)
            {
                if (SingleServer.Static)
                {
                    _logger.LogWarning($"For {SingleServer.Customer}: Maclimit {SingleServer.NodeCount}");
                }
                else
                {
                _logger.LogInformation($"Process Server: {SingleServer.Adress} with User: {SingleServer.Username}");
                HttpResponseMessage Response = await GetLicenseOptionsAsync(SingleServer);
                if (Response is not null)
                {
                    List<LicenseOption> LicenseOptions = await ParseBodyAsync(Response);
                    LicenseOptionLimit Limit = await EvaluateOption(LicenseOptions);
                    _logger.LogWarning($"For {SingleServer.Customer}: {SingleServer.Adress} has Maclimit {Limit.Current}");
                }
                else
                {
                    _logger.LogError($"{SingleServer.Adress} is not available, check connection");
                }
            }
        }
        }

        private string CreateBase64String(string Username, string Password)
        {
            string AuthenticationData = $"{Username}:{Password}";
            byte[] AuthenticationRawData = Encoding.UTF8.GetBytes(AuthenticationData);
            string Base64AutheticationData = Convert.ToBase64String(AuthenticationRawData);

            return Base64AutheticationData;
        }

        private async Task<HttpResponseMessage> GetLicenseOptionsAsync(MacmonServer Server)
        {

            string AuthenticationData = CreateBase64String(Server.Username, Server.Password);

            HttpRequestMessage Request = CreateRequestMessage(Server, AuthenticationData, _defaultApiVersion);

            HttpResponseMessage Response = null;

            try
            {
                Response = await _httpClient.SendAsync(Request);

                if(Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Request to {Request.RequestUri} failed with 404, trying fallback API versions");
                    
                    foreach (string FallBackVersion in _fallbackApiVersion)
                    {
                        Request = CreateRequestMessage(Server, AuthenticationData, FallBackVersion);
                        Response = await _httpClient.SendAsync(Request);

                        if (Response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation($"Fallback to {FallBackVersion} worked for Server {Server.Adress}:{Server.Port}");
                            break;
                        }
                    }
                }
            }catch(HttpRequestException ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            if(Response.IsSuccessStatusCode == false)
            {
                Response = null;
            }

            return Response;
        }

        private async Task<List<LicenseOption>> ParseBodyAsync(HttpResponseMessage Message)
        {
            string RawBody = await Message.Content.ReadAsStringAsync();
            List<LicenseOption> ParsedData = JsonSerializer.Deserialize<List<LicenseOption>>(RawBody);

            return ParsedData;
        }

        private async Task<LicenseOptionLimit> EvaluateOption(List<LicenseOption> Option)
        {
            LicenseOption RequiredOption = Option.Where(option => option.Name.Equals(_configuration.GetValue<string>("Filter:Name"))).First();
        
            LicenseOptionLimit Limit = RequiredOption.Limits.Where(limit => limit.Name.Equals(_configuration.GetValue<string>("Filter:LimitName"))).First();
            return Limit;
        }

        private HttpRequestMessage CreateRequestMessage(MacmonServer Server, string AuthenticationData, string ApiVersion)
        {
            Uri RequestUri = new Uri($"https://{Server.Adress}:{Server.Port}/api/{ApiVersion}/licenseoptions?fields=*");

            HttpRequestMessage Result = new HttpRequestMessage(HttpMethod.Get, RequestUri);

            Result.Headers.Authorization = new AuthenticationHeaderValue("Basic", AuthenticationData);
            Result.Headers.TryAddWithoutValidation("Content-Type", "application/json");

            return Result;
        }
    }
}
