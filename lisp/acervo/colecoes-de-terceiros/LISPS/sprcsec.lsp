;         ============ sprcsec ============
;
;         Compression Spring - Section View
;
;
(defun C:SPRCSEC ()

  (setq
        WireDiam (getreal "\nEnter Wire Diameter: ")
        Coils (getint "\nEnter Number of Coils: ")
        OutsideDiam (getreal "\nEnter Outside Coil Diameter: ")
        
        NominalDiam (- OutsideDiam WireDiam)
        
        StartLocation (getpoint "\nStarting Point: ")
        
        LowerLeft (list (- (car StartLocation) WireDiam)
                        (- (cadr StartLocation) WireDiam))
        UpperRight (list (+ (car StartLocation) (* OutsideDiam 2))
                         (+ (cadr StartLocation) (* WireDiam 2)))
  )
  
  (command "LAYER" "S" "0" "")
  (command "CIRCLE" StartLocation "D" WireDiam)
  (command "LAYER" "S" "1" "")
  (command "HATCH" "U" "45" (/ WireDiam 4) "N" "W" LowerLeft UpperRight "")
  (command "COPY" "W" LowerLeft UpperRight ""
              (list NominalDiam 0.0) "")
  (command "LAYER" "S" "0" "")
  (command "LINE" StartLocation
              (list (+ (car StartLocation) NominalDiam)
                    (cadr StartLocation)) "")
  (command "MOVE" "L" "" (list 0.0 (/ WireDiam 2)) "")
  (command "COPY" "L" "" (list 0.0 (- 0.0 WireDiam)) "")
  (command "ROTATE" "W" LowerLeft UpperRight "" StartLocation "15")
  (command "ARRAY" "P" "" "R" Coils "1" (* WireDiam 2))
  
  (princ)
  
)