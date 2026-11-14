; TIP 849: 3DPP.LSP (c)1993, John R. Ricker
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;                     ========== 3DPP.LSP ==========
;;                         [3][D] [P]oint [P]lot
;;
;;  This progrm will read in almost ANY ASCII data file that contains at
;;  least the 4 following data fields:
;;
;;    Easting (x), Northing (y), Elevation (z) and a Point Name
;;
;;    3d Point Plot Example Data File
;;    Field Lengths
;;    12345678901234567890123456789012345678901234567890
;;
;;    500 1025563.846 586617.565 1650.418 Geo Joint
;;    501 1025564.112 586612.663 1650.444 Geo Joint
;;    502 1025566.532 586607.713 1650.735 Geo Joint
;;    ¨ <-------------( Break between lines = ASCII Code 168 )
;;    503 1025568.910 586604.281 1651.010 Geo Joint
;;    504 1025570.835 586602.764 1651.158 Geo Joint
;;    505 1025568.102 586606.667 1650.967 Geo Joint
;;    506 1025565.178 586611.950 1650.599 Geo Joint
;;    <EOF> <-------------------------------------- (Carriage Return)
;;
;;;;;;;;;;;;;;;;;;;;; < Sample Prompts and Answers > ;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;;     Enter the Number of Title Lines before data points...? : 4
;;     Enter the STARTING COLUMN for points NAME............? : 1
;;     Enter the FIELD LENGTH for the NAME..................? : 3
;;     Enter the Starting Column for points NORTHING..[ Y ] ? : 5
;;     Enter the FIELD LENGTH for the NORTHING..............? : 11
;;     Enter the Starting Column for points EASTING...[ X ] ? : 17
;;     Enter the FIELD LENGTH for the EASTING...............? : 10
;;     Enter the Starting Column for points ELEVATION.[ Z ] ? : 28
;;     Enter the FIELD LENGTH for the ELEVATION.............? : 8
;;
;;     Enter INPUT File to plot WITH EXTension..............? : TEST.DAT
;;
;;     Connect 3-D Points with 3-D Polylines < Y >..........? : Y
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;  Written by: John R. Ricker Jr.
;;              R I C K E R  Computing
;;              1345 E. Mercer Lane
;;              Phoenix, Arizona 85020
;;              (602) 997-2948
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun C:3DPP (/ FIL EOF OLDLYR OLDPD COUNT RFIL$ XYZ NORTH EAST ELEV NAME
                 PT1 PT2 DATA TITL_LI N_L N_S E_L E_S NAME_L NAME_S ELEV_L
                 ELEV_S BLNK_LI DL)

   (setvar "BLIPMODE" 0)                           ; Turn off Blipmode
   (setvar "CMDECHO" 0)                            ; Turn off Command Echo
;;
;; Get the Scale Factor 
;;
   (setq SF 
     (getreal 
       "\nPlease Enter a Scale factor.....................< 1.0 > ? : ")
   )                                               ; end setq
   (if (or (= SF "")(= SF nil))(setq SF 1.0))      ; set scale to 1 if nil 
;;
;; Get the needed information from the data file to be processed
;;
   (princ "\nThe  following information is to DEFINE the FIELDS")
   (princ "\n     of your DATA FILE that you will be using")
   (setq TITL_LI
     (getint "\nEnter the Number of Title Lines before data points...? : ")
         NAME_S
     (getint "\nEnter the STARTING COLUMN for points NAME............? : ")
         NAME_L
     (getint "\nEnter the FIELD LENGTH for the NAME..................? : ")
         N_S
     (getint "\nEnter the Starting Column for points NORTHING..[ Y ] ? : ")
         N_L
     (getint "\nEnter the FIELD LENGTH for the NORTHING..............? : ")
         E_S
     (getint "\nEnter the Starting Column for points EASTING...[ X ] ? : ")
         E_L
     (getint "\nEnter the FIELD LENGTH for the EASTING...............? : ")
         ELEV_S
     (getint "\nEnter the Starting Column for points ELEVATION.[ Z ] ? : ")
         ELEV_L
     (getint "\nEnter the FIELD LENGTH for the ELEVATION.............? : ")
         RFIL$
     (getstring "\nEnter INPUT File to plot WITH EXTension..............? : ")
         DL
     (getstring "\nConnect 3-D Points with 3-D Polylines < Y >..........? : ")
   )                                               ; end setq
   (if (or (= DL "")(= DL nil)(= DL "YES")
           (= DL "Y")(= DL "yes")(= DL "y")
       )                                           ; end or
       (setq DL "YES")                             ; if DL is nil set to Yes
   )                                               ; end if
   (setq FIL (open RFIL$ "r")                      ; open file to read
         EOF "NO"                                  ; set end of file to no
         OLDLYR (getvar "CLAYER")                  ; save old layer
         OLDPD (getvar "PDMODE")                   ; save Pdmode
         COUNT 0                                   ; set counter to 0
         XYZ (if XYZ XYZ 4)                        ; needed for spin wheel
         BLNK_LI 1                                 ; set blank line to 1
   )                                               ; end setq
   (setvar "PDMODE" 0)                             ; set pdmode 
