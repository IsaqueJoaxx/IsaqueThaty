; AA_INT.LSP
; Purpose: Loaded by XINT.LSP to find intersection of
; an arc/circle with an
; arc/circle.  Variables are defined in XINT.LSP
; Version: 1.1
; Author: Glenn S. Lyford  5/20/90
; Revised: George E. Zinsmeister  10/20/90 
;===============================================
;
(setq
  C1 (cdr (assoc 10 EL1))   ;center of first arc/circle
  C2 (cdr (assoc 10 EL2))   ;center of second arc/circle
  R1 (cdr (assoc 40 EL1))   ;radius of first arc/circle
  R2 (cdr (assoc 40 EL2))   ;radius of second arc/circle
  DC (distance C1 C2)       ;distance between centers
  DR (+ R1 R2)              ;sum of the radii
  A1 (angle C1 C2)          ;angle of line between centers
)

(if (< R1 R2)   ;swap so signs will be correct for finding A2
  (setq R R2 R2 R1 R1 R C1 C2 A1 (+ pi A1)))
(cond
  ((< DC DR)
    (if (and (>= (+ DC R1) R2) (>= (+ DC R2) R1)) 
                               ;calculate if one is
      (progn                   ;not completely inside
        (setq                  ;the other
          S (/ (+ R1 R2 DC) 2) ;half the perimeter
          D (- (* R2 R2) (* R1 R1) (* DC DC))
                               ;difference of squares
          A2 (atan (/ (sqrt (* S (- S R1) (- S R2) 
                   (- S DC))) D -0.25))
                               ;angle of triangle at C1
          P1 (polar C1 (- A1 A2) R1) ;first intersection
          P2 (polar C1 (+ A1 A2) R1) ;second intersection
        )
        (if (<= (distance P1 PS1) (distance P2 PS1)) 
                               ; set PNEAR and PFAR
          (setq PNEAR P1 PFAR P2)
          (setq PFAR P1 PNEAR P2)
        )
        (if (= NF 0) (setq P PNEAR) (setq P PFAR)) 
                               ; choose PNEAR or PFAR
      ) ;end progn
      (setq P nil)       ;one is inside other so return nil
    ) ;end if
  )
  ((equal DC DR (* 0.0001 (getvar "dimscale"))) 
                         ;tangent point
    (setq P (polar C1 A1 R1))
  )
  ; note that "fuzz" to determine tangency is related 
  ; to "dimscale" - this criterion can be changed 
  ; according to specific application
  (setq P nil)           ;too far apart
) ; end aa_int.lsp

