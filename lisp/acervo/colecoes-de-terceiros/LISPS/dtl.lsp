;=======================================================================
; Allegro TABLET TOOLS LISP file
; DTL.LSP
;
; Extracts a section of a drawing for a detail.
; Lines, Arcs, & Circles are trimmed to the box boundary.
; P-LINES and equal scale blocks are exploded one level in the detail
; before trimming.
;
; (c) 1988 Robert McNeel & Assoc., 1310 Ward St., Seattle, WA, 98109
; This routine is submitted for private non-resale use by end users.
;=======================================================================
(princ "\nInitial load .. please wait\n")
;=======================================================================
(defun val (x e) (cdr (assoc x e)))
(defun enttype (e) (cdr (assoc 0 e)))
(defun entname (e) (cdr (assoc -1 e)))
(setq >90 (/ pi 2) >270 (* 3 (/ pi 2)))
;=======================================================================
; Find the 'endpoints ' of the LINES, ARCS, & CIRCLES in ss that are
;   outside a rectangle described by the opposite corners pll and pur.
;   and submits them to the command function.
;-----------------------------------------------------------------------
(defun osends (ss pll pur / z eps)
  (ends ss)     ;this puts the 'endpoints' in a list, eps
  (foreach z eps    ;this checks if they are outside the rectangle
    (if (or (< (caadr z) (car pll))
        (< (cadadr z) (cadr pll))
        (> (caadr z) (car pur))
        (> (cadadr z) (cadr pur))
      )
      (command z)
    )
  )
)
;-----------------------------------------------------------------------
; Finds the 'endpoints' of LINES, ARCS, & CIRCLES in ss.
; 'Endpoints' are:
;   LINES:    endpoints
;   ARCS:   endpoints and quadrant points
;   CIRCLES:  quadrant points
; The endpoint lists are consed into the list eps (global).
;-----------------------------------------------------------------------
(defun ends (ss / i ent cen)
  (setq len (sslength ss) i 0)  ;get number of entities
  (while (< i len)        ;loop thru them
    (setq ent (entget (ssname ss i))) ;get assoc list
    (cond       ;Check for LINES, ARCS, & CIRCLES and cons
            ; the appropriate points into eps.
            ;Other entity types are ignored.
          ;LINES
      ((= (enttype ent) "LINE")
        (setq eps (cons (list (entname ent) (val 10 ent)) eps))
        (setq eps (cons (list (entname ent) (val 11 ent)) eps)) )
          ;ARCS
      ((= (enttype ent) "ARC")
        (setq cen (val 10 ent))
        (setq eps (cons 
          (list (entname ent) 
            (polar cen 0 (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent)
            (polar cen >90 (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent) 
          (polar cen pi (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent) 
          (polar cen >270 (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent) 
            (polar cen (val 50 ent) (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent) 
            (polar cen (val 51 ent) (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent) 
          (osnap (polar cen (val 51 ent) (val 40 ent)) "mid")) eps)) )
        ;CIRCLES
      ((= (enttype ent) "CIRCLE")
        (setq cen (val 10 ent))
        (setq eps (cons 
          (list (entname ent) 
          (polar cen 0 (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent)
          (polar cen >90 (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent) 
          (polar cen pi (val 40 ent))) eps))
        (setq eps (cons 
          (list (entname ent) 
          (polar cen >270 (val 40 ent))) eps)) )
    )
    (setq i (1+ i))   ;Next entity
  )
)
;-----------------------------------------------------------------------
;Explodes all p-lines in selection set s
;-----------------------------------------------------------------------
(defun exp_pl (s / i len ent)
  (setq i 0 len (sslength s))
  (while (< i len)
    (setq ent (entget (ssname s i)))
    (cond
      ((= (enttype ent) "POLYLINE")
        (command "explode" (entname ent))
      )
      ((= (enttype ent) "INSERT")
        (if (= (val 41 ent) (val 42 ent) (val 43 ent))  ;check equal scale
          (command "explode" (entname ent))
        )
      )
    )
    (setq i (+ 1 i))
  )
)
;-----------------------------------------------------------------------
;erases parts of exploded p-lines that are outside target area
;-----------------------------------------------------------------------
(defun era_xtra (e)
  (setq ssx (ssadd))
  (while (setq e (entnext e))
    (if (not (ssmemb e ss))
      (ssadd e ssx)
    )
  )
  (command "erase" ssx "") 
)
;-----------------------------------------------------------------------
; Gets the geometry and calls routines to do the trimming.
;-----------------------------------------------------------------------
(defun c:dtl (/ px py pxx pyy xs ss b ssx)
  (setvar "cmdecho" 0)
  (if (and
      ;This defines a rectangle to be copied out to a detail
    (setq px (getpoint "\nFirst corner: "))
    (setq py (getcorner px "\nOther corner: "))
      ;This is the position of the lower left corner of the detail
    (setq pxx (getpoint px "\nNew first corner position: "))
      ;This is a size adjust factor for scaling the detail
    (setq xs (getreal "\nScale factor for detail: ")) )
    (progn
      ;copy out the stuff selected
  (command "copy" "c" px py "" px pxx)
  (command "pline" px (list (car px) (cadr py)) ;draw an outline of
      py (list (car py) (cadr px)) "c")         ; the base area.
          ;these are the new entities that may need trimming.
  (setq ss (ssget "c" pxx (polar pxx (angle px py) (distance px py))))
          ;first explode p-lines because if you trim them, you get too
          ;many new entities to deal with.  (could do blocks to)
  (setq last (entlast)) ;save end of database
  (exp_pl ss) ;this explodes them
    ;then get all the new parts into ss
  (setq ss (ssget "c" pxx (polar pxx (angle px py) (distance px py))))
    ;and erase ones that are clear outside rectangle
  (era_xtra last)
  (command "scale" ss "" pxx xs)    ;Scale the detail
          ;new other corner point of detail
  (setq pyy (polar pxx (angle px py) (* xs (distance px py))))
  (command "pline" pxx (list (car pxx) (cadr pyy))  ;Box around the
      pyy (list (car pyy) (cadr pxx)) "c")          ;detail
  (command "trim" (setq b (entlast)) "")  ;Last P-line is cutting edge
  (osends ss pxx pyy)   ;This finds the ends that are outside the box
              ; and trims them.
  (command "")    ;Terminate trim
  (redraw b)
    )
  )
  (setvar "cmdecho" 1)
  (princ)
)
