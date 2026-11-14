; TRVRS.lsp  Makes TRaVeRSe with getbrg.lsp [L Ramsey 88]
(defun c:tr () (setvar "cmdecho" 0)
  (setq sp (getpoint "Start point...  "))
  (c:gb)  ; or Transfer the bearing program
  (setq brg (* (/ brg 180) pi)
        dst (getreal "\nDistance in feet.dec  ")
        np (polar sp brg dst) )
  (command "LINE" sp np "")
  (setvar "cmdecho" 1) (princ)
 )
