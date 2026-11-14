;(c)COPYRIGHT 1986 by CHASE SYSTEMS, all rights reserved.
;These are autoLISP programs written to run in conjunction
;with a menu system and autoCAD. Authored by Bruce Chase

;3 autoLISP functions used by this program are not available within
; the 2.18 version of autoCAD vs. the 2.5 version.  Changes in the
; program are noted so.



;This program is called LAYER.LSP and should reside on the autoCAD
; directory. This is the main program for selecting
; major-minor-basic layering group name and turning on autolayering.
; There is little need for this to be within the ACAD.LSP
; file since it will ultimately take up memory or paging space that
; is best suited for more heavily used programs.  

(apply '(lambda ( /  c x1)  ;may be deleted if you do not want local variables
 (setvar "cmdecho" 0)       ;turn off screen echo
 (setq c 1)                                     ;initialize cntr
 (mapcar '(lambda (x y z)
   (MENUCMD y)                                  ;call up screen menu with V2.5 only
   (prompt "\n \n \nPick ")                     ;line feeds
   (princ z)                                    ;layer "depth"
   (princ" layer name: <")                      
   (if (eval x) (princ (eval x)))               ;default choice
   (princ "> ")(setq x1 nil)
   (while (null x1)                             ;loop until choice made
    (setq x1 (substr (getstring)))             ;-------NOTE 4 
     (if (and (/= x1 "no chnge")(/= x1 ""))
      (progn
        (while (< (strlen  x1) (if (< c 3) 4 3));insure name is 4 char
                  (setq x1 (strcat x1 "#")))  
      (if (< c 3)(setq x1 (strcat  x1 "-")))    ;append "-" to name
      (set x x1)
    ))
   (setq x1 (eval x)))
   (setq c (+ c 1))                             ;increment counter
 )
 '(laymaj laymin laybas)                        ;layer depth of choice
 (list "s=laymaj" "s=laymin" "s=laybas")        ;screen menus
 (list "MAJOR" "MINOR" "BASIC")                 ;depth of layering
 )
  (setq lay (strcase (strcat laymaj laymin)))   ;string together name
  (setlay laybas)                               ;set the layer to layer name
  (MENUCMD "s=s")                               ;pull up new screen menu v2.5 ONLY
) '()                                           ;for apply lambda
)                                               ;end defun




;THE FOLLOWING TWO DEFUNS & "SETQ" BELONG IN THE ACAD.LSP FILE

;The following program calls up the automatic changing of layers within
; a "layer group". The automatic layering is accomplished by putting
; within the menu or LISP program, the SETLAY defun call with an argument,
; "HVY", "MED", "THN", "TXT", "DIM", "SYM"....per association list below.
; For example- (setlay "TXT") calls up the text layer of the current
; layer group. If there is not a CURRENT LAYER GROUP set with the
; LAYER program then the current layer remains current.

(defun setlay (x)(if lay (setlay2 x)))        ;This allows paging of SETLAY2
                                              ; by version 2.5 only if required
                                              ; without calling up the full
                                              ; program if not required.

(defun setlay2 (x / c temp clr tlay xx)      ;x must be 3 letters from the
                                             ; the assoc. list below
  (setq xx (strcase x) lay (strcase lay))    ;USE ONLY IN VERSION 2.5
                                             ; converts input to upper case.
                                             ; With version 2.18
                                             ; input must be upper case.
  (setq temp (assoc x '(                     ;temp is a number to call up color
     ("HVY" 0)("MED" 1)("THN" 2)("DIM" 3)    ; from the "CLAY" setq in ACAD.LSP
     ("SYM" 4)("TXT" 5)("HAT" 6)             ;CAR is prefix for autoLAYER call
     ("BOR" 7 "BORDER")("CEN" 8 "CENTER")("DDT" 9 "DASHDOT") ;CADDR is linetype call
     ("DSH" 10 "DASHED")("DIV" 11 "DIVIDE")("DOT" 12 "DOT")
     ("HDN" 13 "HIDDEN")("PHM" 14 "PHANTOM"))))
 (if temp (progn                             ;sets layer, color and linetype
   (setq clr (if (and clay (setq c (nth (cadr temp) clay))) c 7))
   (cond ((and layl (cadr temp))(commad "linetype" (caddr temp) "color" clr))
         ((and lay temp)
          (command "layer" "t" (setq tlay (strcat lay (car temp)))
                  "n" tlay "s" tlay;----------------------------NOTE 3 
                  "c" clr  tlay    
                  "lt" (if (> (cadr temp) 6) (caddr temp) "CONTINUOUS") tlay
        "")))                         ;end cond
    )                                 ;end progn 
    (prompt "\nImproper autoLAYER suffix ") ;catch-all for improper (setlay "XXXX")
 )                                    ;end if statement
)

