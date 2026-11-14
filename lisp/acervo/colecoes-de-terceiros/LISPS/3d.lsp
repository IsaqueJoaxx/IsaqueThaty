
; 3D cones, domes, dishes and spheres for AutoCAD 2.6

; by Simon Jones - Autodesk UK Ltd.
; and Duff Kurland - Autodesk, Inc.
; November, 1986

; Save system variables
(defun VARGET ()
   (setq elevation-v (getvar "ELEVATION"))
   (setq thickness-v (getvar "THICKNESS"))
   (setq cmdecho-v   (getvar "CMDECHO"))
   (setq blipmode-v  (getvar "BLIPMODE"))
   (setq highlight-v (getvar "HIGHLIGHT"))
)

; Restore system variables
(defun RESETVAR ()
   (setvar "ELEVATION" elevation-v)
   (setvar "THICKNESS" thickness-v)
   (setvar "CMDECHO"   cmdecho-v)
   (setvar "BLIPMODE"  blipmode-v)
   (setvar "HIGHLIGHT" highlight-v)
)

; Convert degrees to radians
(defun DTR (a)
   (* pi (/ a 180.0))
)

; Calculate new radius for dome/dish/sphere
(defun CALC-R (y)
   (sqrt (- (* rad rad) (* y y)))
)

; Select all entities added since checkpoint.
(defun SELSTUFF (e / ss)
   (gc)
   (setq ss (ssadd))                  ; Form empty selection-set
   (if (null e)                       ; No previous stuff in drawing?
       (setq ss (ssadd (setq e (entnext)) ss))  ; Start with what we drew
   )
   (while (setq e (entnext e))        ; Scan until end of drawing
      (setq ss (ssadd e ss))          ; Add each entity to selection-set
   )
   ss                                 ; Return selection-set
)

; Form a 3-point cone face
(defun 3-CONE-SEG ()
   (setq pt2 (polar cen 0.0 max-rad))
   (setq pt3 (polar cen (dtr (/ 360.0 numseg)) max-rad))
   (command "3DFACE"
            (list (car cen) (cadr cen) (+ elev h))
            (list (car pt2) (cadr pt2) elev)
            (list (car pt3) (cadr pt3) elev)
            ""
            ""
   )
)

; Form a 4-point chopped-cone face
(defun 4-CONE-SEG ()
   (setq pt1 (polar cen 0.0 min-rad))
   (setq pt2 (polar cen 0.0 max-rad))
   (setq pt3 (polar cen (dtr (/ 360.0 numseg)) max-rad))
   (setq pt4 (polar cen (dtr (/ 360.0 numseg)) min-rad))
   (command "3DFACE"
            (list (car pt1) (cadr pt1) (+ elev h))
            (list (car pt2) (cadr pt2) elev )
            (list (car pt3) (cadr pt3) elev )
            (list (car pt4) (cadr pt4) (+ elev h))
            ""
   )
)

; Build upper or lower hemisphere from chopped cones
; with decreasing radii.
(defun HEMISPHERE (which)
   (setq h2 (/ rad 4.0))
   (if (eq which "lower")             ; Doing lower hemisphere?
       (setq h2 (- h2))               ; Yes, use negaitve height
   )
   (setq elev elevation-v h1 0 h (- h2 h1))
   (while (> (* rad rad) (* h2 h2))
          (setq max-rad (calc-r h1) min-rad (calc-r h2) h (- h2 h1))
          (4-cone-seg)
          (setq h1 h2 h2 (+ h2 (* h 0.85)))
          (setq elev (+ elev h) h (- h2 h1))
   )

   ; Now top it off.

   (setq max-rad (calc-r h1))
   (if (eq which "upper")
      (setq h (- (+ elevation-v rad) elev))
      (setq h (- (- elevation-v rad) elev))
   )
   (3-cone-seg)
)

; Draw a 3D cone
(defun C:CONE (/ cen elev h max-rad min-rad pt2 pt3 rad numseg)
   (varget)
   (setvar "THICKNESS" 0)
   (setvar "CMDECHO"   0)
   (setvar "HIGHLIGHT" 0)
   (setq elev elevation-v)
   (initget (+ 1 16))                 ; Center point - 3D okay, cannot be null
   (setq cen     (getpoint "\nCenter point: "))
   (initget 3)                        ; Height cannot be zero or null
   (setq h       (getdist cen "\nHeight: "))
   (initget 7)                        ; Base radius cannot be zero, neg, null
   (setq max-rad (getdist cen "\nBase radius: "))
   (command "CIRCLE" cen max-rad)
   (initget 4)                        ; Top radius cannot be negative
   (setq min-rad (getdist cen "\nTop radius <0>: "))
   (if (= min-rad 0)
       (setq min-rad nil)
   )
   (if min-rad
       (progn
          (setvar "ELEVATION" (+ elev h))
          (command "CIRCLE" cen min-rad)
          (setvar "ELEVATION" elev)
       )
   )
   (initget 6)                        ; Cannot have zero or negative segs
   (setq numseg (getint "\nNumber of segments <15>: "))
   (if (null numseg)
       (setq numseg 15)
   )
   (setvar "BLIPMODE" 0)
   (if min-rad
       (4-cone-seg)                   ; chopped off point
       (3-cone-seg)                   ; full point
   )
   (command "ARRAY" "Last" "" "Polar" cen numseg "360" "")
   (resetvar)
   (princ)
)

