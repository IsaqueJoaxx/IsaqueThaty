;;***************************************************
;;;FMS 3.2 ADE OPERATIONS.......
;;;**************************************************
;Function performs an ADE Retreval based on arguments
;;;**************************************************
(defun fmsade(geo ptlist / laylst csmode yn templ templlst laylst off)
   (if (not F:LAYLST)
      (setq F:LAYLST "*")
   )
   (setvar "regenmode" 0)
   (command "_color" "_bylayer")
   (if (= geo "BUFFERFENCE")
      (setq off (* (getdist (nth 0 ptlist) "\nEnter radius to offset: ") 2))
   )
   (c:adeclrquery)
   (if (/= geo "FENCE")
      (setq csmode (getyn "\nInside or Crossing (I/C) <C>: " "Crossing" "Inside"))
      (setq csmode "Crossing")
   )

   (if (/= geo "BUFFERFENCE")
      (c:_adedefquery "Union(Or)" "Location" geo CSMODE ptlist)
      (c:_adedefquery "Union(Or)" "Location" geo CSMODE ptlist off)
   )
   (c:_adedefquery "Intersection(And)" "Properties"         "LAYER" F:LAYLST)
   (if (not F:ADEMODE)(ademode))
   (if (= F:ADEMODE "Query")
   (progn
      (if (= (getvar "filedia") 1)
         (c:adeddsavequery)
         (c:adesavequery)
      )
   )
      (c:adeexequery F:ADEMODE "*")
   )
   (if (/= F:ADEMODE "Show")(redraw))
   (princ)
)

;;;**************************************************
;Function sets the DRAWING LIST for archive+retrieve
;;;**************************************************
(defun adeset( / chk good len temp tempdir yn mode)
   (setq mode (strcase (getyn "\nOpen or Close Drawings (O/C) <O>: " "Open" "Close")))
   (if (= mode "OPEN")
      (c:adeddopendwgs)
      (c:adeddclosedwgs)
   )
   (princ)
)

;;;**************************************************
;Function sets the ADE MODE for archive+retrieve
;;;**************************************************
(defun ademode ( / dcl_id temp)
   (if (not F:ADEMODE)
      (setq F:ADEMODE "Draw")
   )
   (if (= (getvar "filedia") 1)
   (progn
      (ademode_dcl)
   )
   (progn
      (princ "\nShow Draw Extract File Query")
      (initget "Show Draw Extract File Query")
      (setq temp (getkword (strcat "\nSelect EXECUTE Mode <" F:ADEMODE ">: ")))
      (if temp (setq F:ADEMODE temp))
   ))
   (princ)
)

;;;**********************************************************
;Function Performs a Window FMSADE Retrieval
;;;**********************************************************
(defun adertrv_win( / pt pt2 x1 x2 y1 y2 ptlist laylst templ)
   (setq pt (getpoint "\nSelect first corner: "))
   (setq pt2 (getcorner pt "\nSelect second corner: "))
   (setq x1 (car pt))
   (setq x2 (car pt2))
   (setq y1 (cadr pt))
   (setq y2 (cadr pt2))
   (setq ptlist (list (list x1 y1)(list x2 y1)(list x2 y2)(list x1 y2)))
   (highlt ptlist)
   (fmsade "POLYGON" ptlist)
   (princ)
)

;;;**********************************************************
;Function Performs a Circle FMSADE Retrieval
;;;**********************************************************
(defun adertrv_cir( / pt rad e pcirc ptlist laylst templ)
   (while (not (setq pt (getpoint "\nSelect center of query area: ")))
      (princ "\nERROR: Invalid point entered.")
   )
   (princ "\nEnter Radius of window area: ")
   (command "_circle" pt pause)
   (setq rad (cdr (assoc 40 (entget (entlast)))))
   (setq e (entlast) segln (/ rad 6))
   (cirpline e segln)
   (command "_erase" e "")
   (setq pcirc (entlast))
   (princ "\nAccessing coordinates...")
   (setq ptlist (brkout (entget pcirc) nil nil))
   (command "_erase" pcirc "")
   (highlt ptlist)
   (fmsade "POLYGON" ptlist)
   (princ)
)

;;;**********************************************************
;Function Performs a Polygon FMSADE Retrieval
;;;**********************************************************
(defun adertrv_bnd( / yn e eb found laylst templ clsflg)
   (setq found nil)
   (setq yn
       (getyn "\nWould you like to draw a boundary? (Y/N) <Y>: " "Yes" "No"))
   (if (= yn "Yes")
   (progn
      (drawedge)
      (setq e (entlast))
      (setq eb (entget e))
      (setq ptlist (brkout eb nil nil))
      (command "_erase" e "")
      (highlt ptlist)
      (setq found "True")
   )
   (progn
      (while (not found)
         (princ "\nSelect POLYLINE boundary to reference: ")
         (setq e (ssname (ssget) 0))
         (setq eb (entget e))
         (setq clsflg (rem (cdr (assoc 70 eb)) 2))
         (if (or (/= "POLYLINE" (cdr (assoc 0 eb)))(/= 1 clsflg))
         (progn
            (setq found "True")
            (setq ptlist (brkout eb nil nil))
            (highlt ptlist)
         ))
         (if (not found)
            (princ "\nERROR No valid entities selected.")
         )
      )
   ))
   (if found
      (fmsade "POLYGON" ptlist)
   )
   (princ)
)