;The following SETQ lists the color numbers in order of the association list
; above. This allows changeable layer colors by merely changing this SETQ.
; With some handy programming you may devise a program that asks the user for
; a specific color for a layer type, and that color or color number may be
; written to file, and called upon whenever autolayering is invoked. You may
; choose your own color number in the following SETQ to change the colors
; if you don't want to do that type of programming. Remember, the numbers
; relate to the above assoc. list. Therefor- "HVY" layer is RED (1),
; "MED" is CYAN (4) and so forth.

(setq clay (list 1 4 6 5 2 4 3 3 3 3 3 3 3 3 3 ))


;--------------------------------------END DEFUNS & SETQ FOR ACAD.LSP FILE



;LAYER2.LSP program
;This program allows for freezing, thawing, on, off etc
; of layers on a major, minor, or basic tiered level and is used
; in conjunction with the screen menus below. 

(apply '(lambda ( /  name x names temp)
  (setvar "cmdecho" 0)
  (MENUCMD (strcat "s=" (cadr x)))              ;call up screen menu V2.5 ONLY
  (setq name 1 names "")                        ;initialize values
  (prompt "\n \n \nPick ")
  (princ (caddr x))
  (princ " to ")
  (princ (car x)) 
  (while name 
     (if (/= names "")(progn 
       (princ "\nNames selected: ")
       (princ names)))
     (setq name (getstring "\n Pick from screen menu: <none> "))
     (if (or (= name "none")(= name ""))(setq name nil))
     (if name (progn 
        (setq name (cond ((= (cadr x)"laymaj")(strcat name "*"))
		         ((= (cadr x)"laymin")(strcat "????-" name "*"))  
	                 ((= (cadr x)"laybas")(strcat "????-????-" name))
			 (t (quit))
	           )                            ;end cond
	)                                       ;end setq
        (setq names (if (= names "") name (strcat names "," name)))
       ))                                       ;end progn and if
    (if (= (car x) "set")(setq name nil))
  )                                             ;end while
  (if lay (progn
     (prompt "\n \nThe current LAYER GROUP is ")
     (princ lay)
     (setq temp (substr (getstring          ;----------------------- NOTE 2
      "  Keep it current? <yes> ")))        ;----------------------- NOTE 2
     (if (or (= temp "n")(= temp "N"))      ;------------------------NOTE 2
        (setq laymaj nil laymin nil lay nil)) ;--------------------- NOTE 2
  ))                                         
   (command "layer" )
   (command "t" 0 "s" 0 (car x) names "t" (if lay lay ""))
   (if (and laybas lay)(command "n" (setq temp (strcat lay laybas));--NOTE 1
                                "s" temp "");-------------------------NOTE 1
        (command "")) ;-----------------------------------------------NOTE 1
   (MENUCMD "s=s")                     ;call up your default screen menu V2.5 ONLY
) '())


;------------------SCREEN MENUS------------
;You must put these into your menu. The last 3 screen menus may be changed to
;your layer name liking. If you change the 1st one, make sure you follow
;through with all necessary changes on the LAYER2 program.  
;all $s=xxxxxxx are for version 2.18 only- you may delete the screen menu
;calls with version 2.5 since the programs call up the screen menus as needed.

