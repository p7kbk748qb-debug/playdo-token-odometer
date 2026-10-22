param([int]$ProcessId,[int]$Seconds=20)
$ErrorActionPreference='Stop'
$before=Get-Process -Id $ProcessId
$cpu=$before.TotalProcessorTime.TotalSeconds
$clock=[Diagnostics.Stopwatch]::StartNew()
Start-Sleep -Seconds $Seconds
$after=Get-Process -Id $ProcessId
$elapsed=$clock.Elapsed.TotalSeconds
$result=[ordered]@{elapsedSeconds=[math]::Round($elapsed,2);cpuSeconds=[math]::Round($after.TotalProcessorTime.TotalSeconds-$cpu,4);cpuPercentOfMachine=[math]::Round((($after.TotalProcessorTime.TotalSeconds-$cpu)/$elapsed/[Environment]::ProcessorCount)*100,4);logicalProcessors=[Environment]::ProcessorCount;workingSetMB=[math]::Round($after.WorkingSet64/1MB,2);privateMemoryMB=[math]::Round($after.PrivateMemorySize64/1MB,2);handles=$after.HandleCount}
$result|ConvertTo-Json
