param([string]$arq, [string]$pdf)
$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$xl.DisplayAlerts = $false
try {
    $wb = $xl.Workbooks.Open($arq, 0, $true)
    "aberto: " + $wb.Name + " | janela: " + $xl.ActiveWindow.Caption
    $ws = $wb.Worksheets.Item(1)
    "planilhas: " + ($wb.Worksheets | ForEach-Object { $_.Name }) -join ", "
    "usado: " + $ws.UsedRange.Address()
    "F6=" + $ws.Range("F6").Text + " | I7=" + $ws.Range("I7").Text + " | A6=" + $ws.Range("A6").Text + " | E6=" + $ws.Range("E6").Text + " | K6=" + $ws.Range("K6").Text
    "titulos impressao: " + $ws.PageSetup.PrintTitleRows + " | paginas: " + $ws.PageSetup.Pages.Count
    $ws.ExportAsFixedFormat(0, $pdf, 0, $true, $false, 1, 1)
    "pdf: " + (Get-Item $pdf).Length
    $wb.Close($false)
} catch { "ERRO: " + $_.Exception.Message }
finally { $xl.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xl) }
