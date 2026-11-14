

;                         VSLD -
;                A DRAWING DATABASE MANAGER
;                    by TERRY PRIEST

;   A DRAWING DATABASE MANAGER SHOULD CONSIST OF A VIEWING FUNCTION AND
;AN INSERTION FUNCTION.  ADDITIONALLY WE SHOULD BE ABLE TO COPY, MOVE,
;RENAME AND DELETE OUR DRAWINGS EVEN WHILE WE ARE LOOKING AT THEM.
                         
;    VSLD (STANDS FOR VIEW SLIDE) IS A LISP PROGRAM THAT LISTS THE
;DRAWINGS IN ANY DIRECTORY.  PICKING FROM THE LIST PRODUCES A QUICK
;DISPLAY OF THAT DRAWING (AS A SLIDE).  YOU CAN THEN INSERT THE DRAWING
;OR VIEW ANOTHER.
                         
;   SLIDES ARE THE PERFECT VEHICLE FOR VISUALLY REFERENCING A DRAWING
;BEFORE INSERTING IT.  VSLD.LSP MAKES A DYNAMIC LIST OF EITHER SLIDES
;OR DRAWINGS AND DISPLAYS THAT LIST ON THE SIDE MENU SCREEN.  FOR MAXIMUM
;UTILITY, IT IS ASSUMED (BUT NOT NECESSARY) THAT A DRAWING AND A REPRESENTATIVE
;SLIDE OF THE SAME NAME BOTH EXIST IN THE DIRECTORY.  THE FILE EXTENSION
;(.DWG OR .SLD) IS STRIPPED OFF SO THE LIST CAN BE USED TO VIEW THE SLIDE,
;INSERT THE DRAWING, OR USE THE DOS FUNCTIONS ON BOTH THE DRAWING AND SLIDE.
   
;   TO VIEW A FILE, PICK THE NAME.  ANY NUMBER OF SLIDES CAN BE VIEWED AND
;THE LIST PAGED WITH "NEXT" AND "PREVIOUS" OPTIONS.  THE "INSERT" OPTION
;WILL INSERT THE CURRENTLY DISPLAYED FILE (OR LAST PICKED IF NO SLIDE EXISTS).
;THE DOS FUNCTIONS, "COPY", "MOVE", "RENAME", AND "DELETE", WILL OPERATE
;ON THE CURRENTLY DISPLAYED FILE AS A DRAWING/SLIDE PAIR.  COPY, MOVE, AND
;RENAME CHECK FOR DUPLICATE NAMES.  DELETE REQUIRES A DOUBLE CLICK ON THE
;SAME SPACE TO DELETE THE CURRENT FILE PAIR.  "EXIT" LEAVES THE PROGRAM AND
;REDRAWS THE GRAPHICS SCREEN AND SIDE MENU.
                      
;   THE PROGRAM WORKS WITH ANY DRIVE OR DIRECTORY, WITH BACKSLASHES OR
;FORESLASHES.  VSLD FIRST ASKS FOR A PATH.  IT CAN BE TYPED IN,
;BUT VSLD CALLS AN AUTOCAD MENU SCREEN OF DIRECTORIES.  PSLASH.LSP
;(FROM "INSIDE AUTOLISP") CHANGES THE ACAD MENU FORESLASH TO A
;DOS BACKSLASH.  THIS DIRECTORY INDEX IN OUR MENU LOGS ON VSLD AND ALSO
;PROVIDES A TARGET DIRECTORY FOR COPYING (AS WELL AS BEING USEFUL TO
;OTHER PROGRAMS PUTTING BLOCKS AND SLIDES INTO THE DIRECTORIES).
;WE PICK THE DIRECTORY, THEN THE FILE.  THE USER CAN LOOK THROUGH A LARGE
;NUMBER OF DRAWINGS, CATEGORIZED BY DIRECTORY NAME, AND FIND THE
;DRAWINGS HE WANTS TO USE.
                      
