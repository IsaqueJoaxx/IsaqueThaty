;==========================================================
; SEARCH.LSP Copyright 1992 by Looking Glass Microproducts
;==========================================================
; Find text entities on screen.
(setq SEARCH-VERSION "1.00")
;==========================================================
; Error Handler
(defun SEARCH-ERROR (S)
   (if BLINKING (redraw BLINKING))
   (if (not
          (member S '("Function cancelled" "console break"))
       )
      (princ S)
   )
   (command "_undo" "end")
   (POPVARS)
)
;==========================================================
; Set and Save System Variables
(defun PUSHVARS (VLIST)
   (foreach PAIR VLIST
      (setq
         SYSVARS (cons
                    (cons
                       (strcase (car PAIR))
                       (getvar (car PAIR))
                    )
                    SYSVARS
                 )
      )
      (if (cdr PAIR) (setvar (car PAIR) (cdr PAIR)))
   )
   t
)
;==========================================================
; Restore System Variables
(defun POPVARS ()
   (foreach PAIR SYSVARS (setvar (car PAIR) (cdr PAIR)))
   (setq
      *error* OLD-ERROR
   )
   (princ)
)
;==========================================================
; Disallow transparent invocation of routine.
(defun NOTRANS ()
   (cond
      ((zerop (logand (getvar "cmdactive") (+ 1 2 4 8))))
      ((alert
          "This command may not be invoked transparently."
       )
      )
   )
)
;==========================================================
; Midpoint of Two Points
(defun MIDPOINT (P1 P2)
   (mapcar '(lambda (A B) (* 0.5 (+ A B))) P1 P2)
)
;==========================================================
; Middle point of text entity
(defun GET_MIDDLE (/ ICON)
   (setq ICON (getvar "ucsicon"))
   (setvar "ucsicon" 0)
   (command "_ucs" "e" BLINKING)
   (setq
      MIDDLE (trans
                (apply
                   'MIDPOINT
                   (textbox (entget BLINKING))
                )
                1
                0
             )
   )
   (command "_ucs" "p")
   (setvar "ucsicon" ICON)
   (setq MIDDLE (trans MIDDLE 0 1))
)
;==========================================================
; Show prompt
(defun SHOW_PROMPT ()
   (prompt
      (strcat
         "\rMatch "
         (rtos (1+ I) 2 0)
         "/"
         (rtos N 2 0)
         " : Center/Next/Previous/<eXit>:      "
      )
   )
)
;==========================================================
; Blink entity until key pressed
(defun GET_ACTION ()
   (while (/= 2 (car (setq ACTION (grread t))))
      (if (> (getvar "date") BLINK)
         (progn
            (setq
               BLINK (+ BLINKRATE (getvar "date"))
               ON    (not ON)
            )
            (redraw BLINKING (if ON 1 2))
         )
      )
   )
   (redraw BLINKING)
)
;==========================================================
; Is Point p at center of screen?
(defun CENTERED (P)
   (equal (trans P 1 2) (trans (getvar "viewctr") 1 2) 1E-8)
)
;==========================================================
; Is Point p on screen?
(defun ON_SCREEN (P / VIEWCTR VIEWSIZE VSMIN VSMAX ASPECT 
                      VMIN VMAX)
   (setq
      VIEWCTR  (trans (getvar "viewctr") 1 2)
      VIEWSIZE (getvar "viewsize")
      VSMIN    (trans (getvar "vsmin") 1 2)
      VSMAX    (trans (getvar "vsmax") 1 2)
      ASPECT   (mapcar '- VSMAX VSMIN)
      ASPECT   (/ (car ASPECT) (cadr ASPECT))
      VMIN     (mapcar
                  '-
                  VIEWCTR
                  (list
                     (* 0.5 ASPECT VIEWSIZE)
                     (* 0.5 VIEWSIZE)
                  )
               )
      VMAX     (mapcar
                  '+
                  VIEWCTR
                  (list
                     (* 0.5 ASPECT VIEWSIZE)
                     (* 0.5 VIEWSIZE)
                  )
               )
      P        (trans P 1 2)
   )
   (apply 'and (mapcar '<= VMIN P VMAX))
)
;==========================================================
; Show ss on screen
(defun SHOW (SS / N I AGAIN ACTION BLINK BLINKING BLINKRATE 
                  ON MIDDLE)
   (setq BLINKRATE (/ 0.5 86400.0)) ; in days 
   (setq N (sslength SS) I 0)
   (prompt "\n")
   (setq AGAIN t)
   (while AGAIN
      (setq
         BLINK    (+ BLINKRATE (getvar "date"))
         BLINKING (ssname SS I)
         ON       t
      )
      (GET_MIDDLE)
      (if (not (ON_SCREEN MIDDLE))
         (command
            "_zoom" "c" MIDDLE ""
         )
      )
      (SHOW_PROMPT)
      (GET_ACTION)
      (cond
         ((member ACTION '((2 67) (2 99)))
            (if (not (CENTERED MIDDLE))
               (command
                  "_zoom" "c" MIDDLE ""
               )
            )
         )
         ((member ACTION '((2 78) (2 110)))
            (setq
               I (if (< I (1- N)) (1+ I) 0)
            )
         )
         ((member ACTION '((2 80) (2 112)))
            (setq
               I (1- (if (> I 0) I N))
            )
         )
         ((member ACTION '((2 13) (2 88) (2 120)))
            (setq
               AGAIN nil
            )
         )
      )
   )
)
;==========================================================
; Search main routine
(defun SEARCH (/ PATTERN SS)
   (graphscr)
   (cond
      ((= "" (setq PATTERN (getstring "For pattern: "))))
      ((null
          (setq
             SS (ssget
                   "x"
                   (list (cons 0 "TEXT") (cons 1 PATTERN))
                )
          )
       )
         (prompt "Not found.")
      )
      (t (SHOW SS))
   )
   (setq SS nil)
)
;==========================================================
; Search Command 
(defun C:SEARCH (/ OLD-ERROR SYSVARS)
   (if (NOTRANS)
      (progn
         (setq OLD-ERROR *error* *error* SEARCH-ERROR)
         (PUSHVARS
         '(("cmdecho" . 0) ("blipmode" . 0) ("osmode" . 0))
         )
         (command "_undo" "group")
         (SEARCH)
         (command "_undo" "end")
         (POPVARS)
      )
      (princ)
   )
)
(princ
   (strcat
      "\SEARCH.LSP v"
      SEARCH-VERSION
      " -- Copyright 1992 by Looking Glass Microproducts"
   )
)
(princ)