**laysystem
[autoLAYR]^c^c^c(load "layer")
[Set BSIC]^c^c(setq x '("set" "laybas" "BASIC group")) (load "layer2")
[Choose:]
[ Freeze]
[  *----]^c^c(setq x '("freeze" "laymaj" "MAJOR group")) $s=laymaj (load "layer2")
[  ?-*--]^c^c(setq x '("freeze" "laymin" "MINOR group")) $s=laymin (load "layer2")
[  ?-?-*]^c^c(setq x '("freeze" "laybas" "BASIC group")) $s=laybas (load "layer2")
[ Thaw] 
[  *----]^c^c(setq x '("thaw" "laymaj" "MAJOR group")) $s=laymaj (load "layer2")
[  ?-*--]^c^c(setq x '("thaw" "laymin" "MINOR group")) $s=laymin (load "layer2")
[  ?-?--]^c^c(setq x '("thaw" "laybas" "BASIC group")) $s=laybas (load "layer2")

[ Off]
[  *----]^c^c(setq x '("Off" "laymaj" "MAJOR group")) $s=laymaj (load "layer2")
[  ?-*--]^c^c(setq x '("Off" "laymin" "MINOR group")) $s=laymin (load "layer2")
[  ?-?-*]^c^c(setq x '("Off" "laybas" "BASIC group")) $s=laybas (load "layer2")
[ On] 
[  *----]^c^c(setq x '("On" "laymaj" "MAJOR group")) $s=laymaj (load "layer2")
[  ?-*--]^c^c(setq x '("On" "laymin" "MINOR group")) $s=laymin (load "layer2")
[  ?-?--]^c^c(setq x '("On" "laybas" "BASIC group")) $s=laybas (load "layer2")

**LAYMAJ
[Off](progn (setq laymaj nil lay nil laymin nil)(princ))

[SITE]SITE $s=laymin
[FLR0]FLR0 $s=laymin
[FLR1]FLR1 $s=laymin
[FLR2]FLR2 $s=laymin
[FLR3]FLR3 $s=laymin
[ELV1]ELV1 $s=laymin
[ELV2]ELV2 $s=laymin
[PLM0]PLM0 $s=laymin
[PLM1]PLM1 $s=laymin
[ELC0]ELC0 $s=laymin
[ELC1]ELC1 $s=laymin
[ELC2]ELC2 $s=laymin
[HVC0]HVC0 $s=laymin
[HVC1]HVC1 $s=laymin
[HVC2]HVC2 $s=laymin
[NOTE]NOTE $s=laymin

[--LAST--]^C^C^C $s=

**LAYMIN


[CABT]CABT $s=laybas
[FURN]FURN $s=laybas
[PLUM]PLUM $s=laybas
[TOFF]TOFF $s=laybas
[NOTE]NOTE $s=laybas
[ELEV]ELEV $s=laybas
[ELEC]ELEC $s=laybas
[PLMB]PLMB $s=laybas
[HVAC]HVAC $s=laybas
[REFL]REFL $s=laybas
[STRC]STRC $s=laybas
[COVR]COVR $s=laybas
[MIS1]MIS1 $s=laybas
[MIS2]MIS2 $s=laybas
[MIS3]MIS3 $s=laybas
[NOTE]NOTE $s=laybas

[--LAST--]$s=

**laybas

[DIM]DIM $s=s
[TXT]TXT $s=s
[SYM]SYM $s=s
[HAT]HAT $s=s

[LINE-CON] $s=s
[  HVY]HVY $s=s
[  MED]MED $s=s
[  THN]THN $s=s

[OTHER]
[ CEN]CEN $s=s
[ DDT]DDT $s=s
[ DOT]DOT $s=s
[ DSH]DSH $s=s
[ HDN]HDN $s=s
[ PHN]PHN $s=s










;--------------NOTES-------------------------------------------------
*NOTE 1- You may substitute the following lines if you have version 2.5
      of autoCAD:
   (if (and laybas lay)(command "M" (strcat lay laybas) "")(command ""))

*NOTE 2- The "strcase" lisp function available with version 2.5 allows:
     (setq temp (strcase (substr (getstring
         "  Keep it current? <yes> ") 1 1)))
     (if (= temp "N")(setq laymaj nil laymin nil lay nil))

*NOTE 3- With version 2.5 of autoCAD, substitute:
          "M" tlay 

*NOTE 4- With version 2.5 of autoCAD, substitute:
     (setq x1 (strcase (substr (getstring) 1 4)))