;   A LISP PROGRAM CALLED BLK.LSP BY MIKE PILLERS WAS THE FIRST PROGRAM
;I WAS AWARE OF THAT USED GRTEXT AND GRREAD TO CREATE A SMART MENU
;ON THE SIDE SCREEN.  ANY LIST OF NAMES - LIKE BLOCKS, FILES, VIEWS, ETC. -
;CAN BE WRITTEN TO THE SCREEN WITH GRTEXT.  GRREAD ALLOWS US TO PICK
;OUT ANY NAME FROM THE LIST FOR PROCESSING.  PUT INSIDE A WHILE LOOP,
;WE CAN HAVE A TWO-PART MENU - THE LIST OF FILES AND THE OPTIONS LIST.
;THE LAST PICKED FILE, THE CURRENTLY DISPLAYED, BECOMES THE VARIABLE
;NAME USED BY THE OPTION PICK.

;   WE CAN EASILY HAVE A DIRECTORY OF DRAWINGS THAT EACH HAVE A SLIDE
;OF THE SAME NAME. SLIDES CAN BE MADE FROM DRAWINGS ONE AT A TIME WITH
;THE AUTOCAD MSLIDE COMMAND, OR A SCRIPT FILE CAN GENERATE SLIDES OF MANY
;DRAWINGS AT ONE TIME.  "INSIDE AUTOLISP" CONTAINS A LISTING FOR A FILE
;NAMED SLIDE.LSP THAT WILL CREATE A SCRIPT FILE THAT MAKES SLIDES FROM A
;WHOLE DIRECTORY OF DRAWINGS.  THE SCRIPT FILE IS CALLED CSLIDE.SCR AND
;IS PLACED IN THE SAME DIRECTORY WITH THE DRAWINGS. AUTOCAD IS STARTED
;WITH THE SCRIPT FILE AND THE SLIDES ARE MADE WITHOUT OPERATOR INPUT.

;   TO FURTHER AUTOMATE THE PROCESS, A MENU MACRO IS MADE TO MAKE AND PLACE
;THE FILES CSLIDE.SCR IN EVERY DIRECTORY WHERE SLIDES ARE TO BE MADE.
;AUTOCAD IS THEN STARTED WITH A BATCH FILE THAT CALLS EACH ONE OF THESE
;SCRIPT FILES IN TURN.  IN THAT WAY ALL THE DRAWINGS ON A HARD DISK OR
;SELECTED NETWORK DIRECTORIES CAN HAVE SLIDES MADE. WE CAN FINE TUNE
;SLIDE.LSP JUST TO MAKE SLIDES OF THE DRAWINGS CURRENTLY WITHOUT SLIDES.
     
;   "INSIDE AUTOLISP"  HAS A FUNCTION INDISPENSABLE TO VSLD CALLED GETFIL.LSP
;(MY (GETFLS PATH FSPEC)) TO MAKE A LIST OF THE FILES ON THE DISK/DIR
;YOU SPECIFY.  AUTOCAD SHOULD HIRE THESE GUYS (J. SMITH & R. GESNER) TO
;REWRITE THEIR ACAD.MNU.  IF THERE IS A BETTER AUTOCAD BOOK THAN
;"CUSTOMIZING AUTOCAD" AND "INSIDE AUTOLISP", I HAVE NOT SEEN IT.
                         
;   THE DIRECTORY FILE LIST CAN BE CHANGED TO "DRAWINGS", "SLIDES",
;"DWGwoSLD" (DRAWINGS WITHOUT A SLIDE), AND "SLDwoDWG" (SLIDES WITHOUT
;DRAWINGS). THE LAST LOGGED DIRECTORY REMAINS THE DEFAULT.

;   INSERT WILL EXIT THE PROGRAM IN DEFAULT MODE "EXIT ON".  THIS WORKS
;WITH ATTRIBUTES. "EXIT OFF" MODE WILL PAUSE FOUR TIMES FOR THE INSERT
;AND THEN CONTINUE THE MENU LOOP.  THE "INFO ON/OFF" TOGGLE WILL TURN ON
;A DIR LIST OF JUST THE FILE AS IT IS PICKED.

;   LOGGING THE DIRECTORY FOR THE FILE LIST WITH THE SHELL COMMAND PRODUCES
;AN UP-TO-DATE LIST BUT IS SOMEWHAT SLOW.  THE SAME LIST CAN BE WRITTEN TO
;A FILE AND READ IN AS A LOG-ON LIST ("MAKELIST").  IF IT BECOMES OUTDATED,
;IT CAN BE DELETED ("DEL LIST").

