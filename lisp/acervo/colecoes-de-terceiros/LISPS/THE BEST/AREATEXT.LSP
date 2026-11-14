; FROM THE DESK OF PAUL STANDING
; AREA TEXT COMBINED () ATXT.LSP V.1.0
; Nov 9th, 1994
(princ "\n----- by RJRENGENHARIA  -----  http://rjrengenharia.com.br/  -----" )
(princ "\n----- rjrengenharia.com.br -- rubinhoengenheiro@yahoo.com.br -----" )
(princ "\n----- Digite \"ATXT para Iniciar Comando...\"")
(defun C:atxt (/ e th val unit fmt pt )
  (setvar "cmdecho" 0)
  (setq e (entsel "\nPlease select an entity..."))
  (command "area" "e" e)(setq th (* 2.0 (getvar "dimscale")))
  (setq VAL (getvar "area" ))                               
  (setq unit (getvar "lunits"))
  (setq txt_style (getvar "textstyle"))
  (command "style" "standard" "" "" "" "" "" "" "")
    (if (or(= unit 4)(= unit 5))
       (progn
         (setq val (/ val 144))
         (setq fmt (strcat (rtos val 2 ) " Sq Ft")) 
         (setq PT (getvar "viewctr"))
         (command "text" PT TH "0" Fmt)
         (princ "Place Text")
         (command "move" "SI" "L" PT Pause)
       );ends progn
       (progn
         (setq fmt (rtos val))
         (setq PT (getvar "viewctr"))
         (command "text" PT TH "0" Fmt)
         (princ " Place Text")
         (command "move" "SI" "L" PT pause)
       ); Ends progn 
   ); Ends if statement
   (setvar "textstyle" txt_style)
   (setvar "cmdecho" 1)
   (princ)
); ends atxt.lsp
;(princ "\n------- Digite \"ATXT para iniciar comando...\"")
;(prompt "\nType ATXT to envoke the command")

   

