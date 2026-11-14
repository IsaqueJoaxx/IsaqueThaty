(defun c:TXTarea (/ arquivo area) 
(setvar "cmdecho" 0) 
;| verifica se ja existe um arquivo para gravação |; 
(if (not fileName) 
(c:newFileName) 
) 

;| leitura da área |; 
(command "area") 
(setq area (getvar "area")) 

;| gravação no arquivo |; 
(setq arq (open fileName "a")) 
(write-line (rtos area) arq) 
(close arq) 

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

(prompt "Tecle <TAO> para iniciar o comando.")
(princ) 
)






;_________________
;Marcelo Silva - Cascavel - PR 
;cracksther@gmail.com 
;"Se aprende muito ensinando aos outros"
;rubenscandido... veja se é isto que você precisa! 