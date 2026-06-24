namespace FfxiTempLogCollector.Ipc;

public static class IpcModule
{
    public const string MutexName =
        @"Local\FFXI_LogRep_r.Gui.Singleton";

    public const string PipeName =
        "FFXI_LogRep_r.CommandPipe";
}
