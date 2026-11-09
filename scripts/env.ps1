# All writable tool caches and temporary browser profiles stay on the D drive.
$toolRoot = 'D:\CloudQA-Tools'
New-Item -ItemType Directory -Force "$toolRoot\tmp","$toolRoot\nuget","$toolRoot\selenium","$toolRoot\cli" | Out-Null
$env:DOTNET_ROOT = "$toolRoot\dotnet"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_HOME = "$toolRoot\cli"
$env:NUGET_PACKAGES = "$toolRoot\nuget"
$env:TEMP = "$toolRoot\tmp"
$env:TMP = $env:TEMP
$env:SE_CACHE_PATH = "$toolRoot\selenium"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = "false"
