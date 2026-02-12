using JobTracker.Application.Infrastructure.Discord;
using JobTracker.Application.Infrastructure.RPC;
using TypeGen.Core.TypeAnnotations;

namespace JobTracker.Application.Features.Settings.TestConnection;

[ExportTsInterface]
public record TestConnectionRequest(string WebhookUrl);

[ExportTsInterface]
public record TestConnectionResponse(bool Success, string Message);

public sealed class TestConnectionHandler : RpcHandler<TestConnectionRequest, TestConnectionResponse>
{
    private readonly IDiscordWebhookService _discordWebhookService;
    public override string Command => "settings.testConnection";

    public TestConnectionHandler(IDiscordWebhookService discordWebhookService)
    {
        _discordWebhookService = discordWebhookService;
    }

    protected override async Task<TestConnectionResponse> HandleAsync(TestConnectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WebhookUrl))
        {
            return new TestConnectionResponse(false, "Webhook URL is required.");
        }

        var success = await _discordWebhookService.TestWebhookAsync(request.WebhookUrl);
        
        return success 
            ? new TestConnectionResponse(true, "Connection successful! Check your Discord channel for the test message.")
            : new TestConnectionResponse(false, "Connection failed. Please check your webhook URL and try again.");
    }
}
