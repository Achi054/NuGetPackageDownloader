[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target,
    [Parameter(Position = 0, Mandatory = $false, ValueFromRemainingArguments = $true)]
    [string[]]$ScriptArgs
)

# Create tool manifest
& dotnet new tool-manifest --force
# Install Cake.Tool locally
& dotnet tool install Cake.Tool
# Restore Cake tool
& dotnet tool restore

# Build Cake arguments
$cakeArguments = @("$Script");
if ($Target) { $cakeArguments += "--target=$Target" }
$cakeArguments += $ScriptArgs

& dotnet tool run dotnet-cake -- $cakeArguments
exit $LASTEXITCODE