param (
    [string]$extensionPath = "C:\Users\PC\AppData\Local\ASCON\Pilot-ICE Enterprise\Development"
)

# Загружаем сборки
$assemblies = @()
Get-ChildItem $extensionPath -Filter "*.dll" | ForEach-Object {
    try {
        $assemblies += [System.Reflection.Assembly]::LoadFrom($_.FullName)
        Write-Host "Loaded: $($_.Name)" -ForegroundColor Green
    } catch {
        Write-Host "Failed: $($_.Name) - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Ищем экспорты MEF
Write-Host "`n=== MEF Exports ===" -ForegroundColor Cyan
foreach ($asm in $assemblies) {
    try {
        $exports = $asm.GetTypes() | Where-Object { 
            $_.GetCustomAttributes($true) | Where-Object { 
                $_.GetType().Name -like "*ExportAttribute*" 
            }
        }
        
        if ($exports) {
            Write-Host "`nAssembly: $($asm.GetName().Name)" -ForegroundColor Yellow
            foreach ($export in $exports) {
                $exportAttrs = $export.GetCustomAttributes($true) | Where-Object { $_.GetType().Name -like "*ExportAttribute*" }
                foreach ($attr in $exportAttrs) {
                    Write-Host "  Export: $($export.Name) -> $($attr.GetType().FullName)"
                }
            }
        }
    } catch {
        Write-Host "Error scanning $($asm.GetName().Name): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
