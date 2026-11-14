;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;  CADENCE Magazine     Advanced AutoLISP Concepts  Oct/93
;;;  Bill Kramer   *BBS* 614-792-3386   *CIS* 73717,2635
;;;  
;;;  Orthographic projection to 3D
;;----------------------------------------------------------------
;;;  Listing 1:  Main function
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(prompt "\nFOLD:  Ortho to 3D base")
(defun C:FOLD ( / P1 P2            ;;temp points
                  SS_BASE SS_VIEW  ;;selection sets of windows
                  XB1 XB2 YB1 YB2  ;;extrema of base window
                  XF1 XF2 YF1 YF2  ;;extrema of flange window
                  EN1 EN2          ;;shared entity names
                  EL1 EL2)         ;;shared entity lists
  ;;
  (setvar "CMDECHO" 0)
  ;;
  (setq P1 (getpoint "\nLocate corner of window for base view: "))
  (if P1 
   (progn
     (setq P2 (getcorner P1 "  other corner: "))
     (if P2 
        (setq SS_BASE (ssget "W" P1 P2)
              XB1 (min (car P1) (car P2))
              XB2 (max (car P1) (car P2))
              YB1 (min (cadr P1) (cadr P2))
              YB2 (max (cadr P1) (cadr P2))
        )
     )
     (if SS_BASE 
      (progn
        (while 
           (progn
             (setq P3 
               (getpoint "\nLocate corner of window for side part: "))
             (if P3 
               (setq P4 (getcorner P3 "  other corner: ")) 
               (setq P3 nil))
             (if (and P3 P4) 
                (setq SS_FLNG (ssget "W" P3 P4)
                      XF1 (min (car P3) (car P4))
                      XF2 (max (car P3) (car P4))
                      YF1 (min (cadr P3) (cadr P4))
                      YF2 (max (cadr P3) (cadr P4))
                )
             )
           ) ;;end PROGN predicate test if window selected
          (while 
              (setq EN1 
               (entsel "\nPick shared entity on BASE: "))
             (setq EN2 (entsel "\nPick shared entity on SIDE: "))
             (if EN2 
               (progn
                 (setq EL1 (entget (car EN1))
                       EL2 (entget (car EN2)))
                 (if (and (= (cdr (assoc 0 EL1)) "LINE")
                          (= (cdr (assoc 0 EL2)) "LINE"))
                   (FOLD_IT)
                   (prompt "\n***Selected objects MUST be lines")
                 )
               )
             )
          ) ;;end inner while loop
        ) ;;end outer while loop
      )
     )
   )
  )
  (prompt "\nFOLD Finished")
  (princ)
)
;;----------------------------------------------------------------
;;;  Listing 2:  FOLD_IT Function
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun FOLD_IT ( / P1 P2 P3 P4 A1 A2 SIDE_BASE SIDE_FLNG SS_TMP)
   ;;retrieve the data from the entity lists
   (setq P1 (cdr (assoc 10 EL1))
         P2 (cdr (assoc 11 EL1))
         P3 (cdr (assoc 10 EL2))
         P4 (cdr (assoc 11 EL2))
         A1 (angle P1 P2)
         A2 (angle P3 P4)
   )
   ;;want lines 90 or 180 degrees, swap to correct
   (cond 
     ((equal A1 (* PI 1.5) 0.00001) ;;check for A1=270
        (setq PT P1
              P1 P2
              P2 PT)
     )
     ((or (equal A1 0.0 0.00001) ;;check for A1=0 or 360
          (equal A1 (* 2 PI) 0.00001))
        (setq PT P1
              P1 P2
              P2 PT)
     )
   )
   (cond
     ((equal A2 (* PI 1.5) 0.00001)
        (setq PT P3
              P3 P4
              P4 PT)
     )
     ((or (equal A2 0.0 0.00001)
          (equal A2 (* 2 PI) 0.00001))
        (setq PT P3
              P3 P4
              P4 PT)
     )
   )
   ;;get angles for both lines
   (setq A1 (angle P1 P2) ;;line on base
         A2 (angle P3 P4));;line on flange
   ;;base geometry check
   (if (equal A1 (/ PI 2) 0.000001) ;;equal to 90?
      (setq SIDE_BASE  ;;yes, either left or right side
               (if (> (abs (- (car P1) XB2)) ;;X of P1 closer
                      (abs (- (car P1) XB1)));;to XB1 or XB2?
                                "L"    ;;left side [XB1]
                                "R"    ;;else right side [XB2]
               )
      )
      (setq SIDE_BASE ;;else it must be 180
               (if (> (abs (- (cadr P1) YB2)) ;;pick bottom
                      (abs (- (cadr P1) YB1)));;or top
                                "B"
                                "T"
               )
      )
   )
   ;;flange geometry check
   (if (equal A2 (/ PI 2) 0.000001)
       (setq SIDE_FLNG
                (if (> (abs (- (car P3) XF2))
                       (abs (- (car P3) XF1)))
                                "L"
                                "R"
                )
       )
       (setq SIDE_FLNG
                (if (> (abs (- (cadr P3) YF2))
                       (abs (- (cadr P3) YF1)))
                                "B"
                                "T"
                )
       )
   )

   ;;remove shared line from SS_FLNG, no need to copy
   (ssdel (car EN2) SS_FLNG)

   (if (equal A1 A2 0.000001) ;;views side by side?
       ;;;yes, mirror and copy or just copy?
       (if (= SIDE_BASE SIDE_FLNG) ;;must mirror objects
           (setq SS_TMP (do_mirror SS_FLNG P3 P4))
           (setq SS_TMP (do_copy SS_FLNG P3 P3))
       )
       ;;else we gotta rotate into position...
       (cond
         ((= SIDE_BASE "R")
             (cond
               ((= SIDE_FLNG "T")
                      (setq SS_TMP (do_copy_rotate SS_FLNG P4 (/ PI 2))
                            P3 P4))
               ((= SIDE_FLNG "B")
                      (setq SS_TMP (do_copy_rotate SS_FLNG P3 (/ PI -2))))
             ))
         ((= SIDE_BASE "L")
             (cond
               ((= SIDE_FLNG "T")
                      (setq SS_TMP (do_copy_rotate SS_FLNG P3 (/ PI -2))))
               ((= SIDE_FLNG "B")
                      (setq SS_TMP (do_copy_rotate SS_FLNG P4 (/ PI 2))
                            P3 P4))
             ))
         ((= SIDE_BASE "T")
             (cond
               ((= SIDE_FLNG "L")
                      (setq SS_TMP (do_copy_rotate SS_FLNG P3 (/ PI 2))))
               ((= SIDE_FLNG "R")
                      (setq SS_TMP (do_copy_rotate SS_FLNG P4 (/ PI -2))
                            P3 P4))
             ))
         ((= SIDE_BASE "B")
             (cond
               ((= SIDE_FLNG "L")
                      (setq SS_TMP (do_copy_rotate SS_FLNG P4 (/ PI -2))
                            P3 P4))
               ((= SIDE_FLNG "R")
                      (setq SS_TMP (do_copy_rotate SS_FLNG P3 (/ PI 2))))
             ))
       ) ;;ends COND
   )
   ;;move copy of flange next to base
   (command "_MOVE" SS_TMP "" P3 P1) 
   ;;rotate copy of flange graphics 90 around axis of base line
   (cond
      ((= SIDE_BASE "R")
          (do_rot_@_Y SS_TMP P1 (/ PI 2)))
      ((= SIDE_BASE "L")
          (do_rot_@_Y SS_TMP P1 (* PI 1.5)))
      ((= SIDE_BASE "T")
          (do_rot_@_X SS_TMP P1 (/ PI 2)))
      ((= SIDE_BASE "B")
          (do_rot_@_X SS_TMP P1 (* PI 1.5)))
   )
   (command "_REDRAW")
)

