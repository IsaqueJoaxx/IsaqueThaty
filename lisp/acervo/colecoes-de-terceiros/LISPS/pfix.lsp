; PFIX.LSP  Relocate a portion of a polyline
; 2/4/87
; >>>Lansing Pugh (512) 441-4666, CIS 74375,457.<<<

;If you are experienced enough to be able to read this, you probably
;understand the concept of shareware.  I'd like to try to recover some
;of the cost of my time developing this and other lisp routines for Autocad
;by seeking voluntary payments.  If this routine is useful to you in doing
;paying work, please send $5 to:
;     Lansing Pugh
;     2316 Westrock Drive
;     Austin, Texas 78704
;If you decide to incorporate part or all of this routine in a commercial
;software package, please send a copy of that package to the same address.
;No acknowledgements in the software are required.
;If you just want to learn from this routine (its not a great example of
;LISP, it just works for me), no payment is sought.  If you do write some-
;thing you feel is pretty neat, I'd like a copy in exchange.
;
;Please give others copies of this routine. Do not remove these comments.
;Thanks!

(DEFUN C:PFIX ()

;initialize variables
; l	    number of points picked
; points    list of points
; xy	    current point
 (setq l nil)
 (setq points nil)
 (gc)

; set xy to an arbitrary point to get into the following while statement
 (setq xy (getvar "VIEWCTR"))

; user inputs points until nil
  (while xy
    (progn
     (setq xy (getpoint "Enter point: "))
     (setq points (append points (list xy)))
    )
  )

; clear out value for current point
 (setq xy nil)

; reset counter to 0
 (setq n 0 )

; how many points do we have?
 (setq l (length points))
 (print l)

; snap to near the first point on the list
 (setq b1 (osnap (nth 0 points) "nea"))
; snap to near the last point on the list
 (setq b2 (osnap (nth (- l 2) points) "nea"))

; break between the first and the last point on the list
 (command "break" b1 b2)

; find the other ends of the plines left for later join
 (setq set1 (ssget b1))
 (setq set2 (ssget b2))

; MAKE SURE THAT WE GOT THOSE PLINES (BUG IN 2.5)



; start the pline
 (command "pline" b1)

; main pline loop, repeats less nil at end and first and last points
  (repeat (- l 3)
    (progn
     (setq n (1+ n))
     (print n)
     (setq xy (nth n points))
     (command xy)
    )
  )

; last point
 (command b2)
 (command "")

; select the new pline and join it to the old segments, then do a fitcurve
 (command "pedit" "l")
 (command "j" set1 set2 "" "f" "x")
)

(write-line "New function PFIX to relocate a segment of a pline. ")
(command)
