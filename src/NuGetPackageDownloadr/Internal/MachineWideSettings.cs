using NuGet.Common;
using NuGet.Configuration;

namespace NuGetPackageDownloader.Internal
{
    internal sealed class MachineWideSettings : IMachineWideSettings
    {
        private readonly ISettings _settings;

        internal MachineWideSettings()
        {
            string baseDirectory = NuGetEnvironment.GetFolderPath(NuGetFolderPath.MachineWideConfigDirectory);
            _settings = Settings.LoadMachineWideSettings(baseDirectory);
        }

        ISettings IMachineWideSettings.Settings => _settings;
    }
}
