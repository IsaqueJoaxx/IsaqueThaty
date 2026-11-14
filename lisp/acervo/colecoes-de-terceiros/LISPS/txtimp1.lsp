(defun c:txtimp (/ va)
(defun *error* (st) (prompt (strcat "error: " st "\007\n")))
(defun fa (/ vb vc ve vf vg vh vi)
(setq vb (read-line va) vc (getstring "\nstyle name <standard>: "))
(if (= vc "")(setq vc "standard"))
(setq ve (getpoint "\nstarting point: "))
(setq vf (getdist ve
     (strcat "\nheight<"
     (rtos (getvar "textsize")(getvar "lunits")(getvar "luprec")) ">: ")))
(setq z (getdist "\nenter distance between lines: "))
(if (= vf nil)(setq vf (getvar "textsize")))
(setq vg (getangle ve "\nrotation angle <0>: "))
(if (= vg nil)(setq vg 0.0))
(setq vh (getvar "aunits"))
(setvar "aunits" 4)
(setq vi (getvar "blipmode"))
(setvar "blipmode" 0)
(setvar "cmdecho" 0)
(command "text" "s" vc ve vf vg vb)
(while (/= vb nil)
     (setq ve (polar ve (+ vg (* 1.5 pi))(* (* 8 z) vf )))
     (setq vb (read-line va))
     (command "text" ve "" "" vb ))
(setvar "blipmode" vi)
(setvar "aunits" vh)
(close va))

(setq va (open (getstring "\nname of ascii file to insert: ") "r"))
(if (/= va nil)(fa)(prompt "file not found - try again\007\n")))

ble sample of the finished
; product(s) in final marketed form.  I