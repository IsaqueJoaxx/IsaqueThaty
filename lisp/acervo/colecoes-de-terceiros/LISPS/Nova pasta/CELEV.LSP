;***> Altera ELEV corrente p/ cota de objeto clicado:  (PPinto 28/06/94)

(Defun c:cel ( / cota ent)
   (setq cota nil
         ent  nil
   )

   (while (= cota nil)
      (while (= ent nil)
         (setq ent (nentsel "\nMudar ELEV para o mesmo da entidade: ")
         )
         (if (not ent)
            (princ "\nPonto invalido!!!")
         )
      )
      (setq ent (entget (car ent))
      )
      (setq cota
         (caddr (cdr (assoc 10 ent)
                )
         )
      )
      (if cota
         (progn
            (command "ELEV"  cota "")
            (princ "\nELEV alterado para cota= ")
            (princ cota)
            (princ)
         )
         (progn
            (princ "\nPonto invalido!!!")
            (setq ent  nil
            )
         )
      )
   )
)