; Generate a sphere or a hemisphere (dome/dish)
(defun DOMSPH (which / cen e elev h h1 h2 max-rad min-rad numseg rad)
   (varget)
   (setvar "THICKNESS" 0)
   (setvar "CMDECHO"   0)
   (setvar "HIGHLIGHT" 0)
   (initget (+ 1 16))                 ; Center point - 3d okay, cannot be null
   (setq cen (getpoint "\nCenter point: "))
   (initget 7)                        ; Radius cannot be zero, neg, or null
   (setq rad (getdist cen "\nRadius: "))
   (setvar "BLIPMODE" 0)
   (initget 6)                        ; Cannot have zero or negative segs
   (setq numseg (getint "\nNumber of segments <15>: "))
   (if (null numseg)
       (setq numseg 15)
   )
   (setq e (entlast))                 ; Take database checkpoint
   (if (= (logand which 1) 1)         ; If sphere or dome,
       (hemisphere "upper")           ;   do upper hemisphere
   )
   (if (= (logand which 2) 2)         ; If sphere or dish,
       (hemisphere "lower")           ;   do lower hemisphere
   )
   (command "ARRAY" (selstuff e) "" "Polar" cen numseg "360" "")
   (resetvar)
)

; Draw a 3D dome (upper hemisphere)
(defun C:DOME ()
   (domsph 1)
)

; Draw a 3D dish (lower hemisphere)
(defun C:DISH ()
   (domsph 2)
)

; Draw a sphere
(defun C:SPHERE ()
   (domsph 3)
)

; Draw a torus
(defun C:TORUS ()
   (varget)
   (setvar "THICKNESS" 0)
   (setvar "CMDECHO"   0)
   (setvar "HIGHLIGHT" 0)
   (initget (+ 1 16))                 ; Center point - 3D okay, cannot be null
   (setq cen (getpoint "\nCenter point: "))
   (initget 7)                        ; Radius cannot be zero, neg, or null
   (setq radl (getdist cen "\nLarge radius: "))
   (initget 7)
   (initget 6)                        ; Cannot have zero or negative segs
   (setq numlseg (getint "\nNumber of segments <15>: "))
   (if (null numlseg)
       (setq numlseg 15)
   )
   (setq rads (getdist cen "\nSmall radius: "))
   (setvar "BLIPMODE" 0)
   (initget 6)                        ; Cannot have zero or negative segs
   (setq numsseg (getint "\nNumber of segments <15>: "))
   (if (null numsseg)
       (setq numsseg 15)
   )
   (setq e (entlast))                 ; Take database checkpoint
   (setq deltas (* 2.0 (/ pi numsseg)))
   (setq deltal (* 2.0 (/ pi numlseg)))
   (setq cosa (cos deltal))
   (setq sina (sin deltal))
   (setq xorg (car cen))
   (setq yorg (cadr cen))
   (if (null (setq zorg (caddr cen)))
      (setq zorg (getvar "ELEVATION"))
   )   
   (setq x (+ radl rads))
   (setq px1 (+ x xorg))
   (setq py1 yorg)
   (setq pz1 zorg)
   (setq px2 (+ xorg (* x cosa)))
   (setq py2 (+ yorg (* x sina)))
   (setq pz2 pz1)
   (command "3DFACE"
      (list px1 py1 pz1)
      (list px2 py2 pz2)
   )
   (setq j 1)
   (setq flop 0)
   (while (<= j numsseg)
      (setq beta (* j deltas))
      (setq x (+ radl (* rads (cos beta))))
      (setq px3 (+ xorg (* x cosa)))
      (setq py3 (+ yorg (* x sina)))
      (setq pz3 (+ zorg (* rads (sin beta))))
      (setq px4 (+ xorg x))
      (setq py4 yorg)
      (setq pz4 pz3)
      (if (= 1 flop)
         (command 
            (list px4 py4 pz4)
            (list px3 py3 pz3)
         )
         (command 
            (list px3 py3 pz3)
            (list px4 py4 pz4)
         )
      )
      (setq flop (- 1 flop))
      (setq j (+ j 1))
   )
   (command "")
   (command "ARRAY" (selstuff e) "" "Polar" cen numlseg "360" "Y")
)
