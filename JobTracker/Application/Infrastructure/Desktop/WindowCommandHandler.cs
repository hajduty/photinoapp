/*using JobTracker.Application.Infrastructure.RPC;

namespace JobTracker.Application.Infrastructure.Desktop;

public record WindowCommandRequest(
    string Action,
    int? X = null,
    int? Y = null,
    bool IsStart = false
);
public record WindowCommandResponse(bool Success);

public sealed class WindowCommandHandler
    : RpcHandler<WindowCommandRequest, WindowCommandResponse>
{
    private readonly WindowService _windowService;

    public override string Command => "window.command";

    public WindowCommandHandler(WindowService windowService)
    {
        _windowService = windowService;
    }

    protected override Task<WindowCommandResponse> HandleAsync(
        WindowCommandRequest request)
    {
        switch (request.Action)
        {
            case "minimize":
                _windowService.Minimize();
                break;

            case "maximize":
                _windowService.Maximize();
                break;

            case "restore":
                _windowService.Restore();
                break;

            case "drag":
                if (request.X.HasValue && request.Y.HasValue)
                {
                    _windowService.StartDrag(request.X.Value, request.Y.Value, request.IsStart);
                }
                else
                {
                    return Task.FromResult(new WindowCommandResponse(false));
                }
                break;

            case "close":
                _windowService.Close();
                break;

            default:
                return Task.FromResult(new WindowCommandResponse(false));
        }

        return Task.FromResult(new WindowCommandResponse(true));
    }
}
*/