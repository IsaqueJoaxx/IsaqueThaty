;;;--------------------------------------DELETAR POLYLINE's DA MESMA COR NA SEÇÃO-----------------------------
(princ "\nDesenvolvido por Daniel Roberto R. dos Santos")
(princ "\nTrabalhando para tornar seu serviço mais agradavel")
(princ "\nDigite: ALT ")
(defun C:alt ( / PARAMETRO sel cnt num ELEMENTO CAMADA SS)
  (SETQ ECHO (GETVAR "CMDECHO"))
  (SETQ DYN (GETVAR "DYNMODE"))
  (SETVAR "CMDECHO" 0)
  (SETVAR "DYNMODE" 3)
  (defun *error* (msg)
    (progn
      (setvar "CMDECHO" ECHO)
      (SETVAR "DYNMODE" DYN)
      (setq *error* nil)
      (princ)
    )
  )
(setq PARAMETRO (getstring "\nDigite-> 1 - Deleta Elemento, 2 - Muda a Cor, 3 - Mover, 4 - Copiar: "))
(cond ((EQUAL (STRCASE PARAMETRO) "1")(DELETAR))
      ((EQUAL (STRCASE PARAMETRO) "2")(CR))
      ((EQUAL (STRCASE PARAMETRO) "3")(MOVER))
      ((EQUAL (STRCASE PARAMETRO) "4")(COPIAR))
      )
(SETVAR "CMDECHO" ECHO)
(SETVAR "DYNMODE" DYN)
  );;;DEFUN

;;;deleta elemento
(defun deletar ()
  (princ "\nSelecione os objetos que deseja deletar")
  (setq sel (ssget))
  (setq num (sslength sel))
  (setq cnt 0)
  (repeat num
    (setq ELEMENTO (cdr (assoc 62 (entget (ssname sel cnt)))))
    (setq CAMADA (strcat "" (cdr (assoc 8 (entget (ssname sel cnt))))))
    (setq cnt (+ 1 cnt))
    (princ "\nSelecione o gupo de seleção")
    (setq SS (ssget (list (cons 0 "LWPOLYLINE,TEXT")(VL-LIST* 8 CAMADA)(VL-LIST* 62 ELEMENTO))));SELECIONA O ELEMENTO A SER DELETADO
    (VL-CMDF "ERASE" SS "");APAGA O ELEMENTO
  );repeat
);;;Deletar,

;;;Mudar Cor
(defun cr ()
  (princ "\nSelecione os objetos que deseja mudar a cor")
  (setq sel (ssget))
  (setq num (sslength sel))
  (setq cnt 0)
  (setq cor (getstring "\nDigite o numero ou o nome da cor: "))
  (repeat num
    (setq ELEMENTO (cdr (assoc 62 (entget (ssname sel cnt)))))
    (setq CAMADA (strcat "" (cdr (assoc 8 (entget (ssname sel cnt))))))
    (setq cnt (+ 1 cnt))
    (princ "\nSelecione o gupo de seleção")
    (setq SS (ssget (list (VL-LIST* 0 "LWPOLYLINE,TEXT")(VL-LIST* 8 CAMADA)(VL-LIST* 62 ELEMENTO))));SELECIONA O ELEMENTO A SER ALTERADO
    (VL-CMDF "CHANGE" SS "" "P" "C" COR "");MUDA COR
  )
);;;DEFUN

;;;MOVER
(defun mover ()
  (princ "\nSelecione os objetos que deseja mover")
  (setq sel (ssget))
  (setq num (sslength sel))
  (setq cnt 0)
  (repeat num
    (setq ELEMENTO (cdr (assoc 62 (entget (ssname sel cnt)))))
    (setq CAMADA (strcat "" (cdr (assoc 8 (entget (ssname sel cnt))))))
    (setq cnt (+ 1 cnt))
    (setq SS (ssget (list (cons 0 "LWPOLYLINE,TEXT")(VL-LIST* 8 CAMADA)(VL-LIST* 62 ELEMENTO))));SELECIONA O ELEMENTO A SER MOVIDO
    (command "move" ss "");MOVE O ELEMENTO
  );repeat
);;;Deletar

;;;copiar e colar
(defun copiar ()
  (princ "\nSelecione os objetos que deseja copiar")
  (setq sel (ssget))
  (setq num (sslength sel))
  (setq cnt 0)
  (repeat num
    (setq ELEMENTO (cdr (assoc 62 (entget (ssname sel cnt)))))
    (setq CAMADA (strcat "" (cdr (assoc 8 (entget (ssname sel cnt))))))
    (setq cnt (+ 1 cnt))
    (princ "\nSelecione o gupo de seleção")
    (setq SS (ssget (list (cons 0 "LWPOLYLINE,TEXT")(VL-LIST* 8 CAMADA)(VL-LIST* 62 ELEMENTO))));SELECIONA O ELEMENTO A SER COPIADO
    (command "copy" ss "");copia O ELEMENTO
  );repeat
);;;Deletar