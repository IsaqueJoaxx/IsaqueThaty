;      Title: NO_REGEN.LSP
;Description: Transparent No-Regen Zoom for AutoCAD Release 10 and 11
;             (untested for Release 9)
;   Revision: 1.1
;       Date: October 16, 1992
;      Notes: Command line function added           (1.1)
;             Original release: March 12, 1990      (1.0)
;     Author: Albert M Thalheim, Architect
;             AutoDESK Registered Developer
;    Address: 3340, Boul PŠre LeLiŠvre
;             Qu‚bec, QC Canada G1P 2Y5
;        Tel: (418) 682-6443
;        Fax: (418) 682-6816
; Compuserve: 72441,2005
;
; SCALE option:
;  1.  Define %SCALE as the drawing scale
;  2.  Define screen height for scaled view.
;  These MUST be defined on entry or hard coded here.
;       (Example: a 14" monitor has a graphic screen of 6.3".
;       Some adjustment needed for other monitor dimensions...)
;
      (initget 7)
      (setq %SCRN  (getreal "\nHeight of graphic area(in.): ")
            %SCALE (getvar "LTSCALE")
            X      (nth (if (eq (substr (getvar "ACADVER") 1 2) "11") 1 0)
                        (getvar "SCREENSIZE"))
            %MIN   (/ 32765.0 X)
            %PROP  (apply '/ (getvar "SCREENSIZE")))
;
 (defun MIDPT     (P0 P1 / PA PB)
                  (mapcar '(lambda (PA PB) (/ (+ PA PB) 2.0)) P0 P1))
;
; Main function
;  To use transparently, user issues:
;   '.ZOOM C (ZOOM "XYZ") !H
;  where:
;   '.ZOOM       Transparent original Zoom command
;   C            Center option
;   (ZOOM "XYZ") Lisp function where 'XYZ' stands for:
;                        MIN, IN, SCA, OUT, MAX, WIN or PAN
;   !H           Screen height
;
;  Menu example: [ZOOM WIN]'ZOOM;C;(ZOOM "WIN");!H;
;
 (defun ZOOM      (VAR / P0 P2 PA PB M Y)
   (setq P0  (cond                          ; Input: Points if needed.
               ((eq VAR "WIN")
                  (setq P0 (getvar "VIEWCTR")
                        P2 (getcorner P1 " Second point: "))
                  (MIDPT P1 P2))
               ((eq VAR "MAX")
                  (MIDPT (getvar "VSMAX") (getvar "VSMIN")))
               ((eq VAR "OUT")
                  (getvar "VIEWCTR"))
               ((eq VAR "PAN")
                  (setq P0 (getvar "VIEWCTR")
                        P1 (getpoint " Premier point: ")
                        P2 (getpoint  P1 " Second point: "))
                  (mapcar '(lambda (PA PB PC) (+ PA (- PB PC)))  P0 P1 P2))
               (t (getpoint "\nCentre de la vue: ")))
         Y   (cadr (mapcar '- (getvar "VSMAX") (getvar "VSMIN")))
         H   (cond                          ; Compute final view height
               ((eq VAR "WIN") (min Y
                                    (max (/ Y %MIN)
                                         (/ (abs (- (car P2) (car P1))) %PROP)
                                         (abs (- (cadr P2) (cadr P1))))))
               ((eq VAR "OUT") (min Y (* (getvar "VIEWSIZE") 2.0)))
               ((eq VAR "MIN") (/ Y %MIN))
               ((eq VAR "IN")  (max (/ Y %MIN) (* (getvar "VIEWSIZE") 0.5)))
               ((eq VAR "SCA") (* %SCRN %SCALE))
               ((eq VAR "PAN") (getvar "VIEWSIZE"))
               (t              Y)))
   (if (= H Y)               ; Exit: Center of view.  H holds view height.
     (MIDPT (getvar "VSMIN") (getvar "VSMAX"))
     (setq PA (list (* H %PROP 0.5) (* H 0.5))
           P1 (mapcar '+ (getvar "VSMIN") PA)
           P2 (mapcar '- (getvar "VSMAX") PA)
           P0 (mapcar 'min (mapcar 'max P0 P1) P2))))
;
; Added function (October 1992)
; Command line function (no transparency here...)
; Note: To use old ZOOM command, enter .ZOOM
;
 (command "UNDEFINE" "ZOOM")
;
 (defun C:ZOOM    (/ P1 H)
                  (setvar "CMDECHO" 0)
                  (setvar "MENUECHO" 2)
                  (initget 1 "MAx Out Window In MIn Pan Scale")
                  (setq P1 (getpoint (strcat "\nMax/Out/Scale/In/Min/Pan"
                                             "/<Window first point>: "))
                        P  (ZOOM (if (eq (type P1) 'STR)
                                   (strcase (substr P1 1 3))
                                   "WIN")))
                  (command ".ZOOM" "C" P H)
                  (setvar "CMDECHO" 1)
                  (setvar "MENUECHO" 0))

; EOF
