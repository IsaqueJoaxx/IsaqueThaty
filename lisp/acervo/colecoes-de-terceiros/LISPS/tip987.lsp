; TIP987.LSP: SWAP.LSP   Swap Two Groups of Objects   (c)1994, Hugh J. Williams

; ***************************************************
;
;   SWAP.LSP
;   Author: Hugh J. Williams, Computer Science Corp.
;
;   program to allow Acad objects to trade places..
;

(defun C:SWAP (/ SS2 PT1 PT2)
  (setvar "cmdecho" 0)
  (princ "\nFirst swap-set ")
  (setq P (ssget))
  (princ "\nSecond swap-set ")
  (setq SS2 (ssget)
        PT1 (getpoint "Base point of displacement: ")
        PT2 (getpoint PT1 "Second point of displacement: "))
  (command ".move" P "" PT1 PT2
           ".move" SS2 "" PT2 PT1
           ".redraw")
)

