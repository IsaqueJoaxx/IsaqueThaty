;
;       Tower of Hanoi
;
;                               Kelvin R. Throop   March 1985
;
;       This file implements the Tower of Hanoi problem.  It is as
;       specified in the rules:
;
;          1.  Only one disc may be moved at a time.
;          2.  No disc may be placed on top of a smaller one.
;
;       The only incompatibility with the original is that the universe
;       will not come to an end when this function completes (however, if
;       you run it with the specified number of discs, 64, the protons may
;       decay before it's done).
;
;       One defines the tower with the command TOWER, which asks for the
;       number of discs.  Scaling is automatic, as is clearing away of any
;       previous execution.  Once the tower is defined, the solution may
;       be accomplished with the command SOLVE.
;
;       The solution function, TRANSFER, is as given in Winston and Horn,
;       "LISP", second edition, pp. 112-114.
;
(defun C:TOWER ()

        (setq nrings (getint "Enter number of rings: "))

        (setq bthick 1)            ; base thickness
        (setq rthick 1)            ; ring thickness
        (setq smring 1.5)          ; smallest ring
        (setq ringinc 1.0)         ; ring size increment
        (setq postdia 1)           ; post diameter
        (setq airspace 0.1)        ; air space between rings

        (setq lring (+ smring (* nrings ringinc))) ; largest ring diameter
        (setq posta nil postb nil postc nil)       ; clear possible earlier run
        (setq postl '(posta postb postc))          ; list of post lists
        (setq rspace (+ rthick airspace))          ; ring vertical spacing

        (setvar "blipmode" 0)      ; get rid of annoying distractions
        (setvar "cmdecho" 0)       ; disable echoing of commands
        (setvar "fillmode" 0)      ; make for better display

        ; Clean up if previous display on screen

        (if before (progn
              (command "vpoint" "0,0,0")
              (setq a 1)
              (while (<= a before)
                 (command "erase" "l" "")
                 (setq a (1+ a))
              )
              (command "layer" "set" "base" "")
           )
           (progn
              (command "layer" "new" "base,1,2,3,4,5,6"
                 "colour" 7 "base"
              )
              (setq a 0)
              (while (<= (setq a (1+ a)) 6)
                 (command "colour" a a)
              )
              (command "set" "base" "")
           )
        )

        ; Draw the base

        (command "elev" 0 bthick)
        (setq a (+ lring (* postdia 2)))
        (setq b (+ (* 3 (+ postdia lring)) postdia))
        (command "vpoint" "0,0,0") ; insure ZOOM does the right thing
        (command "solid" '(0 0) (list 0 a) (list b 0) (list b a))
        (command)                  ; terminate solid
        (command "zoom" "e")

        ; Draw the posts

        (command "elev" bthick (* rspace (1+ nrings)))
        (setq a (+ postdia lring))
        (setq b (+ postdia (/ lring 2)))
        (setq postposy (+ postdia (/ lring 2)))
        (setq postposx (list b (+ b a) (+ b a a)))
        (command "circle" (list (car postposx) postposy) "d" postdia)
        (command "circle" (list (cadr postposx) postposy) "d" postdia)
        (command "circle" (list (caddr postposx) postposy) "d" postdia)
        (setq bthick (+ bthick airspace))    ; offset position of lowest ring
        (command "vpoint" "1,1,1") ; establish viewpoint

        ; Put rings on post 1 initially

        (setq i 1 l 5)
        (setq d bthick)
        (setq r (/ lring 2))
        (while (<= i nrings)
           (command "layer" "set" (1+ (setq l (rem (1+ l) 6))) "")
           (command "elev" d rthick)
           (command "circle" (list b postposy) r)
           (setq posta (cons (list r (1+ l)) posta))
           (setq r (- r (/ ringinc 2)))
           (setq d (+ d rspace))
           (setq i (1+ i))
        )

        ; Tell the user how to proceed

        (setq before (+ nrings 4)) ; save previous ring count
        (princ "\nType SOLVE to perform the solution.\n")
)

; Command function to perform solution

(defun C:SOLVE ()
        (transfer 1 2 3 nrings)
        (redraw)
)

; Move disc from one post to another

(defun move-disc (from to)
        (setq lfrom (nth (1- from) postl))
        (setq lto (nth (1- to) postl))
        (setq elfrom (eval lfrom))
        (setq elto (eval lto))
; **    (print (list 'Move 'disc 'from lfrom 'to lto))
        (setq top (car elfrom))
        (set lfrom (cdr elfrom))
        (command "layer" "set" (cadr top) "")
        (command "elev" (+ bthick (* (length elfrom) rspace)) rthick)
        (command "erase" (list (nth (1- from) postposx)
                               (+ postposy (car top))) "")
        (command "elev" (+ bthick (* (length elto) rspace)) rthick)
        (command "circle" (list (nth (1- to) postposx) postposy) (car top))
        (set lto (cons top elto))
)

; Perform transfer of discs  (This is actually the entire solver)

(defun transfer (from to spare n)
        (cond ((= n 1) (move-disc from to))
              (t (transfer from spare to (1- n))
                 (move-disc from to)
                 (transfer spare to from (1- n)
              )
           )
        )
)
