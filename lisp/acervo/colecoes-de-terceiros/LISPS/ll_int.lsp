; LL_INT.LSP
; Purpose: Loaded by XINT.LSP to find intersection 
; of two lines.  Variables are defined in XINT.LSP
; Version: 1.1
; Author: Glenn S. Lyford  5/20/90
; Revised: George E. Zinsmeister  10/20/90 
;==============================================
;
(setq P (inters (cdr (assoc 10 EL1)) (cdr (assoc 11 EL1))
                (cdr (assoc 10 EL2))
  (cdr (assoc 11 EL2)) nil)
) ; end ll_int.lsp

