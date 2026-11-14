; TIP855: THREADS.LSP (c)1993, Paul Davisson

; This routine draws any unified screw thread profile 
; which includes the following; coarse thread series
; (unc/unrc), fine thread series (unf/unrf), extra fine
; thread series (unef/unref), and selected combinations
; (uns/unrs).  The program prompts for the style
; of thread (you need only enter whether it is a 
; "un" or "unr" series of thread, the default is "un".), 
; the pitch diameter, number of threads per inch,
; the approximate length of thread desired, and the
; insertion point on the drawing.  Note the  
; approximate length is used because the thread is 
; constructed in a manner that the length is derived
; using increments of thread pitch.  The user can trim the 
; thread to the length desired.

;                               Paul Davisson   
;                               Nelson Irrigation Corp.
;                               Walla Walla, Wa. 99362


(setq ANS 
   (getstring "unr or un thread series?    UNR or <UN>:   ")

      PIT
                (getreal "enter pitch diameter:   ")

      TI
                (getreal "enter threads per inch:   ")

      TL
   (getreal "enter approximate length of thread desired:  ")

      SP
                (getpoint "select insertion point")
)


(cond ((= ANS "UN")(setq Q 0.0))
      ((= ANS "UNR")(setq Q 1.0))
      ((= ANS "unr")(setq Q 1.0))
      ((= ANS "un")(setq Q 0.0))
      ((= ANS  )(setq Q 0.0))
)


(setq P     (/ 1.0 TI)    
               ; derives thread pitch
   

      TLL   (fix (/ TL P))     
               ; takes desired approximate thread length and     
               ; associates it with the number of threads  
               ; per inch and makes it an intiger
   


      H     (/(* 0.125 P (cos (/ PI 6.0)))
              (* 0.25 (sin (/ PI 6.0))))
               ; literal peak to peak thread height
    

      MAJ   (+ PIT (* 0.375 2.0 H)) 
               ; major diameter
    
      MIN   (- MAJ (* 2.0 0.625 H))
               ; minor diameter for "un" series
    
      MINR   (- MIN (* H 0.125))
               ; minor diameter for "unr" series 

      HMJ   (/ MAJ 2.0)
               ; major radius

      HMN   (/ MIN 2.0)
               ; minor radius for "un" series
    
      HMNR  (/ MINR 2.0)
               ; minor radius for "unr" series

      DIFY  (/ (* (- HMJ HMN) (sin (/ PI 6.0)))
                             (cos (/ PI 6.0)))
               ; major minor difference in y dir for 
               ; "un" series
    

      PA    (* 0.0625 P)
               ; one half width of thread crest
    
      A     (list (+ HMJ (car SP))(cadr SP))
      B     (list (car A)(-(cadr A) PA))
      C     (list (+ HMN (car SP))(-(cadr B) DIFY))
      D     (list (car C)(-(cadr A)(* 0.5 P)))
      E     (list (car D)(-(cadr D)(distance C D)))
      F     (list (car A)(-(cadr E) DIFY))
      G     (list (car F)(-(cadr F) PA))
      AA    (list (-(car SP) HMN)(cadr A))
      BB    (list (car AA)(- (cadr SP) (distance C D)))
      CC    (list (-(car SP) HMJ)(-(cadr BB) DIFY)) 
      DD    (list (car CC)(-(cadr CC) PA))
      EE    (list (car DD)(-(cadr DD) PA))
      FF    (list (car AA)(-(cadr EE) DIFY))
      GG    (list (car FF)(-(cadr FF) (distance C D)))
      I     (list (+ (car SP) HMNR)(- (cadr C)
                  (* 0.108 P (cos (/ PI 6.0)))))
      J     (list (car I)(cadr D))
      K     (list (car I)(- (cadr I)(* 2.0 (distance I J))))
      L     (list (- (car SP) HMNR)(- (cadr SP)
                  (distance I J)))
      M     (list (- (car SP) HMNR)(cadr SP))
      N     (list (car M)(- (cadr FF) (* 0.108 P 
                  (cos (/ PI 6.0)))))
      O     (list (car M)(-(cadr N) (distance I J)))
)


(if (= Q 1.0) (command "pline" M "W" 0.0 ""
            L "A" BB "L" CC DD A B C "A" I "L" J O
                  N "A" FF "L" EE DD ""

         "array" (ssget "l") "" "r" TLL 1 (- P) 

         "pline" J K "A" E "L" F G ""

         "array" (ssget "l") "" "r" TLL 1 (- P)

         "line" A M ""

         "array" (ssget "l") "" "r" 2 1 (- (* TLL P) )
                )  
                 ; routine for "unr" series 
     


          (command "pline" AA "w" 0.0 ""
                    BB CC EE FF GG D C B A DD ""
           
          "array" (ssget "l") "" "r" TLL 1 (- P)
 
          "pline" D E F G ""

          "array" (ssget "l") "" "r" TLL 1 (- P)

          "line" A AA ""

          "array" (ssget "l") "" "r" 2 1 (- (* TLL P) ))
)              
                 ; routine for "un" series  

(command  "line" (list (car SP) (+ (* PIT 0.1) (cadr SP)))
                 (list (car SP) (- (cadr SP) (+ (* TLL P)
                       (* PIT 0.1)))) "" )
                 ; routine for centerline

