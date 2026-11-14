;*****************************************************************************
;*       BRACE.LSP    J Watson                                    12/12/91   *
;*    Draws a large brace when given 2 endpoints.                            *
;*****************************************************************************
;*******************
;*  Error Handler  *
;*******************
(defun CLERR (s)
                                  ; If an error (such as CTRL-C) occurs
   (princ (strcat "\nErroR: " s)) ; while this command is active...
   (setvar "orthomode" os1)             ; Restore saved modes
   (setvar "BLIPMODE" 1)
   (setvar "HIGHLIGHT" 1)
   (setvar "CMDECHO" 1)
   (setq *error* olderr)             ; Restore old *error* handler
   (princ)
)

(defun c:brace(/ olderr x1 x2 x3 y1 y2 y3 rad hyp dist pntr temp
                 errflg os1 stpnt enpnt pt1 pt2 pt3 pt4 pt5 pt6 pt7 pt8)
  (setq olderr  *error*              ;set error handler to our 
        *error* clerr                ;  custom one
        hyp 1.414214                 ;hypotineuse distance
        errflg 1
        os1 (getvar "Orthomode"))
  (setvar "orthomode" 1)
  (setvar "cmdecho" 0)
  (graphscr)
  (initget 1)
  (setq stpnt(getpoint "\nEnter first endpoint of brace: "))
  (initget 1)
  (setq enpnt(getpoint stpnt "\nEnter other endpoint: ")
        dist(abs(distance stpnt enpnt)))
  (setvar "blipmode" 0)
  (cond ((> dist 26)(setq rad 0.375))
        ((> dist 18)(setq rad 0.25))
        ((> dist 9)(setq rad 0.188))
        (T (setq rad 0.125))
  );cond
  (if (> dist 0.625)
   (progn
    (setvar "orthomode" 0)
    (initget 1)
    (setq pntr (getpoint "\nWhich way will brace point: ")
          x1 (car stpnt)  x2 (car enpnt)
          y1 (cadr stpnt)  y2 (cadr enpnt)
          x3 (car pntr) y3 (cadr pntr))
    (cond ((= x1 x2);vertical brace
           (if (> y2 y1)(setq temp stpnt stpnt enpnt enpnt temp))
           (if (> x3 x1);right pointing vert brace
            (progn
             (setq pt2 stpnt
                   pt1 (polar pt2 5.4978 (* rad hyp))
                   pt3 enpnt
                   pt4 (polar pt3 0.7854 (* rad hyp))
                   pt5 (polar pt1 4.7124 (- (/ dist 2) (* rad 2)))
                   pt7 (polar pt4 1.5708 (- (/ dist 2) (* rad 2)))
                   pt6 (polar pt7 0.7854 (* hyp rad))
                   pt8 pt6
             )
             (command "line" pt1 pt5 "")
             (command "line" pt7 pt4 "")
            )
            (progn ;left pointing vert brace
             (setq pt1 stpnt
                   pt2 (polar pt1 3.927 (* rad hyp))
                   pt4 enpnt
                   pt3 (polar pt4 2.3562 (* rad hyp))
                   pt6 (polar pt2 4.7124 (- (/ dist 2) (* rad 2)))
                   pt8 (polar pt3 1.5708 (- (/ dist 2) (* rad 2)))
                   pt7 (polar pt8 2.3562 (* hyp rad))
                   pt5 pt7
             )
             (command "line" pt2 pt6 "")
             (command "line" pt8 pt3 "")
            );progn
           )
          );x1 = x2

          ((= y1 y2);horizontal brace
           (if (< x2 x1)(setq temp stpnt stpnt enpnt enpnt temp))
           (if (> y3 y1);upward pointing horz brace
            (progn
             (setq pt2 stpnt
                   pt1 (polar pt2 0.7854 (* rad hyp))
                   pt3 enpnt
                   pt4 (polar pt3 2.3562 (* rad hyp))
                   pt5 (polar pt1 0.0 (- (/ dist 2) (* rad 2)))
                   pt7 (polar pt4 3.1416 (- (/ dist 2) (* rad 2)))
                   pt6 (polar pt5 0.7854 (* hyp rad))
                   pt8 pt6
             )
             (command "line" pt1 pt5 "")
             (command "line" pt7 pt4 "")
            )
            (progn ;down pointing horz brace
             (setq pt1 stpnt
                   pt2 (polar pt1 5.4978 (* rad hyp))
                   pt4 enpnt
                   pt3 (polar pt4 3.927 (* rad hyp))
                   pt6 (polar pt2 0.0 (- (/ dist 2) (* rad 2)))
                   pt8 (polar pt3 3.1416 (- (/ dist 2) (* rad 2)))
                   pt7 (polar pt8 3.927 (* hyp rad))
                   pt5 pt7
             )
             (command "line" pt2 pt6 "")
             (command "line" pt8 pt3 "")
            );progn
           )
          );y1 = y2
          (T (setq errflg nil)) ;otherwise there's an error
    );end cond
    (if errflg
     (progn
      (command "arc" pt1 "e" pt2 "r" rad)
      (command "arc" pt3 "e" pt4 "r" rad)
      (command "arc" pt5 "e" pt6 "r" rad)
      (command "arc" pt8 "e" pt7 "r" rad)
     )
     (prompt "\nPoints picked were not orthographic.")
    )
   );progn
   (prompt "\nBrace would be to small!")
  );if dist >

  (setvar "orthomode" os1)
  (setvar "blipmode" 1)
  (setvar "cmdecho" 1)
  (setq *error* olderr)             ; Restore old *error* handler
  (princ)
)
