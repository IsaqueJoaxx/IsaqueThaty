;;;;Develope notes.....

;;;EASEL FILE stores parameters for SQL Operations
;;;EASEL FILE stores parameters for DWG Operations
;;;(ads_intsql...) is a function which logs on to the current SQL table
;;;It will reference the current EASEL File for SQL Parameters
;;;(ads_config) is a function which references the current Easel File
;;;This will configure TIMENT to build and access the correct LUT
;;;It will also RETURN a list of necessary parameters to the LISP interface
;;;These parameters include
;;; ASE SQL Variables
;;; FMS SQL Field Names
;;; FMS DWG Relation Variables

;;;FMS 3.2;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;Function to set to the default easel file for templ argument
(defun fmseasel(templ / yn chk laylst defeasel)
   (setvar "cmdecho" 0)
   (if (null ads_timent)(F:XLOAD "timent"))
   (if (null ads_intsql)(f:xload "ads_sqlb"))
   (if (and F:FMSEASEL (/= templ F:TEMPL)) (F:EASEL_DEINIT))
   (setq F:TEMPL templ)
   (setq templ (strcase templ T))
   (setq F:EASELFILE (nth 0 (fmsstr templ "**EASEL")))
   (setq laylst (lts (fmsstr templ "**TSLAY")))
   (setq chk (F:EASEL_SETDEF F:EASELFILE))
   (if (not chk)
   (progn
      (alert "WARNING Default EASEL File does not exist")
      (princ "\nWARNING Default EASEL File does not exist")
      (setq yn (getyn "\nCreate now (Y/N) <Y>: " "Yes" "No"))
      (if (= yn "Yes")
      (progn
         (setq defeasel (ads_eascfg))
         (if defeasel
         (progn
            (setq yn (getyn (strcat "\nSave as default " templ " EASEL File? (Y/N) <Y>: ") "Yes" "No"))
            (if (= yn "Yes")
            (progn
               (setq F:EASELFILE defeasel)
               (F:EASEL_SETDEF F:EASELFILE)
               (fmsmodstr templ "**EASEL" defeasel)
            )
               (exit)
            )
         )
            (exit)
         )
      )
         (exit)
      )
   )
   (progn
      (ads_laycfg laylst)
      (if (not F:FMSEASEL)(F:EASEL_INIT))
   ))
   F:EASELFILE
)

;;;FMS 3.2;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;Function to set to initialise to the current easel file F:EASELFILE
(defun F:EASEL_INIT()
   (if (null ads_timent)(F:XLOAD "timent"))
   (if (null ads_intsql)(f:xload "ads_sqlb"))
   (setq F:EASELST (ads_config))
   (F:EASEL_PARAMS)
   (if (or (= (strcase F:SQLDRV) "DBASE3")
           (= (strcase F:SQLDRV) "DBASE4")
           (= (strcase F:SQLDRV) "PARADOX")
       )
   (progn
      (setq F:SQLUSR "")
      (setq F:SQLPAS "")
   )
   (progn
      (while (not F:SQLUSR)
         (setq answr (fmsgetsql "" 8 "User Name: " "" 8 "Password:  "))
         (if (= answr "bail")
         (progn
            (exit)
         )
         (progn
            (setq F:SQLUSR (nth 0 answr))
            (setq F:SQLPAS (nth 1 answr))
         ))
      )
   ))
   (setq F:FMSEASEL nil)
   (setq F:FMSEASEL (ads_intsql F:SQLUSR F:SQLPAS))
   F:FMSEASEL
)
;;;FMS 3.2;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;Function places arguments from ADS_CONFIG Into Global LISP Variables
(defun C:EASEL_PARAMS()
   (F:EASEL_PARAMS)
   (c:fmsecho)
   (princ)
)
(defun F:EASEL_PARAMS ( / mode attd)
   (setq mode F:SQL_MODE)(setq attd F:SQL_INDEX)
   (setq F:SQLDRV     (nth 0 F:EASELST))
   (setq F:SQLDBM     (nth 1 F:EASELST))
   (setq F:SQLTAB     (nth 2 F:EASELST))
   (setq F:SQLDEF     (nth 3 F:EASELST))
   (setq F:SQL_IDKEY  (nth 4 F:EASELST))
   (setq F:SQL_TSKEY  (nth 5 F:EASELST))
   (setq F:SQL_EA     (nth 6 F:EASELST))
   (setq F:SQL_NO     (nth 7 F:EASELST))
;   (setq F:SQL_EL     (nth 8 F:EASELST))
   (setq F:SQL_LAY   (nth 8 F:EASELST))
   (setq F:SQL_MODE  (nth 9 F:EASELST))
   (setq F:SQL_INDEX (nth 10 F:EASELST))
   (setq F:SQL_DXF  (nth 11 F:EASELST))
   (setq F:SQL_MASK  (nth 12 F:EASELST))
   (setq F:SQLSPA (strcat (substr F:SQLTAB 1 3) "SPA"))
   (if (= F:SQL_LAY "")(setq F:SQL_LAY "*"))
   (if (= F:SQL_MODE "ATTDEF")
   (progn
      (if (and (= mode F:SQL_MODE)(= attd F:SQL_INDEX))
         (princ)
      (progn
         (princ "\n\nBuilding Block list from header file...Please wait.")
         (setq F:EASEL_BLKLST (F:EASEL_MAKE_BLKLST F:SQL_INDEX))
      ))
   ))
   (princ)
)

