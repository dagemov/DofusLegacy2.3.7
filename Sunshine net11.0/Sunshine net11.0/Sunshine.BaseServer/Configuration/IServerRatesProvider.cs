namespace Sunshine.BaseServer.Configuration
{
    public interface IServerRatesProvider
    {
        string LoadedFilePath { get; }

        ServerRatesConfig Current { get; }

        void Reload(string filePath = null);
    }
}
