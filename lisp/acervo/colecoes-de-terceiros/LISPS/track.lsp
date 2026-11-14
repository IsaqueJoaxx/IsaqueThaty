;  DEAN LANGMAID
;    12/19/90
;  REVISED ->

;*******ROUTINE TO LIST ENITITIES IN A DRAWING***********
; PT  - Current point
; SS  - Selection set of enities passing through point PT
; ENT - Current entity
; EG  - List of properties of current entity
; NAM - Current entity type
;--------------------------------------------------------

(defun c:track(/ PT SS ENT EG NAM)
  (princ "\n \nPress any key to quit.")

  ;---SET UP A CONTINUOUS LOOP----
  (while (and (setq PT (grread 5)) (= (car PT) 5))
    (setq SS (ssget (cadr PT)))

    ;----CHECK TO SEE IF ANYTHING WAS FOUND---
    (if SS (progn
      (setq ENT (ssname SS 0)
            EG  (entget ENT)
            NAM (cdr (assoc 0 EG))
            LYR (cdr (assoc 8 EG)))

       (cond ((= NAM "ARC") (doarc))
             ((= NAM "CIRCLE") (circl))
             ((= NAM "TEXT") (dotext))
             ((= NAM "INSERT") (doins))
             ((= NAM "DIMENSION") (dodim))
             (t (progn 
                  (grtext -1 NAM)
                  (grtext -2 (strcat "Layer = " (cdr (assoc 8 EG))))))
       ))

      ;----ELSE CONDITION----
;      (grtext -1 "Nothing found.")
    )
  )

  (grtext)
  (princ)
)


;****SUBROUTINE TO DISPLAY THE LENGTH OF AN ARC*****
; RD  - Radius
; SA  - Starting angle
; EA  - Ending angle
; LEN - Length of curve
;---------------------------------------------------

(defun doarc (/ RD SA EA LEN)
  (setq RD  (cdr (assoc 40 EG))
        SA  (cdr (assoc 50 EG))
        EA  (cdr (assoc 51 EG))
        EA  (cond ((>= SA EA) (+ EA (* 2 PI)))
                  (t          EA))
        LEN (* RD (- EA SA))
  )
  (grtext -1 (strcat NAM "    Layer=" LYR))
  (grtext -2 (strcat "Length = " (rtos LEN 2 2)))
)


;****SUBROUTINE TO DISPLAY RADIUS & LAYER OF A CIRCLE****

(defun circl (/ RD)
  (setq RD  (cdr (assoc 40 EG)))
  (grtext -2 (strcat "Radius=" (rtos RD 2 2) "  Layer=" LYR))
)

;****SUBROUTINE TO DISPLAY TEXT & LAYER**********

(defun dotext (/ TXT)
  (setq TXT (cdr (assoc 1 EG)))
  (grtext -1 (strcat NAM "    Layer=" LYR))
  (grtext -2 TXT)
)

;****SUBROUTINE TO DISPLAY A BLOCK NAME**********
; BNAM - Block name

(defun doins (/ BNAM)
  (setq BNAM (cdr (assoc 2 EG)))
  (grtext -1 (strcat NAM "    Layer=" LYR))
  (grtext -2 (strcat "Block name = " BNAM))
)

;****SUBROUTINE TO DISPLAY A BLOCK**********
; DIMT - Dimension text

(defun dodim (/ DIMT)
  (setq DIMT (cdr (assoc 1 EG))
        DIMT (cond ((= DIMT "") "DEFAULT")
                   (t           "DIMT")))
  (grtext -1 (strcat NAM "    Layer=" LYR))
  (grtext -2 (strcat "Dimension = " DIMT))
)
