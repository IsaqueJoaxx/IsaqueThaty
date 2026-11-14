;*
;*                     ษอออออออออออออออออออออออออป
;*                     บ  ABEAM | HBEAM | VBEAM  บ
;*                     บ          1988           บ
;*                     บ      John Mauldin       บ
;*                     บ      P.O. Box 3641      บ
;*                     บ   Greenville, SC 29608  บ
;*                     ศอออออออออออออออออออออออออผ
;*
;* ABEAM draws beam at angle w/offset ends and prompts for size
;*
;* HBEAM draws horiz. beam w/offset ends and prompts for size
;*
;* VBEAM draws vertical beam w/offset ends and prompts for size
;*
;*
;*          First NOTICE...
;*
;* The Second NOTICE is for you people who are to cheap to send $5.00
;* to me if you find this useful. Besides, who nows what I may send by
;* return mail.
;*
;*
;*          Second NOTICE...
;*
;* The information contained herein is for the benefit of licensed AutoCAD END
;* USERS everywhere. You are free to reproduce, modify and distribute the
;* information contained herein for ANY NON-COMMERCIAL use, provided such
;* reproduction/distribution is NOT FOR PROFIT, and the information contained
;* herein (or any portion thereof) is NOT ALTERED from it's original content in
;* ANY manner, shape, or form.
;*
;* You ARE NOT free to reproduce and/or distribute the information contained
;* herein (regardless of medium or form of reproduction) for profit, or to
;* include it or any portion thereof, as a part of any commercially marketed
;* goods regardless of its intended nature, scope, or form, without the
;* expressed written permission of the author.
;*
;* If you seek such permission, it will be granted provided you furnish
;* ONE fully-functional licensed sample of the finished product(s) in final
;* marketed form. In return for which you may solict and will recieve the
;* author's objective opinions, advice and insight regarding any and all
;* aspects of said product(s), free of charge, with appropriate and complete
;* written assurance of non-disclosure.
;*
;* Additional information can be obtained
;* by writing to...
;*                   John Mauldin
;*                   P.O. Box 3641
;*                   Greenville, SC  29608
;*
;*            Please send MONEY with your request <grin>
;*
;*
(defun dtr
  (d)
  (/
     (* d pi) 180.0))
(defun rtd
  (r)
  (/
     (* r 180.0) pi))
(defun c:abeam
  (/ scl p1 p2 p3 p4 p5 ang midpt angp memx)
  (setq scl
     (getvar "dimscale"))
  (setq p1
     (getpoint "\nStart Beam:"))
  (setq p2
     (getpoint "\nEnd Beam:"))
  (setq ang
     (angle p1 p2))
  (setq d1
     (distance p1 p2))
  (setq p3
     (polar p1 ang
        (- d1
           (- d1
              (*
                 (getvar "dimscale") 0.09375)))))
  (setq p4
     (polar p1 ang
        (- d1
           (*
              (getvar "dimscale") 0.09375))))
  (command "pline" p3 "w"
     (*
        (getvar "dimscale") 0.016) "" p4 "w" 0 "" "")
  (setq midpt
     (polar p1 ang
        (/
           (distance p1 p2) 2)))
  (setq angp
     (+ ang
        (dtr 90)))
  (setq p5
     (polar midpt angp
        (* scl 0.0625)))
  (setq memx
     (getstring 5 "\nEnter member designation: "))
  (command "style" "" "" "0" "" "" "" "" "" "text" "c" P5
     (*
        (getvar "dimscale")0.125)
     (rtd ang) memx))
;*
(defun c:hbeam
  (/ scl p1 p2 p3 p4 p5 ang midpt angp memx)
  (setq scl
     (getvar "dimscale"))
  (setq p1
     (getpoint "\nStart Beam:"))
  (setq d1
     (getdist p1 "\nEnd Beam: "))
  (setq p2
     (polar p1 0 d1))
  (setq ang
     (angle p1 p2))
  (setq p3
     (polar p1 ang
        (- d1
           (- d1
              (*
                 (getvar "dimscale") 0.09375)))))
  (setq p4
     (polar p1 ang
        (- d1
           (*
              (getvar "dimscale") 0.09375))))
  (command "pline" p3 "w"
     (*
        (getvar "dimscale") 0.016) "" p4 "w" 0 "" "")
  (setq midpt
     (polar p1 ang
        (/
           (distance p1 p2) 2)))
  (setq angp
     (+ ang
        (dtr 90)))
  (setq p5
     (polar midpt angp
        (* scl 0.0625)))
  (setq memx
     (getstring 5 "\nEnter member designation: "))
  (command "style" "" "" "0" "" "" "" "" "" "text" "c" P5
     (*
        (getvar "dimscale") 0.125)
     (rtd ang) memx))
;*
(defun c:vbeam
  (/ scl p1 p2 p3 p4 p5 ang midpt angp memx)
  (setq scl
     (getvar "dimscale"))
  (setq p1
     (getpoint "\nStart Beam:"))
  (setq d1
     (getdist p1 "\nEnd Beam: "))
  (setq p2
     (polar p1
        (dtr 90) d1))
  (setq ang
     (angle p1 p2))
  (setq p3
     (polar p1 ang
        (- d1
           (- d1
              (*
                 (getvar "dimscale") 0.09375)))))
  (setq p4
     (polar p1 ang
        (- d1
           (*
              (getvar "dimscale") 0.09375))))
  (command "pline" p3 "w"
     (*
        (getvar "dimscale") 0.016) "" p4 "w" 0 "" "")
  (setq midpt
     (polar p1 ang
        (/
           (distance p1 p2) 2)))
  (setq angp
     (+ ang
        (dtr 90)))
  (setq p5
     (polar midpt angp
        (* scl 0.0625)))
  (setq memx
     (getstring 5 "\nEnter member designation: "))
  (command "style" "" "" "0" "" "" "" "" "" "text" "c" P5
     (*
        (getvar "dimscale") 0.125)
     (rtd ang) memx))
