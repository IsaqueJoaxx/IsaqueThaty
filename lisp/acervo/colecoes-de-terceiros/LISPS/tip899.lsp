;TIP #899:  VSLD1.LSP   Insert Manager   (c)1993, Terry Priest


; Copyright from 8-90 by Terry Priest 
; The user needs to implement **DIRS and **BLANK.
; This is the abbreviated version of VSLD or VSLDLITE. The full featured
; version (with Dos functions) is on Cserve.
;*****************************************************************************
;Global variables: fls, path, flage, flagsv
(defun C:VSLD1 (/ fl pageno readno scrlen pages pathln)
   (setq scrlen 20) ;change screen length here to match your display's # of lines
   (if fls (setq pathln (dirlen (car fls)))
   (setq path (getdir path) fls (getdwg path) pathln (1+ (strlen path))))
   (setq pageno 0  pages (pag#s fls scrlen))
   (menucmd "S=BLANK")                ;**Blank is a blank menu page
   (while (/= readno (+ scrlen 3))  (grtext)                 ;The menu loop
      (grtext (+ scrlen 1) "PREVIOUS")
      (grtext (+ scrlen 2) "NEXT")
      (grtext (+ scrlen 3) "EXIT")
      (grtext (+ scrlen 5) "INSERT")
      (grtext (+ scrlen 7) (if flage "Exit Off"  "Exit On"))
      (grtext (+ scrlen 8) (if flagsv "Sav L ON" "SavL OFF"))
      (prompt "SELECT BLOCK/DWG TO VIEW SLIDE\n")
      (dspfls fls scrlen pathln pageno)              ;display file list
      (grtext -1 (strcat (substr (car fls) 1 (1- pathln))
      " " (if fl (substr fl pathln) "")))
      (grtext -2 (strcat "Page No. " (itoa pageno)))
      (setq readno (nth 1 (grread)))                       ;stop here
      (cond                                 ;which line number does readno contain
         ((and (>= readno 0) (< readno scrlen)(not (listp readno))) ;in the file list
            (if (setq fl (nth (+ readno (* pageno scrlen)) fls))
               (if (findfile (strcat fl ".SLD")) (command "VSLIDE" fl)
         (progn (grclear) (prompt (strcat " No Slide found for " fl "\n"))))))

         ((= readno (+ scrlen 1)) (prevpg))              ;"Previous" page
         ((= readno (+ scrlen 2)) (nextpg))              ;"Next" page

         ((= readno (+ scrlen 5)) (if fl                   ;"Insert" option
               (if (findfile (strcat fl ".DWG"))
                  (if (not flage) (progn  (command "INSERT" fl ) (setq readno (+ scrlen 3))) 
                  (command "REDRAW" "INSERT" fl pause pause pause pause))
         (prompt (strcat " NO DRAWING FOUND FOR " fl "\n"))))) 

         ((= readno (+ scrlen 7))                       ;"Exit On" "Exit Off" toggle
         (if flage (setq flage nil)(setq flage T))) ;flage = flag_exit

         ((= readno (+ scrlen 8))                        ;"Save List On/Off"
         (if flagsv (setq flagsv nil)(setq flagsv T)))  ;flagsv = flag_save_vsldlist
   ))  ;cond and while
   (if (not flagsv) (setq fls nil))
(grtext) (menucmd "S=S")(redraw)) ;exit to your menu screen, end function VSLD
;*****************************************************************************
;Slash operator subroutine - changes menu foreslash to dos backslash (fix)
;pslash is from "Inside Autolisp", Smith & Gesner,-"gratefully acknowledged"
(defun pslash (path / inc slash wpath char)
   (setq inc 1  wpath ""  slash "\\")
   (while (/= "" (setq char (substr path inc 1)))
      (setq wpath (strcat wpath (if (member char '("\\" "/")) slash char))
   inc (1+ inc)))
   (if (and (/= wpath "") (/= (substr wpath (strlen wpath) 1) slash))
   (setq wpath (strcat wpath slash)))
wpath) 

;Subr getdwg is a derivative of GETFIL from "Inside Autolisp" Smith & Gesner
(defun getdwg (path  / fls fl fil)
   (setq fil (open "dir.$" "w")) (close fil)
   (setq fl (strcat path "*.dwg" ))
   (command "SH" (strcat "for %f in (" fl ") do echo %f >> " "dir.$"))
   (command "SH"  "SORT < dir.$ > tmp.$")
   (command "SH"  "copy tmp.$ dir.$")
   (command "SH" "del tmp.$")
   (if (setq fil (open "dir.$" "r")) (progn
         (if (setq fl (read-line fil)) 
            (while (and fl (/= "" fl))            ;the read-line loop
               (setq fls (append fls (list (substr fl 1 (- (strlen fl) 5)))))
         (setq fl (read-line fil))))   ; while if
      (close fil) (command "SH" "del dir.$")) ;progn
   (prompt "\nFile could not be opened ")) ;if
(if fls fls (prompt "\nNo files found ")))

;Number of Screen Pages subroutine
(defun pag#s (fls scrlen / pages)
   (setq pages (/ (length fls) scrlen))
   (if (and (= 0 (rem (length fls) scrlen))(>= pages 1))(setq pages (1- pages)))
pages)    ;returns

;Display Files to Screen subroutine
(defun dspfls (fls scrlen pathln pageno / inc)
   (setq inc 0)
   (repeat scrlen (if (> (length fls) (+ inc (* pageno scrlen)))   
      (grtext inc (substr (nth (+ inc (* pageno scrlen)) fls) pathln)))
(setq inc (1+ inc))))

(defun prevpg ()   ;Subr previous page
(if (/= pageno 0) (setq pageno (1- pageno)) (setq pageno pages)))
(defun nextpg ()   ;Subr next page
(if (/= pageno pages) (setq pageno (1+ pageno)) (setq pageno 0)))

;Subr to get directory and present default. Create **DIRS in your menu to pick
;your block directories. Set up your own primary default. Empty string default
; "" not recommended because findfile will search all Set acad= directories
(defun getdir (tmpdir / tmp)                      
   (if (= tmpdir nil) (setq tmpdir "c:\\acad\\")) ;primary default
   (menucmd "s=blank") (menucmd "s=dirs")         ;**DIRS  [Label]C:/Label
   (if (and (setq tmp (getstring (strcat "\nPATH< " tmpdir " >: ")))
   (/= tmp ""))   (setq tmpdir (pslash tmp)))
(menucmd "s=blank") tmpdir)

;Subr to get directory string length - Get length of file name, start at end
(defun dirlen (fl / slash inc)           ; and count backwards to last slash.
   (setq inc (strlen fl) slash "\\")       ; (dirlen "1234\678") returns 6
   (while (and (/= inc 0) (/= slash (substr fl inc 1)))(setq inc (1- inc)))
(setq inc (1+ inc)) inc)                ;(substr "1234\678" 6) returns "678"
;end vsld1.lsp
