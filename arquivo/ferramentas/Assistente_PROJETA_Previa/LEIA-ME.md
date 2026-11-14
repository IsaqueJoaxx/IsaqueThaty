# Assistente PROJETA — prévia 01

Abra index.html em um navegador. Não exige instalação, internet nem servidor. O painel demonstra uma proposta de experiência para um futuro complemento do Civil 3D 2027; não é um complemento instalado.

## Roteiro de revisão

1. Explore as ferramentas na navegação e prepare uma solicitação de volumes com terreno natural, projeto e limite.
2. Em Drenagem, cadastre identificação, área positiva e unidade explícita. O separador decimal pode ser vírgula ou ponto, sem separador de milhar.
3. Cadastre outra bacia, confira a lista, recarregue a página e exporte o cadastro JSON. Identificações repetidas são recusadas. A exclusão remove o registro do cadastro local.
4. Use a conversa para abrir os fluxos. As respostas são regras demonstrativas locais, sem modelo de IA conectado.

## Dados e limites

O cadastro é salvo no armazenamento local do navegador, quando disponível. Não é compartilhado entre navegadores ou computadores; exporte uma cópia para preservação. O contorno é uma referência textual opcional, sem importação de geometria. Não há bacias, dados de chuva ou cálculos exemplificativos pré-carregados. Os desenhos esquemáticos são apenas ilustrativos. As referências dos formulários de projeto e a conversa não são persistidas.

Nenhum arquivo de projeto é consultado, nenhum desenho é alterado e nenhum cálculo de engenharia é realizado. O protótipo não fornece resultados de volume, vazão, dimensionamento ou taper.

## Caminho para o complemento real

1. Receber a planilha original, suas fórmulas, critérios, fontes e séries de chuva, unidades e casos já conferidos. Mapear entradas, resultados e validações antes de implementar cálculos.
2. Confirmar a arquitetura e as APIs compatíveis com o Civil 3D 2027 na documentação oficial e em ambiente de desenvolvimento separado. Desenvolver painel acoplável e adaptador para ler o documento ativo e selecionar objetos.
3. Implementar primeiro a leitura das superfícies e limites e validar os resultados de volumes com um desenho de referência. Adicionar prévia de operações, confirmação e transações com desfazer para alterações.
4. Incorporar os cálculos validados de drenagem ao painel, mantendo origem dos dados e rastreabilidade. Definir formato de entrada das bacias e, opcionalmente, contornos do InfraWorks.
5. Implementar e testar seções, corredores, cruzamentos e convergências em etapas, com critérios geométricos fornecidos e revisão do responsável técnico.

Civil 3D não foi instalado ou alterado. Nenhuma credencial foi usada.
