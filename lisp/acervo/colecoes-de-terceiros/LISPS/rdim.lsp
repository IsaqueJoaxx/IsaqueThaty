;   Dimentioning routine:                 Mark Vodhanel
;   Acad 2.5x                            CIS (72456,463)
;
;                                        2-01-87
;
;     This routine allows the user to set a reference point and other
; features that will be remembered until reset.  The dimentioning in this
; routine is of an abreviated type that will allow more information to
; be placed in the drawing when only one or two reference planes
; are needed.
;
;     When invoked, this program will check to see if the current text
; style is "DIMTXT", if it isn't "DIMTXT" will be created and made current.
; You will then be given a short list of defaults that you can either
; accept and procceed or stop and change.  These are:  the reference
; point, dimention axis rotation, and scaleing factor for the text &
; arrows.  The reference point should be the intersection of the two
; planes that you want to use as a reference, or, if you are only using
; one plane, it can be any point along this plane.  The dimention axis
; rotation can be a reference plane or any line parallel to it.  The
; Scaling factor, as is, is about right for full scale drawings, but
; can be changed to accomodate other scales ie: .5 scaling factor
; for 2/1 scale drawings.
;
;     You will next be asked for the origin point for the extension
; line.  Pick the desired point and drag the dimention line to where
; you want it, if you are using two dimention planes you automatically
; refer to one or the other by the angle the extension line is dragged.
;
;    Next come the dimention control defaults which may be changed
; on the fly and are more or less self explanitory.  If you want
; a space between the preceding text and the dimention just type a
; space as the first character, and, similarly, a space may be added
; between the dimention and the appending text by typing a space
; as the last character in answering the appending text prompt.
;
;    All the text and arrows and extension lines are put on layer
; "DIM" which is created if not already there.  The text color is
; changed to color 3 and the extemsion lines & arrows are changed to
; color 1 as it is setup now.  All of this is easily changed - see
; the default functions below.
;
;                   Comments are welcomed.
;
;
;
;       <RDIMR> takes it out of memory
;       <RDIM>  invokes it
;
;
;
;
;
(defun c:RDIMR ()
        (setq atomlist (member 'c:clean atomlist))
        'Done
)
;
;
(defun set-defaults ()
   (if (boundp 'default-flag) () (progn
      (setq
         scale       1             ; Scale factor
         style-name "DIMTXT"       ; Text style name
         base       (list 0.0 0.0) ; Reference point
         precision   3             ; Precision
         fraction   "N"            ; N or denominator of fraction - string
         rotang      0.0           ; Rotation angle
         d-layer    "DIM"          ; Layer to put dimentions on
         txt-clr    "3"            ; Color for dimentioning text
         ext-clr    "1"            ; Color for arrow & extsn lines
         pr-text    " "            ; Preceding text
         ap-text    " "            ; Appending text
         default-flag "set")))
)
;
;
(defun dim-scale()
   (if (boundp 'scale-flag) () (progn
      (setq
         arrw-w      (* 0.060 scale)   ; Arrow width
         arrw-h      (* 0.120 scale)   ; Arrow hieght
         arrw-l      (* 0.300 scale)   ; Arrow length
         txt-w       (* 0.125 scale)   ; Text width
         txt-h       (* 0.125 scale)   ; Text height
         ext-gap     (* 0.125 scale)   ; Gap between extension line & origin
         endext-pos  (* 0.300 scale)   ; Dist from arrw to end extsn line
         text-offset (* 0.050 scale)   ; Distance above the extension line
         scale-flag  "set")))
)
;
;
;
;
(defun set-style (/ r1 r2 r3 r4 r5 r6 r7 r8)
        (setq
          r1 style-name            ; Style name  - set above
          r2 "MONOTXT"             ; Style
          r3 (rtos txt-h)          ; Text height - set above
          r4 "1.0"                 ; Width factor
          r5 "0.0"                 ; Obliquing angle
          r6 "N"                   ; Backwards?
          r7 "N")                  ; Upside down?
        (command "style" r1 r2 r3 r4 r5 r6 r7 r8)
)
;
;
(defun c:RDIM (/ basex basey rotangstr scalestr ans)
   (save-vars)
   (setvar "CMDECHO" 0)
   (setvar "EXPERT" 2)
   (setvar "BLIPMODE" 0)
   (set-defaults)
   (dim-scale)
   (if (= style-name (getvar "TEXTSTYLE")) () (set-style))
   (setq basex (rtos (car base) 2 precision)
         basey (rtos (last base) 2 precision)
         rotangstr (angtos rotang 0 precision)
         scalstr (rtos scale 2 precision))
   (princ (strcat "\nReference point:  " basex "," basey))
   (princ (strcat "\nRotation  angle:  " rotangstr))
   (princ (strcat "\nDimention scale:  " scalstr))
   (princ (strcat "\nAccept <Y>:"))
   (setq ans (strcase (substr (getstring) 1 1)))
   (if (= ans "N") (progn
      (princ (strcat "\nReference point  <" basex "," basey ">: "))
      (setq ans (getpoint))
      (if (null ans) () (setq base ans))
      (setq rotang (picang))
      (princ (strcat "\nDimention scale  <" scalstr ">: "))
      (setq ans (getreal))
      (if (null ans) () (progn
                        (setq scale ans)
                        (setq scale-flag nil)
                        (dim-scale)
                        (set-style)))))
   (setq done "N")
   (while (= done "N")
      (dimention))
   (rest-vars)
   'Done
)
;
;
; Subfunctions:
;
;
;
(defun picang (/ blipmdsv entlst entnam type pt1 pt2)
    (setvar "SNAPMODE" 0)
    (setq type nil)
    (while (/= type "LINE")       ; continue until valid entity selected
       (setq entnam (entsel "\nSelect a line to align the dimention axis:"))
       (if (null entnam) (setq type nil) (progn
          (setq entlst (entget (car entnam)))
          (setq type (cdr (assoc 0 entlst)))
          (if (/= type "LINE") (setq type nil) (progn  ; only lines allowed
             (setq pt1 (cdr (assoc 10 entlst)))
             (setq pt2 (cdr (assoc 11 entlst)))
             (angle pt1 pt2))))))
)
;
;
(defun dimention (/ distsrtg)
   (setq point (getpoint "\nExtension line origin? "))
   (if (null point) (setq done "Y") (progn ; Just exit if no point is selected
       (drag-extension)
       (get-distance)               ; Set global variables: dist ref-ang
       (insert-arrow)
       (get-dist-strg)
       (insert-text))
  )
)
;
;
(defun drag-extension ()
    (setvar "BLIPMODE" 0)
    (setvar "ORTHOMODE" 1)
    (setvar "SNAPANG" rotang)
    (setvar "SNAPBASE" base)
    (setvar "SNAPMODE" 0)
    (if (= d-layer (getvar "CLAYER")) () (progn
        (command "layer" "N" d-layer "")
        (command "layer" "S" d-layer "")))
    (setq arrw-pnt nil)
    (while (null arrw-pnt)
       (setq arrw-pnt (getpoint point "\nDrag extension line:  ")))
    (setq ext-ang (angle point arrw-pnt))
    (setq ext-base (polar point ext-ang ext-gap))
    (setq ext-end (polar arrw-pnt ext-ang endext-pos))
)
;
;
(defun get-distance (/ ref)
    (setq base-ref (polar base ext-ang 100))
    (setq point-ref (polar point (+ ext-ang (/ pi 2)) 100))
    (setq ref (inters base base-ref point point-ref nil))
    (setq dist (distance ref point))      ; Distance from origin to ref axis
    (setq ref-ang (angle ref point))      ; Used to place text and arrow
)
;
;
(defun insert-arrow (/ p1 p2 p3 ref1 ref2)
     (if (> dist 0.000005) () (progn
        (setq ref nill)
        (while (null ref1)
           (princ "\nThis extension line is on the reference plane.")
           (princ "\nPick the side that you want the text on:  ")
           (setq ref2 (polar ext-end (+ ext-ang pi) arrow-pos))
           (setvar "ORTHOMODE" 1)
           (setvar "SNAPMODE" 0)
           (setq ref1 (getpoint ref2)))
        (setq ref-ang (angle ref2 ref1))
        (setq dist 0)))
     (setq
        p2 (polar arrw-pnt (+ ref-ang pi) arrw-h)
        p3 (polar arrw-pnt (+ ref-ang pi) arrw-l))
     (command "pline" arrw-pnt "w" 0 arrw-w p2 "w" 0 0 p3 "")
     (command "change" "l" "" "p" "c" ext-clr "")
)
;
;
(defun get-dist-strg (/ ans ref)
     (distrng-calc)
     (princ (strcat "\nDecimal places:    [" (itoa precision) "]"))
     (princ (strcat "\nRound to fraction: [" fraction "]"))
     (princ (strcat "\nPreceding text:    [" pr-text "]"))
     (princ (strcat "\nDimention:         [" distrng "]"))
     (princ (strcat "\nAppending text:    [" ap-text "]"))
     (princ (strcat "\nAccept? <Y>:  "))
     (setq ans (strcase (substr (getstring) 1 1)))
     (if (= ans "N") (progn
        (setq ans nil)
        (while (null ans)
           (setq precstrng (itoa precision))
           (princ (strcat "\nDecimal places       <" precstrng ">:"))
           (setq ans (getint))
           (cond
              ((null ans) (setq ans "set"))
              ((<= ans 6) (if (>= ans 0) (setq precision ans)
                                         (setq ans nil)))
              (t (setq ans nil))))
        (setq ans nil)
        (while (null ans)
           (princ (strcat "\nDenominator, or N    <" fraction ">:"))
           (setq ans (getstring))
           (cond
              ((= ans "") (setq ans "set"))
              ((= (strcase ans) "N") (setq fraction "N"))
              ((<= (atoi ans) 64) (if (> (atoi ans) 0) (setq fraction ans)
                                         (setq ans nil)))
              (t (setq ans nil))))
        (princ (strcat "\nPreceding text, or N <" pr-text ">:"))
        (setq ans (getstring T))
        (cond
           ((= (strcase ans) "N") (setq pr-text " "))
           ((/= ans "") (setq pr-text ans)))
        (distrng-calc)
        (princ (strcat "\nDimention            <" distrng ">:"))
        (setq ans (getstring))
        (if (= ans "") () (setq distrng ans))
        (princ (strcat "\nAppending text, or N <" ap-text ">:"))
        (setq ans (getstring T))
        (cond
           ((= (strcase ans) "N") (setq ap-text " "))
           ((/= ans "") (setq ap-text ans)))))
)
;
;
(defun distrng-calc (/ ref)
     (if (= fraction "N") (setq distrng (rtos dist 2 precision)) (progn
              (setq ref (/ 1.00 (atoi fraction)))
              (setq distrng (rtos (round dist ref) 2 precision))))
)
;
;
; Round.lsp by Duff Kurland
(defun round (num frac / over half)
   (setq half (/ frac 2.0))
   (setq over (rem num frac))    ; Get remainder
   (if (>= over half)
       (+ num frac (- over))     ; Round up
       (- num over)              ; Round down
   )
)
;
;
(defun insert-text (/ add-fctr ext-end-mod ext-flag)
     (setq txtpnt (polar arrw-pnt ref-ang text-offset))
     (setq txtpnt-flp (polar arrw-pnt ref-ang (+ text-offset txt-h)))
     (setq txtang (- ref-ang (/ pi 2)))
     (cond
         ((>= txtang (* pi 2)) (setq txtang (- txtang (* pi 2))))
         ((< txtang 0) (setq txtang (+ txtang (* pi 2)))))
     (if (>= (+ txtang 0.05) (/ pi 2))
         (if (< (+ txtang 0.05) (/ (* pi 3) 2))
             (progn (setq txtang (+ txtang pi))
             (setq txtpnt txtpnt-flp))))
     (setq txtang (angtos txtang 0 precision))
     (setq ext-flag nil)
     (if (= pr-text " ") () (progn
         (setq distrng (strcat pr-text distrng))
         (setq ext-flag "set")))
     (if (= ap-text " ") () (progn
         (setq distrng (strcat  distrng ap-text))
         (setq ext-flag "set")))
     (command "text" "C" txtpnt txtang distrng)
     (command "change" "L" "" "P" "C" txt-clr "")
     (setq add-fctr (+ (strlen pr-text) (strlen ap-text)))
     (setq ext-end-mod (/ (* txt-w add-fctr) 2.0))
     (if (= ext-flag "set")
         (setq ext-end (polar ext-end (angle ext-base ext-end) ext-end-mod)))
    (command "line" ext-base ext-end "")
    (command "change" "l" "" "p" "c" ext-clr "")
)
;
;
(defun save-vars ()
    (setq
       cmdecho-sv    (getvar "CMDECHO")
       expert-sv     (getvar "EXPERT")
       blipmode-sv   (getvar "BLIPMODE")
       snapmode-sv   (getvar "SNAPMODE")
       snapang-sv    (getvar "SNAPANG")
       snapbase-sv   (getvar "SNAPBASE")
       orthomode-sv  (getvar "ORTHOMODE")
    ))
;
;
(defun rest-vars ()
    (setvar "CMDECHO" cmdecho-sv)
    (setvar "EXPERT" expert-sv)
    (setvar "BLIPMODE" blipmode-sv)
    (setvar "SNAPMODE" snapmode-sv)
    (setvar "SNAPANG" snapang-sv)
    (setvar "SNAPBASE" snapbase-sv)
    (setvar "ORTHOMODE" orthomode-sv)
)
