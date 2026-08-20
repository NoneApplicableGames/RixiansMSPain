$ProjectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $ProjectRoot

$ExePath = "./.game/SlayTheSpire2.exe"
$TotalPlayers = 2

Start-Process $ExePath -ArgumentList "-fastmp host_standard"

$NumClients = $TotalPlayers - 1

for ($i = 1; $i -le $NumClients; $i++) {
    $cid = 1000 * $i
    Start-Process $ExePath -ArgumentList "-fastmp join -clientId $cid"
}