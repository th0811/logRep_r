using FfxiTempLogCollector.Core;
using FfxiTempLogCollector.Ipc;
using System.Runtime.InteropServices;

namespace FfxiTempLogCollector.App;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        return args.Length == 0
            ? RunGui()
            : RunCliAsync(args).GetAwaiter().GetResult();
    }

    private static int RunGui()
    {
        using var singleInstance = new SingleInstanceManager();

        if (!singleInstance.TryAcquire())
        {
            return ShowExistingInstanceAsync()
                .GetAwaiter()
                .GetResult();
        }

        if (OperatingSystem.IsWindows())
        {
            FreeConsole();
        }

        var application = new App();
        var configStore = new ConfigStore();
        var configLoader = new ConfigLoader(configStore);
        var configPath = configLoader.ResolvePath();
        var config = configLoader.Load();
        var collectorService = new CollectorService();
        var configEditService = new ConfigEditService(
            configStore,
            collectorService,
            configPath);
        var controller = new GuiCommandController(
            collectorService,
            config,
            configEditService,
            configLoader);
        var viewModel = new MainViewModel(
            controller,
            application.Dispatcher);
        var mainWindow = new MainWindow(viewModel);
        controller.AttachWindow(mainWindow);
        var server = new NamedPipeCommandServer(
            controller.HandleIpcCommandAsync);
        server.Start();

        try
        {
            return application.Run(mainWindow);
        }
        finally
        {
            server.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private static async Task<int> RunCliAsync(string[] args)
    {
        var output = CliOutputWriter.CreateConsole();
        var parseResult = new CliCommandParser().Parse(args);

        if (!parseResult.Success || parseResult.Command is null)
        {
            output.WriteError(parseResult.Error);
            output.WriteError(
                "使用方法を確認するには help を実行してください。");
            return (int)CliExitCode.InvalidArguments;
        }

        var configStore = new ConfigStore();
        var configLoader = new ConfigLoader(configStore);
        await using var controller = new CliCommandController(
            configStore,
            configLoader,
            new CollectorService(),
            output,
            new NamedPipeCommandClient());
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler? cancelHandler = null;

        if (parseResult.Command.Kind == CliCommandKind.Start)
        {
            cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;
        }

        try
        {
            var exitCode = await controller.ExecuteAsync(
                parseResult.Command,
                cancellation.Token);
            return (int)exitCode;
        }
        finally
        {
            if (cancelHandler is not null)
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
    }

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    private static async Task<int> ShowExistingInstanceAsync()
    {
        var client = new NamedPipeCommandClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.TrySendAsync(
                new IpcCommand { Name = "show" },
                TimeSpan.FromSeconds(2));

            if (response is not null)
            {
                return response.Success ? 0 : 1;
            }

            await Task.Delay(200);
        }

        return 1;
    }
}