;;;**********************************************************
;Function Performs a Fence FMSADE Retrieval
;;;**********************************************************
(defun adertrv_fnc( / yn e eb found laylst templ)
   (setq found nil)
   (setq yn
       (getyn "\nWould you like to draw a fence? (Y/N) <Y>: " "Yes" "No"))
   (if (= yn "Yes")
   (progn
      (drawfnc)
      (setq e (entlast))
      (setq eb (entget e))
      (setq ptlist (brkout eb nil nil))
      (command "_erase" e "")
      (highlt2 ptlist)
      (setq found "True")
   )
   (progn
      (while (not found)
         (princ "\nSelect POLYLINE fence to reference: ")
         (setq e (ssname (ssget) 0))
         (setq eb (entget e))
         (if (= "POLYLINE" (cdr (assoc 0 eb)))
         (progn
            (setq found "True")
            (setq ptlist (brkout eb nil nil))
            (highlt2 ptlist)
         ))
         (if (not found)
            (princ "\nERROR No valid entities selected.")
         )
      )
   ))
   (if found
      (fmsade "FENCE" ptlist)
   )
   (princ)
)

;;;**********************************************************
;Function Performs a Buffer Fence FMSADE Retrieval
;;;**********************************************************
(defun adertrv_buf( / yn e eb found laylst templ)
   (setq found nil)
   (setq yn
       (getyn "\nWould you like to draw a center line? (Y/N) <Y>: " "Yes" "No"))
   (if (= yn "Yes")
   (progn
      (drawfnc)
      (setq e (entlast))
      (setq eb (entget e))
      (setq ptlist (brkout eb nil nil))
      (command "_erase" e "")
      (highlt2 ptlist)
      (setq found "True")
   )
   (progn
      (while (not found)
         (princ "\nSelect POLYLINE fence to reference: ")
         (setq e (ssname (ssget) 0))
         (setq eb (entget e))
         (if (= "POLYLINE" (cdr (assoc 0 eb)))
         (progn
            (setq found "True")
            (setq ptlist (brkout eb nil nil))
            (highlt2 ptlist)
         ))
         (if (not found)
            (princ "\nERROR No valid entities selected.")
         )
      )
   ))
   (if found
      (fmsade "BUFFERFENCE" ptlist)
   )
   (princ)
)

;;;**********************************************************
;Function Performs an FMSADE Retrieval from list 1 at a time
;;;**********************************************************
(defun rtrvtslst(tslist / ts)
   (if (not F:ADEMODE)(ademode))
   (if (> (length tslist) 0)
   (progn
      (foreach ts tslist
         (c:adeclrquery)
         (c:_adedefquery "Union(Or)" "Properties" "EED" (strcat F:ADEMASK ts))
         (c:adeexequery F:ADEMODE "*")
      )
      (c:adeclrquery)
   ))
   (princ)
)
;;;**********************************************************
;Function Performs an FMSADE Retrieval from list in batch
;;;**********************************************************
(defun rtrvtslstall(tslist / ts)
   (if (not F:ADEMODE)(ademode))
   (c:adeclrquery)
   (if (> (length tslist) 0)
   (progn
      (foreach ts tslist
         (c:_adedefquery "Union(Or)" "Properties" "EED" (strcat F:ADEMASK ts))
      )
      (c:adeexequery F:ADEMODE "*")
   ))
   (princ)
)


;;;************************************************************
;Function Purges DWG of ENTITIES meeting the following criteria
;NOT on layers in ADE.STR **KEYLAY listing
;NOT Retrieved in EXTRACT mode with Current ADE REGAPP
;;;************************************************************
(defun adepurge( / choice ss ss2 n e eb ct len lname laylst rapp ct2 len2)
   (setq F:ADEAPP (c:adegetcfg "APPNAME"))
   (if ads_anslhi (F:XUNLOAD "aiment"))
   (if (null ads_anslhi)(F:XLOAD "aiment"))
   (setq laylst (fmsstr "ade" "**KEYLAY"))
   (setq choice (getyn "\nPurge Whole/Subset? (W/S) <W>: " "Whole" "Subset"))
   (setq choice (substr (strcase choice) 1 1))
   (if (= choice "W")
   (progn
      (setvar "highlight" 0)
      (setq ss nil)
      (setq lname (strcase (strcat "~" (nth 0 laylst))))
      (princ "\nAccessing entities...Please wait.")
      (setq ss (ssget "X" (list (cons 8 lname))))
      (setq laylst (cdr laylst))
      (princ "\nBypassing KEYMAP entities...Please wait.")
      (setq ct2 0)
      (setq len2 (length laylst))
      (while (< ct2 len2)
         (setq lname (nth ct2 laylst))
         (setq ss (ssget "p" (list (cons 8 (strcat "~" lname)))))
         (if ss
            (setq ct2 (+ ct2 1))
            (setq ct2 len2)
         )
      )
      (princ "\nBypassing READ/WRITE entities...Please wait.")
      (setq ss (ads_ansllo ss F:ADEAPP "CmFm"))
      (setvar "highlight" 1)
   ))
   (if (= choice "S")
   (progn
      (setq ss nil)
      (setq ss (ssget))
      (princ "\nBypassing KEYMAP entities...Please wait.")
      (setq ct2 0)
      (setq len2 (length laylst))
      (while (< ct2 len2)
         (setq lname (nth ct2 laylst))
         (setq ss (ssget "p" (list (cons 8 (strcat "~" lname)))))
         (if ss
            (setq ct2 (+ ct2 1))
            (setq ct2 len2)
         )
      )
      (princ "\nBypassing READ/WRITE entities...Please wait.")
      (setq ss (ads_ansllo ss F:ADEAPP "CmFm"))
   ))
   (if ss 
      (command "_erase" ss "")
      (if (/= choice "Q")
         (princ "\nNo valid entities found.")
      )
   )
   (if ads_anslhi (F:XUNLOAD "aiment"))
   (princ)
)

