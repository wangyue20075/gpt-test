Write-Host "Scanning for 'bin' and 'obj' folders..." -ForegroundColor Cyan

$folders = Get-ChildItem -Path . -Recurse -Directory -Force -ErrorAction SilentlyContinue |
           Where-Object { $_.Name -eq "bin" -or $_.Name -eq "obj" }

$total = $folders.Count
$deleted = 0

foreach ($folder in $folders) {
    try {
        Write-Host "Deleting: $($folder.FullName)" -ForegroundColor Yellow
        
        # 使用 LiteralPath 避免路径转义问题
        Remove-Item -LiteralPath $folder.FullName -Recurse -Force -ErrorAction Stop
        
        $deleted++
        Write-Host "Deleted successfully." -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to delete: $($folder.FullName)" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor DarkRed
    }
}

Write-Host ""
Write-Host "Completed. Deleted $deleted / $total folders." -ForegroundColor Cyan

Read-Host -Prompt "Press Enter to exit"