;;;FMS 3.2;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;Function to get the default easel file from fmsease.cfg file
(defun C:EASEL_GETDEF()
   (F:EASEL_GETDEF)
   (princ)
)
(defun F:EASEL_GETDEF( / f defeasel)
   (if (setq f (open (strcat V:FMS "config/fmsease.cfg") "r"))
   (progn
      (setq defeasel (read-line f))
      (close f)
   )
      (setq defeasel "")
   )
   defeasel
)
;;;FMS 3.2;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;Function to set the default easel file from fmsease.cfg file
(defun C:EASEL_SETDEF( / fname)
   (if F:EASELFILE (setq fname F:EASELFILE)(setq fname ""))
   (setq fname (strcase (fmsgetstr fname 100 "SET EASEL File") T))
   (if (/= (substr fname (- (strlen fname) 3)) ".esl")
      (setq fname (strcat fname ".esl"))
   )
   (F:EASEL_SETDEF fname)
   (princ)
)
(defun F:EASEL_SETDEF(defeasel / f)
   (if (setq f (open (strcat V:FMS "config/fmsease.cfg") "w"))
   (progn
      (write-line defeasel f)
      (write-line "" f)
      (close f)
   ))
   (if (setq f (open defeasel "r"))
   (progn
      (close f)
      (setq F:EASELFILE defeasel)
   )
      (setq defeasel nil)
   )
   defeasel
)

;;;FMS 3.2;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;Function to de initialize SQL Driver
(defun C:EASEL_DEINIT()
   (F:EASEL_DEINIT)
   (princ)
)
(defun F:EASEL_DEINIT( / yn)
   (if ads_deinit
   (progn
      (ads_deinit)
   ))
   (setq F:TEMPL nil)
   (setq F:SQLUSR nil F:SQLPAS nil F:FMSEASEL nil F:EASELST nil)
   (princ)
)

;;;FMS 3.2;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;Function to unload and de initialize SQL Driver
(defun C:EASEL_DEINIT2()
   (F:EASEL_DEINIT2)
   (princ)
)
(defun F:EASEL_DEINIT2( / yn)
   (if ads_deinit
   (progn
      (ads_deinit)
      (setq yn (getyn "\nUnload driver? (Y/N) <N>: " "No" "Yes"))
      (if (= yn "Yes")(f:xunload "ads_sqlb"))
   ))
   (setq F:TEMPL nil)
   (setq F:SQLUSR nil F:SQLPAS nil F:FMSEASEL nil F:EASELST nil)
   (princ)
)
;;;FMS 3.2;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;Function to modify selected EASEL File
(defun C:EASEL_MODIFY( / prmpt chk)
   (setq defeasel (F:EASEL_GETDEF))
   (setq prmpt "EASEL File to Modify")
   (if (= (getvar "filedia") 1)
   (progn
      (setq chk (getfiled prmpt defeasel "esl" 0))
      (if (= chk 1)
         (setq defeasel (fmsgetstr defeasel 100 prmpt))
         (setq defeasel chk)
      )
   )
      (setq defeasel (fmsgetstr defeasel 100 prmpt))
   )
   (if defeasel
   (progn
      (setq F:EASELFILE defeasel)
      (F:EASEL_SETDEF F:EASELFILE)
      (F:EASEL_MODIFY)
   ))
   (princ)
)
(defun F:EASEL_MODIFY( / defeasel yn)
   (if F:FMSEASEL (F:EASEL_DEINIT))
   ;;;This should return the name of an EASEL File or nil....
   (if (setq defeasel (ads_eascfg))
   (progn
      (setq F:EASELFILE defeasel)
      (F:EASEL_SETDEF F:EASELFILE)
   ))
   (if (F:EASEL_INIT)
   (progn
      (setq yn (getyn "\nBuild New Lookup Table? (Y/N) <Y>: " "Yes" "No"))
      (if (= yn "Yes")(F:EASEL_BLD))
   ))
   (princ)
)

;Access to config dialog to create new EASEL File
(defun C:EASEL_NEW()
   (F:EASEL_NEW)
   (princ)
)
(defun F:EASEL_NEW( / yn newesl)
   (if F:FMSEASEL (F:EASEL_DEINIT))
   (setq F:FMSEASEL nil)
   ;;;This should return the name of an EASEL File or nil....
   (if (setq newesl (ads_eascfg))
   (progn
      (setq yn (getyn (strcat "\nSave as default EASEL File? (Y/N) <Y>: ") "Yes" "No"))
      (if (= yn "Yes")
      (progn
         (setq F:EASELFILE newesl)
         (F:EASEL_SETDEF F:EASELFILE)
      ))
      (if (F:EASEL_INIT)
      (progn
         (setq yn (getyn "\nBuild New Lookup Table? (Y/N) <Y>: " "Yes" "No"))
         (if (= yn "Yes")(F:EASEL_BLD))
      ))
   ))
   (princ)
)