;;;************************************************************
;Function Archives DWG of ENTITIES meeting the following criteria
;NOT on layers in ADE.STR **KEYLAY listing
;NOT Retrieved in EXTRACT mode with Current ADE REGAPP
;;;************************************************************
(defun adearchv( / ss yn ct mnunme)
   (setq F:ADEAPP (c:adegetcfg "APPNAME"))
   (princ "\nThis is currently only saving through ADE Came From Option...")
   (c:adesavedwgs "*" "c")
   (setq yn (getyn "\nCheck integrity if save operation? (Y/N) <N>: " "No" "Yes"))
   (if (= yn "Yes")
   (progn
      (gc)
      (if (setq ss (ssget "X" (list (list -3 (list F:ADEAPP)))))
      (progn
         (setq ct 0)
         (while (< ct (sslength ss))
            (setq e (ssname ss ct))
            (if (fmsxchkmask F:ADEAPP "CmFm" e)
               (setq ct (+ ct 1))
               (ssdel e ss)
            )
         )
         (if ss
         (progn
            (if (> (sslength ss) 0)
            (progn
               (command "_select" ss "")
               (textscr)
               (princ "\n\n\n\n\n\n")
               (princ "\n\n\n\n\n\n")
               (princ "\n\t\t**************   ALERT   ****************")
               (princ "\n\t\tWARNING: Not all retrieved data processed")
               (princ "\n\n\n\n\n\n")
               (princ "\nAccess skiped data through (P)revious selection set.")
               (princ "\n\nTo rectify this situation...")
               (princ "\nYou should END and RE-ACTIVATE this drawing session.")
            ))
         ))
      ))
   ))
   (princ)
)

;;;**********************************************************************
;Function to Add to Archived Drawing map data referencing grid boundaries
;;;**********************************************************************
(defun adesave ( / choice ct ct2 len len2 ssgrid hilo yn ss
                   pt1 pt2 ptx1 ptx2 pty1 pty2 ptlist
                   laylst dwglst e dwgnme)
   (setq laylst (fmsstr "ade" "**KEYLAY"))
   (setq dwglst (car (c:adeproplist "DWG")))
   (setvar "expert" 4)
   (setvar "regenmode" 0)
   (setq choice (getyn "\nArchive Whole/Subset? (W/S) <S>: " "Subset" "Whole"))
   (setq choice (substr (strcase choice) 1 1))
   (if (= choice "W")
      (setq ssgrid (ssget "X" (list (cons 0 "POLYLINE")(cons 8 "ADEGRID")(list -3 (list "ADECODE")))))
   (progn
      (command "_layer" "_t" "adegrid" "")
      (setq yn (getyn "\nModify view? (Y/N) <Y>: " "Yes" "No"))
      (if (= yn "Yes")
      (progn
         (princ "\nPlease wait, zooming to extents.")
         (command "_zoom" "_e")
      ))
      (princ "\nSelect Map Facet Grids To Process: ")
      (setq ssgrid (ssget))
      (setq ssgrid (ssget "P" (list (cons 0 "POLYLINE")(cons 8 "ADEGRID")(list -3 (list "ADECODE")))))
   ))
   (if ssgrid
   (progn
      (setq ct 0)
      (setq len (sslength ssgrid))
      (while (< ct len)
         (setq e (ssname ssgrid ct))
         (setq ct (+ ct 1))
         (grtext -1 (strcat "Processing " (itoa ct) " of " (itoa len) " Facets"))
         (setq dwgnme (fmsxchkmask "ADECODE" "DWGNAME=" e))
         (if (member dwgnme dwglst)
         (progn
            (setq eb (entget e))
            (setq ptlist (brkout eb nil nil))
            (highlt ptlist)
            (setq hilo (getbox ptlist))
            ;;zoom stuf
            (setq pt1 (car hilo))
            (setq pt2 (cadr hilo))
            (setq offst (/ (distance pt1 pt2) 10))
            (setq ptx1 (- (car pt1) offst))
            (setq pty1 (- (cadr pt1) offst))
            (setq ptx2 (+ (car pt2) offst))
            (setq pty2 (+ (cadr pt2) offst))
            (setq pt1 (list ptx1 pty1 0.0))
            (setq pt2 (list ptx2 pty2 0.0))
            (princ "\nPlease wait, zooming to facet borders.")
            (command "_zoom" "_w" pt1 pt2)
            (highlt ptlist)
            (princ "\nAccessing Entities..")
            (setq ss (ssget "CP" ptlist))
            (if ss
            (progn
               (princ "\nBypassing KEYMAP entities...Please wait.")
               (setq ss (ssget "P" (list (cons 8 "~ADEGRID"))))
               (setq ct2 0)
               (setq len2 (length laylst))
               (while (< ct2 len2)
                  (setq lname (nth ct2 laylst))
                  (setq ss (ssget "p" (list (cons 8 (strcat "~" lname)))))
                  (if ss
                     (setq ct2 (+ ct2 1))
                     (setq ct2 len2)
                  )
               )
               (if ss
                  (if (> (sslength ss) 0)
                  (progn
                     (princ "\nAccess Facet Entities with a 'P' For Previous.\n")
                     (command "_select" ss "")
                     (c:adesavedwgs dwgnme "S")
                  )
                     (princ (strcat "\nEmpty facet, Drawing " dwgnme " not updated..\n"))
                  )
                  (princ (strcat "\nEmpty facet, Drawing " dwgnme " not updated..\n"))
               )
            )
               (princ (strcat "\nEmpty facet, Drawing " dwgnme " not updated..\n"))
            )
            (redraw e)
         )
            (princ (strcat "\nUnreferenced facet, Drawing " dwgnme " not open..\n"))
         )
      )
      (grtext)
   )
      (princ "\nINVALID: No Map Facets Selected: ")
   )
   (if (= yn "Yes")
   (progn
      (princ "\nPlease wait, zooming to extents.")
      (command "_zoom" "_e")
   ))
   (setvar "expert" 0)
   (grtext)
   (princ "\nDone.")
   (princ)
)
(defun adelay2( / templ templlst indx)
   (setq templ (getstring "\nTemplate: "))
   (if (/= templ "")(setq templlst (list templ)))
   (setq indx "**KEYLAY")
   (adethawlay templlst indx)
   (princ)
)

