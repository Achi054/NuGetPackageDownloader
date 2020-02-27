namespace NuGetDownloader
{
    internal class PackageConstants
    {
        internal class Options
        {
            internal const string Help = "--help";
            internal const string Info = "--info";

            internal class Abrivatives
            {
                internal const string Help = "-h";
                internal const string Info = "-i";
            }
        }

        internal class Commands
        {
            internal const string Download = "download";
            internal const string Extract = "extract";

            internal class CommandOptions
            {
                internal const string Name = "--name";
                internal const string Framework = "--framework";
                internal const string OutputPath = "--output-path";
                internal const string Version = "--version";
                internal const string IncludePrerelease = "--include-prerelease";

                internal class Abrivatives
                {
                    internal const string Name = "-n";
                    internal const string Framework = "-f";
                    internal const string OutputPath = "-op";
                    internal const string Version = "-v";
                    internal const string IncludePrerelease = "-ipr";
                }
            }
        }
    }
}
