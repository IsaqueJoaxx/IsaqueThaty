; Bubble.lsp Cloud.lsp
; 18 July 1988
; Mark Farrow
; das Werkes G.S.
;
;
;          First NOTICE...
;
; The Second NOTICE is for you people who are to cheap to send $5.00
; to me if you find this useful. Besides, who nows what I may send by
; return mail.
;
;
;          Second NOTICE...
;
; The information contained herein is for the benefit of licensed AutoCAD END
; USERS everywhere. You are free to reproduce, modify and distribute the
; information contained herein for ANY NON-COMMERCIAL use, provided such
; reproduction/distribution is NOT FOR PROFIT, and the information contained
; herein (or any portion thereof) is NOT ALTERED from it's original content in
; ANY manner, shape, or form.
;
; You ARE NOT free to reproduce and/or distribute the information contained
; herein (regardless of medium or form of reproduction) for profit, or to
; include it or any portion thereof, as a part of any commercially marketed
; goods regardless of its intended nature, scope, or form, without the
; expressed written permission of the author.
;
; If you seek such permission, it will be granted provided you furnish
; ONE fully-functional licensed sample of the finished product(s) in final
; marketed form. In return for which you may solict and will recieve the
; author's objective opinions, advice and insight regarding any and all
; aspects of said product(s), free of charge, with appropriate and complete
; written assurance of non-disclosure.
;
; Additional information can be obtained
; by writing to...
;                   Mark Farrow
;                   das Werkes G.S.
;                   2060 Tulip St.
;                   Baton Rouge, La. 70806
;
;            Please send MONEY with your request
;
; System and Restore are wider ranging versions of echo_off & echo_on
;
(defun system ()
   (setq l (getvar "clayer")
         os (getvar "osmode")
         om (getvar "orthomode")
         echo (getvar "cmdecho")
   )
   (setvar "cmdecho" 0)
   (setvar "osmode" 0)
   (setvar "orthomode" 0)
)
(defun restore ()
   (setvar "osmode" os)
   (setvar "orthomode" om)
   (command "layer" "s" l "")
   (setvar "cmdecho" echo)
   (grtext)
   (gc)
)
;
; Cloud will add an easy revision cloud routine to scope new work...
;
(defun c:cloud (/ p1 p2 mid ang ds i)   ; Clockwise Please...
   (system)
   (graphscr)
   (grtext -1 "Starting point...")
   (setq p1 (getpoint "\nStarting point <Enter> to quit...\n")
         i 2)
   (grtext -1 "To point 2...      ")
   (while (/= nil (setq p2
                     (getpoint p1
                        (strcat "To point " (itoa i) "...\r")
                     )
                  )
          )
          (setq ang (angle p1 p2)
                ds (distance p1 p2)
                mid (polar (polar p1 ang (/ ds 2)) (+ (/ pi 2) ang) (* 0.2 ds))
                i (1+ i))
          (command "arc" p1 mid p2)
          (setq p1 p2)
          (grtext -1 (strcat "To point " (itoa i) "...")))
   (restore)
   (print)
   (print)
)
;
; Bubble will make piece marking piping drawings a breeze
;
(defun c:bubble (/ p1 p2 cpt ang rad txth test)
   (defun gtext ()
      (while (= test nil)
             (setq txt (getstring "\nEnter balloon text (2 char. max.): "))
             (if (> (strlen txt) 2)
                 (princ "Invalid...Too Many Characters")
                 (setq test 1))))
   (system)
   (graphscr)
   (setq p1 (getpoint "\nEnter bubble start point..<Enter> to quit: ")
         cpt (getpoint  p1 "\nEnter balloon center location: ")
         ang (angle p1 cpt)
         rad 0.125
         p2 (polar cpt ang (- 0 rad))
         txth 0.10
         test nil)
   (command "line" p1 p2 "" "circle" cpt rad)
   (gtext)
   (command "text" "m" cpt txth 0 txt)
   (while (/= (setq test (getstring "\nMore <N> ?")) "")
          (command "circle" (setq cpt (polar cpt (/ pi 2) (* 2 rad))) rad)
          (setq test nil)
          (gtext)
          (command "text" "m" cpt txth 0 txt))
   (restore)
   (print)
)