(defun adelay( / templ alltempl templlst indx f echo)
   (if (= (getvar "filedia") 0)
   (progn
      (setq templ (getstring "\nTemplate: "))
      (if (/= templ "")(setq templlst (list templ)))
   )
   (progn
      (if (setq f (open (strcat V:TXT "template.lst") "r"))
      (progn
         (while (setq echo (read-line f))
            (if (not alltempl)
               (setq alltempl (list echo))
               (if (not (member echo alltempl))
                  (setq alltempl (append alltempl (list echo)))
               )
            )
         )
         (close f)
      ))
      (setq templlst (f:fmslst_dcl alltempl "FMS APPLICATIONS" ""))
      (cond
         ((= templlst "*")(setq templlst alltempl))
         ((= templlst "")(setq templlst nil))
         (T (setq templlst (stl templlst)))
      )
   ))
   (setq indx "**TSLAY")
   (if templlst
      (adethawlay templlst indx)
   )
   (princ)
)
(defun adethawlay(templlst indx / templ templlst mode laylst)
   (setq mode (getyn "\nFreeze Unreferenced Layer(s)? (Y/N) <N>: " "No" "Yes"))
   (if (= Mode "Yes")(setq mode 0)(setq mode 1))
   (if templlst
   (progn
      (foreach templ templlst
         (if (not laylst)
            (setq laylst (fmsstr templ indx))
            (setq laylst (append (fmsstr templ indx) laylst))
         )
      )
      (setvar "regenmode" 0)
      (if (= mode 0)
         (command "_layer" "_t" (nth 0 laylst) "_m" (nth 0 laylst) "_f" (strcat "~" (nth 0 laylst)) "_t" (lts laylst) "")
         (command "_layer" "_t" (nth 0 laylst) "_m" (nth 0 laylst) "_t" (lts laylst) "")
      )
      (setq yn (getyn "\nRegenerate Drawing? (Y/N) <N>: " "No" "Yes"))
      (if (= yn "Yes")(command "_regen"))
   ))
   (princ)
)
;;;*************************************************
;;---------------MAP TILER SECTION-----------------
;;;*************************************************
(defun c:adetile_dcl()
   (adetile_dcl)
   (princ)
)
(defun adetile_dcl( / dcl_id temp mode)
   (setq mode "set")
   (setq temp "set")
   (if (= (getvar "filedia") 1)
   (progn
      (setq dcl_id (load_dialog "fmstile"))
      (if (not (new_dialog "fmstile_dial" dcl_id))
         (exit)
      )
      (mode_tile mode 2)
      (action_tile
         "set"
         "(setq temp \"Set\")"
      )
      (action_tile
         "make"
         "(setq temp \"Make\")"
      )
      (action_tile
         "list"
         "(setq temp \"List\")"
      )
      (action_tile
         "name"
         "(setq temp \"Name\")"
      )
      (action_tile
         "tile"
         "(setq temp \"Tile\")"
      )
      (action_tile
         "accept"
         (strcat
            "(setq mode temp)"
            "(done_dialog)"
         )
      )
      (action_tile
         "cancel"
         (strcat
            "(setq mode nil)"
            "(done_dialog)"
         )
      )
      (start_dialog)
      (unload_dialog dcl_id)
   )
   (progn
      (princ "\nTILE Options.")
      (initget "Make List Name Tile")
      (setq mode (getkword "\nMake List Name Tile (M/L/N/T): "))
   ))
   (cond
     ((= mode "Set")
        (adedir)
     )
     ((= mode "Make")
        (adegrid_dcl)
     )
     ((= mode "List")
        (adeecho)
     )
     ((= mode "Name")
        (adecode)
     )
     ((= mode "Tile")
        (adetile)
     )
   )
   (princ)
)
(defun adegrid_dcl( / done ptx pty rownum rowsiz colnum colsiz answr
                      pt1 pt2 rowln colln)
   (setq ptx "")
   (setq pty "")
   (setq rownum "")
   (setq colnum "")
   (setq rowsiz "")
   (setq colsiz "")
   (setq done nil)
   (while (not done)
      (setq answr (ade_tile_dcl2 ptx pty rownum rowsiz colnum colsiz))
      (if (= (nth 0 answr) "PICK")
      (progn
         (setq pt (getpoint "\nSelect point: "))
         (setq ptx (rtos (car pt)))
         (setq pty (rtos (cadr pt)))
      ))
      (if (= (nth 0 answr) "SHOW")
      (progn
         (setq ptx (atof (nth 1 answr)))
         (setq pty (atof (nth 2 answr)))
         (setq rownum (atoi (nth 3 answr)))
         (setq rowsiz (atof (nth 4 answr)))
         (setq colnum (atoi (nth 5 answr)))
         (setq colsiz (atof (nth 6 answr)))
         (setq rowln (* rownum rowsiz))
         (setq colln (* colnum colsiz))
         (setq ct 0)
         (repeat (+ colnum 1)
            (setq pt1 (list (+ (* colsiz ct) ptx) pty))
            (setq pt2 (list (+ (* colsiz ct) ptx) (+ rowln pty)))
            (grdraw pt1 pt2 1 -1)
            (setq ct (+ ct 1))
         )
         (setq ct 0)
         (repeat (+ rownum 1)
            (setq pt1 (list ptx (+ (* rowsiz ct) pty)))
            (setq pt2 (list (+ colln ptx)(+ (* rowsiz ct) pty)))
            (grdraw pt1 pt2 1 -1)
            (setq ct (+ ct 1))
         )
         (setq ptx (rtos ptx))
         (setq pty (rtos pty))
         (setq rownum (itoa rownum))
         (setq rowsiz (rtos rowsiz))
         (setq colnum (itoa colnum))
         (setq colsiz (rtos colsiz))
         (getstring "\nPress ENTER To continue: ")
         (redraw)
      ))
      (if (= (nth 0 answr) "OK")
      (progn
         (setvar "regenmode" 0)
         (F:MLAY "adegrid")
         (setq ptx (atof (nth 1 answr)))
         (setq pty (atof (nth 2 answr)))
         (setq rownum (atoi (nth 3 answr)))
         (setq rowsiz (atof (nth 4 answr)))
         (setq colnum (atoi (nth 5 answr)))
         (setq colsiz (atof (nth 6 answr)))
         (setq done T)
         (setq pt1 (list ptx pty))
         (setq pt2 (list (+ ptx colsiz) (+ pty rowsiz)))
         (command "_pline" pt1 (list (car pt2) (cadr pt1))
                           pt2 (list (car pt1) (cadr pt2)) "_c")
         (command "_array" (entlast) "" "_r" rownum colnum rowsiz colsiz)
         (princ "\n Undesired facets may be erased and/or repositioned, stretched as needed.")
      ))
      (if (= (nth 0 answr) "BAIL")
         (setq done T)
      )
   )
   (princ)
)
(defun ade_tile_dcl2(ptx pty rownum rowsiz colnum colsiz /
                     answr resbuf dcl_id)
   (setq dcl_id (load_dialog "fmstile"))
   (if (not (new_dialog "fmsgrid_dial" dcl_id))
      (exit)
   )
   (set_tile "x" ptx)
   (set_tile "y" pty)
   (set_tile "row" rownum)
   (set_tile "height" rowsiz)
   (set_tile "col" colnum)
   (set_tile "width" colsiz)

   (action_tile "x"
      (strcat
         "(if (/= $reason 2)(progn"
            "(setq ptx (get_tile \"x\"))"
            "(mode_tile \"y\" 2)"
         "))"
      )
   )
   (action_tile "y"
      (strcat
         "(if (/= $reason 2)(progn"
            "(setq pty (get_tile \"y\"))"
            "(mode_tile \"height\" 2)"
         "))"
      )
   )
   (action_tile "height"
      (strcat
         "(if (/= $reason 2)(progn"
            "(setq rowsiz (get_tile \"height\"))"
            "(mode_tile \"row\" 2)"
         "))"
      )
   )
   (action_tile "row"
      (strcat
         "(if (/= $reason 2)(progn"
            "(setq rownum (get_tile \"row\"))"
            "(mode_tile \"width\" 2)"
         "))"
      )
   )
   (action_tile "width"
      (strcat
         "(if (/= $reason 2)(progn"
            "(setq colsiz (get_tile \"width\"))"
            "(mode_tile \"col\" 2)"
         "))"
      )
   )
   (action_tile "col"
      (strcat
         "(if (/= $reason 2)(progn"
            "(setq colnum (get_tile \"col\"))"
            "(mode_tile \"show\" 2)"
         "))"
      )
   )
   (action_tile "pick"
      (strcat
         "(if (/= $reason 2)(progn"
            "(setq resbuf \"PICK\")"
            "(setq F:ADEDIR (get_tile \"dir\"))"
            "(done_dialog)"
         "))"
      )
   )
   (action_tile
      "show"
      (strcat
         "(if (/= $reason 2)(progn"
            "(setq resbuf \"SHOW\")"
            "(done_dialog)"
         "))"
      )
   )
   (action_tile
      "accept"
      (strcat
         "(setq resbuf \"OK\")"
         "(done_dialog)"
      )
   )
   (action_tile
      "cancel"
      (strcat
         "(setq resbuf \"BAIL\")"
         "(done_dialog)"
      )
   )
   (start_dialog)
   (unload_dialog dcl_id)
   (setq answr (list resbuf ptx pty rownum rowsiz colnum colsiz))
)
(defun adedir( / tempdir temp len chk yn done pltfrm f)
   (if (not F:ADEDIR)
      (setq F:ADEDIR (getenv "LFMA"))
   )
   (setq done nil)
   (while (not done)
      (setq tempdir F:ADEDIR)
      (if (not tempdir)(setq tempdir ""))
      (setq tempdir (strcase tempdir T))
      (setq temp (fmsgetstr tempdir 100 "Enter Storage Path"))
;      (setq temp (strcase (getstring (strcat "\nEnter Storage Path < " tempdir " > : ")) T))
      (if temp
      (progn
         (setq tempdir (strcase temp T))
         (setq len (strlen tempdir))
         (if (= len 0)
            (setq chk tempdir)
            (setq chk (substr tempdir len 1))
         )
         (if (or (= chk "\\")(= chk "/"))
            (princ)
            (setq tempdir (strcat tempdir "\\"))
         )
         (setq pltfrm (getvar "PLATFORM"))
         (if (or (= pltfrm "Microsoft Windows")(= pltfrm "386 DOS Extender"))
            (setq tempdir (frn2bac tempdir))
            (setq tempdir (bac2frn tempdir))
         )
         (setq yn (getyn (strcat "\nAccept path: < " tempdir " >? (Y/N) <Y>: ") "Yes" "No"))
         (if (= yn "Yes")
         (progn
            (if (setq f (open (strcat tempdir "fmstmp.tmp") "w"))
            (progn
               (close f)
               (ads_delfil (strcat tempdir "fmstmp.tmp"))
               (setq done T)
               (setq F:ADEDIR tempdir)
            )
               (princ "\nERROR: Unable to access directory.")
            )
         ))
      )
         (exit)
      )
   )
   (princ)
)

