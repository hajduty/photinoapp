using JobTracker.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ProxySharp;
using System.Diagnostics;
using System.Net;

namespace JobTracker.Application.Infrastructure.Services.Scraper;

public class LinkedInScraper
{
    private readonly IDbContextFactory<AppDbContext> _dbContext;

    public LinkedInScraper(IDbContextFactory<AppDbContext> dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> FetchJobsAsync(string keyword)
    {
        await using var db = await _dbContext.CreateDbContextAsync();

        var client = GetWorkingProxy();

        Debug.WriteLine("We are being called?");

        return await Task.FromResult(0);
    }

    private static HttpClient? GetWorkingProxy()
    {
        int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            HttpClient client = null;

            try
            {
                var proxyServer = Proxy.GetSingleRandomProxy();
                var proxyParts = proxyServer.Split(':');

                if (proxyParts.Length != 2)
                    continue;

                var webProxy = new WebProxy(proxyParts[0], int.Parse(proxyParts[1]));

                var handler = new HttpClientHandler
                {
                    Proxy = webProxy,
                    UseProxy = true,
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                client = new HttpClient(handler);

                var testTask = client.GetAsync("https://www.linkedin.com/jobs", HttpCompletionOption.ResponseHeadersRead);
                if (testTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    var response = testTask.Result;
                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"Proxy works: {proxyServer}");
                        return client;
                    }
                }

                client.Dispose();
                Debug.WriteLine($"Proxy dead: {proxyServer}");
            }
            catch (Exception)
            {
                client?.Dispose();
            }
        }

        Debug.WriteLine("No working proxies found after 10 attempts");
        return null;
    }
}
