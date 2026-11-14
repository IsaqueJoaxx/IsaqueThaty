param([string]$arq, [string]$pdf)
$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$xl.DisplayAlerts = $false
try {
    $wb = $xl.Workbooks.Open($arq, 0, $true)
    foreach ($ws in $wb.Worksheets) { "aba " + $ws.Name + ": usado " + $ws.UsedRange.Address() + " | paginas " + $ws.PageSetup.Pages.Count + " | titulos " + $ws.PageSetup.PrintTitleRows }
    $wb.ExportAsFixedFormat(0, $pdf, 0, $true, $false)
    "pdf: " + (Get-Item $pdf).Length
    $wb.Close($false)
} catch { "ERRO: " + $_.Exception.Message }
finally { $xl.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xl) }
