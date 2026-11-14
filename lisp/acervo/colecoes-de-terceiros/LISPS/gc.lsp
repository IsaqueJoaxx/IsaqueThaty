;From CADENCE Magazine November , 1991
;Page 111

;**************************************************
;*                                                *
;*   GCLOCK.LSP                                   *
;*   A Graphic Clock for AutoCAD.                 *
;*   Paul J. Viellieux     01/26/91               *
;*   2297 Country Lane                            *
;*   Neenah, WI 54956                             *
;*                                                *
;**************************************************
;
(defun C:GC ()
(setq more T)
   (setq siz (getvar "VIEWSIZE")        ;**Get height and location of
         ctr (getvar "VIEWCTR")         ;  current view.
         oldbas (getvar "ANGBASE")      ;**Get current angular units
         olddir (getvar "ANGDIR")       ;  settings.
         xcr (car ctr)                  ;**Get 'x' and 'y' of view
         ycr (cadr ctr)                 ;  center.
         degrad (/ pi 180.0)            ;**Degree to radian conversion.
         mins 0
         sec 0
         hr 0)
   (setvar "ANGBASE" 90.0)              ;**Set 12:00 at 90 degrees.
   (setvar "ANGDIR" 1)                  ;**Set rotation clockwise.
   (setq p1 (list (+ (* -0.0625 siz) xcr) (+ (* 0.46875 siz) ycr) 0.0)                ;**Sets points for drawing
         p2 (list (+ (* -0.03125 siz) xcr) (+ (* 0.40625 siz) ycr) 0.0)                ;  clock face numerals.
         p3 (list (+ (* -0.0625 siz) xcr) (+ (* 0.40625 siz) ycr) 0.0)
         p4 (list (+ (* -0.03125 siz) xcr) (+ (* 0.46875 siz) ycr) 0.0)
         p5 (list (+ (* -0.0 siz) xcr) (+ (* 0.46875 siz) ycr) 0.0)
         p6 (list (+ (* -0.0 siz) xcr) (+ (* 0.40625 siz) ycr) 0.0)
         p7 (list (+ (* 0.03125 siz) xcr) (+ (* 0.40625 siz) ycr) 0.0)
         p8 (list (+ (* 0.03125 siz) xcr) (+ (* 0.46875 siz) ycr) 0.0)
         p9 (list (+ (* 0.40625 siz) xcr) (+ (* 0.03125 siz) ycr) 0.0)
         p10 (list (+ (* 0.40625 siz) xcr) (+ (* -0.03125 siz) ycr) 0.0)
         p11 (list (+ (* 0.4375 siz) xcr) (+ (* -0.03125 siz) ycr) 0.0)
         p12 (list (+ (* 0.4375 siz) xcr) (+ (* 0.03125 siz) ycr) 0.0)
         p13 (list (+ (* 0.46875 siz) xcr) (+ (* 0.03125 siz) ycr) 0.0)
         p14 (list (+ (* 0.46875 siz) xcr) (+ (* -0.03125 siz) ycr) 0.0)
         p15 (list (+ (* -0.0625 siz) xcr) (+ (* -0.40625 siz) ycr) 0.0)
         p16 (list (+ (* -0.03125 siz) xcr) (+ (* -0.46875 siz) ycr) 0.0)
         p17 (list (+ (* -0.0 siz) xcr) (+ (* -0.40625 siz) ycr) 0.0)
         p18 (list (+ (* 0.03125 siz) xcr) (+ (* -0.40625 siz) ycr) 0.0)
         p19 (list (+ (* 0.03125 siz) xcr) (+ (* -0.46875 siz) ycr) 0.0)
         p20 (list (+ (* -0.46875 siz) xcr) (+ (* 0.03125 siz) ycr) 0.0)
         p21 (list (+ (* -0.46875 siz) xcr) (+ (* -0.03125 siz) ycr) 0.0)
         p22 (list (+ (* -0.4375 siz) xcr) (+ (* -0.03125 siz) ycr) 0.0)
         p23 (list (+ (* -0.40625 siz) xcr) (+ (* 0.03125 siz) ycr) 0.0)
         p24 (list (+ (* -0.4375 siz) xcr) (+ (* 0.03125 siz) ycr) 0.0)
         p25 (list (+ (* -0.40625 siz) xcr) (+ (* -0.03125 siz) ycr) 0.0)
         p26 (list (+ (* 0.2213 siz) xcr) (+ (* 0.3793 siz) ycr) 0.0)
         p27 (list (+ (* 0.2163 siz) xcr) (+ (* 0.3707 siz) ycr) 0.0)
         p28 (list (+ (* 0.3793 siz) xcr) (+ (* 0.2213 siz) ycr) 0.0)
         p29 (list (+ (* 0.3707 siz) xcr) (+ (* 0.2163 siz) ycr) 0.0)
         p30 (list (+ (* 0.3793 siz) xcr) (+ (* -0.2213 siz) ycr) 0.0)
         p31 (list (+ (* 0.3707 siz) xcr) (+ (* -0.2163 siz) ycr) 0.0)
         p32 (list (+ (* 0.2213 siz) xcr) (+ (* -0.3793 siz) ycr) 0.0)
         p33 (list (+ (* 0.2163 siz) xcr) (+ (* -0.3707 siz) ycr) 0.0)
         p34 (list (+ (* -0.2213 siz) xcr) (+ (* -0.3793 siz) ycr) 0.0)
         p35 (list (+ (* -0.2163 siz) xcr) (+ (* -0.3707 siz) ycr) 0.0)
         p36 (list (+ (* -0.3793 siz) xcr) (+ (* -0.2213 siz) ycr) 0.0)
         p37 (list (+ (* -0.3707 siz) xcr) (+ (* -0.2163 siz) ycr) 0.0)
         p38 (list (+ (* -0.3793 siz) xcr) (+ (* 0.2213 siz) ycr) 0.0)
         p39 (list (+ (* -0.3707 siz) xcr) (+ (* 0.2163 siz) ycr) 0.0)
         p40 (list (+ (* -0.2213 siz) xcr) (+ (* 0.3793 siz) ycr) 0.0)
         p41 (list (+ (* -0.2163 siz) xcr) (+ (* 0.3707 siz) ycr) 0.0)
   )
   (grclear)                            ;**Temporarily clears the screen.
   (grdraw p1 p2 5)                     ;**Draws clock face numerals.
   (grdraw p3 p4 5)    (grdraw p5 p6 5)    (grdraw p7 p8 5)
   (grdraw p9 p10 5)   (grdraw p11 p12 5)  (grdraw p13 p14 5)
   (grdraw p15 p16 5)  (grdraw p16 p17 5)  (grdraw p18 p19 5)
   (grdraw p20 p21 5)  (grdraw p22 p23 5)  (grdraw p24 p25 5)
   (grdraw p26 p27 5)  (grdraw p28 p29 5)  (grdraw p30 p31 5)
   (grdraw p32 p33 5)  (grdraw p34 p35 5)  (grdraw p36 p37 5)
   (grdraw p38 p39 5)  (grdraw p40 p41 5)
   (prompt "Press any key to return to drawing.")
   (setq start(grread T))
(while more
   (grdraw (list (+ (car ctr) (* 0.01 siz))(+ (cadr ctr) (* 0.01 siz)) 0.0)
           (list (+ (car ctr) (* 0.01 siz))(+ (cadr ctr) (* -0.01 siz)) 0.0) 5)
   (grdraw (list (+ (car ctr) (* 0.01 siz))(+ (cadr ctr) (* -0.01 siz)) 0.0) 
           (list (+ (car ctr) (* -0.01 siz))(+ (cadr ctr) (* -0.01 siz)) 0.0) 5)
   (grdraw (list (+ (car ctr) (* -0.01 siz))(+ (cadr ctr) (* -0.01 siz)) 0.0)
           (list (+ (car ctr) (* -0.01 siz))(+ (cadr ctr) (* 0.01 siz)) 0.0) 5)
   (grdraw (list (+ (car ctr) (* -0.01 siz))(+ (cadr ctr) (* 0.01 siz)) 0.0)
           (list (+ (car ctr) (* 0.01 siz))(+ (cadr ctr) (* 0.01 siz)) 0.0) 5)
   (princ)
   (setq clocks (substr (rtos (getvar "cdate") 2 6) 10)  ;**Gets current time.
         hr (atoi (substr clocks 1 2))       ;**Hours.
         chkmin (atoi (substr clocks 3 2))   ;**Minutes
         chksec (atoi (substr clocks 5 2)))  ;**Seconds.
   (if (>= hr 12)
      (setq hr (- hr 12))
   )

   (if(= (- chkmin mins) 1)(progn
     (grdraw ctr minute 0)
     (grdraw ctr hour 0)

   )
  )
   (setq mins chkmin)
   (if(= (- chksec sec) 1)(progn
     (grdraw ctr second 0)
     (if(= hr 0)(setq hrtxt "12")(setq hrtxt (itoa hr)))
     (if(< chksec 10)(setq sectxt (strcat "0" (itoa chksec)))
                     (setq sectxt(itoa chksec)))
     (if(< chkmin 10)(setq mintxt (strcat "0" (itoa chkmin)))
                     (setq mintxt(itoa chkmin)))
     (grtext 24 (strcat hrtxt  ":" mintxt ":" sectxt))
     (grtext 24 "" 1)
  )
)
   (setq sec chksec)
   (setq srot (* (* sec 6.0) degrad)         ;**Second hand rotation.
         mrot (* (* mins 6.0) degrad)        ;**Minute hand rotation.
         hrot (* (+ (* hr 30.0) (* mins 0.5)) degrad)  ;**Hour hand rotation.
         sx (* 0.375 (sin srot))             ;**Second hand rotation converted
         sy (* 0.375 (cos srot))             ;  to 'x' and 'y' coordinates.
         mx (* 0.375 (sin mrot))             ;**Minute hand rotation converted
         my (* 0.375 (cos mrot))             ;  to 'x' and 'y' coordinates.
         hx (* 0.28125 (sin hrot))           ;**Hour hand rotation converted
         hy (* 0.28125 (cos hrot)))          ;  to 'x' and 'y' coordiates.
   (setq second (list (+ (* sx siz) xcr) (+ (* sy siz) ycr) 0.0))
   (setq minute (list (+ (* mx siz) xcr) (+ (* my siz) ycr) 0.0))
   (setq hour (list (+ (* hx siz) xcr) (+ (* hy siz) ycr) 0.0))
   (grdraw ctr second 1)                     ;**Second minute hand.
   (grdraw ctr minute 5)                     ;**Draw minute hand.
   (if (< (abs (- mrot hrot)) 0.02)
      (grdraw ctr hour 7 1)
      (grdraw ctr hour 7)                       ;**Draw hour hand.
   )
   (setq z(grread T))
   (if (not(equal start z))(setq more nil))
   (if (= (car z) 2)(setq more nil))
           )
   (grtext 24 "        ")
   (setvar "ANGBASE" oldbas)                 ;**Reset angle units rotation
   (setvar "ANGDIR" olddir)                  ;  and direction to original.
   (redraw)                                  ;**Redisplays drawing.
   (princ)
)

