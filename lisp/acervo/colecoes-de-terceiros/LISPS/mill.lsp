;From CADENCE Magazine November , 1991
;Page 64ff

; COOL commands & MACRO commands for BOSS10 code are  commented OUT.

;Write g1 g2 g3 g41 g42 code to file
;Global variables indentified with "#" prefix.
;UserI4 AutoCAD variable used.
;----------------------------------------------
(princ "\n")(Setq bcount 0)
 
(defun BUMP ()
  (setq bcount (1+ bcount))
  (princ (strcat "\rCad to CNC " (itoa bcount))))

(bump) 

;Calculate the Feeds & Speeds for tools.
;1=alum 2=brass 3=mild steel 4=tool steel

(defun TOOL-SET (/ cutter mtl dia rpm cutter-mtl sfm rpm fpt teeth)
  (setq #fpt (if #fpt #fpt 0.0015)                       ;Initialize
        hss '((1 300)(2 100)(3 55)(4 20))
        crb '((1 600) (2 250)(3 120)(4 100))
        #rpm (if #rpm #rpm 1000)
        #feed (if #feed #feed 6.0)
        #teeth (if #teeth #teeth 2)
        #dia (if #dia #dia 0.250)
        #mtl (if #mtl #mtl 1)
        #t# (if #t# #t# 0)
        #cutter (if #cutter   #cutter "H"))
  (princ (strcat "\n;Notes: " (getstring 1 "\nNotes: ")) #opn) ;Tool Number
  (setq tool (getint (strcat "\nTool No <" (itoa (1+ #t#)) ">: ")))
  (if (boundp 'tool)(setq #t# tool)(setq #t# (1+ #t#)))
  ;Coolant Y/N
  ;(setq #cool (if (=(strcase(getstring "\nCoolant ? Y/N <Y>: "))"N")nil T))
  ;cutter dia
  (setq dia (getreal (strcat "\nCutter dia <" (rtos #dia 2 4) ">: ")))
  (if (boundp 'dia)(setq #dia dia))
  ;Number of cutter teeth
  (setq teeth (getint (strcat "\nNo of teeth <" (itoa #teeth) ">: ")))
  (if (boundp 'teeth)(setq #teeth teeth))
  ;What are you cutting ?
  (setq mtl (getint (strcat "\nMaterial 1.Alum 2.Brass 3.Mild-Steel 4.Alloy-Steel <" (itoa #mtl) ">: ")))
  (if (boundp 'mtl)(setq #mtl mtl))
  ;What is the cutter made of ?
  (initget "H C") 
  (setq cutter (getkword (strcat "\nCutter hss/Carbide<" #cutter ">: ")))
  (if (/= cutter nil)(setq #cutter cutter))
  (if (= #cutter "H")(setq cutter-mtl hss))
  (if (= #cutter "C")(setq cutter-mtl crb))
  (setq sfm (cadr (assoc #mtl cutter-mtl)))
  ;Spindle rpm data
  (initget "C") 
  (setq rpm (getint (strcat "\nSpindle rpm.. Calculate/<" (itoa #rpm) ">: ")))
  (if (numberp rpm)(setq #rpm (min 4000 rpm)))
  (if (= rpm "C") (progn
      (setq #rpm (min 4000 (fix (* sfm (/ 3.82 #dia)))))
      (princ "\nrpm calculated to ")
      (princ #rpm)))
  ;feed per tooth
  (setq fpt (getreal (strcat "\nfeed per tooth <" (rtos #fpt 2 4) ">: ")))
  (if (boundp 'fpt)(setq #fpt fpt))
  ;feed (in inches per minute)
  (initget "C") 
  (setq feed (getreal (strcat "\nfeed.. Calculate/<" (rtos #feed) ">: ")))
  (if (numberp feed)(setq #feed feed))
  (if (= feed "C")(progn
      (setq #feed (* #fpt #rpm #teeth))
      (princ "\nfeed calculated to ")
      (princ #feed)))
  (setq #plunge (atof (rtos (/ #feed 3) 2 1))) ;plunge feed.
  (setq plunge (getreal (strcat "\nPlunge feed (Z) <" (rtos #plunge 2 1) ">: ")))
  (if (boundp 'plunge)(setq #plunge plunge))
  (prin1)
) ;END TOOL-SET

(bump) 

(defun CURVE (pt cn nx / tmpx tmpy tmpz i& j&)
  (setq tmpx (car nx)
        tmpy (cadr nx) 
        tmpz (caddr nx))             ; get new xyz
  (princ #g #opn)
  (if (not (equal tmpx #x 0.0001))   ; is the X new?
    (princ (strcat "X" (nozero (- tmpx (car #00))) " ") #opn) ; use it
    (princ "" #opn))                                  ;endif
  ; or forget it.
  (if (not (equal tmpy #y 0.0001))   ; is the Y new?
    (princ (strcat "Y" (nozero (- tmpy (cadr #00))) " ") #opn) ; use it
    (princ "" #opn))                                  ;endif
  ; or forget it.
  (if (not (equal tmpz #z 0.0001))   ; is the Z new?
    (princ (strcat "Z" (nozero (- tmpz (caddr #00))) " ") #opn) ; use it
    (princ "" #opn))                                  ;endif
  ; or forget it.
  (setq i& (nozero (- (car cn) (car #00))) ; calc Arc center
        j& (nozero (- (cadr cn) (cadr #00)))) ;endsetq
  (princ (strcat "I" i& " ") #opn)   ; print the
  (princ (strcat "J" j&) #opn)       ; arc center
  (setq #x (car nx)
        #y (cadr nx) 
        #z (caddr nx) 
        #g "g1 ")                    ; reset
) 
;end curve function

  
(bump) 

(defun RAPID (vtx / tmpx tmpy tmpz)
  (setq tmpx (car vtx)     tmpy (cadr vtx)
        tmpz (caddr vtx))            ; get new xyz
  (cond
    (#g41 (setq #g (strcat #g "g41 "))) ;Look for g41 or
    (#g42 (setq #g (strcat #g "g42 ")))) ;g42 codes.
; (princ (strcat "\n;AutoCAD Layer "#layer) #opn)
  (princ (strcat "\n" #g "X" (nozero (- tmpx (car #00))) " Y" (nozero (- tmpy (cadr #00))) " Z" (nozero (- #cp (caddr #00)))) #opn) ;
; (if (not(null #cool))(princ "\nM8" #opn))
  (princ (strcat "\nG1 Z" (nozero (- tmpz (caddr #00))) " F" (rtos #plunge 2 1)) #opn)
  (princ (strcat "\nF" (rtos #feed 2 1)) #opn)
  (setq #x (car vtx)    #y (cadr vtx)
        #z (caddr vtx)  #g "")
) ;end rapid function

(bump) 

(defun ROUND (num pl)(atof (rtos num 2 pl)))

(defun NOZERO (num / t$)
  (setvar "DIMZIN" 4)                ; output string t$
  (cond
    ((= (fix (round num 4)) (round num 4)) ; if it's a whole no.
     (setq t$ (strcat (itoa (fix (round num 4))) ".")))
    ((/= (fix (round num 4)) (round num 4)) ; if it's NOT a whole no.
     (progn
       (setvar "DIMZIN" 12)
       (setq t$ (rtos num 2 4)))
     (setvar "DIMZIN" 4)))           ;endcond
  (if (= t$ "0.")(setq t$ "0"))      ; if it was a 0,remove
  (eval t$)                          ; the "."
) 
;end nozero function

(bump) 

(defun VERTEX (vtx / tmpx tmpy tmpz found xyz$)
  (setq tmpx (car vtx) tmpy (cadr vtx) tmpz (caddr vtx)
        found nil xyz$ "")
  (if (not (equal tmpx #x 0.0001))(progn   ; is the X new?
      (setq xyz$ (strcat "X" (nozero (- tmpx (car #00))) " "))
      (setq found T)))               ;endif or forget it.
  (if (not (equal tmpy #y 0.0001))(progn   ; is the Y new?
      (setq xyz$ (strcat xyz$ "Y" (nozero (- tmpy (cadr #00))) " "))
      (setq found T)))               ;endif or forget it.
  (if (not (equal tmpz #z 0.0001))(progn   ;is the Z new?
      (setq xyz$ (strcat xyz$ "Z" (nozero (- tmpz (caddr #00)))))
      (setq found T)))               ;endif or forget it.
  (if (and #LAST (or #g41 #g42))(setq #g "g1 g40 "))
  (if found (princ (strcat "\n" #g xyz$) #opn))
  (setq #x (car vtx) #y (cadr vtx) #z (caddr vtx)); reset global xyz
  (if found (setq #g "")))
;end vertex & Gcode to none

(bump) 

(defun MILLPREP (/ mac g00 m25 c25 cp rpm tool feed)
  (setq #cp (if #cp #cp 0.02)
        #00 (if #00 #00 '(0.0 0.0 0.0))
        #END (if #end #end "G0 M25"))  ;      #MAC nil #Mcnt 0
  (if (= (getvar "USERI4") 0)(setvar "USERI4" 78))                                  ; initialize
  ; (initget 1 "Y N")
  ; (setq mac (getkword "\nMacro ? Y/N : "))
  ; (if(= mac "Y")(setq #mac T))
  (setq m25 (strcase (getstring (strcat "\nQuill up at end of path? Y/N <" (chr (getvar "useri4")) ">: "))))
  (if (/= m25 "")(setvar "useri4" (ascii m25)))
  (if (= (getvar "useri4") 89)(setq #end "G0 M25\n;EOS")(setq #end ";EOS"))
  (princ (strcat "\nG0 G90 T" (itoa #t#) " M26 ;'T" (itoa #t#)
                 "..[End Mill] is " (rtos #dia 2 3) " dia.("
                 (itoa #teeth) " flutes)'") #opn)
  (princ (strcat "\nS" (itoa #rpm)) #opn) 
  ; (if #mac (princ (strcat "\n#" (itoa(setq #mcnt (1+ #mcnt))))#opn))
  (princ "\nBase <")(princ #00)(princ ">: ") ; set up an
  (setq g00 (getpoint))                      ; incremental base.
  (if (boundp 'g00)(setq #00 g00))
  (setq cp (getreal (strcat "\nClear plane<" (rtos #cp 2 3) ">: ")))
  (if (boundp 'cp)(setq #cp cp))
)

(bump) 

(defun FILTER (/ fn cmd)
  (setq #fnam (if #fnam #fnam (strcat (getvar "dwgname") ".TXT")))
  (setq fn (getstring (strcat "\nFile <" #fnam ">: ")))
  (if (/= fn "")(setq #fnam fn))
  (if (findfile #fnam)(progn
      (initget "A O") 
      (setq cmd (getkword "\nFile exists...Append/Overwrite <A>:"))
      (if (= cmd "O")(setq cmd "w")(setq cmd "a")))
    (setq cmd "w"))
  (setq #opn (open #fnam cmd))
  (if (cmd "w")     ; If new file, print filename as header
    (write-line (strcat ";[Data extracted from System File "
                        (getvar "dwgname") ".DWG") #opn))
  (princ (strcat "\n" #fnam " opened for " (if (= cmd "w") "writing"
                 "appending") " to."))
) ;end opnfile

(bump) 

; Move a POINT along the Polyline.. (show what's happening).
(defun CHANGER (pt)
  (setq #point (subst (cons 10 pt) (assoc 10 #point) #point))
  (entmod #point)
);end changer

; Bulge function
(defun BULGER (pt cv nx / ca ag ch cg rd cn ct)
  (setq ca (* 4 (atan (abs cv)))     ;  Included angle
        ag (- (/ pi 2) (/ ca 2))     ;  Angle E
        ch (distance pt nx)          ;  chord
        cg (angle pt nx)             ;  chord angle(on screen)
        rd (/ (/ ch 2) (cos ag))     ;  Side A / COS E = HYP
        ct (+ cg (* ag (if (minusp cv)-1 1)));  Screen Angle(pt to cen)
        cn (polar pt ct rd)          ;  Find center on screen
        #g (if (minusp cv) "\nG2 " "\nG3 ")
        #center cn)
  (eval 'cn)
) ;end bulgef

; Determine the type of poly.
(defun FLAGER (ename closed)
  ; #FLAG.... 0. Regular open..Multi segment
  ;            1. CIRCLE
  ;            2. SINGLE ARC
  ;            3. SINGLE LINE
  ;            4. CLOSED
  ;
  (setq #flag 0)                     ;INITIALIZE
  (cond
    ((and (= (field 0 (next (field -1 (next (entnext ename))))) "SEQEND") 
       closed)(setq #FLAG 1))                  ;CIRCLE
    ((and (= (field 0 (next (field -1 (next (entnext ename))))) "SEQEND") 
       (not closed))(cond
       ((/= (field 42 (next (entnext ename))) 0.0) (setq #flag 2)) ;SINGLE ARC
       (T (setq #flag 3))))              ;SINGLE LINE
    (closed (setq #flag 4)))           ;CLOSED
);end flager

;Step ahead function
(defun NEXT (e) (entget (entnext e)))

;Get the assoc field data
(defun FIELD (n e)(cdr (assoc n e)))

;Main polydoc function
(defun POLYDOC (ent / pt ent ename elist thispt nextpt bulge
                      closed header last)
  (setq ename ent (car ent) elist (entget ename) header elist
        #layer (field 8 elist) closed (if (= (field 70 elist) 1) T nil)
        count 0 #first T #last nil #g41 nil #g42 nil #x nil #y
        nil #z nil #g "G0 ")
  (cond
    ((= #layer "g41") (setq #g41 T))
    ((= #layer "g42") (setq #g42 T)))
  (setvar "pdmode" 3)
  (setvar "pdsize" -3.0)             ;Make a "NODE" at the start point.
  (setq pt (list (cons 0 "POINT") (cons 10 (field 10 (next ename)))))
  (entmake pt) 
  (setq #point (entget (entlast)))   ;Save it for later.
  (flager ename closed)              ;Indentify the POLY.
  (if closed(setq saved (field 10 (entget (entnext ename)))))
  (while (/= (field 0 (entget (setq ename (entnext ename)))) "SEQEND")
    (setq elist (entget ename)     bulge (field 42 elist)
          thispt (field 10 elist)  nextpt (field 10 (next ename))
          #first (if #first thispt))
    (cond
      ((/= bulge 0.0)
       (cond
        ((null nextpt)(changer thispt)
          (if (or #g41 #g42)(setq #last T)) ;CCOMP ON? LAST?
          (vertex thispt)            ;Last Polyline Vertex.
          (if closed                 ;Closed ?
            (progn
              (changer (bulger thispt bulge saved)) 
              (curve thispt #center saved) ;Arc Center.
              (changer saved))))        ;Polyline closed with start point.
         (T (changer (if #first #first thispt))     ;First point ?
           (if #first (rapid #first)(vertex thispt));Start point of Polyline.
                                                    ;or Polyline Vertex.
           (setq #first nil)
           (changer (bulger thispt bulge nextpt)) ;Regular vertex ?
           (curve thispt #center nextpt)))) ;Arc Center.
      (T (cond
          ((null nextpt)             ;No more vertices.
           (changer thispt)          ;(this is the last)
           (setq #last T)
           (vertex thispt)           ;Last Polyline Vertex.
           (if closed                ;Use start point to
             (progn                  ;close
                                     ;Polyline closed with start point.
               (changer saved) 
               (vertex saved))))
          (T  (changer (if #first #first thispt))   ;Polyline start point
            (if #first(rapid #first)(vertex thispt));Start point of Polyline.
                                                    ;or Polyline Vertex.
            (setq #first nil))))))
  (setq #g41 nil #g42 nil)
  (princ "\nEnd of poly")
  (entdel (cdr (assoc -1 #point)))   ; (if(and(not(null #cool))(= #END "G0 M25"))(princ "\nM9" #opn))
  (princ (strcat "\n" #end) #opn)
  (setq #flag nil)
  (prin1)
) 

(bump) 

(defun C:MILL (/ ent ss ssl cnt choice)
  (princ "\nPolyline to Boss10 NC Code")
  (filer)(tool-set)
  (princ "\nSelect Polyline(s) defining tool path..")
  (while (not (setq ss (ssget))))
  (setq ssl (sslength ss) cnt -1)
  (if ss (millprep))
  (repeat ssl
    (setq ent (ssname ss (setq cnt (1+ cnt))))
    (if (= (cdr (assoc 0 (entget ent))) "POLYLINE") 
      (polydoc ent)
      (princ "\nNot a POLYLINE entity, selection discarded !.")))
    ; (if (and (= #end ";EOS")(not(null #cool)))(princ "\nM9" #opn))
  (if (= #end ";EOS")(princ "\nG0 M25" #opn))                                  ; (if #mac (princ "\n$" #opn))
  (setq #opn (close #opn))
  (princ "\nFile closed.")
  (prin1))
