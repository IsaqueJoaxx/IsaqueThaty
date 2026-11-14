;__________________________________________________________
; BUST TEXT.LSP
; Break a text string
; 10/31/86
;__________________________________________________________

(defun c:expltext ()
  (setvar "CMDECHO" 0)
  (setq bm (getvar "BLIPMODE"))
  (setvar "BLIPMODE" 0)
  (command "STYLE" "MONO" "MONOTXT" "0" "1" "0" "N" "N")
  (setq r-d (/ 360 pi 2))
  (setq a (ssget))
  (setq b (ssname a 0))
  (setq c (entget b))
  (setq txts (assoc '1. c))
  (setq tht (assoc '40. c))
  (setq pt1 (assoc '10. c))
  (setq pt (cdr pt1))
  (setq ht (cdr tht))
  (setq rot (assoc '50. c))
  (setq ang (cdr rot))
  (setq rang (* ang r-d))
  (setq f (cdr txts))
  (setq g (substr f 1))
  (setq LG (strlen g))
  (setq x 0)
  (setq st1 (getstring "STYLE to use for exploded text <MONO> "))
  (if (= st1 "") (setq st1 "mono"))
  (command "erase" "p" "")
  (tryit)
)

(defun tryit ()
; G is the total text string, with length lg
; x = beginning point of substring in g
  (setq x (+ 1 x))
  (setq test (substr g x 1))
  (terpri)
  (command "text" "s" st1 pt ht rang test)
  (setq pt (polar pt ang ht))
  (if (< x lg) (tryit) (cont))
)

(defun cont ()
  (TERPRI)
  (SETVAR "CMDECHO" 1)
  (setvar "BLIPMODE" BM)
)

(prompt "  Command: EXPLTEXT  - Explode a text string into individual letters ")

