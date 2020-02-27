using System;

namespace NuGetDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            Func<string, bool> IsValidCommandOption = arg =>
            {
                return (arg == PackageConstants.Commands.CommandOptions.Name
                        || arg == PackageConstants.Commands.CommandOptions.Abrivatives.Name) ||
                    (arg == PackageConstants.Commands.CommandOptions.Framework
                        || arg == PackageConstants.Commands.CommandOptions.Abrivatives.Framework) ||
                    (arg == PackageConstants.Commands.CommandOptions.OutputPath
                        || arg == PackageConstants.Commands.CommandOptions.Abrivatives.OutputPath) ||
                    (arg == PackageConstants.Commands.CommandOptions.Version
                        || arg == PackageConstants.Commands.CommandOptions.Abrivatives.Version) ||
                    (arg == PackageConstants.Commands.CommandOptions.IncludePrerelease
                        || arg == PackageConstants.Commands.CommandOptions.Abrivatives.IncludePrerelease);
            };

            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
                HelpContent.RenderHelp();
            else if (args[0] == PackageConstants.Options.Abrivatives.Help || args[0] == PackageConstants.Options.Help)
                HelpContent.RenderHelp();
            else if (args[0] == PackageConstants.Options.Abrivatives.Info || args[0] == PackageConstants.Options.Info)
                HelpContent.RenderInfo();

            if (args[0] == PackageConstants.Commands.Download)
            {
                if (args[1] == PackageConstants.Options.Abrivatives.Help || args[1] == PackageConstants.Options.Help)
                    HelpContent.RenderCommandHelp(PackageConstants.Commands.Download);
                else if (IsValidCommandOption(args[1]))
                    Console.WriteLine(args[1]);
                else
                    HelpContent.RenderCommandHelp(PackageConstants.Commands.Download);
            }
            else if (args[0] == PackageConstants.Commands.Extract)
            {
                if (args[1] == PackageConstants.Options.Abrivatives.Help || args[1] == PackageConstants.Options.Help)
                    HelpContent.RenderCommandHelp(PackageConstants.Commands.Extract);
                else if (IsValidCommandOption(args[1]))
                    Console.WriteLine(args[1]);
                else
                    HelpContent.RenderCommandHelp(PackageConstants.Commands.Extract);
            }
        }
    }
}
