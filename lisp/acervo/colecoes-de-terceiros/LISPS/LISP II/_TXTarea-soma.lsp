(defun c:TXTarea (/ arq sel selLength cont area obj prop typeObj subTotal lineTXT) 
(setvar "cmdecho" 0) 

(defun *error* (cancel) 
(if cancel 
(princ "\nErro...!") 
) 
) 

;| verifica se ja existe um arquivo para gravação |; 
(if (not fileName) (c:newFileName)) 

(setq sel (ssget) 
cont 0 
arq (open fileName "a") 
subTotal 0 
) 

;| variável global que acumula o total da área |; 
(if (not areaTotal) 
(progn 
(setq areaTotal 0) 
(write-line "INICIO DA SOMA" arq) 
) 
) 

(while (setq obj (ssname sel cont)) 
(setq prop (entget obj) 
typeObj (cdr (assoc 0 prop)) 
cont (1+ cont) 
) 
(if (= typeObj "LWPOLYLINE") 
(progn 
(command "area" "o" obj) 
(setq area (getvar "area") 
subTotal (+ subTotal area) 
areaTotal (+ areaTotal area) 
) 

;| gravação no arquivo |; 
(setq lineTXT (strcat "," (rtos area))) 
(write-line lineTXT arq) 
) 
) 
) 

;| SubTotal desde o ultimo subtotal |; 
(setq lineTXT (strcat "Sub-Total," (rtos subTotal))) 
(write-line lineTXT arq) 

;| Total desde o inicio |; 
;| no arquivo o inicio sera marcado por INICIO DA SOMA |; 
;| todo vez que rodar a 1° vez a rotina sera feito a marca, assim pode-se escolher um arquivo existente |; 
;| se pegar um arquivo existente, vai pedir para substituir, mas na verdade vai acrescentar |; 
(setq lineTXT (strcat "Total," (rtos areaTotal))) 
(write-line lineTXT arq) 

;| linha em branco no arquivo |; 
(write-line " " arq) 

(close arq) 
(princ "\nBy Cracksther") 
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
