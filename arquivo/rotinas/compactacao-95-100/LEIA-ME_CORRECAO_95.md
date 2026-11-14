# Correção de exibição GO-220 — 10/10/2026

Causa: os objetos COMP_95 e COMP_100 estavam com Visible=false, embora as camadas estivessem ligadas e descongeladas. Os dois DWGs completos inspecionados apresentavam o mesmo defeito. A seção-tipo não contém classificação de compactação.

Correção: reconstruídos os preenchimentos de compactação e materiais nas posições atuais das 358 vistas, com base nos mesmos polígonos de cálculo e perfis já registrados. Adicionadas 76 hachuras diagonais ANSI31 de 95%, em camada exclusiva COMP_95_HACHURA, visíveis sobre os materiais; mantidas as 3.241 regiões de 100%. Cores dos materiais preservadas: sub-base azul superior 0,20 m; base laranja 0,20 m; reforço cinza inferior 0,60 m. Taludes 1,5H:1V e largura TOP 13,40 m.

Tabelas: 358 tabelas com conteúdo mantido, margens internas reduzidas e afastamento de 12 m da borda direita de cada vista para liberar também os rótulos de elevação. Verificação dos retângulos gerados: nenhuma sobreposição entre tabelas ou com vistas, inclusive vizinhas.

Validação: DWG reaberto no AutoCAD; 76 hachuras visíveis confirmadas; auditoria com zero erros. Amostra plotada da estaca 0 conferida visualmente, mostrando as hachuras sobre o reforço e a tabela afastada. Somente trechos 0–300 e 350–400, incluindo intermediárias; intervalo 300–350 excluído. Cálculos e contornos de compactação preservados; não houve recálculo de movimento de terra. Objetos estáticos sem vínculo automático às superfícies. Os originais permanecem preservados.
