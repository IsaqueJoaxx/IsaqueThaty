;TIP913.LSP : TNM.LSP  Identifying the Creator  (c)1993, Thomas N. Meccia

;This file (begining with line 9) contains the three commands needed
;to encode and read both codes.  Publication of only this portion
;is enough to use the coding process.
;*****************************************************************/

(DEFUN C:TNM ()
   (command "zoom" "ALL")
   (command "color" "red")
   (command "text" ".0000005,.0000005"".000001""0")
   (command (getstring "\nEnter Student's Initials"))
   (COMMAND "COPY" "window"".0000003,.0000001"".000004,.000002""si""0,0""0,8")
   (command "color" "bylayer")
   (COMMAND "ZOOM" "P")
); end tnm.lsp

(defun C:read ()
   (COMMAND "ZOOM" "0,0" "5,3")
   (COMMAND "REGEN")
   (COMMAND "ZOOM" "0,0" ".5,.3")
   (COMMAND "REGEN")
   (COMMAND "ZOOM" "0,0" ".05,.03")
   (COMMAND "REGEN")
   (COMMAND "ZOOM" "0,0" ".0005,.0003")
   (COMMAND "REGEN")
   (COMMAND "ZOOM" "0,0" ".000006,.000003")
); end read.lsp

(DEFUN C:READTWO ()
   (COMMAND "ZOOM" "0,8" ".05,8.03")
   (COMMAND "REGEN")
   (COMMAND "ZOOM" "0,8" ".0005,8.0003")
   (COMMAND "REGEN")
   (COMMAND "ZOOM" "0,8" ".000006,8.000003")
); end readtwo.lsp

