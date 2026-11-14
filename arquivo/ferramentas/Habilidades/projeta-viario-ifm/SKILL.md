---
name: projeta-viario-ifm
description: Aplicar os padrões confirmados da PROJETA em projetos viários e rotinas do Civil 3D, incluindo superfícies, greides, corredores e templates IFM. Usar ao trabalhar nos projetos da PROJETA, sem presumir acesso ao desenho aberto.
---

# PROJETA VIÁRIO — IFM

Use os padrões abaixo nas tarefas viárias da PROJETA. Novas instruções do usuário prevalecem. Leia o AGENTS.md do projeto e os documentos específicos da tarefa antes de alterar rotinas existentes.

## Padrões confirmados

- Assistentes desenvolvidos para o projeto exibem nome e iniciais em maiúsculas: `NOME DO ASSISTENTE — IFM`, inclusive títulos e identificação visível.
- Superfícies: prefixo `1` indica terreno natural/primitivo; `2` indica projeto. Preferir `1 — Primitivo` e `2 — Projeto`. O nome sugere função; não valida a geometria. Não renomear automaticamente. Havendo múltiplos candidatos para uma função, apresentar as opções e obter escolha explícita.
- Greides: rampa longitudinal máxima autorizada de 7%, podendo usar limite menor. O usuário informou DER-MG como referência; não alegar conformidade normativa apenas pelo teto de rampa. O documento acadêmico Controle geométrico de terraplenagem (2012) não é norma oficial.
- O usuário prefere inserir/editar curvas verticais manualmente. Na simplificação, reduzir PIVs desnecessários preservando extremos, teto de rampa e tolerância escolhida. Não confundir PIV do perfil com PI do alinhamento horizontal.
- Selecionar perfis pertencentes ao alinhamento correto. Distinguir terreno EG de projeto FG e conferir a cobertura das estacas. Não transformar a escolha automática legada de um perfil em preferência aprovada pelo usuário.

## Template e estilos

Referência registrada: `C:\Users\User\AppData\Local\Autodesk\C3D 2027\ptb\Template\TEMPLATE TER & INFRA - PROJETA.dwt`.

Existe uma variante `r11`; não substituir a referência por ela sem esclarecer a versão desejada. O usuário informou que `INFRA` identifica estilos preferidos. Quando for solicitado o levantamento, consultar por categoria e incluir estilos filhos; listar os candidatos INFRA. O conteúdo interno da template ainda não foi inspecionado nesta habilidade. Não inventar estilos nem tratar a mera existência do DWT como validação de seu conteúdo.

## Rotinas, instalação e evidências

Projeto principal conhecido: `D:\PROJETA ENGENHARIA\_FERRAMENTAS\Assistente_PROJETA_Civil3D`. Existe uma cópia com sufixo `_Redes`; confirmar qual é a base ativa antes de editar.

Consultar o manifesto instalado em `C:\Program Files\Autodesk\ApplicationPlugins\Projeta.Civil3D.bundle\PackageContents.xml` para identificar a instalação atual. Não fixar a versão mais recente pela memória ou por um documento antigo. Conferir compatibilidade com as DLLs da versão/atualização instalada do Civil 3D.

Ao entregar uma rotina, distinguir: código preparado, compilação concluída, pacote instalado e execução validada no Civil 3D. Só afirmar criação no desenho após evidência da sessão. Ao atualizar instalação já autorizada, preservar backup e conferir manifesto e hashes das DLLs; não encerrar processos com desenhos abertos por iniciativa própria.

O comando `JUNCAOIFM` desta base cria uma curva entre dois trechos retos selecionados, perfil estático entre as cotas dos greides e corredor independente. Raio refere-se ao eixo da ligação. Não representa fusão automática de corredores nem objeto nativo Intersection; curvas verticais e recortes existentes são manuais. Conferir o código/manual atual antes de prometer capacidades adicionais.

## Informações de engenharia ainda não estabelecidas

Não presumir largura de faixa/acostamento, superelevação, velocidade de projeto, raio mínimo, critérios de visibilidade, K de curva vertical, tolerância de simplificação, fatores de volume ou intervalo de seções. Obter apenas os parâmetros necessários à tarefa atual, usando os dados explicitamente aprovados no projeto quando existirem. Diferenciar hipótese de cálculo, preferência do usuário e requisito normativo verificado.

## Uso com integração ao Civil 3D

Esta habilidade orienta decisões; ela não estabelece uma conexão com o Civil 3D. Se houver ferramenta de comunicação disponível, identificar documento e versão e ler os objetos relevantes antes de alterá-los. Se não houver, trabalhar com os arquivos disponíveis e declarar qual validação em sessão permanece pendente. Não afirmar que uma ponte MCP está conectada apenas porque seu instalador ou configuração existe.