;;----------------------------------------------------------------
;;;  Listing 3: DO AutoCAD commands
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DO_MIRROR (SS P1 P2)
  (setq E_MARK (MARK_PLACE))
  (command "_MIRROR" SS "" P1 P2 "N")
  (MARK_SS E_MARK)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  
(defun DO_COPY (SS P1 P2)
  (setq E_MARK (MARK_PLACE))
  (command "_COPY" SS "" P1 P2)
  (MARK_SS E_MARK) 
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DO_COPY_ROTATE (SS P1 ROT)
  (setq E_MARK (MARK_PLACE))
  (command "_COPY" SS "" P1 P1)
  (setq SS (MARK_SS E_MARK))
  (command "_ROTATE" SS "" P1 (angtos ROT))
  SS
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DO_ROT_@_X (SS P1 ROT)
  (setq E_MARK (MARK_PLACE))
  (command "_COPY" SS "" P1 P1)
  (setq SS (MARK_SS E_MARK))
  (command "_UCS" "_Y" 90
           "_ROTATE" SS "" (trans P1 0 1) (angtos ROT)
           "_UCS" "_W")
  SS
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun DO_ROT_@_Y (SS P1 ROT)
  (setq E_MARK (MARK_PLACE))
  (command "_COPY" SS "" P1 P1)
  (setq SS (MARK_SS E_MARK))
  (command "_UCS" "_X" 90
           "_ROTATE" SS "" (trans P1 0 1) (angtos ROT)
           "_UCS" "_W")
  SS
)
;;----------------------------------------------------------------
;;;  Listing 4: Mark place and selection set make
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun MARK_PLACE ()
  (command "_POINT" (list 0 0 0))
  (entlast)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun MARK_SS (EN / E SS_T)
  (setq E (entnext EN)
        SS_T (ssadd E)
  )
  (while (setq E (entnext E))
    (ssadd E SS_T))
  (entdel EN)
  SS_T
)
(princ)
