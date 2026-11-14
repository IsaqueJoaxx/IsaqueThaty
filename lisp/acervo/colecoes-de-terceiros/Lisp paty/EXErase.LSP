(defun c:EXErase ()
  
  (princ "\nDigite \"EXERASE\" para iniciar a rotina")
  
  (setq PlnLst nil)
  (setq S1 -1)
  (setq PlnSel (entsel "\nSelecione a Polilinha:"))
  (setq PlnGet (entget (car PlnSel)))
  
  (foreach xx PlnGet (if (= 10 (car xx)) (setq S1 (1+ S1) PlnLst (cons (cdr xx) PlnLst))))
  (setq ObjLst (ssget "_WP" PlnLst))
  (setq ObjCnt (rtos (sslength ObjLst) 2 0))
  (command "erase" ObjLst "r" PlnSel "")
;;; (alert (strcat "Pronto Nádia!!! " ObjCnt " objetos foram apagados!!"))
)
  