;   PAGE LENGTH CAN BE CHANGED WITH A DIFFERENT VALUE ASSIGNED TO THE SCRLEN
;VARIABLE SO ALL DISPLAYS CAN USE ALL THEIR SIDE MENU LINES.
                           
;   VSLD.LSP CAN BE USED TO DISPLAY BLOCKS, FINISHED DRAWINGS, MANUAL PAGES,
;STANDARDS BOOK PAGES, HELP, SCHEDULES, CHARTS OR GRAPHS.  NO ONE HAS TO
;MAINTAIN AN UP-TO-DATE LIST OF BLOCKS OR TYPE IN A SINGLE BLOCK NAME.
;ON THE NETWORK, EVERYBODY CAN USE THE SAME DIRECTORY INDEX, TO USE
;THE SAME DRAWINGS, WHICH BECOME STANDARD BLOCKS.
                         
; VSLD takes a path and puts a list of the slides (or dwgs) in that directory
; on the side menu screen. The user should implement a menu screen of
; directories called **DIRS and a fully blank screen called **BLANK
; in his Autocad menu.  Picking from the list produces a display of the
; slide.  Look at as many slides as you like, then pick your option.
; "PREVIOUS"  - Displays the previous page of files
; "NEXT"  - Displays the next page of files
; "EXIT" - Exits the program
;
; "DRAWINGS" or " SLIDES" or "DWGwoSLD" or "SLDwoDWG" - Brings up a file
;             filter sub-menu to change the file list. wo = without            
; "INSERT" - Inserts the currently displayed file
;
; "COPY" - Copies the currently displayed (or picked) file (both .DWG & .SLD)
; "MOVE" - Moves the currently displayed file (both .DWG & .SLD)
; "RENAME" - Renames the currently displayed file (both .DWG & .SLD)
; "DELETE" - Deletes the currently displayed file (both .DWG & .SLD)
;
; "Info Off" or "Info On" - Displays (when on) a dir list of the displayed
;    file.  This works well with dual screen and not at all with single screen.
; "Exit On" or "Exit Off" - Insert and Exit or Insert and Stay
; "MAKELIST" - Make dir list file on hard drive to speed up log-on
; "DEL LIST" - Delete hard drive dir list file

; Copyright as an unpublished work 9-19-90 by Terry Priest
; New material and changes Copyright 11-13-90, 12-3-90
; Any problems or comments contact me at Geo. Koch Sons
; Custom Machinery Fabricator  812-426-9881
; or at home  11428 DENZER ROAD   EVANSVILLE, IND.  47712


(defun *ERROR* (ERR)
(princ"\n")
(princ ERR)
(grtext)
(princ)
)


;Subr to remove word from list. Used to update file list after deleting
;file without relogging hard disk
(DEFUN REMOVE (WORD LST / TMP TMPLST)
(SETQ  TMPLST NIL  TMP (LIST (CAR LST)))
(WHILE WORD
  (IF (NOT(EQUAL TMP (LIST WORD)))
         (SETQ TMPLST (APPEND TMPLST TMP)           ;then
               LST (CDR LST)
               TMP (LIST (CAR LST)))
         (SETQ LST (CDR LST) TMPLST (APPEND TMPLST LST) WORD NIL)) ;else
)TMPLST
)


