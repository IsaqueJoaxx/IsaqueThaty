$S = Join-Path (Split-Path $PSScriptRoot) 'src'
$u = New-Object Text.UTF8Encoding($false)
function Troca($arq, $de, $para) {
  $f = Join-Path $S $arq; $t = [IO.File]::ReadAllText($f)
  $de2 = $de -replace "`r`n", "`n"; $t2 = $t -replace "`r`n", "`n"
  if (-not $t2.Contains($de2)) { "NAO ACHEI em ${arq}: $($de.Substring(0,[Math]::Min(60,$de.Length)))"; return }
  $t2 = $t2.Replace($de2, ($para -replace "`r`n", "`n")); [IO.File]::WriteAllText($f, $t2, $u); "ok $arq"
}
# 1) estaca sem "-0" e com arredondamento correto
Troca 'Util.cs' @'
            int est = (int)Math.Floor(station / passo + 1e-9);
            double resto = station - est * passo;
            if (resto < 0) resto = 0;
            return est.ToString(BR) + "+" + resto.ToString("0.00", BR);
'@ @'
            double cm = Math.Round(station * 100.0);           // trabalha em centímetros
            if (cm < 0) cm = 0;
            long passoCm = (long)Math.Round(passo * 100.0);
            long est = (long)cm / passoCm;
            double resto = (cm - est * passoCm) / 100.0;
            return est.ToString(BR) + "+" + resto.ToString("0.00", BR);
'@
# 2) palavras-chave: o AutoCAD já mostra [A/B] sozinho
Troca 'Util.cs' @'
            var o = new PromptKeywordOptions("\n" + msg);
'@ @'
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"\s*\[[^\]]*\]\s*$", "");
            var o = new PromptKeywordOptions("\n" + msg);
'@
# 3) salvar a planilha na pasta do desenho (ou em Documentos)
Troca 'Alinhamentos.cs' @'
            var sfd = new Autodesk.AutoCAD.Windows.SaveFileDialog("Salvar nota de serviço", $"Nota de servico - {nomeAl}.xlsx", "xlsx", "IQNotaServico", Autodesk.AutoCAD.Windows.SaveFileDialog.SaveFileDialogFlags.DoNotTransferRemoteFiles);
'@ @'
            string pasta = null;
            try { if (U.Db.Filename != null && File.Exists(U.Db.Filename) && !U.Db.Filename.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)) pasta = Path.GetDirectoryName(U.Db.Filename); } catch { }
            if (pasta == null) pasta = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var sfd = new Autodesk.AutoCAD.Windows.SaveFileDialog("Salvar nota de serviço", Path.Combine(pasta, $"Nota de servico - {nomeAl}.xlsx"), "xlsx", "IQNotaServico", Autodesk.AutoCAD.Windows.SaveFileDialog.SaveFileDialogFlags.DoNotTransferRemoteFiles);
'@