;;
;; Create the needed layers
;;
   (command "LAYER" "M" "3D_TXT" "C" "Magenta" ""  
                    "M" "3D_PTS" "C" "CYAN"    ""
                    "M" "3D_PL"  "C" "Yellow"  ""
                    "S" "0" ""
   )                                               ; end command 
;;
;; Read in the blank lines at the beginning of the data file
;;
   (if (/= TITL_LI 0)
     (repeat TITL_LI (read-line FIL))
   )                                               ; end if
;;
;; read in first line of data
;;
   (setq DATA (read-line FIL))               
;;
;; Statement to print 
;;
   (if (= DL "YES")
     (princ "\nPlotting and Connecting 3-D Points and 3-D Polyline......  ")
     (princ "\nPlotting 3-D points..........................  ")   ; else
   )                                               ; end if
;;
;; Enter while loop to start processing point
;;
   (while (= EOF "NO")  
     (setq COUNT (+ COUNT 1))                      ; Increment counter
;;
;; Spin function to show the user that something is going on
;;
     (princ
       (cond ((= (rem (setq XYZ (1+ XYZ)) 4) 0) "\010|")
             ((= (rem XYZ 4) 1) "\010/")
             ((= (rem XYZ 4) 2) "\010-")
             (t "\010\\")
       )                                           ; end condition
     )                                             ; end princ
     (if (= DATA nil)
       (setq EOF "YES")                            ; if data is nil 
       (progn
         (setq NORTH   (atof (substr DATA N_S N_L))       ; get y value
               EAST    (atof (substr DATA E_S E_L))       ; get x value
               ELEV    (atof (substr DATA ELEV_S ELEV_L)) ; get z value
               NAME    (substr DATA NAME_S NAME_L)        ; get pt name
               PT1     (list EAST NORTH ELEV)             ; x y z value
         )                                                ; end setq  
         (if (or (/= NORTH 0.0)(/= NORTH nil)(/= NORTH ""))
           (progn
             (command "LAYER" "SET" "3D_PTS" "")   ; set layer to 3d_pts
             (command "POINT" PT1)                 ; draw point
           )                                       ; end progn
         )                                         ; end if
         (if (or (= NORTH 0.0)(= EAST 0.0))
           (command "ERASE" "L" "" "")             ; erase invalid point
         )
         (if (or (/= NORTH nil)(/= NORTH ""))
           (progn
             (command "LAYER" "S" "3D_TXT" "")     ; set layer to 3d_txt
             (command "TEXT" "C" PT1 (* 0.08 SF) "" NAME) ; draw text
           )                                       ; end progn
         )                                         ; end if
;;
;; if draw 3d polylines is tes
;;
         (if (= DL "YES")
           (if (> COUNT BLNK_LI)                   ; do if count is greater
                                                   ; then Blnk_ln
             (progn
               (if (or (= NORTH 0.0)
                       (= EAST 0.0)
                   )                               ; end or
                   (setq PT1 PT2)                  
               )                                   ; end if
               (command "LAYER" "SET" "3D_PL" "")  ; set layer to 3d_pl
               (command "3DPOLY" PT1 PT2 "")       ; draw 3d_poly
               (setq PT2 PT1)                      ; set second point to be
                                                   ; the first point
             )                                     ; end progn
             (setq PT2 PT1)                        ; set second point to be
                                                   ; first point
           )                                       ; end if 2
         )                                         ; end if 1
         (setq DATA (read-line FIL))               ; read next line of data
         (if (= DATA "¨")                          ; if data is <Alt-168>
                                                   ;   start next line
                                                   ; space, between 3d
                                                   ; polylines
           (setq COUNT 0                           ; set count to 0
                 DATA (read-line FIL)              ; read next line of data
           )                                       ; end setq
         )                                         ; end if
       )                                           ; end progn
     )                                             ; end IF
   )                                               ; end while
   (close FIL)                                     ; CLOSE FILE
   (command "LAYER" "S" OLDLYR "F" "3D_TXT" "")    ; restore to original
                                                   ; layer and freeze the
                                                   ; 3d_txt layer
;; 
;; erase any points that might be stray around 0,0,0
;;
   (command "ERASE" "W" "-1.0,-1.0,-1.0" "1.0,1.0,1.0" "")
   (command "ZOOM" "E")                            ; zoom to extense
;;
;; See if user wants to view in viewpoint
;;
   (setq DVP          
     (strcase
       (getstring "\nDo a ViewPoint on the drawing <N> : ? ")
     )                                             ; end strcase
   )                                               ; end setq
   (if (or (= DVP "Y")(= DVP "YES"))(command "VPOINT" ""))
;;
;; Exiting message
;;
   (princ "\nFinished processing points..... Thank you..")
   (princ)
)                                                  ; end defun 3DPP
        
        
