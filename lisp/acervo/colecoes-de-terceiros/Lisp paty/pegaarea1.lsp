;--------------------Pega número da estaca e área da Hachura -----------------------
(princ "\nDesenvolvido por Antonio J. Barbosa")
(princ "\nDigite pegaarea")
;
;Este aplicativo pega o número da estaca, a área da hachura e salva em um arquivo txt
;------------------------------------------------------------------------------------
(defun c:pegaarea (/ areas temmais estaca tmarea)
   (defun *error* (msg)
      (progn
	(alert (strcat "\nArquivo criado com sucesso: \n" (rtos cnt 2 0) " linhas adicionadas!" "\n\nÚltima " est))
	(close arquivo)
	(setq *error* nil)
        (princ)
     )
  )
  (if arquivo (close arquivo))
  (setq OsBKP (getvar "osmode"))
  (setq cnt 0)
  (setq DWGInfo (strcat (getvar "dwgprefix") (vl-string-right-trim ".dwg" (getvar "dwgname"))))
  (command "_.STYLE" "ROMANS" "romans" "0" "1.0" "0" "" "" "")
  (setq FileName
	 (getfiled "Arquivo de dados a ser criado" DWGInfo "csv" 1))
  (setq arquivo (open FileName "w"))
;;;  (setq temmais "S")
  (While (/= temmais "N")
     
     ;;;início captura
     (setq Sel (entsel "\nSelecione o texto com o número da estaca:"))
     (if (not Sel)
       (quit)
     )
     (setq ent(car Sel))                
     (setq entl(entget ent))            ; Retorna a lista de associação da entidade 
     (setq est(cdr(assoc 1 entl)))      ; Pega o valor associado ao par "1" (texto)
	(setq estaca
	       (if (vl-string-search "Estaca:" (substr est 1 9))
		 (if (vl-string-search "+" (substr est 9))
		   (setq estaca (vl-string-subst ";+;" "+" (substr est 9)))
		   (setq estaca (strcat (substr est 9) ";+;0"))
		 )
		 (if (and
		       (vl-string-search "+" (substr est 1 9))
		       (not (vl-string-search "Estaca:" (substr est 1 9)))
		     )
		   (setq estaca (vl-string-subst ";+;" "+" (substr est 1)))
		   (setq estaca (strcat (substr est 1) ";;"))
		 )
	       )
	)
    (setq objeto(entsel "\nSelecione a hachura:"))
     (if (not objeto)
       (quit)
       )
     (setq cmddia "0")
     (command "._AREA" "O" objeto)
     (setq temp (getvar "area"))
     (setq areatmp (vl-string-subst "," "." (rtos temp 2 4)))
;;;     (princ areatmp)


     (princ (setq aream(strcat "AREA: " areatmp "m²")))
     ;(princ tmparea)
     (setvar "osmode" 1)
     (SETQ pt (GETPOINT "\nPonto de inserção : "))
     (if (not pt)
       (quit)
     )
     (command ".text" "j" "bc" pt 1.0 0 aream)
     (setvar "osmode" OsBKP)
     ;;;término captura
    
 

    (princ estaca arquivo)
    (princ ";" arquivo)
;;;    (princ ";" arquivo)
;;;    (princ ";" arquivo)
    (princ areatmp arquivo)
    (princ (chr 10) arquivo)
    (princ (chr 10) arquivo)
    (setq cnt (1+ cnt))
;;;    (setq temmais (strcase (getstring "\nOutra estaca?<S,N>")))
  )
;;;  (princ "END" arquivo)
  (close arquivo)
  (princ (strcat "\nArquivo criado com sucesso: " (rtos cnt 2 0) " linhas adicionadas!"))
  (princ)
;;;  (setvar "osmode" 0)
)