;Delete Selected Easel File
(defun C:EASEL_DELETE()
   (F:EASEL_DELETE)
   (princ)
)
(defun F:EASEL_DELETE( / chk f deleasel defeasel prmpt)
   (setq defeasel (F:EASEL_GETDEF))
   (setq prmpt "EASEL File to Delete")
   (if (= (getvar "filedia") 1)
   (progn
      (setq chk (getfiled prmpt defeasel "esl" 0))
      (if (= chk 1)
         (setq deleasel (fmsgetstr defeasel 100 prmpt))
         (setq deleasel chk)
      )
   )
      (setq deleasel (fmsgetstr defeasel 100 prmpt))
   )
   (if deleasel
   (progn
      (if (equal deleasel defeasel)
         (alert "\nWARNING: This is the currently referenced EASEL File.")
      )
      (if (setq f (open deleasel "a"))
      (progn
         (close f)
         (setq yn (getyn (strcat "\nDelete file " deleasel "? (Y/N) <Y>: ") "Yes" "No"))
         (if (= yn "Yes")(ads_delfil deleasel))
      )
         (princ "\nERROR: Unable to remove this file from disk.")
      )
   ))
   (princ)
)
;Create Layer Filter list for Existing EASEL File
(defun C:EASEL_LAYER()
   (F:EASEL_LAYER)
   (princ)
)
(defun F:EASEL_LAYER( / dcl_id f echo templlst tmplst tslay val len ct
                       yn laylst lay sellst mindxlst tindxlst holdlst
                       indx)
                       ;global F:SQL_LAY F:EASEL_TEMPL tindx mindx
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (setq holdlst F:SQL_LAY)
   (if (or (not F:SQL_LAY)(= F:SQL_LAY ""))
      (setq F:SQL_LAY "*")
   )
   (if F:EASEL_TEMPL (setq templlst (list F:EASEL_TEMPL)))
   ;;;accesses all layers in layer table
   (setq laylst (list (cdr (assoc 2 (tblnext "layer" T)))))
   (while (setq lname (tblnext "layer"))
      (setq lname (cdr (assoc 2 lname)))
      (setq laylst (append (list lname) laylst))
   )
   (setq laylst (acad_strlsort laylst))
   (setq indx (val2idx F:SQL_LAY laylst))
   (if (not indx)(setq indx ""))
   (setq sellst (f:fmslst_dcl laylst "LAYER FILTER" indx))
   (if (= sellst "")
      (setq sellst "*")
   )
   ;;;NEED TO PUT IN CHECK FOR CANCEL???
   (if sellst (setq F:SQL_LAY sellst))
   (ads_laycfg F:SQL_LAY)
   (princ)
)

(defun F:EASEL_PACK()
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (if (or (= (strcase F:SQLDRV) "DBASE3")(= (strcase F:SQLDRV) "DBASE4"))
   (progn
      (princ "\nPacking DBF file...")
      (ads_sqlpck F:SQLDBM F:SQLTAB)
   )
      (princ "\nERROR: Invalid driver, PACK not applicable.")
   )
   (princ)
)

;Build Look Up Table with current EASEL Parameters
(defun C:EASEL_BLD()
   (F:EASEL_BLD)
   (princ)
)
(defun F:EASEL_BLD( / ss blklst laylst)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (setq tmp (F:EASEL_CHK_FILT))
   (setq blklst (car tmp) laylst (cadr tmp))
   (cond
      ((= F:SQL_MODE "REGAPP")
         (setq ss (ssget "x" (list (cons 8 laylst)(list -3 (list F:SQL_INDEX)))))
      )
      ((= F:SQL_MODE "ATTDEF")
         (if blklst
         (progn
            (setq ss (ssget "x" (list (cons 0 "INSERT")(cons 2 blklst)(cons 8 laylst))))
         ))
      )
      (t
         (princ "\nERROR: Invalid Link mode.")
      )
   )
   (if ss
      (ads_bildhi ss (substr F:SQLTAB 1 3))
   (progn
      (ads_delfil (strcase (strcat (substr F:SQLTAB 1 3) ".LUT")))
      (ads_delfil (strcase (strcat (substr F:SQLTAB 1 3) ".LUX")))
      (princ "\nWARNING: No Valid Entities Found.")
   ))
   (princ)
)


;Change All Marked EASEL Entities to Color BYLAYER
(defun C:EASEL_UNMARK()
   (F:EASEL_UNMARK)
   (princ)
)
(defun F:EASEL_UNMARK( / blklst laylst ss)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (setq tmp (F:EASEL_CHK_FILT))
   (setq blklst (car tmp) laylst (cadr tmp))
   (if (= F:SQL_MODE "ATTDEF")
   (progn
      (setq blklst (lts F:EASEL_BLKLST))
      (setq ss (ssget "x" (list
               (cons 0 "INSERT")
               (cons 2 blklst)
               (cons 8 laylst)
               (cons -4 "<")(cons 62 256)
      )))
   ))
   (if (= F:SQL_MODE "REGAPP")
   (progn
      (setq ss (ssget "X" (list
               (cons 8 laylst)
               (cons -4 "<")(cons 62 256)
               (list -3 (list F:SQL_INDEX))
      )))
      (if (/= F:SQL_MASK "")
      (progn
         (princ "\nSorting Selection on MASK...Please Wait.")
         (setq ss (F:EASEL_SORTSS ss))
      ))
      (if ss
         (command "_chprop" ss "" "_c" "_bylayer" "")
         (princ "\nWARNING: No Marked Entities Found.")
      )
   ))
   (setq ss nil)
   (princ)
)

