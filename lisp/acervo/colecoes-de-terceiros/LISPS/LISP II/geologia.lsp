;;;modificacoes nao sao autorizadas,falar com J.Laurenti-DistrSul
;------------------------------------------------------------------------------
(defun c:geologia ()
   (prompt msgdoce) (terpri) 
   (prompt "*Rotina p/alterar cor de entidades-DWG's do Vulcan*versao 1.0-abr97*")
   (princ)
   (initget 7)
   (setq sel (ssget "x")
           num (sslength sel)
           ind 0)
   (while (< ind num)
      (progn
         (setq nome (ssname sel ind)
               tx (entget nome)
               cor (cdr (assoc 62 tx)))
         (cond 
              ((= cor 2) (setq cor 10));hd
              ((= cor 3) (setq cor 6));hm
              ((= cor 4) (setq cor 210));hp
              ((= cor 5) (setq cor 174)); IH 
              ((= cor 6) (setq cor 172)); ID
              ((= cor 7) (setq cor 134)); IM
              ((= cor 8) (setq cor 131)); IP
              ((= cor 9) (setq cor 162)); MD
              ((= cor 10) (setq cor 190));
              ((= cor 11) (setq cor 135));
              ((= cor 12) (setq cor 70));qf
              ((= cor 13) (setq cor 30));in
              ((= cor 14) (setq cor 41));
              ((= cor 15) (setq cor 24));mp
              ((= cor 16) (setq cor 2));nl
              ((= cor 17) (setq cor 23));
              ((= cor 18) (setq cor 62));cg
              ((= cor 19) (setq cor 254));
              ((= cor 20) (setq cor 51));
	      ((= cor 21) (setq cor 52));
	      ((= cor 32) (setq cor 10));
         )
         (setq tx (subst (cons 62 cor) (assoc 62 tx) tx))
         (entmod tx)
         (terpri)
         (setq indi (+ ind 1))
         (prompt (rtos indi 2 0))
         (prompt " Entidades Modificadas.")
         (terpri)(princ)
      )
      (setq ind (1+ ind))
   )
   (prompt "FINAL DAS MODIFICACOES")
   (princ)
)
;------------------------------------------------------------------------------
(defun rop ()
(setq rop (strcat (chr 32) (chr 45) (chr 32) (chr 112) (chr 111) (chr 114) (chr 32))
      itnerual (strcat (chr 74) (chr 46) (chr 76) (chr 97) (chr 117) (chr 114) (chr 101) (chr 110) (chr 116) (chr 105))
      msgdoce (strcat "\n\t  ** DOCEGEO-Distr.Sul" rop itnerual " **  "))
)
(rop)
;------------------------------------------------------------------------------
(prompt "Tecle <geologia> para iniciar o comando.")
(princ)
                        
                 
