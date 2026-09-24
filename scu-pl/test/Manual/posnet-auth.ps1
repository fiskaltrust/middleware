# Reads the POSNET printer's authorization status and, with -Code, enters an authorization code.
#
# POT-I-DEV-05 p.33: `auth co<14 characters>` enters the code the POSNET dealer issues; `authstateget`
# answers `il<remaining days>` while a time-limited authorization runs, and 480 ERR_AUTH_AUTHORIZED once
# the device is authorized without a time limit — that error is the success signal. A printer whose
# authorization has run out refuses every sale with 484 ERR_AUTH_BLOCKED (status commands still work).
#
# Uses the SCU assemblies built by the acceptance test project, so framing, CRC and the transport
# (tcp://host:port or serial://COMx) are the production ones. Build the solution first.
#
#   .\posnet-auth.ps1 -DeviceUrl serial://COM9
#   .\posnet-auth.ps1 -DeviceUrl serial://COM9 -Code 00000000000000
param(
    [string] $DeviceUrl = 'serial://COM9',
    [string] $Code
)

$bin = Join-Path $PSScriptRoot '..\fiskaltrust.Middleware.SCU.PL.AcceptanceTest\bin\Debug\net8.0'
if (-not (Test-Path (Join-Path $bin 'fiskaltrust.Middleware.SCU.PL.PosNet.dll'))) {
    throw "Build scu-pl/fiskaltrust.Middleware.SCU.PL.sln first; no assemblies under $bin."
}
foreach ($assembly in 'System.IO.Ports', 'fiskaltrust.interface', 'fiskaltrust.Middleware.SCU.PL.Abstraction', 'fiskaltrust.Middleware.SCU.PL.PosNet') {
    Add-Type -Path (Join-Path $bin "$assembly.dll")
}

$configuration = [fiskaltrust.Middleware.SCU.PL.PosNet.PosNetConfiguration]::new()
$configuration.DeviceUrl = $DeviceUrl
$configuration.ReceiveTimeoutMs = 15000
$transport = [fiskaltrust.Middleware.SCU.PL.PosNet.Transport.PosNetTransportFactory]::Create($configuration)
$client = [fiskaltrust.Middleware.SCU.PL.PosNet.Client.PosNetClient]::new($transport)

function Send-PosNet([string] $Mnemonic, [string] $Key, [string] $Value) {
    $parameters = [System.Collections.Generic.List[System.Collections.Generic.KeyValuePair[string, string]]]::new()
    if ($Key) { $parameters.Add([System.Collections.Generic.KeyValuePair[string, string]]::new($Key, $Value)) }
    $command = [fiskaltrust.Middleware.SCU.PL.PosNet.Protocol.PosNetCommand]::new($Mnemonic, $parameters)
    try {
        $response = $client.ExecuteAsync($command).GetAwaiter().GetResult()
        $fields = ($response.Parameters.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ' '
        "$Mnemonic -> OK $fields"
    }
    catch {
        $inner = $_.Exception.InnerException ?? $_.Exception
        "$Mnemonic -> $($inner.GetType().Name): $($inner.Message)"
    }
}

try {
    Send-PosNet 'scomm'
    Send-PosNet 'authstateget'
    if ($Code) {
        if ($Code.Length -ne 14) { throw "The authorization code has to be 14 characters long (got $($Code.Length))." }
        Send-PosNet 'auth' 'co' $Code
        Send-PosNet 'authstateget'
    }
}
finally {
    $client.Dispose()
}