;;;*************************************************
;Function to create rectalinear grid
;;;*************************************************
(defun adegrid ( / cols hgt p1 p2 rows wdt)
   (setvar "regenmode" 0)
   (F:MLAY "adegrid")
   (if (= (getvar "filedia") 1)
      (adegrid_dcl)
   (progn
      (setq p1 (getpoint "\n Enter starting coordinate (Lower Left Corner) : "))
      (setq rows (getint "\n Enter Number of Rows: ")
            cols (getint "\n Enter Number of Columns: ")
            wdt (getdist p1 "\n Enter Facet Width: ")
            hgt (getdist p1 "\n Enter Facet Height: ")
            p2 (list (+ (car p1) wdt) (+ (cadr p1) hgt)))
      (command "_pline" p1 (list (car p2) (cadr p1))
                        p2 (list (car p1) (cadr p2)) "_c")
      (command "_array" (entlast) "" "_r" rows cols hgt wdt)
      (princ "\n Undesired facets may be erased and/or repositioned, stretched as needed.")
   ))
   (princ)
)
;;;*************************************************
;Function to name map grid boundaries
;;;*************************************************
(defun adecode ( / ss e e2 eb ct len name ptlist lbl temp dwgnme yn pre ok)
   (setq yn (getyn "\nName All tiles or Subset? (A/S) <A>: " "All" "Subset"))
   (if (= yn "All")
      (setq ss (ssget "X" (list (cons 0 "POLYLINE")(cons 8 "ADEGRID"))))
   (progn
      (princ "\nSelect Facet Boundaries.")
      (setq ss (ssget))
      (setq ss (ssget "P" (list (cons 0 "POLYLINE")(cons 8 "ADEGRID"))))
   ))
   (if ss
   (progn
      (setq len (sslength ss))
      (setq ct 0)
      (setq lbl "")
      (setq yn (getyn "\nName Manually or Incrementally? (M/I) <M>: " "Manually" "Incrementally"))
      (if (= yn "Incrementally")
      (progn
         (setq ok nil)
         (while (not ok)
            (setq pre (fmsgetstr "" 6 "Enter Map Facet Prefix: "))
            (if (not pre)(setq pre ""))
            (princ "\nCurrent Prefix: ")(princ pre)
            (setq agin (getyn "\nAccept? (Y/N) <Y>: " "Yes" "No"))
            (if (= agin "Yes")(setq ok T))
         )
      ))
      (while (< ct len)
         (setq temp lbl)
         (setq e (ssname ss ct))
         (setq ct (+ ct 1))
         (setq eb (entget e))
         (setq ptlist (brkout eb nil nil))
         (highlt ptlist)
         (if (= yn "Manually")
         (progn
            (setq dwgnme (fmsxchkmask "ADECODE" "DWGNAME=" e))
            (if dwgnme (setq lbl (substr dwgnme 1 8)))
            (setq lbl (fmsgetstr lbl 8 "Enter Map Facet Name: "))
         )
         (progn
            (if (< ct 10)
               (setq lbl (strcat pre "0" (itoa ct)))
               (setq lbl (strcat pre (itoa ct)))
            )
         ))
         (if (/= lbl "")
         (progn
            (if (fmsxchk e "ADECODE")
               (if (fmsxchkmask "ADECODE" "DWGNAME=" e)
                  (modrapp e "ADECODE" "DWGNAME=" (cons 1000 (strcat "DWGNAME=" lbl)))
                  (updrapp e "ADECODE" (cons 1000 (strcat "DWGNAME=" lbl)))
               )
               (addrapp e "ADECODE" (cons 1000 (strcat "DWGNAME=" lbl)))
            )
         )
         (progn
            (princ "\nINVALID drawing name entered.")
            (setq ct (- ct 1))
         ))

      )
   )
      (princ "\nNo Valid Entities Selected.")
   )
   (princ)
)
;;;*************************************************
;Function to echo name of map grid boundary
;;;*************************************************
(defun c:adeecho()
   (adeecho)
   (princ)
)
(defun adeecho ( / ss e e2 eb ct len name ptlist lbl temp dwgnme yn pre ok)
   (princ "\nSelect Facet Boundary.")
   (setq ss (ssget))
   (setq ss (ssget "P" (list (cons 0 "POLYLINE")(cons 8 "ADEGRID"))))
   (if ss
   (progn
      (setq e (ssname ss 0))
      (setq eb (entget e))
      (setq ptlist (brkout eb nil nil))
      (highlt ptlist)
      (setq dwgnme (fmsxchkmask "ADECODE" "DWGNAME=" e))
      (if dwgnme (setq lbl (substr dwgnme 1 8)))
      (fmsgetstr lbl 8 "Map Facet Name: ")
      (redraw e)
   )
      (princ "\nNo Valid Entities Selected.")
   )
   (princ)
)

