using System;

using NuGet.Frameworks;

namespace NugetPackageDownloader.Constants
{
    public enum TargetFramework
    {
        NETCOREAPP3_1,
        NETCOREAPP3_0,
        NETCOREAPP2_2,
        NETCOREAPP2_1,
        NETCOREAPP2_0,
        NETSTANDARD2_1,
        NETSTANDARD2_0,
        NET48,
        NET472,
        NET471,
        NET47,
    }

    internal static class TargetFrameworkExtension
    {
        static (string Name, string FrameworkName) frameworkString(TargetFramework targetFramework)
            => targetFramework switch
            {
                TargetFramework.NETSTANDARD2_1 => (Name: "netstandard2.1", FrameworkName: ".NETStandard,Version=v2.1"),
                TargetFramework.NETSTANDARD2_0 => (Name: "netstandard2.0", FrameworkName: ".NETStandard,Version=v2.0"),
                TargetFramework.NETCOREAPP3_1 => (Name: "netcoreapp3.1", FrameworkName: ".NETCoreApp,Version=v3.1"),
                TargetFramework.NETCOREAPP3_0 => (Name: "netcoreapp3.0", FrameworkName: ".NETCoreApp,Version=v3.0"),
                TargetFramework.NETCOREAPP2_2 => (Name: "netcoreapp2.2", FrameworkName: ".NETCoreApp,Version=v2.2"),
                TargetFramework.NETCOREAPP2_1 => (Name: "netcoreapp2.1", FrameworkName: ".NETCoreApp,Version=v2.1"),
                TargetFramework.NETCOREAPP2_0 => (Name: "netcoreapp2.0", FrameworkName: ".NETCoreApp,Version=v2.0"),
                TargetFramework.NET48 => (Name: "net48", FrameworkName: ".NETFramework,Version=v4.8"),
                TargetFramework.NET472 => (Name: "net472", FrameworkName: ".NETFramework,Version=v4.7.2"),
                TargetFramework.NET471 => (Name: "net471", FrameworkName: ".NETFramework,Version=v4.7.1"),
                TargetFramework.NET47 => (Name: "net47", FrameworkName: ".NETFramework,Version=v4.7"),
                _ => throw new ArgumentException(nameof(targetFramework))
            };

        internal static NuGetFramework ToNuGetFramework(this TargetFramework targetFramework)
            => NuGetFramework.ParseFrameworkName(frameworkString(targetFramework).FrameworkName, new DefaultFrameworkNameProvider());
    }
}
