; LA_INT.LSP
; Purpose: Loaded by XINT.LSP to find intersection 
; of a line with an arc/circle. Variables are defined 
; in XINT.LSP
; Version: 1.1
; Author: Glenn S. Lyford  5/20/90
; Revised: George E. Zinsmeister  10/20/90
;================================================
;
(setq
  PL1 (cdr (assoc 10 EL1))      ;start of line
  PL2 (cdr (assoc 11 EL1))      ;end of line
  C2 (cdr (assoc 10 EL2))       ;center of arc/circle
  R2 (cdr (assoc 40 EL2))       ;radius of arc/circle
  AL (angle PL1 PL2)            ;absolute angle of line
  A1 (angle PL1 C2)             ;absolute angle from first
                                ;line point to center
  A2 (abs (- A1 AL))            ;relative angle between 
                                ;line and pt-ctr line
  DC (distance PL1 C2)          ;distance from first point
                                ;to center
  D (* DC (sin A2))             ;distance of center from 
                                ;closest point of line
  PP (polar PL1 AL (* DC (cos A2)))  ;closest point of line
)
(cond
  ((< D R2)
    (progn                       ;otherwise calculate point
                                 ;of intersection
      (setq
        DP (sqrt (- (* R2 R2) (* D D))) ;distance of 
                                   ;desired point from PP
        P1 (polar PP AL DP)        ; point on one side of PP
        P2 (polar PP (+ AL pi) DP) ;point on other side 
                                   ;of PP
      )
      (if (<= (distance P1 PS1) (distance P2 PS1)) 
                                  ;set PNEAR and PFAR
        (setq PNEAR P1 PFAR P2)
        (setq PFAR P1 PNEAR P2)
      )
      (if (= NF 0) (setq P PNEAR) (setq P PFAR))
                                  ;choose PNEAR or PFAR
    ) ;end progn
  )
  ((equal D R2 (* 0.0001 (getvar "dimscale"))) (setq P PP))
                                 ;tangent point
; note that "fuzz" to determine tangency is related 
; to "dimscale" - this
; criterion can be changed according to specific application
  (T (setq P nil))            ;no intersection so return nil
);end la_int.lsp

