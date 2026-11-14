(defun C:TXTIN (/ va);V1.20
;By Scott Hull, 07-24-86
;SAH Mechanical Design (415) 343-4015
;Imports ASCII text files

(defun *error* (st) (prompt (strcat "error: "  st "\007\n")))

(defun FA (/ vb vc ve vf vg vh vi)
(setq vb (read-line va) vc (getstring "\nStyle name <STANDARD>: "))
(if (= vc "") (setq vc "STANDARD"))
(setq ve (getpoint "\nStarting point: "))
(setq vf (getdist ve
   (strcat "\nHeight <"
   (rtos (getvar "TEXTSIZE") (getvar "LUNITS")  (getvar "LUPREC")) ">: ")))
(if (= vf nil) (setq vf (getvar "TEXTSIZE")))
(setq vg (getangle ve "\nRotation Angle <0>: "))
(if (= vg nil) (setq vg 0.0))
(setq vh (getvar "AUNITS"))
(setvar "AUNITS" 4)
(setq vi (getvar "BLIPMODE"))
(setvar "BLIPMODE" 0)
(setvar "CMDECHO" 0)
(command "TEXT" "S" vc ve vf vg vb)
(while (/= vb nil)
  (setq ve (polar ve (+ vg (* 1.5 pi)) (* (/ 5.0 3.0) vf )))
  (setq vb (read-line va))
  (command "TEXT" ve "" "" vb ))
(setvar "BLIPMODE" vi)
(setvar "AUNITS" vh)
(close va))

(setq va (open (getstring "\nName of ASCII file to insert: ") "r"))
(if (/= va nil) (FA) (prompt "File not found - try again\007\n")))