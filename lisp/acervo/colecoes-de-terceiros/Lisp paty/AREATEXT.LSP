; FROM THE DESK OF PAUL STANDING
; AREA TEXT COMBINED () ATXT.LSP V.1.0
; Nov 9th, 1994

(defun C:at (/ e th val unit fmt pt )
  (setvar "cmdecho" 0)
  (setq e (entsel "\nSelecione o Objeto..."))
  (command "area" "e" e)
  (setq th (Getreal "\nAltura do Texto="))
  (setq VAL (getvar "area"))                            
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
         (princ "Place Text")
         (command "move" "SI" "L" PT pause)
       ); Ends progn 
   ); Ends if statement
   (setvar "textstyle" txt_style)
   (setvar "cmdecho" 1)
   (princ)
); ends atxt.lsp
(prompt "\nType ATXT to envoke the command")

   