;;;******************************************************************
;Function to Chop Drawing into pieces referencing map grid boundaries
;;;******************************************************************
(defun c:adetile()
   (adetile)
   (princ)
)
(defun adetile ( / ct ct2 len len2 ssgrid hilo yn ss
                   pt1 pt2 ptx1 ptx2 pty1 pty2 choice f)
   (setq laylst (fmsstr "ade" "**KEYLAY"))
   (setvar "expert" 4)
   (setvar "regenmode" 0)
   (if (= F:ADEDIR nil)(adedir))
   (setq choice (getyn "\nTile Whole/Subset? (W/S) <S>: " "Subset" "Whole"))
   (setq choice (substr (strcase choice) 1 1))
   (if (= choice "W")
      (setq ssgrid (ssget "X" (list (cons 0 "POLYLINE")(cons 8 "ADEGRID")(list -3 (list "ADECODE")))))
   (progn
      (command "_layer" "_t" "adegrid" "")
      (setq yn (getyn "\nModify view? (Y/N) <Y>: " "Yes" "No"))
      (if (= yn "Yes")
      (progn
         (princ "\nPlease wait, zooming to extents.")
         (command "_zoom" "_e")
      ))
      (princ "\nSelect Map Facets To Tile: ")
      (setq ssgrid (ssget))
      (setq ssgrid (ssget "P" (list (cons 0 "POLYLINE")(cons 8 "ADEGRID")(list -3 (list "ADECODE")))))
   ))
   (if ssgrid
   (progn
      (setq ct 0)
      (setq len (sslength ssgrid))
      (while (< ct len)
         (setq e (ssname ssgrid ct))
         (setq ct (+ ct 1))
         (grtext -1 (strcat "Processing " (itoa ct) " of " (itoa len) " Facets"))
         (setq dwgnme (fmsxchkmask "ADECODE" "DWGNAME=" e))
         (setq dwgnme (strcat F:ADEDIR dwgnme))
         (setq eb (entget e))
         (setq ptlist (brkout eb nil nil))
         (highlt ptlist)
         (setq hilo (getbox ptlist))
         ;;zoom stuf
         (setq pt1 (car hilo))
         (setq pt2 (cadr hilo))
         (setq offst (/ (distance pt1 pt2) 10))
         (setq ptx1 (- (car pt1) offst))
         (setq pty1 (- (cadr pt1) offst))
         (setq ptx2 (+ (car pt2) offst))
         (setq pty2 (+ (cadr pt2) offst))
         (setq pt1 (list ptx1 pty1 0.0))
         (setq pt2 (list ptx2 pty2 0.0))
         (princ "\nPlease wait, zooming to facet borders.")
         (command "_zoom" "_w" pt1 pt2)
         (highlt ptlist)
         (princ "\nCreating Block..")
         (setq ss (ssget "CP" ptlist))
         (redraw e)
         (if ss
         (progn
            (princ "\nBypassing KEYMAP entities...Please wait.")
            (setq ss (ssget "P" (list (cons 8 "~ADEGRID"))))
            (setq ct2 0)
            (setq len2 (length laylst))
            (while (< ct2 len2)
               (setq lname (nth ct2 laylst))
               (setq ss (ssget "p" (list (cons 8 (strcat "~" lname)))))
               (if ss
                  (setq ct2 (+ ct2 1))
                  (setq ct2 len2)
               )
            )
            (if ss
               (if (> (sslength ss) 0)
               (progn
                  (if (setq f (open (strcat dwgnme ".dwg") "r"))
                  (progn
                     (princ "\nWARNING: Facet ")(princ dwgnme)(princ " already exists.")
                     (setq yn (getyn "\nOverwrite (Y/N) <N>: " "No" "Yes"))
                     (close f)
                  )
                     (setq yn "Yes")
                  )
                  (if (= yn "Yes")
                     (command "_wblock" dwgnme "" "0,0" "_P" "")
                  )
               )
                  (princ (strcat "\nEmpty block, " dwgnme " not written to disk..\n"))
               )
               (princ (strcat "\nEmpty block, " dwgnme " not written to disk..\n"))
            )
         )
            (princ (strcat "\nEmpty block, " dwgnme " not written to disk..\n"))
         )
      )
      (grtext)
   )
      (princ "\nINVALID: No Map Facets Selected: ")
   )
   (princ "\nPlease wait, zooming to extents.")
   (command "_zoom" "_e")
   (setvar "expert" 0)
   (grtext)
   (princ "\nDone.")
   (princ)
)