;Erase All Inserts on layer 'EASEL'HATCH
(defun C:EASEL_UNHATCH()
   (F:EASEL_UNHATCH)
   (princ)
)
(defun F:EASEL_UNHATCH( / ss)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (setq ss (ssget "X" (list
            (cons 0 "INSERT")
            (cons 8 (strcat (substr F:SQLTAB 1 3) "HATCH"))
   )))
   (if ss
      (command "_erase" ss "")
      (princ "\nWARNING: No Hatched Entities Found.")
   )
   (princ)
)

;Erase All Solids on layer 'EASEL'HATCH
(defun C:EASEL_UNFILL()
   (F:EASEL_UNFILL)
   (princ)
)
(defun F:EASEL_UNFILL( / ss)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (setq ss (ssget "X" (list
            (cons 0 "SOLID")
            (cons 8 (strcat (substr F:SQLTAB 1 3) "HATCH"))
   )))
   (if ss
      (command "_erase" ss "")
      (princ "\nWARNING: No Filled Entities Found.")
   )
   (princ)
)

;Erase All Text Found on layer 'EASEL'TEMP
(defun C:EASEL_CLRATT()
   (F:EASEL_CLRATT)
   (princ)
)
(defun F:EASEL_CLRATT( / ss)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (setq ss (ssget "X" (list
            (cons 0 "TEXT")
            (cons 8 (strcat (substr F:SQLTAB 1 3) "TEMP"))
   )))
   (if ss
      (command "_erase" ss "")
      (princ "\nWARNING: No Attribute Data Found.")
   )
   (princ)
)

;LOCATE a feature from easting northing coords...If in dwg mark
(defun C:EASEL_ZOOM( / zm)
   (setq zm (nth 0 (fmsstr "fmsac" "**ZOOM")))
   (F:EASEL_ZOOM zm)
   (princ)
)
(defun F:EASEL_ZOOM(zm / )
   (setvar "expert" 1)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (if (setq ll (ads_locate zm))
      (fmszm ll)
      (princ "ERROR: ID not found in database.")
   )
   (setvar "expert" 0)
   (princ)
)
;EXTRACT BLOCK Data to ASCII File
(defun C:EASEL_EXTR( / fname1 fname2)
   (setq fname1 (getfiled "Template File Selection" (strcat V:TXT (substr F:SQLTAB 1 3) "ext.txt") "txt" 0))
   (setq fname2 (getfiled "Extract File Name" F:SQLTAB "txt" 5))
   (F:EASEL_EXTR fname1 fname2)
   (princ)
)
(defun F:EASEL_EXTR(fname1 fname2 / yn)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (setq yn (getyn "\nEXTRACT all features in DWG? (Y/N) <Y>: " "Yes" "No"))
   (if (= yn "No")
   (progn
      (princ "\nSelect entities to EXTRACT: ")
      (setq ss (ssget))
      (if ss
         (command "_attext" "_e" "_p" "" "_c" fname1 fname2)
      )
   )
      (command "_attext" "_c" fname1 fname2)
   )
   (princ)
)
;EXTRACT BLOCK Data to ASCII File...
;;;THIS WILL NEED TO BE IMPLEMENTED LATER........
(defun F:EASEL_X_EXTR(fname1 fname2 / yn)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (princ)
)

