using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace JaySilk.Github.Cli
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("secrets.json", false, true)
            .Build();

        private static async Task ProcessRepositories()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config["oAuthKey"]);
        
            var stringTask = client.GetStringAsync("https://api.github.com/orgs/xpologistics/repos?type=private");

            var msg = await stringTask;
            Console.Write(msg);
        }
        static async Task Main(string[] args)
        {
            await ProcessRepositories();
        }
    }
}
