; TIP904.LSP  Intersection of Line and Pline   LP_INT .LSP (C)1993, Steve Torchinsky

(defun lp_int (EPOLY LP1 LP2 / ENXT PIKPT LST P1 P2 D DD PT0)
   (setq PIKPT (cadr EPOLY)
      ENXT (entnext (car EPOLY))
      LST (entget ENXT)
      P1 (cdr(assoc 10 LST))
      ENXT (entnext ENXT)
      LST (entget ENXT)
      P2 (cdr(assoc 10 LST))
      PT (inters P1 P2 LP1 LP2)
      PT0 PT 
      D 10000000.0
   )
   (while (/= "SEQEND" (cdr (assoc 0 LST)))
      (if (setq PT (inters P1 P2 LP1 LP2))
         (progn
            (setq DD (distance PT PIKPT))
            (if (< DD D)
               (setq PT0 PT
                  D DD
               )
            )
         )
      )
      (setq  ENXT (entnext ENXT)
         LST (entget ENXT)
         P1 P2
         P2 (cdr (assoc 10 LST))
      )
   )
   (setq PT PT0)
); end lp_int.lsp


