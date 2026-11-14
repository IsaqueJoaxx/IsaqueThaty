; TIP897:  SET-UP.LSP  Setup Information   (C)1993, John K. Sherman

; ****************************************************************
; Calculate and set TEXTSIZE, DIMSCALE, LTSCALE
; and record SET-UP parameters.
;
; --------------------- PRDATA -------------------
(defun PRDATA ()
   (setq dnam (getvar "DWGNAME"))
   (setq dnam (strcase dnam))
   (setq fnam (strcat dnam ".PAR"))
   (setq setdat (open fnam "w"))
   (write-line (strcat "DRAWING NAME:   " dnam) setdat)
   (write-line "SET-UP DATA / SET-UP.LSP" setdat)
   (write-line "------------------------" setdat)
   (write-line (strcat "RUN MVSETUP 1st    " mv1) setdat)
   (write-line (strcat "UNIT TYPE:         " ut) setdat)
   (write-line (strcat "SCALE FACTOR:      " (rtos sf 2 1)) setdat)
   (write-line (strcat "TEXTSIZE:          " (rtos ts 2 3)) setdat)
   (write-line (strcat "PLOTTED TEXTSIZE:  " (rtos tso 2 5)) setdat)
   (write-line (strcat "LTSCALE:           " (rtos lsc 2 4)) setdat)
   (write-line (strcat "DIMSCALE:          " (rtos ds 2 4)) setdat)
   (write-line (strcat "PAPER SIZE:        " psz) setdat)
   (close setdat)
)
; ---------- MAIN PROGRAM --------------
(defun C:SET-UP (/ ans d dnam ds fnam lu l lsc mv1 psz sf ts tso 
   u ut ux uy)
   (graphscr)
   (setvar "CMDECHO" 0)
   ;
   ; ---------- SCALE FACTOR --------------
   (setq lu (getpoint
   "\nPick or Enter coordinates of upper right corner: ")) 
   (terpri)
   (if (/= mv_sc nil)
      (progn
         (setq sf mv_sc)
         (setq mv1 "YES")
      )
      (progn
         (princ "To convert a SCALE of 1/2")
         (princ (chr 34))
         (princ" = 1'-0")
         (princ (chr 34))
         (princ " to SCALE FACTOR, work in the same units, ")
         (terpri)
         (princ "use 1/2:12/1 converts to 1:24/1 or 1:24 to get a ")
         (princ "SCALE FACTOR of 24.")
         (setq sf (getreal "\nEnter Scale Factor "))
         (setq mv1 "NO")
      )
   )
   ; ---------- UNIT TYPE --------------
   (prompt "UNIT TYPES: 1=SCI: 2=DEC: 3=ENGR: 4=ARCH: 5=METRIC")
   (setq ans (getreal "\nEnter Unit Type Number: "))
   (cond ((= ans 1) (setq ut "SCIENTIFIC"))
      ((= ans 2) (setq ut "DECIMAL"))
      ((= ans 3) (setq ut "ENGINEERING"))
      ((= ans 4) (setq ut "ARCHITECTURAL"))
      ((= ans 5) (setq ut "METRIC"))
   )
   ; ------------ TEXT SIZE ---------------
   (setq ts (getreal
   "\nEnter text size desired on plot <3/32>: ")) 
   ; CHANGE TO SUIT
   (terpri)
   (if (= ts nil) ;accept default with <CR>
      (setq ts 0.09375)               
      ; CHANGE TO AGREE WITH ABOVE CHANGE
   )
   (cond
      ((/= ans 5) (setq ts (* ts sf)))
      ((= ans 5) (setq ts (* ts sf 25.4)))
   )
   (command "TEXTSIZE" ts)
   (setq tso (/ ts sf))
   ;
   ; --------- DIMSCALE ----------
   (setq ds (/ ts (getvar "DIMTXT")))
   (command "DIMSCALE" ds)
   ;
   ; --------- LTSCALE -----------
   (setq l (getvar "limmin"))
   (setq u (getvar "limmax"))
   (setq d (distance l u))
   (setq lsc (fix (/ d 36)))  
   ; CHANGE TO SUIT
   (if (< lsc 1.0) (setq lsc (/ d 36)))  
   ; CHANGE TO SUIT
   (command "ltscale" lsc)
   ;
   ; -------- PAPER SIZE ---------
   (setq ux (/ (car lu) sf))
   (setq uy (/ (cadr lu) sf))
   (setq ux (rtos ux 2 1))
   (setq uy (rtos uy 2 1))
   (setq psz (strcat  ux " X " uy )
   )
   ; -----------------------------
   (PRDATA)
   (princ "\nSee or import ")(princ fnam)
   (princ " for listing of SET-UP parameters.") (princ)
)
