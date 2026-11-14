; FILE MANAGER FOR AUTOCAD DRAWING FILES
(defun C:FM ( / DLIST DWGNAM FID KEY NEWNAM REC SAVC)
        (princ "\nFM - File manager for Drawing Files.\n")
        (setq SAVC (getvar "cmdecho"))
        (setvar "cmdecho" 0)
        (setq *error* PERR)
        (command "shell" "DIR *.DWG>D$LIST")
        (graphscr)       ; switch to graphic screen
        (setq DLIST (open "D$LIST" "r"))
        (repeat 5      ; skip first 4 records in DIR list
        (setq REC (read-Line DLIST)))
        (while (and REC (/=" " (substr REC 1 1)))
               (setq DWGNAM (getnam REC))
               (princ "\n") (princ REC)
               (princ "\nNext/View/Delete/Copy/Rename/exit<v>:")
               (setq KEY (getkey))         ; get key pressed
               (cond
                      ((= KEY "N")          ; Next drawing
                              (setq REC (read-line DLIST))
                      )
             ((or (=  KEY "V") (= KEY "\r"))  ; View current drawing
                  (if (setq FID (open (strcat DWGNAM  ".SLD") "r"))
                      (progn
                          (close FID) ; close the file if it exists
                           (command "vslide" DWGNAM)
                           (grtext -1 REC)
                           (grtext -2 "")
                           (setq REC (read-line DLIST))
                  )
                  (princ (strcat "\nNo slide file for " DWGNAM))
            )
       )
       ((= KEY "D")               ; Delete current drawing
               (setq DWGNAM (strcat DWGNAM ".DWG"))
               (princ (strcat "\nDelete " DWGNAM " ? (Y/N)<N>:"))
               (if (= "Y" (getkey))
                     (progn
                              (command "files" "3" DWGNAM "" "0")
                              (princ (strcat DWGNAM " deleted"))
                              (setq REC (read-line DLIST))
                 )
                (princ (strcat DWGNAM "not deleted"))
            )
       )
       ((= KEY "C")              ; Copy current drawing
               (setq DWGNAM (strcat DWGNAM "DWG"))
            (setq NEWNAM (getstring "\n Enter destination name : "))
            (while NEWNAM
                  (setq NEWNAM (strcat NEWNAM ".DWG"))
      (princ (strcat "\nCopy " DWGNAM " to " NEWNAM " ? (Y/N)<N>:"))
     (if (= "Y" (getkey))
           (command "files" "5" DWGNAM "" "0")
     )
     (setq NEWNAM nil)
     )
   )
   ((= Key "R")              ; Rename current drawing
           (setq DWGNAM (strcat DWGNAM ".DWG"))
           (setq NEWNAM (getstring "\nEnter new name : "))
           (while NEWNAM
                  (setq NEWNAM (strcat NEWNAM ".DWG"))
    (princ (strcat "\nRename " DWGNAM " as " NEWNAM " ? (Y/N)<N>:"))
        (if (= "Y" (getkey))
              (command "files" "4" DWGNAM NEWNAM "0")
         )
         (setq NEWNAM nil)
        )
     )
     ((=KEY "X")             ;exit File Manager
          (setq REC nil)     ;to exit from while Loop
     )
     ((boundp KEY)
         (princ "\nEnter N,V,D,C,R or X please")
     )
 )        ; end of 'cond' test
         (graphscr)
     )                      ; end of while loop
     (close DLIST)          ; close DIR list file
     (grtext)               ; restore top line
     (redraw)               ; restore graphic screen
     (setvar "cmdecho" SAVC)
     (if REC (princ "\nNo more Drawing Files"))
     (prin1)
 
     )
 
     ;get Uppercase character of key pressed
     (defun getkey ( / INPV)
          (setq INPV (grread))
          (if (= (car INPV) 2)
          (strcase (chr (last INPV)))
          )
      )
 
;get DWG name without trailing blanks from line REC
(defun getnam ( REC / DWGNAM CNT)
      (setq CNT 8)           ; max length of file name
      (setq DWGNAM (substr REC 1 CNT))
      (while (= " " (substr DWGNAM CNT 1))
             (setq CNT (1- CNT))
      )
      (substr DWGNAM 1 CNT)
 
      )
 
;print Error message
(defun PERR (STR)
      (princ "\nError ; ")
      (princ STR)
      (terpri)
      (princ REC)
      (close DLIST)
      (grtext)                       ; restore top line
      (redraw)                        ; restore graphic screen
      (setvar "cmdecho " SAVC)
      (prin1)
      )
 
;CREATE SCRIPT FILE SLD.SCR TO MAKE SLIDES OF ALL DRAWING FILES
;RUN THE SCRIPT FILE BY ACAD X SLD
(defun c:SLD ( / DLIST DWGNAM FID REC)
 (princ "\nSLD - make slides of all drawing files\n")
 (command "shell" "DIR *.DWG>D$LIST")
 (setq FID (open "SLD.SCR" "w"))   ;open script file
 (setq DLIST (open "D$LIST" "r"))  ;open DIR list file
 (repeat 5                         ;skip first 4 records in DIR list
      (setq REC (read-line DLIST)))
 (while (and REC (/= " " (substr REC 1 1)))
      (setq DWGNAM (getnam REC))
      (princ (strcat "2 " DWGNAM " MSLIDE  QUIT Y\n") FID)
      (setq REC (read-line DLIST))
 )
 (princ "0\n" FID)
 (close DLIST) (close FID)
 (princ "\nNow exit AutoCad and at DOS prompt say : ACAD x SLD\n")
 (prin1)
 
)
 
;end of program
