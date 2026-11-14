;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;; GERA UM ARQUIVO TXT COM AS COORDENADAS DOS PONTOS DO DESENHO ;;;
;;;; André Augusto Orsi Dutra - 13/04/00 ;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;; MANDE UM E-mail: meulisp@yahoo.com.br ;;;;;;;
;;;; VISITE A PÁGINA WWW.GEOCITIES.COM/MEULISP ;;;
;;;; LÁ VOCÊ ENCONTRA MUITOS OUTROS APLICATIVOS ;;
;;;; OU ENTRE EM CONTATO PELO FONE 97037749 ;;;;;;

(defun testa()
 (If (Findfile "c:\\ewqerw.m10")
  (PROGN  
   (setq ler (open "c:\\ewqerw.m10" "r"))
   (setq N (read-line ler))
   (CLOSE ler)
   (if (> (distof n) 10)
     (progn
       (princ)(princ)(princ)(princ)
       (Alert "\n\nE ai ?  Está gostando ?  Você ultrapassou seu limite de uso do aplicativo e para continuar usando você deve registrá-lo através de depósito de R$10,00 em conta bancária no Itau - Agência 1685 conta nº 032631.
\n Mande um E-mail para    meulisp@yahoo.com.br   e informe o aplicativo, a data e o valor do depósito que no mesmo dia você estará recebendo o aplicativo registrado em seu E-mail !!!")
       (princ)(princ)(princ)(princ)
       (EXIT)
     );progn
   );if
   (setq incr (+ 0 (distof n)))
   (setq ASC (open "c:\\ewqerw.m10" "w"))
   (write-line (itoa (fix incr)) ASC) 
   (CLOSE ASC)
   (princ)(princ)(princ)(princ)
  );progn
  (PROGN
   (setq ASC (open "c:\\ewqerw.m10" "w"))
   (princ linha)
   (write-line (itoa 1) ASC) 
   (princ)(princ)(princ)(princ)
   (Alert "\n\nVocê está usando o aplicativo pela primeira vez !!! Se achar que ele aumenta sua produtividade, por favor, registre-o !!!
O custo do registro é mínimo R$10,00 (depósito em conta bancária); e você contribui para que profissionais com eu, continuem produzindo os aplicativos !!!
Visite a página    www.geocities.com/meulisp    e baixe outros aplicativos ou envie um E-mail para  meulisp@yahoo.com.br.
Todos os direitos para André Augusto Orsi Dutra.")
   (CLOSE ASC)
   (princ)(princ)(princ)(princ)
  );progn
 );if 
);defun



(defun c:pontos(/ X Y Z XYZ NOME DADOS LINHA ASC CONT N)

   (testa)
   (setq GRUPO (ssget "X" '((0 . "POINT"))))
   (setq NUM 0)
   (IF GRUPO
      (setq NUM (sslength GRUPO))
   )
   (setq CONT 0)
   (setq ASC (open "c:\\pontos.txt" "w"))

   (repeat NUM
    
      (setq NOME (ssname GRUPO CONT))
      (setq DADOS (entget NOME))
      (setq XYZ (cdr (assoc 10 DADOS)))
      (setq X (car XYZ))
      (setq Y (cdr XYZ))
      (setq Y (car Y))
      (setq Z (cddr XYZ))
      (setq Z (car Z))
      (setq X (rtos X))
      (setq Y (rtos Y))
      (setq Z (rtos Z))
      (Setq N (+ 1 CONT))
      (Setq N (Itoa N))
      (setq LINHA (Strcat N ":" X ":" Y ":" Z))
      (write-line LINHA ASC) 
      (setq CONT (+ CONT 1))
   )
   (close ASC)

   (Alert (Strcat "\n\n"(ITOA NUM)" PONTOS ENCONTRADOS NO DESENHO E GRAVADOS EM ARQUIVO!!\n\n"))

   (princ "\n Pontos encontrados: ") (princ N)
   (princ "\n O arquivo PONTOS.TXT esta no arquivo c:/pontos.txt   -")
   (princ "   O formato e N:X:Y:Z com espacos.")
   (princ)
)