;Access Main SQL Interface with current SQL + EASEL Parameters
(defun C:EASEL_OPEN( / temp temp2)
   (if (not F:SQLMODE)(setq F:SQLMODE 0))
   (setq ok nil)
   (while (not ok)
      (setq temp2 (itoa F:SQLMODE))
      (initget "? 0 1 2 3 4 5 6 7 8 9")
      (setq temp (getkword (strcat "\n\n\n\nEnter SQL option or ? for list <" temp2 ">: ")))
      (princ "\n")
      (graphscr)
      (if (or (= temp "")(not temp))(setq temp (itoa F:SQLMODE)))
      (if (= temp "?")
      (progn
         (textscr)
         (princ "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n")
         (princ "\n 0:  Access MAIN SQL Table")
         (princ "\n 1:  Access SPATIAL SQL Table")
         (princ "\n 2:  Create RECTILINIAR Spatial SQL Table")
         (princ "\n 3:  Create RADIAL Spatial SQL Table")
         (princ "\n 4:  Create POLYGONAL Spatial SQL Table")
         (princ "\n 5:  Create MANUAL Spatial SQL Table")
         (princ "\n 6:  Create Spatial SQL Table From Network TRACE")
         (princ "\n 7:  Select ENTITY and Edit SQL Record")
         (princ "\n 8:  Select AREA and Edit SQL Record")
;         (princ "\n 9:  EASEL File Manipulation Options.")
         (princ "\n10:  MODIFY Current SQL Table")
      )
      (progn
         (setq F:SQLMODE (atoi temp))
         (setq ok T)
      ))
   )
   ;;;ADDED!!!!!!!!
   (if F:FMSEASEL (F:EASEL_EASEL2))
   (F:EASEL_OPEN F:SQLMODE)
   (princ)
)
(defun F:EASEL_OPEN(mode / blklst e ea eb efnc f found id indx laylst no
                          p1 p2 pcirc pt ptlist qury rad segln ss tmp ts
                          x1 x2 y1 y2 answr dbecho yn adeprop adeindx
                          adeval ok srch blk_id_att bname
                          fldct indx yn
                          ang space dbl multi
                          result fname temp)
   (setq answr nil)
   (while (not F:FMSEASEL)
      (if (F:EASEL_EASEL "SET Current Easel File")
      (progn
         (setq yn (getyn "\nBuild New Lookup Table? (Y/N) <Y>: " "Yes" "No"))
         (if (= yn "Yes")(F:EASEL_BLD))
      ))
   )
   (setvar "regenmode" 0)
   (F:MLAY (strcat (substr F:SQLTAB 1 3) "HATCH"))
   (F:MLAY (strcat (substr F:SQLTAB 1 3) "TEMP"))
   (setq tmp (F:EASEL_CHK_FILT))
   (setq blklst (car tmp) laylst (cadr tmp))
   (if (setq f (open "fmshmlss.txt" "r"))
   (progn
      (close f)
      (ads_delfil "fmshmlss.txt")
   ))
   (if (setq f (open "fmsads.lst" "r"))
   (progn
      (close f)
      (ads_delfil "fmsads.lst")
   ))
   (cond
;;;;;;MAIN SQL TABLE
      ((= mode 0)
         (ads_sqlman F:SQLDBM F:SQLTAB)
      )
;;;;;;SPATIAL SQL TABLE
      ((= mode 1)
         (ads_sqlman F:SQLDBM F:SQLSPA)
      )
;;;;;;RECTILINEAR SUBSET
      ((= mode 2)
         (if (or (= F:SQL_NO "")(= F:SQL_EA ""))
            (princ "\nERROR: No Easting/Northing Coordinate fields defined in EASEL.")
         (progn
            (setvar "blipmode" 1)
            (setq p1 (getpoint "\nSelect window, Lower Left first: "))
            (setq p2 (getcorner p1 "\nSelect window, Upper Right next: "))
            (setvar "blipmode" 0)
            (setq x1 (rtos (car p1) 2 2) y1 (rtos (cadr p1) 2 2))
            (setq x2 (rtos (car p2) 2 2) y2 (rtos (cadr p2) 2 2))
            (if (< x2 x1)(progn(setq tmp x1)(setq x1 x2)(setq x2 tmp)(setq tmp nil)))
            (if (< y2 y1)(progn(setq tmp y1)(setq y1 y2)(setq y2 tmp)(setq tmp nil)))
            (setq qury
            (strcat "INSERT INTO " F:SQLSPA " SELECT * FROM " F:SQLTAB " WHERE " F:SQL_EA " BETWEEN " x1 " AND " x2 " AND " F:SQL_NO " BETWEEN " y1 " AND " y2))
            (ads_sqlqry F:SQLDBM F:SQLSPA qury)
            (ads_sqlman F:SQLDBM F:SQLSPA)
         ))
      )
;;;;;;RADIUS SUBSET
      ((= mode 3)
         (setvar "blipmode" 1)
         (F:MLAY "temp")
         (while (not (setq pt (getpoint "\nSelect center of query area: ")))
            (princ "\nERROR: Invalid point entered.")
         )
         (princ "\nEnter Radius of window area: ")
         (command "_circle" pt pause)
         (setq rad (cdr (assoc 40 (entget (entlast)))))
         (setvar "blipmode" 0)
         (setq e (entlast) segln (/ rad 6))
         (cirpline e segln)
         (command "_erase" e "")
         (setq pcirc (entlast))
         (princ "\nAccessing coordinates...")
         (setq ptlist (brkout (entget pcirc) nil nil))
         (setq yn (getyn "\nWindow or Crossing? (W/C) <C>: " "Crossing" "Window"))
         (setq ss (F:EASEL_PWIN ptlist yn))
         (command "_erase" pcirc "")
         (if ss
         (progn
            (setq qury (strcat "INSERT INTO " F:SQLSPA " SELECT * FROM "
                                F:SQLTAB " WHERE " F:SQL_TSKEY " IN "))
            (ads_sildhi ss F:SQLDBM F:SQLSPA qury)
            (ads_sqlman F:SQLDBM F:SQLSPA)
         )
            (princ "\nERROR: No valid entities selected")
         )
      )
;;;;;;POLYGON SUBSET
      ((= mode 4)
         (setq found nil)
         (while (= found nil)
            (setq yn
               (getyn "\nWould you like to draw a boundary? (Y/N) <Y>: " "Yes" "No"))
            (if (= yn "Yes")
            (progn
               (F:MLAY "temp")
               (drawedge)
               (setq efnc (entlast)
                     eb (entget efnc)
                     tmp T
               )
            )
            (progn
               (princ "\nPoint to POLYLINE boundary to reference: ")
               (setq efnc (ssname (ssget) 0))
               (setq eb (entget efnc))
               (setq tmp nil)
            ))
            (if (or (/= "POLYLINE" (cdr (assoc 0 eb)))(/= 1 (rem (cdr (assoc 70 eb)) 2)))
               (princ "\nEntity is not a Closed Pline")
               (setq found 1)
            )
         )
         (setq eb (entget efnc))
         (princ "\nAccessing coordinates...")
         (setq ptlist (brkout eb nil nil))
         (setq yn (getyn "\nWindow or Crossing? (W/C) <C>: " "Crossing" "Window"))
         (setq ss (F:EASEL_PWIN ptlist yn))
         (if tmp (command "_erase" efnc ""))
         (if ss
         (progn
            (setq qury (strcat "INSERT INTO " F:SQLSPA " SELECT * FROM "
                                F:SQLTAB " WHERE " F:SQL_TSKEY " IN "))
            (ads_sildhi ss F:SQLDBM F:SQLSPA qury)
            (ads_sqlman F:SQLDBM F:SQLSPA)
         )
            (princ "\nERROR: No valid entities selected")
         )
      )
;;;;;Manual Select
      ((= mode 5)
         (setq ss (ssget))
         (if ss
         (progn
            (princ "\nSorting Selection...Please wait.")
            (if (= F:SQL_MODE "REGAPP")
            (progn
               (setq ss (ssget "P" (list (cons 8 laylst)(list -3 (list F:SQL_INDEX)))))
               (if (/= F:SQL_MASK "")
               (progn
                  (princ "\nSorting Selection on MASK...Please Wait.")
                  (setq ss (F:EASEL_SORTSS ss))
              ))
            ))
            (if (= F:SQL_MODE "ATTDEF")
            (progn
               (setq ss (ssget "P" (list (cons 2 blklst))))
            ))
         ))
         (if (> (sslength ss) 0)
         (progn
            (setq qury (strcat "INSERT INTO " F:SQLSPA " SELECT * FROM "
                                F:SQLTAB " WHERE " F:SQL_TSKEY " IN "))
            (ads_sildhi ss F:SQLDBM F:SQLSPA qury)
            (ads_sqlman F:SQLDBM F:SQLSPA)
         ))
      )
;;;;;ISOLATE TRACE
      ((= mode 6)
         (setq qury (strcat "INSERT INTO " F:SQLSPA " SELECT * FROM "
                            F:SQLTAB " WHERE " F:SQL_TSKEY " IN "))
         (ads_tildhi (strcat (strcase (substr F:TEMPL 1 3) T) "trace.txt") F:SQLDBM F:SQLSPA qury)
         (ads_sqlman F:SQLDBM F:SQLSPA)
      )
;;;;;SELECT/EDIT .....No Areas
      ((= mode 7)
         (princ "\nSelect Entity, or <CR> to Enter ID: ")
         (setq ss (ssget))
         (if ss
         (progn
            (setq answr (F:EASEL_GETENT ss))
            (setq e (car answr))
            (setq id (cadr answr))
         )
         (progn
            (setq id (fmsgetstr "" 8 "Enter ID of Pertinent Feature"))
         ))
         (if id (setq qury (strcat F:SQL_TSKEY " LIKE '" id "'")))
         (if (not qury)
            (princ "\nNo valid entity referenced")
         (progn
;            (PRINC "\nQURY: ")(PRINC QURY)(GETSTRING "\nADS_SQLSEE...:")
            (setq answr (list e (ads_sqlsee F:SQLDBM F:SQLTAB qury)))
         ))
      )
;;;;;SELECT/EDIT .....Areas
      ((= mode 8)
         (if (= F:SQL_MODE "REGAPP")
         (progn
            (setq done nil)
            (setq srch (atof (nth 0 (fmsstr "fmsac" "**SRCHDIST"))))
            (while (not done)
               (princ "\nSelect Area, or <CR> for other options: ")
               (setq pt (getpoint))
               (if pt
               (progn
                  (if (setq e (F:EASEL_PICK pt srch))
                  (progn
                     (cond
                        ((= F:SQL_DXF "1000")
                           (setq id (fmsxchkmask F:SQL_INDEX F:SQL_MASK e))
                        )
                        ((= F:SQL_DXF "1040")
                           (setq id (F:EASEL_XDATA e F:SQL_INDEX 1040))
                           (if id (setq id (rtos (cdr id))))
                        )
                        ((= F:SQL_DXF "1070")
                           (setq id (F:EASEL_XDATA e F:SQL_INDEX 1070))
                           (if id (setq id (itoa (cdr id))))
                        )
                     )
                     (setq done T)
                  ))
               )
               (progn
                  (princ "\nSelect Entity, or <CR> to Enter ID: ")
                  (setq ss (ssget))
                  (if ss
                  (progn
                     (setq answr (F:EASEL_GETENT ss))
                     (setq e (car answr))
                     (setq id (cadr answr))
                     (setq done T)
                  )
                  (progn
                     (setq id (fmsgetstr "" 50 "Enter ID of Pertinent Feature"))
                     (setq done T)
                  ))
               ))
            )
            (if id (setq qury (strcat F:SQL_TSKEY " LIKE '" id "'")))
            (if (not qury)
               (princ "\nNo valid entity referenced")
            (progn
;               (PRINC "\nQURY: ")(PRINC QURY)(GETSTRING "\nADS_SQLSEE...:")
               (setq answr (ads_sqlsee F:SQLDBM F:SQLTAB qury))
            ))
         ))
      )
      ;;;Modify EASEL File
      ((= mode 9)
;         ;;;This should return the name of an EASEL File or nil....
;         (ads_eascfg)
;         (fmseasel F:TEMPL)
;         (setq yn (getyn "\nModify Layer Parameter Filter List? (Y/N) <N>: " "No" "Yes"))
;         (if (= yn "Yes")(F:EASEL_LAYER))
;         (setq yn (getyn "\nBuild New Lookup Table? (Y/N) <Y>: " "Yes" "No"))
;         (if (= yn "Yes")(F:EASEL_BLD))
          (fmscfg F:TEMPL)
      )
      ;;;Modify SQL TABLE
      ((= mode 10)
         (ads_sqlmod)
      )
      (T
         (princ "\nERROR: Invalid option keyword")
      )
   )
   (if (setq f (open "fmsads.lst" "r"))
   (progn
      (if (eval (read "c:adexload"))
      (progn
         (setq ok nil)
         (if (and (= F:SQL_MODE "REGAPP")(/= F:SQL_MASK ""))
         (progn
            (setq adeprop "EEd")
            ;;;adeconfig....regapp F:SQL_INDEX
            (setq adeindx F:SQL_MASK)
            (setq ok T)
         ))
         (if (= F:SQL_MODE "ATTDEF")
         (progn
            (setq adeprop "ATtrib")
            (setq adeindx (strcat F:SQL_INDEX "="))
            (setq ok T)
         ))
         (if ok
         (progn
            (if (not F:ADEMODE)(c:ademode))
            (c:_adeclrquery)
            (while (setq adeval (read-line f))
               (C:_ADEDEFQUERY "Union(Or)" "Properties" adeprop (strcat adeindx adeval))
            )
            (c:_adeexequery F:ADEMODE "*")
            (c:_adeclrfea)
            (if (/= F:ADEMODE "Show")(redraw))
         )
            (princ "\nERROR: Current EASEL Cannot Query ADE.")
         )
      )
         (princ "\nERROR: ADE Not loaded.")
      )
      (close f)
   ))
   (if (setq f (open "fmshmlss.txt" "r"))
   (progn
      (princ "\nWARNING: Entities Referenced which are not in current lookup table.")
      (if (eval (read "c:adexload"))
      (progn
         (setq ok nil)
         (if (not F:ADEMODE)(c:ademode))
         (if (and (= F:SQL_MODE "REGAPP")(/= F:SQL_MASK ""))
         (progn
            (if (/= F:SQL_INDEX (c:adegetcfg "APPNAME"))
            (progn
               (setq yn (getyn "\nModify ADE Registered Application Name? (Y/N) <Y>:" "Yes" "No"))
               (if (= yn "Yes")
               (progn
                  (c:adeconfig "APPNAME" F:SQL_INDEX)
               ))
            ))
            (setq adeprop "EEd")
            (setq adeindx F:SQL_MASK)
            (setq ok T)
         ))
         (if (= F:SQL_MODE "ATTDEF")
         (progn
            (setq adeprop "ATtrib")
            (setq adeindx F:SQL_INDEX)
            (setq ok T)
         ))
         (if ok
         (progn
            (setq yn (getyn "\nRetrieve through ADE? (Y/N) <Y>: " "Yes" "No"))
            (if (= yn "Yes")
            (progn
               (if (not F:ADEMODE)(c:ademode))
               (c:_adeclrfea)
               (c:_adeclrquery)
               (setq col (itoa (getvar "USERI1")))
               (setq result (getvar "USERI2"))
               (if (and col (= result 1))
                  (c:_adedeffea "Color" col)
               )
               (while (setq adeval (read-line f))
                  (C:_ADEDEFQUERY "Union(Or)" "Properties" adeprop (strcat adeindx adeval))
               )
               (close f)
               (c:_adeexequery F:ADEMODE "*")
               (c:_adeclrfea)
               (if (/= F:ADEMODE "Show")(redraw))
               ;;;******************************************
               ;;;FILL
               (if (= result 2)
               (progn
                  (F:EASEL_BLD)
                  (F:MLAY (strcat (substr F:SQLTAB 1 3) "HATCH"))
                  (F:XLOAD "ads_fill")
                  (command "_color" col)
                  (setq f (open "fmshmlss.txt" "r"))
                  (while (setq ts (read-line f))
                     (if (setq e (ads_timent ts (strcase (substr F:TEMPL 1 3) T)))
                        (fill e)
                     )
                  )
                  (close f)
                  (F:XUNLOAD "ads_fill")
               ))
               ;;;HATCH
               (if (= result 3)
               (progn
                  (F:EASEL_BLD)
                  (setq fname (getenv F:SQLDBM))
                  (setq temp (substr fname (strlen fname)))
                  (if (or (= temp "/")(= temp "\\"))
                     (princ)
                     (setq fname (strcat fname "/"))
                  )
                  (setq fname (strcat fname (strcase (substr F:SQLTAB 1 3)) "HCH.DTA"))
                  (setq f (open fname "r"))
                  (read-line f)
                  (setq col (read-line f))
                  (setq ang (atof (read-line f)))
                  (setq space (atof (read-line f)))
                  (setq dbl (read-line f))   ;;Double 0 or 1=double
                  (setq multi (read-line f)) ;;Single Entity 1 = 1 at a time
                  (close f)
                  (if (= dbl 0)(setq dbl "N")(setq dbl "Y"))
                  (command "_color" col)
                  (setq ss (ssadd))
                  (setq f (open "fmshmlss.txt" "r"))
                  (while (setq ts (read-line f))
                     (if (setq e (ads_timent ts (strcase (substr F:TEMPL 1 3) T)))
                        (if (= multi "1")
                           (command "_hatch" "_u" ang space dbl e "")
                           (ssadd e ss)
                        )
                     )
                  )
                  (if (= multi "0")
                     (command "_hatch" "_u" ang space dbl ss "")
                  )
               ))
               (setvar "USERI1" 0)
               (setvar "USERI2" 0)
            ))
         ))
      ))
   ))
   answr
)

