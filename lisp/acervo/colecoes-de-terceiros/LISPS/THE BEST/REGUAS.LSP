;
;        ***> Seta estilos padrao de reguas <***
;             (se nao estao definidos, cria)
;                                                  PPinto 29/07/94

(defun C:Reguas ( / escala unidade fonte fator oldech olderr
                )

   (defun MStyl (nome fonte altura fator obliq)
      ; Verifica estilo
      (if (or
             (not (tblsearch "STYLE" nome))                          ; Se nao existe ou
             (/= (cdr (assoc 40 (tblsearch "STYLE" nome))) altura)   ; Se altura <> padrao
          )
          (command "STYLE" nome fonte altura fator obliq "N" "N" "N"
          )
      )
   )


   ; ***> Captura variaveis ambientais:
   (setq oldech (getvar "CMDECHO")
         olderr *ERROR*
   )

   ; ***> Configura ambiente:
   (setvar "CMDECHO" 0)

   ; ***> Define nova rotina para tratamento de erros:
   (defun *ERROR* (msg)
      (terpri)
      (terpri)
      (princ msg)
      (setvar "CMDECHO" oldech)
      (setq *ERROR*      olderr)        ; Restaura funcao de erros original
   )

   ;***> Captura escala de trabalho:
   (initget 7)
   (setq escala (getreal "\nDefinir estilos p/ escala: ")
   )

   ;***> Captura unidade de trabalho:
   (initget 7 "ME CE MI")
   (setq unidade
         (getkword "\nUnidade de trabalho (MEtro,CEntimetro MIlimetro): ")
   )
   (if (= unidade "ME")
      (setq fator 1000.0)
      (if (= unidade "CE")
         (setq fator 100.0)
         (setq fator   1.0)
      )
   )

   ;***> Captura fonte:
   (setq fonte (getstring "\nFonte a ser usada:<ROMANS> ")
   )
   (if (= fonte "")
      (setq fonte "ROMANS")
   )


   ; Seta reguas p/ escala e unidade definida
   (MStyl "R50"  fonte (* 1.2   (/ escala fator)) 1 0)
   (MStyl "R60"  fonte (* 1.5   (/ escala fator)) 1 0)
   (MStyl "R80"  fonte (* 2.0   (/ escala fator)) 1 0)
   (MStyl "R100" fonte (* 2.5   (/ escala fator)) 1 0)
   (MStyl "R120" fonte (* 3.0   (/ escala fator)) 1 0)
   (MStyl "R140" fonte (* 3.5   (/ escala fator)) 1 0)
   (MStyl "R175" fonte (* 4.375 (/ escala fator)) 1 0)
   (MStyl "R200" fonte (* 5.0   (/ escala fator)) 1 0)
   (MStyl "R240" fonte (* 6.0   (/ escala fator)) 1 0)
   (MStyl "R290" fonte (* 7.5   (/ escala fator)) 1 0)
   (MStyl "R350" fonte (* 9.0   (/ escala fator)) 1 0)
   (MStyl "R425" fonte (* 10.0  (/ escala fator)) 1 0)
   (MStyl "R500" fonte (* 12.0  (/ escala fator)) 1 0)


   ; ***> Restaura ambiente original:
   (setvar "CMDECHO" oldech)
   (setq *ERROR*      olderr)        ; Restaura funcao de erros original
   (princ)
   (setq MStyl nil)
)

