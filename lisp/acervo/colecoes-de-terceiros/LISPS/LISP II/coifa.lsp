(defun c:coifa ( ) 
(setq oldosmode (getvar "osmode")) ;guarda o valor de osmode na variavel oldosmode 
(setq oldcmdecho (getvar "cmdecho")) ;guarda o valor de cmdecho na variavel oldcmdecho 
(setvar "osmode" 35) ;seta osmode para endpoint, midpoint e intersection 
(setvar "cmdecho" 0) 
(setq pontoini (getpoint "\nEspecifique o Ponto de Início:")) 
(setq comp (getdist "\nEspecifique o Comprimento:")) 
(setq larg (getreal "\nEspecifique a Largura:")) 
(setq alt (getreal "\nEspecifique a Altura:")) 
(setq testa (getreal "\nEspecifique a Altura da Testa:")) 
(command "line" pontoini comp ) 