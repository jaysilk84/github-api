using System;
using System.Linq;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace JaySilk.Github.Cli
{
    static class Utilities {
        public static string ForCsv(this string value) {
            if (value == null) return value;

            return value.Insert(0, "\"").Insert(value.Length + 1, "\"");
        }
    }

    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("secrets.json", false, true)
            .Build();

        private static void WriteRepositoriesToFile(List<Repository> repos)
        {
            using var fs = File.Open("output.csv", FileMode.Create);
            using var s = new StreamWriter(fs);
            
            s.WriteLine("Name, Description, Url, PushedDate, CreatedDate, UpdatedDate");
            foreach (var r in repos) {
                s.WriteLine($"{r.Name.ForCsv()},{r.Description.ForCsv()},{r.Url.ForCsv()},{r.PushedDate},{r.CreatedDate},{r.UpdatedDate}");
            }
        }

        private static async Task<GithubResponse> ProcessRepositories(string requestUri)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config["oAuthKey"]);
            client.DefaultRequestHeaders.Add("User-Agent", "XpoLogistics Repo Lister");

            //var stringTask = client.GetStringAsync("https://api.github.com/orgs/xpologistics/repos?type=private");
            var response = await client.GetAsync(requestUri);
            return new GithubResponse(await JsonSerializer.DeserializeAsync<List<Repository>>(await response.Content.ReadAsStreamAsync()),
                LinkHeader.LinksFromHeader(response.Headers.GetValues("Link").FirstOrDefault()));
        }

        static async Task Main(string[] args)
        {
            var repos = new List<Repository>();
            int p = 1;
            string url = "https://api.github.com/orgs/xpologistics/repos?type=private&per_page=100";
            do {
                Console.WriteLine($"Processing page {p}");
                var result = await ProcessRepositories(url);
                repos.AddRange(result.Repositories);
                url = result.LinkHeader.NextLink;
                p++;
            } while (!string.IsNullOrEmpty(url));

            Console.WriteLine($"Found {repos.Count} repositories");

            WriteRepositoriesToFile(repos);
    
        }

        private class GithubResponse
        {
            public List<Repository> Repositories { get; }
            public LinkHeader LinkHeader { get; }
            public GithubResponse(List<Repository> repositories, LinkHeader linkHeader)
            {
                Repositories = repositories;
                LinkHeader = linkHeader;
            }
        }
        private class Repository
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("html_url")]
            public string Url { get; set; }

            [JsonPropertyName("pushed_at")]
            public DateTime PushedDate { get; set; }

            [JsonPropertyName("created_at")]
            public DateTime CreatedDate { get; set; }

            [JsonPropertyName("updated_at")]
            public DateTime UpdatedDate { get; set; }
        }

        public class LinkHeader
        {
            public string FirstLink { get; set; }
            public string PrevLink { get; set; }
            public string NextLink { get; set; }
            public string LastLink { get; set; }

            public static LinkHeader LinksFromHeader(string linkHeaderStr)
            {
                LinkHeader linkHeader = null;

                if (!string.IsNullOrWhiteSpace(linkHeaderStr))
                {
                    string[] linkStrings = linkHeaderStr.Split(',');

                    if (linkStrings != null && linkStrings.Any())
                    {
                        linkHeader = new LinkHeader();

                        foreach (string linkString in linkStrings)
                        {
                            var relMatch = Regex.Match(linkString, "(?<=rel=\").+?(?=\")", RegexOptions.IgnoreCase);
                            var linkMatch = Regex.Match(linkString, "(?<=<).+?(?=>)", RegexOptions.IgnoreCase);

                            if (relMatch.Success && linkMatch.Success)
                            {
                                string rel = relMatch.Value.ToUpper();
                                string link = linkMatch.Value;

                                switch (rel)
                                {
                                    case "FIRST":
                                        linkHeader.FirstLink = link;
                                        break;
                                    case "PREV":
                                        linkHeader.PrevLink = link;
                                        break;
                                    case "NEXT":
                                        linkHeader.NextLink = link;
                                        break;
                                    case "LAST":
                                        linkHeader.LastLink = link;
                                        break;
                                }
                            }
                        }
                    }
                }

                return linkHeader;
            }
        }
    }
}