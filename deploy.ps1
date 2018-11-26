param(
  [Parameter(Mandatory = $true, HelpMessage="Enter a key")]
  [ValidateNotNullorEmpty()]
  [string] $Key
)

function Invoke-CallWithExitCodeCheck {
  param (
    [scriptblock]$ScriptBlock,
    [int[]]$SuccessCodes = @(0)
  )

  & $ScriptBlock

  if ($SuccessCodes -notcontains $LastExitCode) {
    throw "Exe { $ScriptBlock } returned exit code $LastExitCode"
  }
}

$ErrorActionPreference = 'Stop'

$deployDir = (Resolve-Path .\).Path

try {
  Set-Location "saitonakamura.Watchdog"
  Remove-Item -Recurse -Force "bin/Release"
  Invoke-CallWithExitCodeCheck { dotnet pack -c Release }
  $packages = Get-ChildItem -Path "bin/Release" -Filter "*.nupkg"
  $package = $packages[0]
  $packagePath = $package.FullName
  Invoke-CallWithExitCodeCheck {
    dotnet nuget push $packagePath -k $Key -s https://api.nuget.org/v3/index.json
  }
} finally {
  Set-Location $deployDir
}