;;;**********************************************************
;Function sets the ADE Layer Filter List for archive+retrieve
;;;**********************************************************
(defun adefilter ( / f echo templlst yn laylst worklst done)

   (if (not F:LAYLST)(setq F:LAYLST "*"))
   (if (= (getvar "filedia") 1)
   (progn
      (adefilter_dcl)
      (if (setq f (open (strcat V:TXT "template.lst") "r"))
      (progn
         (while (setq echo (read-line f))
            (if (not templlst)
               (setq templlst (list echo))
               (if (not (member echo templlst))
                  (setq templlst (append templlst (list echo)))
               )
            )
         )
         (close f)
      ))
      (setq templlst (acad_strlsort templlst))
      (setq laylst (car (c:adeproplist "layer")))
      (setq laylst (acad_strlsort laylst))
      (setq laylst (f:fmslst2_dcl templlst laylst "By Template" "By Layer" F:LAYLST))
      (if laylst (setq F:LAYLST laylst))
   )
   (progn
      (setq worklst (stl F:LAYLST))
      (princ "\nCurrent Layer Filter List: \n")
      (princ (lts worklst))
      (setq yn (getyn "\nAdd Layers? (Y/N) <N>: " "No" "Yes"))
      (if (= yn "Yes")
         (setq done nil)
         (setq done T)
      )
      (while (not done)
         (princ "\nCurrent Layer Filter List: \n")
         (princ (lts worklst))
         (setq lname (strcase (getstring "\nAdd Layer Name: ")))
         (if (/= lname "")
         (progn
            (if (not (member lname worklst))
            (progn
               (setq worklst (append (list lname) worklst))
               (setq worklst (acad_strlsort worklst))
            ))
         ))
         (princ "\nCurrent Layer Filter List: \n")
         (princ (lts worklst))
         (setq yn (getyn "\nAdd More Layers? (Y/N) <Y>: " "Yes" "No"))
         (if (= yn "No")(setq done T))
      )
      (princ "\nCurrent Layer Filter List: \n")
      (princ (lts worklst))
      (setq yn (getyn "\nRemove Layers? (Y/N) <N>: " "No" "Yes"))
      (if (= yn "Yes")
         (setq done nil)
         (setq done T)
      )
      (while (not done)
         (princ "\nCurrent Layer Filter List: \n")
         (princ (lts worklst))
         (setq lname (strcase (getstring "\nRemove Layer Name : ")))
         (if (/= lname "")
            (if (member lname worklst)
            (progn
               (setq tmp1 (cdr (member lname worklst)))
               (setq tmp2 (cdr (member lname (reverse worklst))))
               (setq worklst (append tmp1 (reverse tmp2)))
               (setq worklst (acad_strlsort worklst))
            ))
         )
         (princ "\nCurrent Layer Filter List: \n")
         (princ (lts worklst))
         (setq yn (getyn "\nRemove More Layers? (Y/N) <Y>: " "Yes" "No"))
         (if (= yn "No")(setq done T))
      )
      (princ "\nCurrent Layer Filter List: \n")
      (princ (lts worklst))
      (setq yn (getyn "\nAccept Changes? (Y/N) <Y>: " "Yes" "No"))
      (if (= yn "Yes")(setq F:LAYLST (lts worklst)))
   ))
   (princ)
)