;Edit Graphic entity's link structure
(defun C:EASEL_MOD_LINK()
   (F:EASEL_MOD_LINK)
   (princ)
)
(defun F:EASEL_MOD_LINK( / blklst laylst e id answr newid tmp)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (setq tmp (F:EASEL_CHK_FILT))
   (setq blklst (car tmp) laylst (cadr tmp))
   (princ "\nSelect Entity, (or <CR> to Enter ID??????): ")
   (setq ss (ssget))
   (if ss
   (progn
      (if (setq answr (F:EASEL_GETENT ss))
      (progn
         (setq e (car answr))
         (setq id (cadr answr))
      ))
   ))
   (if (and e id)
   (progn
      (setq newid (fmsgetstr id 35 "Modify Current Graphic Link"))
      (if (and e newid)
         (modrapp e F:SQL_INDEX F:SQL_MASK (cons 1000 (strcat F:SQL_MASK newid)))
      )
   )
      (princ "\nInvalid Entity Selected")
   )
   (princ)
)

;Create Graphic entity's link structure
(defun C:EASEL_ADD_LINK()
   (F:EASEL_ADD_LINK)
   (princ)
)
(defun F:EASEL_ADD_LINK( / blklst laylst e id answr newid)
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (if (= F:SQL_MODE "ATTDEF")
      (princ "\nINVALID, Attdef cannot be added to existing entity.")
   (progn
      (princ "\nSelect Entity: ")
      (setq ss (ssget))
      (if ss
      (progn
         (setq answr (F:EASEL_GETENT ss))
         (if (car answr)
            (princ "\nINVALID, SQL Link Already exists on entity...Use Edit.")
         (progn
            (setq e (ssname ss 0))
            (setq id "")
         ))
      ))
;      (princ "\nE: ")(princ e)(princ "\nID: ")(princ id)(getstring "\nGO: ")
      (if (and e id)
      (progn
         (setq newid (fmsgetstr id 35 "Create Current Graphic Link"))
         (SETQ NEWID (STRCAT F:EASEL_MASK NEWID))
         (if (fmsxchk e F:SQL_INDEX)
            (updrapp e F:SQL_INDEX (cons 1000 newid))
            (addrapp e F:SQL_INDEX (cons 1000 newid))
         )
      ))
   ))
   (princ)
)
;Function to walk a regapp group to UPDATE the 1000 group with the mask
;using the string argument
(defun C:EASEL_XD_UPDATE( / ss e rapp mask newval yn)
   (setq rapp (strcase (getstring "\nRAPP: ")))
   (setq mask (strcase (getstring "\nMASK: ")))
   (setq newval (strcase (getstring "\nNEWVAL: ")))
   (setq e (ssname (ssget) 0))
   (F:EASEL_XD_UPDATE rapp mask e newval)
)
(defun F:EASEL_XD_UPDATE(rapp mask e newval / dxfgrp)
   (setq dxfgrp (cons "1000" newval))
   (if (fmsxchk e rapp)
      (if (fmsxchkmask rapp mask e)
         (modrapp e rapp mask dxfgrp)
         (updrapp e rapp dxfgrp)
      )
      (addrapp e rapp dxfgrp)
   )
)
;Creates list of blocks in header table which reference ATTDEF given
(defun C:EASEL_MAKE_BLKLST( / att)
   (setq att (strcase (getstring "\nATTDEF TO REFERENCE: ")))
   (F:EASEL_MAKE_BLKLST att)
)
;Creates list of blocks found in header table which reference ATTDEF arg
(defun F:EASEL_MAKE_BLKLST(att / blklst blkchk bname ct donetbl doneblk
                                e eb ebb etype)
   (setq blklst ())
   (setq ct 0)
   (setq donetbl nil)
   (while (not donetbl)
      (if (= ct 0)
         (setq ebb (tblnext "block" T))
         (setq ebb (tblnext "block"))
      )
      (setq ct (+ ct 1))
      (if ebb
      (progn
         (setq bname (cdr (assoc 2 ebb)))
         (setq e (cdr (assoc -2 ebb)))
         (setq doneblk nil)
         (while (not doneblk)
            (setq eb (entget e))
            (setq etype (cdr (assoc 0 eb)))
            (if (= etype "ATTDEF")
            (progn
               (if (= (cdr (assoc 2 eb)) att)
                  (setq blklst (append (list bname) blklst))
               )
            ))
            (setq e (entnext e))
            (if (or (= etype "ENDBLK")(not e))
               (setq doneblk T)
            )
         )
      )
         (setq donetbl t)
      )
   )
   blklst
)


;Function to isolate a group of records from a list of keys in a text file
(defun C:EASEL_UPD_ASCII()
   (while (not F:FMSEASEL)
      (F:EASEL_EASEL "SET Current Easel File")
   )
   (F:EASEL_UPD_ASCII)
   (princ)
)
(defun F:EASEL_UPD_ASCII()
   ;;;bit on end???
   (setq prmpt "File to Reference")
   (if (= (getvar "filedia") 1)
      (setq fname (getfiled prmpt "" "*" 0))
      (setq fname (fmsgetstr "" 100 prmpt))
   )
   (if fname
   (progn
      (setq fldlst (ads_sqllst))
      ;;;Make a dcl call in fmsdia for list...sel 1 or many....
      (setq keyfld (fmsgetlst fldlst 0))
      (setq qury (strcat "INSERT INTO " F:SQLSPA " SELECT * FROM "
                         F:SQLTAB " WHERE " keyfld " IN "))
      (ads_tildhi fname F:SQLDBM F:SQLSPA qury)
      (ads_sqlman F:SQLDBM F:SQLSPA)
   ))
   (princ)
)

