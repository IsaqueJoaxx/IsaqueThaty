# Cota de perfil em planta — Civil 3D 2026 em português

## Carregar e usar
1. No Civil 3D 2026, digite NETLOAD e selecione:
   D:\PROJETA ENGENHARIA\Rotina_Cota_Perfil\bin\Release\net8.0-windows\CotaPerfil.dll
2. Se o Civil solicitar confiança, autorize a pasta da rotina em Locais confiáveis.
3. Digite COTAPERFIL.
4. Clique no alinhamento Civil 3D próximo da estaca desejada e depois no corredor Civil 3D.
5. Se houver várias baselines ligadas àquele eixo, escolha pelo número e nome.
6. Escolha Primitivo (perfil EG de superfície), Projeto (perfis não EG) ou Todos. Enter assume Projeto.
7. Escolha o perfil pelo número. A lista mostra nome real, tipo, limites em metros e qual perfil está usado na baseline. Mesmo com um único perfil, há escolha explícita.
8. Enter usa a estaca do primeiro clique. Para a última estaca, escolha FimEixo ou FimPerfil. Também existem InicioEixo e InicioPerfil.
9. Clique na posição do rótulo em planta.

O resultado é um rótulo nativo de estaca/deslocamento, com texto de referência ao perfil escolhido. Cota com três casas decimais e estaca com duas; o formato de estacas segue as configurações do desenho (no teste, intervalo de 20 m). O afastamento da posição do texto não muda a estaca de consulta. O vínculo é salvo no DWG e é mantido pelo Civil 3D, sem comando de atualização da rotina. A DLL só precisa ser carregada para criar novos rótulos. Não exploda o rótulo que deseja manter associado.

## Limites e escolhas importantes
- Requer Civil 3D 2026 em português, com desenho em metros; não é uma rotina de AutoCAD puro. Os campos foram validados na instalação em português deste computador.
- O eixo deve ser um Alignment e o corredor um Corridor, e ambos precisam estar no desenho atual. Não aceita polilinha, sólido exportado, XREF ou baseline por feature line. Baselines deslocadas que não constem diretamente em Corridor.Baselines não são tratadas.
- Não escolhe perfil pelo nome presumido. Primitivo significa o tipo nativo EG; perfis primitivos importados como outro tipo podem ser encontrados em Todos. Projeto reúne os tipos não EG, mostrando o tipo real para escolha.
- O corredor serve para verificar e identificar a baseline do eixo. A cota vem do perfil explicitamente escolhido, podendo ser diferente do perfil usado na baseline; não é cota da superfície do corredor nem cota de bordo.
- A estaca precisa existir no eixo e no perfil. FimPerfil fora do eixo é recusado, sem aproximar ou extrapolar. FimEixo fora do perfil também é recusado. No modelo GO-220 o perfil acaba em 45196,395 m, mas o eixo acaba em 44970,492 m: o último ponto consultável em planta é FimEixo.
- O rótulo permanece ligado ao mesmo perfil. Trocar o perfil da baseline não substitui silenciosamente essa referência. Se apagar o perfil, precisará selecionar outro pelo recurso nativo do rótulo ou criar novo rótulo.
- A estaca fica associada à posição longitudinal escolhida; as opções FimEixo/FimPerfil escolhem o fim atual. Não representam uma regra para acompanhar futuras extensões do comprimento.
- Perfis referenciados precisam estar válidos e sincronizados no Civil 3D. A rotina não sincroniza atalhos de dados.
- A seleção por cliques e aparência na interface gráfica ainda não foram exercitadas neste ambiente. A compilação, criação nativa, texto avaliado, atualização por alteração de PVI e persistência após salvar/reabrir foram testados no mecanismo instalado do Civil 3D.

## Validação realizada
Cópia usada: D:\PROJETA ENGENHARIA\Rotina_Cota_Perfil\validacao.dwg
Origem: GO-220_Compactacao_95_100.dwg, copiado sem alterar o original.
Perfil: GO-220_VERTICAL (Alterado), eixo GO-220 Executivo R01.

| Ponto | Antes | Após elevar todos os PVIs em 1 m | Após salvar/reabrir |
|---|---:|---:|---:|
| EST. 1129+18,20 | 905,448 | 906,448 | 906,448 |
| EST. 2248+10,49 — fim do eixo | 902,682 | 903,682 | 903,682 |

O texto foi extraído da representação nativa do rótulo antes/depois, sem sobrescrever a cota, sem evento personalizado e sem executar comando de atualização. Na reabertura também foi lido o ObjectId do perfil de referência. O DWG de validação tem o greide elevado somente para demonstrar o teste; não use essa cópia como projeto executivo.
Evidências: teste.txt e reabertura.txt; registros do mecanismo: teste.log e reabrir.log.

## Fonte e recompilação
Fonte: CotaPerfil.cs; projeto: CotaPerfil.csproj. Requer .NET SDK 8 e as bibliotecas instaladas do Civil 3D 2026.
Compilar: dotnet build CotaPerfil.csproj -c Release -p:NoWarn=MSB3277
O SDK relata avisos de versões de bibliotecas da Autodesk quando não suprimidos; a compilação não apresenta erros.
Os comandos de teste ficam protegidos pela definição COTA_VALIDACAO e não são incluídos na DLL de uso. A pasta validacao_bin é exclusiva do teste; seus comandos alteram a cópia e não devem ser executados em projetos.

Referências de API Autodesk:
- https://help.autodesk.com/cloudhelp/2026/ENU/Civil3D-API/files/html/d061e773-7263-165e-619c-b910784733fa.htm
- https://help.autodesk.com/cloudhelp/2021/ENU/Civil3D-API/files/html/554dbeaa-77bf-91cf-66bf-e1d8e19057e3.htm
