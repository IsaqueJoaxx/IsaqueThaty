# ALINHAMENTO INTELIGENTE — IFM · prévia 0.3

> **Documento histórico.** Não siga os passos abaixo. O conector atual e o roteiro de teste estão em `docs/CONECTOR_04.md`. A afirmação de que o greide é preservado está incompleta: as cotas dos PVIs se mantêm, mas as estacas podem mudar quando o eixo muda de comprimento.

## Comportamento entregue no código

Conector independente compilado para Civil 3D 2027 (.NET 10), sem substituir os componentes PROJETA/GreideIFM. O usuário edita o alinhamento nativo. O conector acompanha o eixo e perfis associados e, se ativado explicitamente, reconstrói apenas o corredor selecionado após concluir a edição. O painel POINT TOP mostra a geometria amostrada, comprimento, perfis e estado do corredor.

Não há reconstrução a cada quadro do arraste: ocorre após soltar o ponto e terminar o comando. Reconstrução pode ser demorada em corredor com frequência de 1 m. O conector consulta IsOutOfDate e só reconstrói quando necessário, depois relê o corredor em outra transação. Ausência de erro não significa conformidade geométrica ou alvos corretos.

Greide e curvas verticais são preservados. Perfis de terreno precisam estar configurados como dinâmicos no Civil 3D para acompanhar terreno. O conector não converte perfis estáticos, não recalcula projeto vertical, não estende regiões, não cria trechos faltantes nem corrige referências/alvos. Seções, superfícies de volume, relatórios e GeoTIFF não são recalculados por este conector. O que acompanha depende dos vínculos nativos existentes; qualquer resultado externo anterior continua referente ao cálculo original.

## Ativar e testar

1. No Civil 3D 2027, em uma cópia de teste, execute NETLOAD e selecione:

       D:\POINT TOP\civil3d\bin\Release\net10.0-windows\PointTop.Civil3D.dll

2. Execute PTVINCULAR. Selecione explicitamente o alinhamento e depois o corredor. O vínculo é recusado se o corredor não possuir linha de base associada ao eixo ou for referência externa. Enter permite acompanhar sem corredor.
3. Escolha Sim para reconstrução automática após edição, ou Nao para acompanhamento. Essa escolha habilita escritas futuras no corredor escolhido.
4. Abra o POINT TOP e escolha Civil 3D · Alinhamento. Se houver múltiplas sessões, selecione a pretendida; não há seleção automática de desenho ambíguo.
5. Mova um PI do alinhamento no Civil 3D. Termine a edição e confira o novo traçado no painel e o estado do corredor. Compare seções e alvos no Civil 3D.
6. Execute PTATUALIZAR para reconstrução/verificação manual; PTDESLIGAR encerra o vínculo. Fechar o desenho também encerra.

Desfazer/refazer suspende a automação para não reconstruir imediatamente o que o usuário desfez. Erros e comandos cancelados também suspendem. Execute PTVINCULAR novamente para reativar. A reconstrução é um comando próprio: pode exigir um desfazer para a reconstrução e outro para a edição do eixo. Não é prometido desfazer único do conjunto; o comportamento precisa de teste em sessão.

## Conexão e limites

Publicação JSON atômica em runtime/civil3d/session-PID.json. Um heartbeat indica que o conector está ocioso e ativo, separadamente da data de leitura da geometria. O painel consulta a cada 2 segundos e trata mais de 15 segundos sem comunicação como desatualizado. Durante comandos longos a comunicação pode ficar desatualizada; isso não prova falha no Civil 3D.

A geometria é amostrada pela API nativa em até 1.001 pontos; não é uma exportação exata de curvas e não deve ser usada para locação. Falhas de estaqueamento, por exemplo em equações de estaca, são contadas. O vínculo é por sessão; reabrir DWG exige vincular novamente.

## Diagnóstico do desenho de teste

Leitura ao vivo em 06/11/2026, sem modificar o desenho:
- Desenho: Ponte Integração Interior Sede] - Traçado 2 - teste.dwg.
- Eixo ALINHAMENTO PONTE INTERGRAÇÃO 02, handle 8E6B5, comprimento 5430.7651486499635 m.
- Perfil Primitivo (EG), extensão de aproximadamente 5430.765 m.
- Greide VERTICAL02 (FG), término em 2558.6539142072525 m.
- Corredor 02, handle 8E70B, vinculado ao eixo e ao VERTICAL02; atualizado, reconstrução automática nativa desativada.
- Uma região de 0 até 2538.006334 m, seção tipo de 7 metros e frequências de 1 m; 2630 estações aplicadas informadas pela API.

O eixo é maior que o greide e o corredor. Isso não foi tratado como erro automático nem corrigido: os limites podem ser intencionais e devem ser conferidos pelo projetista.

## Validação realizada e pendências

DLL compilada com zero erros e zero avisos contra as referências instaladas do Civil 3D 2027. Oito verificações do leitor de sessões e 24 verificações da integração de volumes passaram. Isso não valida os eventos internos do Civil 3D.

A comunicação disponível nesta sessão não possui uma operação para carregar DLLs. Execute C# arbitrário está desabilitado; não foi habilitado. Não foi usada automação de tela para contornar essa limitação. Por isso, carregar a DLL, vincular os objetos e efetivamente arrastar o PI ainda dependem do usuário no Civil 3D. A DLL compilada não deve ser descrita como instalada ou já testada com arraste.

Base técnica: Autodesk Corridor.Rebuild e IsOutOfDate (documentação consultada via Context7), com verificação de compilação nas DLLs locais 2027:
https://help.autodesk.com/cloudhelp/2021/ENU/Civil3D-API/files/html/117d76da-9e87-3947-368e-c3de897023f3.htm
