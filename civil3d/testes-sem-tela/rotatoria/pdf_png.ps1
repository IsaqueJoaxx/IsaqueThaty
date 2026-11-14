param([string]$Pdf, [string]$Png, [int]$Largura = 1600)
# Renderiza a 1ª página de um PDF em PNG com a API do próprio Windows (Windows.Data.Pdf), sem instalar nada.
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
$null = [Windows.Data.Pdf.PdfDocument, Windows.Data.Pdf, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.InMemoryRandomAccessStream, Windows.Storage.Streams, ContentType = WindowsRuntime]
$asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
$asTaskAction = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' })[0]
function Await($op, [Type]$t) { $task = $asTask.MakeGenericMethod($t).Invoke($null, @($op)); $task.Wait() | Out-Null; $task.Result }
function AwaitAction($op) { $task = $asTaskAction.Invoke($null, @($op)); $task.Wait() | Out-Null }
$file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($Pdf)) ([Windows.Storage.StorageFile])
$doc = Await ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($file)) ([Windows.Data.Pdf.PdfDocument])
$page = $doc.GetPage(0)
$opt = New-Object Windows.Data.Pdf.PdfPageRenderOptions
$opt.DestinationWidth = $Largura
$ms = New-Object Windows.Storage.Streams.InMemoryRandomAccessStream
AwaitAction ($page.RenderToStreamAsync($ms, $opt))
$net = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($ms.GetInputStreamAt(0))
$fs = [System.IO.File]::Create($Png)
$net.CopyTo($fs)
$fs.Close()
"ok $Png"
