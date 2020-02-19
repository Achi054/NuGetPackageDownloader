using NuGet.Common;
using NuGet.Configuration;

namespace NugetPackageDownloader.Resources.NuGet
{
    public class MachineWideSettings : IMachineWideSettings
    {
        private readonly ISettings _settings;

        public MachineWideSettings()
        {
            var baseDirectory = NuGetEnvironment.GetFolderPath(NuGetFolderPath.MachineWideConfigDirectory);
            _settings = Settings.LoadMachineWideSettings(baseDirectory);
        }

        ISettings IMachineWideSettings.Settings => _settings;
    }
}
