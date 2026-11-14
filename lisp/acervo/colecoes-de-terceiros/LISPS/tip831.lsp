; TIP #831: ARROW.LSP (c)1993, Walter J. Reini

; ARROW.LSP inserts an arrow on to the drawing 
; and allows four (4) lines of text comments to be entered.    

(defun C:ARROW()
  (setq #DWGSC 1.0)                                            
  (setq $LAYER (getvar "clayer"))
  (command "style" "std1-8" "simplex" (* 0.125 #DWGSC) "" "" "" "" "")  
  (command "layer" "make" "arrow"                                    
           "color" "red" "" "")                            
  (setq INSPT (getpoint "\nEnter insertion point:"))                 
  (command "insert" "arrow" INSPT #DWGSC #DWGSC pause)               
  (command "explode" "last")                                         
  (setq SS1 (ssget "X" (list (cons 0 "POINT")                         
  (cons 8 "arrow"))))
  (if SS1
    (progn
      (setq COUNT 0
        PMAX (sslength SS1)                                    
        $TXTEVAL (getvar "TEXTEVAL")
      );setq
      (setvar "TEXTEVAL" 1)
      (while (< COUNT PMAX)
        (setq EN (ssname SS1 COUNT)
          ED (entget EN)
          BLKN (dxf 2 ED)
        );setq
        (if (= COUNT 0)                                        
          (progn
            (setq PT1 (dxf 10 ED)                              
              X1 (nth 0 (dxf 10 ED))                         
              Y1 (nth 1 (dxf 10 ED))                         
            );setq
          );progn
          (princ)
        );if
        (if (= COUNT 1)                                    
          (progn
            (setq PT0 (dxf 10 ED)                                 
              X0 (nth 0 (dxf 10 ED))                       
              Y0 (nth 1 (dxf 10 ED))                       
            );setq
          );progn
          (princ)
        );if
        (setq COUNT (+ COUNT 1))
      );while
      (setq THETA (angtos (angle PT0 PT1) 0 2))            
      (setq ANG (atof THETA))                              
      (setq ETA (angle PT0 PT1))                           
    );progn
    (princ)
  );ss1
  (command "erase" SS1 "")                                 
  (princ "\nEnter text:")
  (if (<= ANG 90)
    (command "dtext" "style" "std1-8" "justify" "fit" PT0 PT1)
    (cond 
      ((> ANG 270) (command "dtext" "Style" "STD1-8" 
                    "Justify" "fit" PT0 PT1))
      (ANG 
        (progn

          ; else if
          (setq TX0 (+ X0 (* (* 0.5155 #DWGSC) (cos (- ETA (/ pi 2)))))
            TY0 (+ Y0 (* (* 0.5155 #DWGSC) (sin (- ETA (/ pi 2)))))
            TX1 (+ X1 (* (* 0.5155 #DWGSC) (cos (- ETA (/ pi 2)))))
            TY1 (+ Y1 (* (* 0.5155 #DWGSC) (sin (- ETA (/ pi 2)))))
            TPT0 (list TX1 TY1)
            TPT1 (list TX0 TY0)
          );setq
          (command "dtext" "style" "std1-8" "justify" "fit" TPT0 TPT1)
          (princ)
      ));progn
    );cond

  );if <=90
  (command "layer" "set" $LAYER "")
  (princ)      
);defun  

(defun dxf (code elist)
  (cdr (assoc code elist))
);defun  
;end arrow.lsp

