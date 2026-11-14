; TIP993.LSP: ONE.LSP    Numerals to Text  (c)1994, Simon White
;                        $50 BONUS WINNER

  ; One: convert a numeral to its English equivalent.
  ; Copyright 1992  Rocket Software
  ; Useful for convincing alien life forms that you are not edible.

 (defun C:ONE (/ CYCLE WHOLE NOS SET2 SET3 SET4
                          STR LAST3 STRN ONE TWO THR LNE NUM OK)
  (setvar "cmdecho" 0)
  (setq CYCLE 0)
  (setq WHOLE "")
  (setq NOS (list "" "one" "two" "three" "four"
                   "five" "six" "seven" "eight" "nine"))
  (setq SET2 (list "ten" "eleven" "twelve" "thirteen" "fourteen"
                   "fifteen" "sixteen" "seventeen" "eighteen" "nineteen"))
  (setq SET3 (list "" "" "twenty" "thirty" "forty" "fifty" "sixty"
                   "seventy" "eighty" "ninety"))
  (setq SET4 (list "" "thousand" "million" "billion" "trillion" "quadrillion"
                   "pentillion" "hexillion" "heptillion" "octillion"
                   "nonillion" "decillion" "zillion" "bazillion"))
  (setq STR (cdr (assoc 1 (entget (car (entsel "Select text:\n"))))))
  (while (> (strlen STR) 0)
  (while (< (strlen STR) 3) (setq STR (strcat "0" STR)))
  (setq LAST3 (substr STR (- (strlen STR) 2)))
  (if (> (strlen STR) 3) (setq STR (substr STR 1 (- (strlen STR) 3)))
                         (setq STR ""))
; ********************* Process next three *********************
  (setq STRN "")
  (setq ONE (substr LAST3 1 1))
  (setq TWO (substr LAST3 2 1))
  (setq THR (substr LAST3 3 1))
  (if (/= ONE "0") (setq STRN (strcat (nth (read ONE) NOS) " hundred")))
  (cond ((= TWO "1") (setq STRN (strcat STRN 
                                        (if (/= STRN "") " and " "")
                                        (nth (read THR) SET2))))
        ((= TWO "0"))
        (t (setq STRN (strcat STRN 
                              (if (= STRN "") "" " and ")
                              (nth (read TWO) SET3)))))
  (if (/= TWO "1")
      (setq STRN (strcat STRN
                   (if (and (/= STRN "") (= TWO "0") (/= THR "0")) " and " "")
                   (if (and (/= STRN "") (/= TWO "0")
                            (/= TWO "1") (/= THR "0")) " " "")
                   (nth (read THR) NOS))))
; ***** add "and" for example in one thousand and sixty two but not in 0062.
; ***** (And only do this for the last three digits, that is if cycle = 0)
  (if (and (/= STR "") (= ONE "0") (/= STRN "") (= CYCLE 0))
      (progn
            (setq LNE (strlen STR))
            (setq NUM 1)
            (setq OK ())
            (while (<= NUM LNE)
                   (if (/= (substr STR NUM 1) "0") (setq OK t))
                   (setq NUM (1+ NUM)))
            (if OK (setq STRN (strcat "and " STRN)))))
; ********************* Process three End *********************
  (setq WHOLE (strcat STRN 
                      (if (= STRN "")
                          "" 
                          (strcat " " (nth CYCLE SET4) " "))
                      WHOLE))
  (setq CYCLE (1+ CYCLE)))
  (write-line WHOLE)

(princ))

