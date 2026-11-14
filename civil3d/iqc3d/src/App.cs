using System;
using System.Windows.Input;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(IQC3D.App))]
[assembly: CommandClass(typeof(IQC3D.App))]

namespace IQC3D
{
    public class App : IExtensionApplication
    {
        public const string TabId = "IQC3D_TAB";

        public void Initialize()
        {
            try
            {
                if (ComponentManager.Ribbon != null) CriarRibbon();
                else ComponentManager.ItemInitialized += AoIniciarRibbon;
                AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nIQ C3D carregado. Aba \"IQ C3D\" no ribbon; comandos começam com IQ_. IQ_MENU recria a aba.");
            }
            catch { }
        }

        public void Terminate() { }

        private void AoIniciarRibbon(object s, RibbonItemEventArgs e)
        {
            if (ComponentManager.Ribbon == null) return;
            ComponentManager.ItemInitialized -= AoIniciarRibbon;
            CriarRibbon();
        }

        [CommandMethod("IQ_MENU")]
        public void Menu() => CriarRibbon();

        [CommandMethod("IQ_AJUDA")]
        public void Ajuda() => U.Rodar(Ajuda_);
        private void Ajuda_()
        {
            U.Msg("IQ C3D - ferramentas (v0.1)");
            U.Msg(" PONTOS:      IQ_CONVERTERPONTOS  IQ_PONTOS3DPOLY  IQ_APAGARPROXIMOS  IQ_CRIARPOINTGROUPS  IQ_MOVERLABELS");
            U.Msg(" SUPERFÍCIES: IQ_SOLIDOSUPERFICIE  IQ_PERFISSUPERFICIE  IQ_BREAKLINELAYER");
            U.Msg(" ALINHAMENTOS: IQ_MARCAINTERSECOES  IQ_EXTRAIRPOLILINHA  IQ_ESTENDERTANGENTES  IQ_NOTASERVICO");
        }

        public static void CriarRibbon()
        {
            var rib = ComponentManager.Ribbon;
            if (rib == null) return;
            var antigo = rib.FindTab(TabId);
            if (antigo != null) rib.Tabs.Remove(antigo);

            var tab = new RibbonTab { Title = "IQ C3D", Id = TabId };
            rib.Tabs.Add(tab);

            tab.Panels.Add(Painel("Pontos",
                ("Converter\nPontos", "IQ_CONVERTERPONTOS", "Pontos/blocos do AutoCAD <-> pontos COGO"),
                ("Pontos em\n3D Poly", "IQ_PONTOS3DPOLY", "Liga pontos COGO numa polilinha 3D"),
                ("Apagar\nPróximos", "IQ_APAGARPROXIMOS", "Apaga pontos duplicados ou muito próximos"),
                ("Criar Point\nGroups", "IQ_CRIARPOINTGROUPS", "Um point group por descrição"),
                ("Mover\nLabels", "IQ_MOVERLABELS", "Desloca, gira ou reseta rótulos de pontos")));
            tab.Panels.Add(Painel("Superfícies",
                ("Sólido de\nSuperfície", "IQ_SOLIDOSUPERFICIE", "Sólido 3D abaixo da superfície"),
                ("Perfis de\nSuperfície", "IQ_PERFISSUPERFICIE", "Perfil do terreno em vários alinhamentos"),
                ("Breakline\nno Layer", "IQ_BREAKLINELAYER", "Tudo de um layer vira breakline")));
            tab.Panels.Add(Painel("Alinhamentos",
                ("Marca\nInterseções", "IQ_MARCAINTERSECOES", "Marca cruzamentos com as estacas"),
                ("Extrair\nPolilinha", "IQ_EXTRAIRPOLILINHA", "Alinhamento -> polilinha"),
                ("Estender\nTangentes", "IQ_ESTENDERTANGENTES", "Poligonal de PIs"),
                ("Nota de\nServiço", "IQ_NOTASERVICO", "Nota de serviço em Excel")));
            tab.Panels.Add(Painel("Ajuda", ("Comandos", "IQ_AJUDA", "Lista os comandos")));
            tab.IsActive = false;
        }

        private static RibbonPanel Painel(string titulo, params (string texto, string cmd, string dica)[] botoes)
        {
            var src = new RibbonPanelSource { Title = titulo };
            foreach (var b in botoes)
            {
                src.Items.Add(new RibbonButton
                {
                    Text = b.texto,
                    ShowText = true,
                    ShowImage = false,
                    Orientation = System.Windows.Controls.Orientation.Vertical,
                    Size = RibbonItemSize.Large,
                    ToolTip = b.dica,
                    CommandParameter = b.cmd,
                    CommandHandler = new Comando()
                });
            }
            return new RibbonPanel { Source = src };
        }

        private class Comando : ICommand
        {
#pragma warning disable 67
            public event EventHandler CanExecuteChanged;
#pragma warning restore 67
            public bool CanExecute(object p) => true;
            public void Execute(object p)
            {
                var cmd = (p as RibbonButton)?.CommandParameter as string ?? p as string;
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc != null && cmd != null) doc.SendStringToExecute("\x03\x03_" + cmd + " ", true, false, true);
            }
        }
    }
}