;Subr to remove sublist from list. Used to obtain a list of drawings that
;do not have slides, and vice versa
(DEFUN EXTRACT (SUBLST LST  / TMPLST WORD)
(SETQ TMPLST NIL)
(WHILE LST
  (SETQ WORD (CAR LST)
        LST (CDR LST))
  (IF (NOT (MEMBER WORD SUBLST))
      (SETQ TMPLST (CONS WORD TMPLST))
 ))
(IF (NOT TMPLST)
      (PROMPT (STRCAT "\n ALL NAMES HAVE A MATCH \n")))
TMPLST
)

                     
;Slash operator subroutine - changes menu foreslash to dos backslash
;pslash is from "Inside Autolisp" Smith & Gesner
;These guys know their business
(defun pslash (path / inc slash wpath char)
 (setq inc 1  wpath ""  slash "\\")
 (while (/= "" (setq char (substr path inc 1)))
            (setq wpath
               (strcat wpath (if (member char '("\\" "/")) slash char))
               inc (1+ inc))
 )
 (if (and (/= wpath "") (/= (substr wpath (strlen wpath) 1) slash))
         (setq wpath (strcat wpath slash))
 )
 wpath
) 


;getFLS is a condensed version of GETFIL "Inside Autolisp"
;I have added code to look for a list file on the hard drive
;and use that if it exists, which is much quicker
;Autodesk wish list item: new lisp command (getfls path pattern)
(defun getFLS (path FSPEC / FL FLS fil bootlist)
(IF (= FSPEC "*.DWG")
    (SETQ BOOTLIST "DWG.TXT")
    (SETQ BOOTLIST "SLD.TXT"))
(IF  (SETQ FIL (OPEN (STRCAT PATH BOOTLIST) "r")) ;look for bootlist
   (PROGN                                         ;if found  
        (SETQ FL (LIST(READ-LINE FIL))
              FLS NIL)
        (WHILE (AND FL (/= "" FL))
               (SETQ FLS (APPEND FLS FL))
                (IF (SETQ FL (READ-LINE FIL))
                    (SETQ FL (LIST FL)))
         )
       (CLOSE FIL)
   )
   (PROGN                           ;else if bootlist is not found
 (setq fspec (strcat path fspec))
 (close (open "DIR.$" "w"))
 (COMMAND "SH" (strcat "for %f in (" fspec ") do echo %f >> DIR.$" )) 
 (setq fil (open "DIR.$" "r"))
 (if (setq fspec (read-line fil))
        (progn
           (setq FLS nil)
           (prompt "\nMaking file listing.")
           (while (and fspec (/= "" fspec))
              (prompt ".")
              (setq FLS 
                    (append FLS (LIST (SUBSTR FSPEC 1 (- (STRLEN FSPEC)5)))))
              (setq fspec (read-line fil))
           )
           (close fil)
         (command "FILES" "3" "DIR.$" "" "")
        )
        (prompt (strcat "\nNo " " files " " found\n"))
 )
)) ;progn if
 FLS    ;returns
)


;Number of Screen Pages subroutine
(DEFUN PAG#S (FLS SCRLEN / pages)
  (SETQ PAGES (/ (LENGTH FLS) SCRLEN))
  (IF (AND (= 0 (REM  (LENGTH FLS) SCRLEN)) (>= PAGES 1) )
	    (SETQ PAGES (1- PAGES)) 
  )PAGES    ;returns
)


;Displays Files to Screen subroutine
(DEFUN DSPFLS (FLS SCRLEN PATHLN PAGENO / I)
  (SETQ I 0)
  (REPEAT SCRLEN
    (IF (> (LENGTH FLS) (+ I (* PAGENO SCRLEN)))	
           (GRTEXT I (SUBSTR (NTH (+ I (* PAGENO SCRLEN)) FLS) PATHLN))
    )
   (SETQ I (1+ I))
  )
)


;Fllst uses getFLS to make the list file and put it on disk for faster
;access by VSLD.  
(DEFUN FLLST (PATH FSPEC / BOOTLIST FIL FLS )
(DLST PATH FSPEC)            ;delete the old list or getFLS will use it again
(IF (= FSPEC "*.DWG" )
    (SETQ BOOTLIST "DWG.TXT")
    (SETQ BOOTLIST "SLD.TXT"))
(SETQ FLS (GETFLS PATH FSPEC))
(IF FLS (PROGN
(SETQ FIL (OPEN (STRCAT PATH BOOTLIST) "w"))
(WHILE FLS
(WRITE-LINE (CAR FLS) FIL)
(SETQ FLS (CDR FLS))
)
(CLOSE FIL)))
)


;Dlst deletes the list file
(DEFUN DLST (PATH FSPEC)
(IF (= FSPEC "*.DWG")
(COMMAND "SH" (STRCAT "DEL " PATH "DWG.TXT"))
(COMMAND "SH" (STRCAT "DEL " PATH "SLD.TXT"))))


;Subr to get a path and present default. Path is a global variable
(DEFUN getPATH ( / temp)
  (if (= path nil) (setq path ""))
  (if (AND (setq temp (getstring (strcat "\nPATH< " path " >: ")))
           (/= TEMP ""))
           (setq path (pslash temp)))
)


;Subr to make file list FLS according to the setting of global variable FILTER.
(DEFUN fltFLS (FILTER / FSPEC FLS DWGFLS SLDFLS)
(COND
  ((= FILTER NIL)  ;default logs drawings
   (SETQ FSPEC "*.DWG"
         FLS (GETFLS PATH FSPEC)))

  ((= FILTER 1)    ;logs slides
   (SETQ FSPEC "*.SLD"
         FLS (GETFLS PATH FSPEC)))

  ((= FILTER 2)    ;drawings without slides
   (SETQ FLS NIL
         DWGFLS (GETFLS PATH "*.DWG")
         SLDFLS (GETFLS PATH "*.SLD")
         FLS (EXTRACT SLDFLS DWGFLS)))

  ((= FILTER 3)     ;slides without drawings
   (SETQ FLS NIL
         DWGFLS (GETFLS PATH "*.DWG")
         SLDFLS (GETFLS PATH "*.SLD")
         FLS (EXTRACT DWGFLS SLDFLS)))
 
  (T (PROMPT "INVALID "))
)
FLS
)


;The Main Program
(DEFUN C:VSLD (/ FL FLS PATHLN PAGENO READNO scrlen
                  PAGES NEWNAM pathln)
(GETPATH)
(MENUCMD "S=BLANK")                        ;**Blank is a blank menu page
(SETQ FLS (FLTFLS FILTER)
      PATHLN (1+ (strlen path))    ;pathln used to display files without path
      PAGENO 0  READNO nil  scrlen 21 ;reset screen length here for different
      pages (pag#s FLS scrlen))       ;displays. this is for 40 line display.
 (WHILE   (/= READNO (+ scrlen 3))     ;The menu loop
             (GRTEXT)
             (GRTEXT (+ scrlen 1) "PREVIOUS")
             (GRTEXT (+ scrlen 2) "NEXT")
             (GRTEXT (+ scrlen 3) "EXIT")
             (GRTEXT (+ scrlen 5)          ;reads filter variable and displays
                     (COND                 ;current file list mode
                       ((= FILTER NIL) "DRAWINGS")
                       ((= FILTER 1) " SLIDES")
                       ((= FILTER 2) "DWGwoSLD")
                       ((= FILTER 3) "SLDwoDWG")))
             (GRTEXT (+ scrlen 7) "INSERT")
	     (GRTEXT (+ scrlen 8) "COPY")            ;dos functions
             (GRTEXT (+ scrlen 9) "MOVE")
             (GRTEXT (+ scrlen 10) "RENAME")
             (GRTEXT (+ scrlen 11) "DELETE")
             (GRTEXT (+ scrlen 13) (if (not flagi) "Info Off" "Info On"))
             (GRTEXT (+ scrlen 14) (if (not flage) "Exit On" "Exit Off"))
             (GRTEXT (+ scrlen 15) "MAKELIST")         ;make list file
             (grtext (+ scrlen 16) "DEL LIST")         ;delete list file
             (PROMPT "SELECT BLOCK/DWG TO VIEW SLIDE\n")
             (DSPFLS FLS scrlen pathln pageno) 
             (grtext -1 (strcat path " " (if FL (substr FL pathln) "")))
             (grtext -2 (strcat "Page No. " (itoa pageno)))
	     (SETQ READNO (NTH 1 (GRREAD)))

         (COND 
              ( (AND (>= READNO 0) (< READNO scrlen)(NOT (LISTP READNO)))
                       (IF (SETQ FL (NTH (+ READNO (* PAGENO scrlen)) FLS))
                            (progn
                        (if flagi  (COMMAND "SH" (strcat "DIR "  FL ".*")))                        
                             (IF (FINDFILE (STRCAT FL ".SLD"))
                                     (Command "VSLIDE" FL)
                                     (progn (grclear)
                           (prompt (strcat "\nNo Slide found for " FL "\n"))))
                            ) ;progn
                          ))

    		((= READNO (+ scrlen 1))                      ;"Previous" page
    		   (IF (/= PAGENO 0) (SETQ PAGENO (1- PAGENO))
    				     (SETQ PAGENO PAGES)))

    		((= READNO (+ scrlen 2))                        ;"Next" page
		   (IF (/= PAGENO PAGES) (SETQ PAGENO (1+ PAGENO))
		                         (SETQ PAGENO 0)))

                ((= READNO (+ scrlen 5))(SETQ READNO NIL)  ;File filter pick
          (WHILE (NOT (AND (>= READNO (+ scrlen 4)) (<= READNO (+ scrlen 7))))
                           (GRTEXT)
                           (GRTEXT (+ scrlen 1) " PICK")
                           (GRTEXT (+ scrlen 2) "FILTER")
                           (GRTEXT (+ scrlen 4) "DRAWINGS")
                           (GRTEXT (+ scrlen 5) " SLIDES")
                           (GRTEXT (+ scrlen 6) "DWGwoSLD")
                           (GRTEXT (+ scrlen 7) "SLDwoDWG")
                           (SETQ READNO (NTH 1 (GRREAD)))
         
                                   (COND
                                     ((= READNO (+ scrlen 4))   ;"Drawings"
                                      (SETQ FILTER NIL))
                                      ((= READNO (+ scrlen 5))  ;"Slides"
                                      (SETQ FILTER 1))
                                     ((= READNO (+ scrlen 6))  ;"DWGwoSLD"
                                      (SETQ FILTER 2))
                                      ((= READNO (+ scrlen 7))  ;"SLDwoDWG"
                                      (SETQ FILTER 3))) ;cond
                                  (SETQ FLS (FLTFLS FILTER)
                                        PAGENO 0  
                                        pages (pag#s FLS scrlen))
                              )) ;while and readno

        ((= READNO (+ scrlen 7))             ;"Insert" option
         (IF (= FL NIL) (SETQ FL ""))
         (IF (FINDFILE (STRCAT FL ".DWG"))
              (PROGN
                (if (not flage)      ;if flage is nil then we Insert and Exit
                     (progn                       ;nil flage is default
                       (COMMAND "INSERT" FL )
                       (setq readno (+ scrlen 3)))           ;exit
                     (progn  (redraw) ;else if flage is T we Insert and Stay
                       (COMMAND "INSERT" FL pause pause pause pause))))
              (PROMPT (STRCAT "\nNO DRAWING FOUND FOR " FL "\n"))) ;if not find
        )


;an important part of the program is the menu of directories here (below)
;called "S=DIRS". Make an index of your directories on one or several pages.
;A new page call will not trigger GETSTRING until a selection is made.
;Write the directory with the standard acad menu foreslash (this is what
;pslash is for!).
;Use the same directory menu index to start VSLD
;The dos functions will use the file displayed in the upper left of the screen
                ((= READNO (+ scrlen 8))           ;"Copy" option
                          (IF FL (PROGN 
                            (prompt (strcat "\nCopy " FL)) 
                            (MENUCMD "S=DIRS")
                            (SETQ DIR (PSLASH (GETSTRING "\nPICK SUB-DIR\n")))
                            (IF (AND
                     (NOT (FINDFILE (STRCAT DIR (SUBSTR FL PATHLN) ".DWG")))
                     (NOT (FINDFILE (STRCAT DIR (SUBSTR FL PATHLN) ".SLD"))))
                                (PROGN
                                 (SETQ DIR (SUBSTR DIR 1 (1- (STRLEN DIR))))
                                 (COMMAND "SH" (STRCAT "COPY " FL  ".* " DIR))
                                ) ;progn
                                (PROMPT "\nNOT ALLOWED - DUPLICATE  NAME\n"))
                            (MENUCMD "S=BLANK")  )))

                ((= READNO (+ scrlen 9))           ;"Move" option
                         (IF FL (PROGN  
                           (prompt (strcat "\nMove " FL)) 
                           (MENUCMD "S=DIRS")
                           (SETQ DIR (PSLASH (GETSTRING "\nPICK SUB-DIR\n")))
                           (IF (AND
                     (NOT (FINDFILE (STRCAT DIR (SUBSTR FL PATHLN) ".DWG")))
                     (NOT (FINDFILE (STRCAT DIR (SUBSTR FL PATHLN) ".SLD"))))
                               (PROGN
                                (SETQ DIR (SUBSTR DIR 1 (1- (STRLEN DIR))))
                                (COMMAND "SH" (STRCAT "COPY " FL  ".* " DIR))
                                (COMMAND "SH" (STRCAT "DEL " FL  ".* " ))
                                (SETQ FLS (REMOVE FL FLS)
                                      FL NIL  PAGES (PAG#S FLS scrlen))
                               ) ;progn
                               (PROMPT "\nNOT ALLOWED - DUPLICATE  NAME\n"))
                           (MENUCMD "S=BLANK")  )))

                ((= READNO (+ scrlen 10))           ;"Rename" option
                     (IF FL (PROGN
                       (prompt (strcat "\nRename " FL)) 
                       (setq newnam (strcase (GETSTRING "\nNew file name\n")))
                           (IF (NOT (MEMBER (STRCAT PATH NEWNAM) FLS))
                               (PROGN 
                                (COMMAND "SH" 
                                (STRCAT "RENAME " FL ".dwg "  newnam ".dwg"))
                                (COMMAND "SH" 
                                (STRCAT "RENAME " FL ".sld "  newnam ".sld"))
                                (SETQ FLS (SUBST (STRCAT PATH NEWNAM) FL FLS)
                                      FL (STRCAT PATH NEWNAM))
                               ) ;progn
                               (PROMPT "\nNOT ALLOWED - DUPLICATE NAME\n"))
                             )))

                ((= READNO (+ scrlen 11))           ;"Delete" option
                 (prompt "\nDouble Click on Delete to DELETE Displayed file\n")
                 (prompt (strcat "OK to DELETE "  FL " .DWG & .SLD\n"))
                       (SETQ READNO (NTH 1 (GRREAD)))
                       (IF (and FL (= readno (+ scrlen 11)))
                           (PROGN 
                             (COMMAND "SH" (STRCAT "DEL " FL  ".* " ))
                             (grclear)
                             (SETQ FLS (REMOVE FL FLS)
                                   FL NIL  PAGES (PAG#S FLS scrlen))
                           ) ;progn
                           (prompt "\nCANCELLED\n" ))
                         )

                          
                ((= READNO (+ scrlen 13))         ;"Info Off" "Info On" toggle
                   (if (not flagi)(setq flagi T)(setq flagi nil)))

                ((= READNO (+ scrlen 14))         ;"Exit On" "Exit Off" toggle
                   (if (not flage)(setq flage T)(setq flage nil)))

                 ((= READNO (+ scrlen 15))     ;Makes a list file on the dir
                    (cond
                        ((= filter nil) 
                          (Fllst PATH "*.DWG")
                          (SETQ FLS (FLTFLS FILTER)
                                pageno 0 
                                pages (pag#s FLS scrlen)))
                        ((= filter 1) 
                          (Fllst PATH "*.SLD")
                          (SETQ FLS (FLTFLS FILTER)
                                pageno 0
                                pages (pag#s FLS scrlen)))
                        ( T 
                  (prompt "\nNOT ALLOWED - LOG EITHER DRAWINGS OR SLIDES\n"))
                    ))

                  ((= READNO (+ scrlen 16))  ;Deletes the list file
                     (cond
                        ((= filter nil)
                          (Dlst PATH "*.DWG")
                          (SETQ FLS (FLTFLS FILTER)
                                pageno 0
                                pages (pag#s FLS scrlen)))
                        ((= filter 1)
                          (Dlst PATH "*.SLD")
                          (SETQ FLS (FLTFLS FILTER)
                                pageno 0
                                pages (pag#s FLS scrlen)))
                        ( T
                  (prompt "\nNOT ALLOWED - LOG EITHER DWGS OR SLDS\n"))
                    ))

          ) ;Cond
  ) ;While
(GRTEXT) (MENUCMD "S=DIRS" )
(REDRAW) 
)
