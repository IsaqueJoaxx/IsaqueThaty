(vmon)
;GET INFORMATION FOR BOX
(defun inbox ()
     (setq l (getdist "\nENTER LENGTH OF BOX "))
     (setq h (getdist "\nENTER HEIGHT OF BOX "))
     (setq wt (getdist "\nENTER WALL THICKNESS "))
     (setq tst (getdist "\nENTER TOP SLAB THICKNESS "))
     (setq bst (getdist "\nENTER BOTTOM SLAB THICKNESS "))
     (setq fe (getdist "\nENTER FOOTING EXTENSION "))
     (setq swtofd (getdist "\nENTER WALL OUTSIDE FACE DOWEL LENGTH "))
     (setq swtifd (getdist "\nENTER WALL INSIDE FACE DOWEL LENGTH "))
     (setq ol (+ l (* 2 wt)))
     (setq oh (+ h tst))
     (setq el (getdist "\nWHAT IS THE TOP OF THE BOTTOM SLAB ELEVATION? "))
)

;DRAW PLANS
(defun plans ()
     (setq spoy (+ 10 bst oh))
     ;SETUP COORDINATES OF OUTSIDE OF BOX
     (setq s 10)
     (setq spx1 (+ s ol))
     ;SETUP COORDINATESOF INSIDE WALL FOR SECTIONAL PLAN
     (setq spx2 (+ s wt))
     (setq spx3 (+ spx2 l))
     ;SETUP COORDINATES FOR FOOTING EXTENSION
     (setq spx4 (- s fe))
     (setq spx5 (+ spx1 fe))
)
;DRAW SECTOIN 
(defun sec1 ()
     ;SETUP COORDINATES FOR OUTSIDE CONCRETE
     (setq s1x1 (- s fe))
     (setq s1x2 (+ s1x1 (* 2 fe) ol))
     (setq s1x3 (- s1x2 fe))
     (setq s1x4 (+ s1x1 fe))
     (setq s1x5 (+ s1x4 wt))
     (setq s1x6 (+ s1x5 l))
     (setq sy1 5)
     (setq sy2 (+ sy1 bst))
     (setq sy3 (+ sy2 oh))
     (setq sy4 (- sy3 tst))
     (setq s11 (list s1x1 sy1))   ;outside concrete
     (setq s12 (list s1x2 sy1))
     (setq s13 (list s1x2 sy2))
     (setq s14 (list s1x3 sy2))
     (setq s15 (list s1x3 sy3))
     (setq s16 (list s1x4 sy3))
     (setq s17 (list s1x4 sy2))
     (setq s18 (list s1x1 sy2))
     (setq s19 (list s1x5 sy2))  ;inside concrete
     (setq s120 (list s1x6 sy2))
     (setq s121 (list s1x6 sy4))
     (setq s122 (list s1x5 sy4))
     ;SETUP COORDINATES OF WALL REINF
     (setq s1rx1 (- s1x5 0.16))   ;if dowel start
     (setq s1rx2 (+ s1x4 0.16))   ;of dowel start
     (setq s1rx3 (- s1rx1 0.08))   ;if wall steel start 
     (setq s1dx1 (- s1rx3 0.08))
     (setq s1rx4 (+ s1rx2 0.08))   ;of wall steel start 
     (setq s1dx2 (+ s1rx4 0.08))
     (setq s1dy1 (+ sy2 0.25))
     (setq s1dy2 (- sy4 0.25))
     (setq s1ry1 (+ sy1 bst swtifd))  ;if dowel start
     (setq s1ry2 (+ sy1 bst swtofd))  ;of dowel start
     (setq s1ry3 (+ sy1 0.42))     ;if vert dowel end
     (setq s1ry4 (+ sy1 0.33))     ;of vert dowel end
     (setq s1ry5 (+ s1ry1 0.16))   ;if end of bend around dowel for wall steel
     (setq s1ry6 (+ s1ry2 0.16))   ;of end of bend around   "
     (setq s1ry7 (- sy3 0.42))     ;end of if wall steel
     (setq s1ry8 (- sy3 0.33))     ;end of of wall steel
     (setq s1r1 (list s1rx1 s1ry1))   ;if dowel
     (setq s1r2 (list s1rx1 s1ry3))    
     (setq s1r3 (list s1rx2 s1ry2))   ;of dowel
     (setq s1r4 (list s1rx2 s1ry4))
     (setq s1r5 (list s1rx3 sy2))   ;if wall reinf
     (setq s1r6 (list s1rx3 s1ry1))
     (setq s1r7 (list s1rx1 s1ry5))
     (setq s1r8 (list s1rx1 s1ry7))
     (setq s1r9 (list s1rx4 sy2))    ;of wall reinf
     (setq s1r10 (list s1rx4 s1ry2))
     (setq s1r11 (list s1rx2 s1ry6))
     (setq s1r12 (list s1rx2 s1ry8))
     (setq s1dx3 (+ s1x4 0.25))        ;top slab dots
     (setq s1dx4 (+ s1x1 0.25))        ;bot slab dots
     (setq s1dy3 (+ sy1 0.33))        ;bot slab bot dot
     (setq s1dy4 (- sy2 0.25))        ;bot slab top dot
     (setq s1dy5 (+ sy4 0.25))       ;top slab bot dot
     (setq s1dy6 (- sy3 0.25))      ;top slab top dot
     (setq s1d1 (list s1dx1 s1dy1))   ;bot wall dots
     (setq s1d2 (list s1dx2 s1dy1))
     (setq s1d3 (list s1dx1 s1dy2))  ;top wall dots
     (setq s1d4 (list s1dx2 s1dy2))
     (setq s1d5 (list s1dx4 s1dy3))    ;bot slab dots
     (setq s1d6 (list s1dx4 s1dy4))
     (setq s1d7 (list s1dx3 s1dy5))   ;top slab dots
     (setq s1d8 (list s1dx3 s1dy6))
     ;DRAW WALL REINF
     (command "layer" "s" "s1" \r
              "line" s1r1 s1r2 "@1<180" ""
              "line" s1r3 s1r4 "@1<0" ""
              "line" s1r5 s1r6 s1r7 s1r8 ""
              "line" s1r9 s1r10 s1r11 s1r12 "@1<0" ""
              "insert" "/acad/strblk/dot" s1d1 1.5 1.5 0
              "insert" "/acad/strblk/dot" s1d2 1.5 1.5 0
              "insert" "/acad/strblk/dot" s1d3 1.5 1.5 0
              "insert" "/acad/strblk/dot" s1d4 1.5 1.5 0
              "insert" "/acad/strblk/dot" s1d5 1.5 1.5 0
              "insert" "/acad/strblk/dot" s1d6 1.5 1.5 0
              "insert" "/acad/strblk/dot" s1d7 1.5 1.5 0
              "insert" "/acad/strblk/dot" s1d8 1.5 1.5 0)
     ;MIRROR WALL REINF TO OTHER SIDE
     (setq s1x (+ s1x5 (/ l 2)))
     (setq smw2 (list s1x spoy))
     (setq smp1 (list s1x 0))
     (setq smp2 (list s1x 1))
     ;INSERT KEYS
     (command "insert" "/acad/strblk/wskey" s17 wt 1 0
              "insert" "/acad/strblk/key" s122 wt 1 180  
              "mirror" "w" '(0 0) smw2 "" smp1 smp2 "")
     ;DRAW CONCRETE
     (command "layer" "s" "g3" \r
              "line" s11 s12 s13 s14 s15 s16 s17 s18 s11 ""
              "line" s19 s120 s121 s122 s19 "")
     ;SETUP DIMENSION LINE LOCATIONS
     (setq wodloc (list (- s 9) sy1))
     (setq wofdloc (- s 2))
     (setq wifdloc (+ s1x5 2))
     (setq ofds (list wofdloc sy2))
     (setq ifds (list wifdloc sy2))  
     (setq ods1 (rtos (* 12 bst) 4 1))
     (setq ods2 (rtos (* 12 h) 4 1))
     (setq ods3 (rtos (* 12 tst) 4 1))
     (setq ods4 (rtos (* 12 swtofd) 4 1))
     (setq ods5 (rtos (* 12 swtifd) 4 1))
     :DRAW DIMENSION
     (COMMAND "LAYER" "S" "sooo" \r
              "dim" "vert" s11 s18 wodloc ods1 "cont" s122 ods2 "cont" s16 ods3 "exit"
              "dim" "vert" ofds s1r3 ofds ods4 "exit"
              "dim" "vert" ifds s1r1 ifds ods5 "exit")

     ;DRAW TOP AND BOTTOM SLAB REINF
     (setq s1rx5 (+ s1x1 0.16))    ;bottom rein
     (setq s1rx6 (- s1x2 0.16))    
     (setq s1rx7 (+ s1x4 0.16))    ;top reinf
     (setq s1rx8 (- s1x3 0.16))
     (setq s1ry9 (+ sy1 0.25))   ;bottom reinf
     (setq s1ry10 (- sy2 0.16))
     (setq s1ry11 (+ sy4 0.16))  ;top reinf
     (setq s1ry12 (- sy3 0.16))
     (setq s1r13 (list s1rx5 s1ry9))
     (setq s1r14 (list s1rx6 s1ry9))
     (setq s1r15 (list s1rx5 s1ry10))
     (setq s1r16 (list s1rx6 s1ry10))
     (setq s1r17 (list s1rx7 s1ry11))
     (setq s1r18 (list s1rx8 s1ry11))
     (setq s1r19 (list s1rx7 s1ry12))
     (setq s1r20 (list s1rx8 s1ry12))
     (command "layer" "s" "s1" \r
              "line" s1r13 s1r14 ""
              "line" s1r15 s1r16 ""
              "line" s1r17 s1r18 ""
              "line" s1r19 s1r20 "")
     ;FIND OUT IF THERE IS A LEFT PIPE
     (setq pleft (getstring "\nIS THERE A PIPE ON THE LEFT SIDE? (Y or N)"))
     (if (= pleft "Y") (progn
     ;GET INFO ON PIPE
     (setq plinv (getdist "\nWHAT IS THE INVERT OF THE LEFT PIPE?"))
     (setq pldiam (getdist "\nWHAT IS THE DIAMETER OF THE LEFT PIPE?"))
     ;SET UP COORDINATES OF PIPE
     (setq yplscl (+ (- plinv el) sy2 (/ pldiam 2)))
     (setq plscl (list s1x5 yplscl))
     ;INSERT PIPES
     (command "layer" "s" "g3" \r
              "insert" "/acad/strblk/rcp" plscl 4 pldiam 180))
     )
    ;FIND OUT IF THERE IS A RIGHT PIPE
     (setq pright (getstring "\nIS THERE A PIPE ON THE RIGHT SIDE? (Y or N)"))
     (if (= pright "Y") (progn
     ;GET INFO ON PIPE
     (setq prinv (getdist "\nWHAT IS THE INVERT OF THE RIGHT PIPE?"))
     (setq prdiam (getdist "\nWHAT IS THE DIAMETER OF THE RIGHT PIPE?"))
     ;SET UP COORDINATES OF PIPE
     (setq yprscl (+ (- prinv el) sy2 (/ prdiam 2)))
     (setq prscl (list s1x6 yprscl))
     ;INSERT PIPES
     (command "layer" "s" "g3" \r
              "insert" "/acad/strblk/rcp" prscl 4 prdiam 0))
     )
     ;LABEL HORIZ WALL REIN
     (setq s1dhwr (getstring "\nWHAT IS THE HORIZONTAL WALL REINFORCING? "))
     (setq s1ddloc (list (- s 7) s1dy1))
     (command "layer" "s" "sooo" \r
              "dim" "vert" s1d1 s1d3 s1ddloc s1dhwr "exit")
     ;LABEL VERT WALL REIN
     (setq ofr (getstring "\nWHAT IS THE VERTICAL OUTSIDE FACE WALL REINF? "))
     (setq ifr (getstring "\nWHAT IS THE VERTICAL INSIDE FACE WALL REINF? "))
     (if (= pleft "Y") 
             (setq vwry (+ yplscl (/ pldiam 2) (/ (+ pldiam 1) 12) 2))
             (setq vwry (+ s1ry5 (/ (distance s1r7 s1r8) 2)))
     )
     (setq ofvwr (list s1rx2 vwry))
     (setq ifvwr (list s1rx1 vwry))
     (command "dim" "leader" ofvwr "@2<150" "" ofr "exit"
              "dim" "leader" ifvwr "@2<30" "" ifr "exit")
     ;LABEL TOP AND BOTTOM SLAB REINF
     (setq tstr (getstring "\nWHAT IS THE TOP SLAB TOP REINF? "))
     (setq tsbr (getstring "\nWHAT IS THE TOP SLAB BOTTOM REINF? "))
     (setq bstr (getstring "\nWHAT IS THE BOTTOM SLAB TOP REINF? "))
     (setq bsbr (getstring "\nWHAT IS THE BOTTOM SLAB BOTTOM REINF? "))
     (setq rxloc (+ s1rx7 (/ (distance s1r19 s1r20) 2)))
     (setq tstrloc (list rxloc s1ry12))
     (setq tsbrloc (list rxloc s1ry11))
     (setq bstrloc (list rxloc s1ry10))
     (setq bsbrloc (list rxloc s1ry9))
     (command "dim" "leader" tstrloc "@2<30" "" tstr "exit"
              "dim" "leader" tsbrloc "@2<210" "" tsbr "exit"
              "dim" "leader" bstrloc "@2<30" "" bstr "exit"
              "dim" "leader" bsbrloc "@2<210" "" bsbr "exit")
)


(defun C:BOXSEC ()
     (inbox)
     (plans)
     (sec1)
)
