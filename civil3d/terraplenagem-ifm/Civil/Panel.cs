using System.ComponentModel;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace Terraplenagem.IFM;

internal sealed class Panel : Form {
 public Project Project {get;private set;} public Result? Result {get;private set;}
 public bool DrawRequested {get;private set;}
 readonly TabControl tabs=new(){Dock=DockStyle.Fill};
 readonly Label status=new(){AutoSize=true,MaximumSize=new Size(1000,0)};
 readonly TextBox title=new(){Width=330};
 readonly NumericUpDown max=new(){Maximum=1000000000,Width=140,DecimalPlaces=1},free=new(){Maximum=1000000000,Width=120,DecimalPlaces=1};
 readonly ComboBox objective=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=240};
 readonly List<DataGridView> grids=new();bool binding;
 public Panel(Project p){
  Project=p;Text="DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM";Width=1280;Height=820;MinimumSize=new Size(950,600);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",9);
  var bar=new FlowLayoutPanel(){Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8),WrapContents=true};
  void Button(string text,Action action){var b=new Button(){Text=text,AutoSize=true,Height=32};b.Click+=(_,_)=>Safe(action);bar.Controls.Add(b);}
  Button("Novo",()=>{if(Confirm("Descartar o cenário atual e iniciar outro?"))Bind(new());});
  Button("Abrir projeto",()=>{using var d=new OpenFileDialog(){Filter="Projeto IFM|*.json"};if(d.ShowDialog()==DialogResult.OK)Bind(Files.Load(d.FileName));});
  Button("Salvar projeto",()=>{Sync();using var d=new SaveFileDialog(){Filter="Projeto IFM|*.json",FileName="terraplenagem-ifm.json"};if(d.ShowDialog()==DialogResult.OK)Files.Save(d.FileName,Project);});
  Button("Exemplo",()=>{if(Confirm("Substituir as entradas por um exemplo fictício?"))Bind(Project.Example());});
  Button("Importar áreas CSV",ImportAreas);
  Button("Gerar origens/aterros",Classify);
  Button("Calcular distribuição",Calculate);
  Button("Exportar CSV",()=>Export(false));Button("Relatório HTML",()=>Export(true));
  Button("Desenhar diagrama",()=>{RequireResult();DrawRequested=true;DialogResult=DialogResult.OK;Close();});
  var settings=new FlowLayoutPanel(){Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8)};
  settings.Controls.AddRange(new Control[]{new Label(){Text="Cenário",AutoSize=true},title,new Label(){Text="DMT máximo (m)",AutoSize=true},max,new Label(){Text="Franquia (m)",AutoSize=true},free,objective});
  objective.Items.AddRange(new object[]{"Minimizar momento total","Minimizar momento extraordinário","Minimizar custo por faixa"});
  var note=new Label(){Dock=DockStyle.Top,Height=57,Padding=new Padding(10),Text="Corte em m³ de banco. Aterro em m³ geométricos × fator banco/geométrico. ADME: capacidade em banco, fator 1. CBR/expansão/permeabilidade são entradas geotécnicas. Revise os valores iniciais antes de calcular."};
  var footer=new FlowLayoutPanel(){Dock=DockStyle.Bottom,AutoSize=true,Padding=new Padding(8)};footer.Controls.Add(status);
  Controls.Add(tabs);Controls.Add(note);Controls.Add(settings);Controls.Add(bar);Controls.Add(footer);
  title.TextChanged+=(_,_)=>InvalidateResult();max.ValueChanged+=(_,_)=>InvalidateResult();free.ValueChanged+=(_,_)=>InvalidateResult();objective.SelectedIndexChanged+=(_,_)=>InvalidateResult();
  Bind(p);
 }
 static bool Confirm(string s)=>MessageBox.Show(s,"DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;
 void Safe(Action a){try{a();}catch(Exception e){MessageBox.Show(e.Message,"DISTRIBUIÇÃO DE TERRAPLENAGEM — IFM",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
 void InvalidateResult(){if(binding)return;Result=null;status.Text="Entradas alteradas. Calcule novamente antes de exportar/desenhar.";foreach(TabPage t in tabs.TabPages.Cast<TabPage>().Where(t=>t.Tag as string=="resultado").ToArray())tabs.TabPages.Remove(t);}
 DataGridView Grid<T>(string name,List<T> data,bool readOnly=false){
  var tab=new TabPage(name);var grid=new DataGridView(){Dock=DockStyle.Fill,AutoGenerateColumns=true,AllowUserToAddRows=!readOnly,AllowUserToDeleteRows=!readOnly,ReadOnly=readOnly,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,DataSource=new BindingList<T>(data)};
  grid.DataError+=(_,e)=>{e.ThrowException=false;MessageBox.Show("Valor inválido. Use números sem separadores de milhar.",Text);};
  if(!readOnly){grid.CellValueChanged+=(_,_)=>InvalidateResult();grid.UserDeletedRow+=(_,_)=>InvalidateResult();grid.UserAddedRow+=(_,_)=>InvalidateResult();grids.Add(grid);}
  tab.Controls.Add(grid);if(readOnly&&name!="Compensação lateral")tab.Tag="resultado";tabs.TabPages.Add(tab);return grid;
 }
 void Bind(Project p){binding=true;Project=p;Result=null;tabs.TabPages.Clear();grids.Clear();title.Text=p.Nome;max.Value=(decimal)Math.Clamp(p.DMT_max_m,0,1e9);free.Value=(decimal)Math.Clamp(p.Franquia_m,0,1e9);objective.SelectedIndex=p.UsarCustos?2:p.MinimizarExtraordinario?1:0;
  Grid("Origens / cortes / jazidas",p.Origens);Grid("Destinos / aterros / ADME",p.Destinos);Grid("Faixas de DMT e custos",p.Faixas);Grid("Rotas e bloqueios",p.Rotas);Grid("Barreiras",p.Barreiras);Grid("Cubação",p.Cubacao);Grid("Compensação lateral",p.CompensacaoLateral,true);
  status.Text="Fonte: "+p.Fonte+". DMT de rota = -1 usa progressivas + acessos; DMT explícita representa rota alternativa às barreiras.";binding=false;
 }
 void Sync(){Validate();foreach(var g in grids){if(!g.EndEdit())throw new Exception("Corrija a célula em edição.");if(g.DataSource is not null)BindingContext?[g.DataSource]?.EndCurrentEdit();}Project.Nome=title.Text;Project.DMT_max_m=(double)max.Value;Project.Franquia_m=(double)free.Value;Project.UsarCustos=objective.SelectedIndex==2;Project.MinimizarExtraordinario=objective.SelectedIndex==1;}
 void ImportAreas(){using var d=new OpenFileDialog(){Filter="CSV de áreas|*.csv;*.txt"};if(d.ShowDialog()!=DialogResult.OK)return;var rows=Files.ReadAreas(File.ReadAllText(d.FileName));Sync();Project.Cubacao=rows;Project.Fonte="CSV: "+d.FileName;Bind(Project);tabs.SelectedIndex=5;}
 void Classify(){Sync();using var form=new Form(){Text="CUBAÇÃO — IFM",Width=530,Height=280,StartPosition=FormStartPosition.CenterParent};var flow=new FlowLayoutPanel(){Dock=DockStyle.Fill,Padding=new Padding(12),FlowDirection=FlowDirection.TopDown};var factor=new NumericUpDown(){DecimalPlaces=4,Minimum=.0001m,Maximum=100,Value=1.13m,Width=150};var length=new NumericUpDown(){Minimum=1,Maximum=100000,Value=200,Width=150};var lateral=new CheckBox(){Text="Compensação lateral: todo corte é compatível com aterro local",AutoSize=true};var go=new Button(){Text="Gerar",DialogResult=DialogResult.OK,AutoSize=true};flow.Controls.AddRange(new Control[]{new Label(){Text="Fator banco / geométrico do aterro (confirme com a geotecnia)",AutoSize=true},factor,new Label(){Text="Comprimento máximo de agrupamento (m)",AutoSize=true},length,new Label(){Text="Substitui origens/destinos. Revise propriedades de cada grupo depois.",AutoSize=true},lateral,go});form.Controls.Add(flow);if(form.ShowDialog(this)!=DialogResult.OK)return;Cubation.Build(Project,Project.Cubacao,(double)factor.Value,(double)length.Value,lateral.Checked);Bind(Project);}
 void Calculate(){Sync();var r=Solver.Run(Project);Result=r;binding=true;foreach(TabPage t in tabs.TabPages.Cast<TabPage>().Where(t=>t.Tag as string=="resultado").ToArray())tabs.TabPages.Remove(t);Grid("Distribuição",r.Movimentos,true);Grid("Pendências / reservas",r.Pendencias,true);binding=false;tabs.SelectedIndex=tabs.TabPages.Count-2;status.Text=$"Distribuído: {r.Distribuido_m3:N3} m³ de banco | Momento: {r.Movimentos.Sum(x=>x.Momento_m3_km):N3} m³·km | {r.Pendencias.Count} pendências/reservas. Revise o relatório.";}
 void RequireResult(){Sync();if(Result is null)throw new Exception("Calcule a distribuição com as entradas atuais primeiro.");}
 void Export(bool html){RequireResult();using var d=new SaveFileDialog(){Filter=html?"Relatório HTML|*.html":"Quadro CSV|*.csv",FileName=html?"terraplenagem-ifm.html":"distribuicao-ifm.csv"};if(d.ShowDialog()!=DialogResult.OK)return;if(html)Report.Html(d.FileName,Project,Result!);else Files.Export(d.FileName,Result!);status.Text="Exportado: "+d.FileName;}
}
