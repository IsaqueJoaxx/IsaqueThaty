(defun c:cof ( / #dist dist ent spt pti dist) 
(command "undo" "begin") 
(setq #dist (getdist "\n Distância do offset: " ));captura a distância 
(setq n (getint "\n Quantidade de Offsets: "));captura a qtde. de offsets 
(while 
(not (setq ent (entsel "\n Selecione objeto para Offset: ")))) 
(setq pti (osnap (cadr ent) "_nea"));;while (enquanto não seleciona sel, continue selecionando 
(setq spt (getpoint pti "\n Lado do Offset"));captura lado do offset 
(setq dist #dist) 
(repeat n ;repete n vezes a qtde. fornecida em n 
(command "_.offset" dist ent spt "");;faz o offset 
(setq dist (+ dist #dist));incrementa a distância para usar com o próximo offset 
) 
(princ "\n Pronto!") 
(command "undo" "end") 
(princ))