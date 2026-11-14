; TIP997.LSP: WARP.LSP   Draw Warped Lines   (c)1994, Randall Lewis

(defun C:WARP (/ P1 P2 ST X1 Y1 X2 Y2 SX SY B X5 Y5 P3 P4)
(setq P1 (getpoint "\nUpper left corner: "))
(setq P2 (getpoint "\nLower right corner: "))
(setq ST (getint "\nStep amount: "))
(setq X1 (car P1))
(setq Y1 (cadr P1))
(setq X2 (car P2))
(setq Y2 (cadr P2))
(setq SX (/ (- X2 X1) ST))
(setq SY (/ (- Y1 Y2) ST))
(setq B 0)
(repeat ST
        (setq B (+ B 1))
        (setq X5 (+ X1 (* B SX)))
        (setq Y5 (- Y1 (* SY (- B 1))))
        (setq P3 (list X1 Y5))
        (setq P4 (list X5 Y2))
        (command "line" P3 P4 "")
)
); end warp.lsp

