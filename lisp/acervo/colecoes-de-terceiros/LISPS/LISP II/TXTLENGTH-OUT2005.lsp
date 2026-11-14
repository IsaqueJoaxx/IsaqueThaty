(defun c:TXTLENGTH (/ arq sel cont compri obj prop typeObj) 
(setvar "cmdecho" 0) 

(defun *error* (cancel) 
(if cancel 
 
) 

;| verifica se ja existe um arquivo para gravação |; 
(if (not fileName) 
(c:newFileName) 
) 

(setq sel (ssget) 
cont 0 
) 
(while (setq obj (ssname sel cont)) 
(setq prop (entget obj) 
typeObj (cdr (assoc 0 prop)) 
cont (1+ cont) 
) 
(if (= typeObj "LWPOLYLINE") 
(progn 
(command "list" obj) 
(setq compri (getvar "Length")) 

;| gravação no arquivo |; 
(setq arq (open fileName "a")) 
(write-line (rtos compri) arq) 
(close arq) 
) 
 
) 
(princ) 
) 






;| abre janela solicitando local e nome para o arquivo, e grava na variável global fileName, para ser utilizada nas proximas chamadas a função, mantenha a extenção do arquivo como sendo CSV que basta dar dois clique no mesmo para que seja aberto na Excel 
Quando desejar alterar o arquivo basta digitar newFileName|; 
(defun c:newFileName () 
(setq fileName 
(getfiled 
"Indique o local e nome do arquivo" 
(getvar "dwgprefix") 
"csv" 
1 
) 
) 

(princ) 
)