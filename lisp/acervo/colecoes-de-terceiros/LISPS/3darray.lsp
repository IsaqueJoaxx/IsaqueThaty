;**************************** 3DARRAY.LSP ********************************

;       By Simon Jones   Autodesk Ltd,London   March 1987

;  Functions included:
;       1) Rectangular ARRAYS (rows, columns & levels)
;       2) Orthogonal Circular ARRAYS around either X or Y axis
;       3) Orthogonal ROTATION around either X or Y axis

;  All are loaded by: (Load "3darray")

;  And run by:
;       Command: 3darray
;       Command: Array/Rotate: (select appropriate command)

;  NOTE: Only 3DFACES,3DLINES,LINES & SOLIDS will be edited with
;        POLAR ARRAYS and ROTATIONS all others will be ignored.

;***********************************************************************

(vmon)
(prompt "\nLoading. Please wait...")
(terpri)

(defun MODES (a)
   (setq MLST '())
   (repeat (length a)
      (setq MLST (append MLST (list (list (car a) (getvar (car a))))))
      (setq a (cdr a)))
)

(defun MODER ()
   (repeat (length MLST)
      (setvar (caar MLST) (cadar MLST))
      (setq MLST (cdr MLST))
   )
)

(defun *ERROR* (st)
  (moder)
  (terpri)
  (princ "\nError: ")
  (princ st)
  (princ)
)

;*************************** DTR ***************************

;  Convert degrees to radians
(defun DTR (a)
  (* pi (/ a 180.0))
)

;************************** TURN ***************************

  ; Retrieve and rotate point around specified axis

(defun TURN (n / pt ip2 z38)
          (setq pt (cdr (assoc n elist)))
          (if (or
                (= (cdadr elist) "LINE")
                (= (cdadr elist) "SOLID")
              )
              (progn
                (setq z38 (cdr (assoc 38 elist)))
                (if (null z38) (setq z38 0))
                (setq pt (append pt (list z38)))
              )
          )
          (if (= flag 0)          ;flag set by ORTH-AX
              (setq ip2 (list (cadr pt) (caddr pt)))
              (setq ip2 (list (car pt) (caddr pt)))
          )
          (setq ip2 (polar ip1
                           (+ ang (angle ip1 ip2))
                           (distance ip1 ip2)
                    )
          )
          (if (= flag 0)
              (list (car pt) (car ip2) (cadr ip2))
              (list (car ip2) (cadr pt) (cadr ip2))
          )
)

;************************* ORTH-AX *************************

   ; Define orthogonal axis of rotation

(defun ORTH-AX ()
   (setvar "ORTHOMODE" 1)

     ; define base points
   (initget (+ 1 16))
   (setq bpt1 (getpoint "\nFirst point of rotationl axis: "))
   (setvar "ELEVATION" (caddr bpt1))
   (initget (+ 1 16))
   (setq bpt2 (getpoint bpt1 "\nSecond point of rotational axis: "))

     ;set flag (1 = vertical  0 = horizontal)
   (setq flag (abs (fix (sin (angle bpt1 bpt2)))))
   (setq z (caddr bpt1))
)

;************************* FILTER **************************

  ; Filter 3DFACES, 3DLINES, LINES & SOLIDS of original
  ; selection set to be rotated for polar arrays and rotations

(defun FILTER (/ e elist)
   (setq e (ssname ss c))
   (setq elist (entget e))
   (cond ((eq (cdr (assoc 0 elist)) "3DFACE")
          (command "3DFACE"
                   (turn 10) (turn 11) (turn 12) (turn 13) ""
          )
          (ssadd e ss2)
          (entmod (subst (assoc 8 elist)
                         (assoc 8 (entget (entlast)))
                         (entget (entlast))))
         )
         ((eq (cdr (assoc 0 elist)) "SOLID")
          (command "3DFACE"
                   (turn 10) (turn 11) (turn 13) (turn 12) ""
          )
          (ssadd e ss2)
          (entmod (subst  (assoc 8 elist)
                          (assoc 8 (entget (entlast)))
                          (entget (entlast))))
         )
         ((or
            (eq (cdr (assoc 0 elist)) "3DLINE")
            (eq (cdr (assoc 0 elist)) "LINE")
          )
          (command "3DLINE"
                   (turn 10) (turn 11) ""
          )
          (ssadd e ss2)
          (entmod (subst (assoc 8 elist)
                         (assoc 8 (entget (entlast)))
                         (entget (entlast))))
         )
   )
   (setq c (1+ c))
)

;***************************** P-ARRAY *********************

  ; Perform polar (circular) array around either X or Y axis

(defun P-ARRAY (/ n af as ang ss2)
   (orth-ax) ; Define orthoganal axis
   (setvar "BLIPMODE" 0)
   (if (= flag 0)    ; Set imaginary base point
       (setq ip1 (list (cadr bpt1) z))
       (setq ip1 (list (car bpt1) z))
   )

   ; Define number of items in array
   (setq n nil)
   (while (<= n 1)
          (initget (+ 1 2 4))
          (setq n (getint "\nNumber of items: "))
          (if (= n 1)
              (prompt "\nError: Value must be greater than 1")
          )
   )
   (initget 2)
   (setq af (getreal "\nAngle to fill <360>: "))
   (if (= af nil) (setq af 360))
   (setq af (dtr af))
   (if (= (abs af) (* 2 pi))
       (progn
        (setq as (/ af n))
        (setq af (- af as))
       )
       (setq as (/ af (1- n)))
   )
   (setq ang as)

   (while (<= (abs ang) (abs af))
          (setq c 0)
          (setq ss2 (ssadd))
          (while (< c (sslength ss))
                 (filter)
          )
          (setq ang (+ ang as))   ; increment roatationl angle
   )

   (if (= (sslength ss2) 0)
       (prompt "\nNo suitable entities found. ")
   )
)

;***************************** 3DROTATE ********************

(defun 3drotate (/ c)
   (orth-ax)   ; Define orthogonal axis
               ;about which to rotate

   (setvar "BLIPMODE" 0)
   (if (= flag 0)    ; Set imaginary base point
       (setq ip1 (list (cadr bpt1) z))
       (setq ip1 (list (car bpt1) z))
   )
   (initget (+ 1 2))
   (setq ang (getangle "\nRotational angle: "))
   (setq c 0)
   (setq ss2 (ssadd))
   (while (< c (sslength ss))
          (filter)     ; filtering is necessary since only 3dfaces
                       ;and lines can be drawn at an angle to the
                       ;vertical axis
   )

   ; Delete only the filtered entities of the original
   ;selection set
   (setvar "HIGHLIGHT" 0)
   (if (/= (sslength ss2) 0)
       (command "ERASE" ss2 "")
       (prompt "\nNo suitable entities found. ")
   )
)


;****************************** R-ARRAY ********************

(defun R-ARRAY (/ flag nr nc nl e el c x y z)

   ; Set array parameters
   (initget (+ 2 4))
   (setq nr (getint "\nNumber of rows (---) <1>: "))
   (if (null nr) (setq nr 1))
   (initget (+ 2 4))
   (setq nc (getint "\nNumber of columns (|||) <1>: "))
   (if (null nc) (setq nc 1))
   (initget (+ 1 2 4))
   (setq nl (getint "\nNumber of levels (...): "))
   (setvar "ORTHOMODE" 1)
   (setvar "HIGHLIGHT" 0)
   (setq flag 0)    ; Command style flag
   (cond ((/= nr 1)
          (initget (+ 1 2))
          (setq y (getdist "\nDistance between rows: "))
          (setq flag 1)
         )
   )
   (cond ((/= nc 1)
          (initget (+ 1 2))
          (setq x (getdist "\nDistance between columns: "))
          (setq flag (+ flag 2))
         )
   )
   (cond  ((/= nl 1)
           (initget (+ 1 2))
           (setq z (getdist "\nDistance between levels: "))
          )
   )
   (setvar "BLIPMODE" 0)

   (setq c 1)
   (setq el (entlast))  ;Reference entity

   ; Copy the selected entities one level at a time
   (while (< c nl)
          (command "COPY" ss ""
                          "0,0,0"
                          (append (list 0 0) (list (* c z)))
          )
          (setq c (1+ c))
   )

   (setq ss2 (ssadd))          ;create a new selection set
   (setq e (entnext el))       ;of all the new entities since
   (while e                    ;the reference entity.
       (ssadd e ss2)
       (setq e (entnext e))
   )

   ; Array original selection set and copied entities
   (cond
     ((= flag 1) (command "ARRAY" ss ss2 "" "R" nr "1" y))
     ((= flag 2) (command "ARRAY" ss ss2 "" "R" "1" nc x))
     ((= flag 3) (command "ARRAY" ss ss2 "" "R" nr nc y x))
   )
)

;***************************** MAIN PROGRAM ****************

(defun C:3DARRAY (/ xx c ss ss2 z bpt1 bpt2 ip1 flag ang)

   (modes '("elevation" "cmdecho" "blipmode" "highlight" "orthomode"))
   (setvar "CMDECHO" 0)
   (command "UNDO" "MARK")
   (graphscr)

   (setq ss nil ss2 nil c 0)
   (while  (null ss)  ; Ensure selection of entities
           (setq ss (ssget))
   )

   (initget "Array Rotate")
   (setq xx (getkword "\n<Array>/Rotate: "))
   ; Branch to specific function
   (cond ((eq xx "Rotate")  (3drotate))
         (T
          (initget 1 "Rectangular Polar Circular")
          (setq xx (getkword "\nRectangular or Polar array (R/P): "))
          (cond ((eq xx "Rectangular") (r-array))
                (T (p-array))
          )
         )
   )
   (moder) ; Restore system variables
   (princ)
)
