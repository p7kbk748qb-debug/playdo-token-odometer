param([Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'

# Spread the published Codex example over a rolling week so native chart checks
# exercise real date buckets on any runner. Keep its known aggregate unchanged.
$source = Join-Path $PSScriptRoot 'fixtures'
New-Item -ItemType Directory -Path $Destination | Out-Null
foreach ($provider in @('claude', 'gemini')) {
    Copy-Item -LiteralPath (Join-Path $source $provider) -Destination $Destination -Recurse
}
$sessions = Join-Path $Destination 'codex/sessions'
New-Item -ItemType Directory -Path $sessions -Force | Out-Null
$example = Get-Content -LiteralPath (Join-Path $source 'codex/sessions/demo.jsonl')
$context = $example[0] | ConvertFrom-Json
$event = $example[1] | ConvertFrom-Json
$totals = ($example[1] | ConvertFrom-Json).payload.info.total_token_usage
$now = [DateTimeOffset]::Now
$weights = @(6, 16, 9, 26, 12, 21, 10)
$cumulative = 0
$lines = for ($i = 0; $i -lt $weights.Count; $i++) {
    $stamp = $now.AddDays($i - 6)
    if ($i -eq 0) {
        $context.timestamp = $stamp.AddSeconds(-1).UtcDateTime.ToString('o')
        $context | ConvertTo-Json -Depth 8 -Compress
    }
    $cumulative += $weights[$i]
    $event.timestamp = $stamp.UtcDateTime.ToString('o')
    foreach ($field in $totals.PSObject.Properties.Name) {
        $full = $totals.$field
        $count = [long][Math]::Floor($full * $cumulative / 100.0)
        $previous = [long][Math]::Floor($full * ($cumulative - $weights[$i]) / 100.0)
        $event.payload.info.last_token_usage.$field = $count - $previous
        $event.payload.info.total_token_usage.$field = $count
    }
    $event.payload.rate_limits.primary.resets_at = $stamp.AddHours(3).ToUnixTimeSeconds()
    $event.payload.rate_limits.secondary.resets_at = $stamp.AddDays(4).ToUnixTimeSeconds()
    $event | ConvertTo-Json -Depth 8 -Compress
}
[IO.File]::WriteAllLines((Join-Path $sessions 'demo.jsonl'), [string[]]$lines)
