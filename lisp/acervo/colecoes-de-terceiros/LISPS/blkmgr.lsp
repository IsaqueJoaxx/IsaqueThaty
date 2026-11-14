
; BLKMGR.LSP Copyright as an unpublished work 4-1-91 by Terry Priest
; Any problems or comments contact me at:
; GEO. KOCH SONS Custom Machinery Fabricator 812-426-9881
; or at home -  11428 Denzer Road   Evansville, Ind.  47712

;  We need a function that lists the blocks in the drawing on the side screen.
;(We don't have lisp commands for dialogue boxes or pulldowns, besides they
;would get in the way.)  Picking from the list we can Insert, Wblock (with or
;without making a slide), or Rename.  We can also try to identify a block
;by making it flash, and display the number of inserts in the drawing.

;  This is useful but not spectacular.  Two things which make this routine
;more powerful are 1) We can use LISP's versatile list processing tools to
;pare down the list to what we want ("New List") and 2) We can Wblock the
;entire list at one time ("EnMasse").

;  The biggest question I had to answer in this routine was - What happens
;when we pick the block name? On the main screen, picking the block name
;simply loads the name into a variable (and also makes a count of the
;inserts).  That gives us the flexibility to then pick the option -
;Insert, Wblock or Rename. There are other ways to write the program
;so that picking the block name makes something happen.  "BLKCYCLE" -
;a sub-screen - inserts the block and optionally erases the last block
;inserted.

;  For the block defined in the drawing there is no such thing as a preview,
;the way the Insert command can use Vslide as a preview to the block.dwg
;on disk.  The last menu option "BLKCYC" is my attempt at a block preview
;cycle. One way to view the block is to insert it. To see it we might have
;to Zoom.  Once we have a good view of it, we might want to leave it,
;get rid of it, Wblock, Mslide, or Rename, or pick another block name
;to insert.  That is the essence of the function - a block insertion,
;zoom capability, Wblock option with or without Mslide, Rename option,
;or pick another block with or without Erase Last being generated.

;  The user should implement a completely blank menu screen "**BLANK"
;and a menu screen of personal (or network) directories "**DIRS" into
;his Autocad menu.  The variable scrlen should be set to work with your
;display.

;  VSLD.lsp was written first. In general, VSLD gets blocks out of the
;directories and into the drawing, while BLKMGR gets them into the
;directories.
 
;***************************************************************************
;The technique of loading subprograms from separate files with a menu
;pick and then setq'ing them to nil after they are exited is necessary
;on the 286 version of Autocad.  On the 386, all should be loaded at once
;and none set to nil.

;****************************************************************************
; BLKMGR side screen:
; BLKMGR displays the blocks contained in a drawing on the side screen.
; The number of block names displayed depends on the scrlen variable setting
; 
; "PREVIOUS" will display the previous page of blocks
; "NEXT" will display the next page of blocks
; "EXIT" is the only way (except ^C) to exit the program

; "BLK NAME" will display the currently picked block name
; "# NSRTED" will display the number of inserts of the block name above.
             ;if picked and non-zero all inserts will flash

; "WBLOCK" will Wblock and display your list of directories to pick from
; " Msld On" " MsldOff" a toggle which will optionally make a slide
                       ;immediately after the Wblock command
; " EnMasse" will Wblock all the blocks in the current list
; "INSERT" will Insert the current block name
; " Exit On" " ExitOff" a toggle which will optionally exit after
                       ;a single insert (which makes it attribute compatible)
; "RENAME" will Rename the current block name
; "NEW LIST" brings up a subprogram which has several ways to pare the block
            ;list. See below.
; "Save Off"  "Save On" a toggle which will optionally save the current block
                       ;list. If off, all the blocks will be read into the
                       ;list when BLKMGR initiates. 
; "BLK CYCL" brings up a subprogram "Block Insert Cycle". See below

;*****************************************************************************
; NEW LIST side screen:
; The number of block names displayed depends on the scrlen variable setting
; 
; "PREVIOUS" will display the previous page of blocks
; "NEXT" will display the next page of blocks
; "EXIT" is the only way (except ^C) to exit the program

; "*" will reset the list to all block names in the drawing
; "WILDCARD" will accept wildcards to filter the current (!)(i.e. displayed)
            ;block list
; "PICK OUT" will let you pick block names out of the list
; "PICK IN" will let you start a new list by picking the names in it
; "NSRTDNLY" - Inserted Only -
; "NON-NSRT" - Non-Inserted Blocks only -
; "NAMELIST" create a name for the current list and save it to that name.
             ;read a list of list names, put them on the screen and use any
             ;of them to replace the block list

;*****************************************************************************
; BLK CYCL side screen:
; The number of block names displayed depends on the scrlen variable setting
; 
; "PREVIOUS" will display the previous page of blocks
; "NEXT" will display the next page of blocks
; "EXIT" is the only way (except ^C) to exit the program
;
; "ER L ON" "ER L OFF" a toggle which will optionally Erase Last the last
                       ;Block Inserted as you pick a new Block name. It starts
                       ;with the second Block inserted. You cannot
                       ;accidentally erase the last entity created before
                       ;starting BLKMGR. Whether you erase the last Block
                       ;inserted before exiting BLKCYCL depends on this toggle

; "BLK NAME" will display the currently picked block name
; "# NSRTED" will display the number of inserts of the block name above
             ;if picked and non-zero all inserts will flash

; "WBLOCK" will Wblock and display your list of directories to pick from
; " Msld On" " MsldOff" a toggle which will optionally make a slide
                       ;immediately after the Wblock command
; "RENAME" will Rename the current block name
; "zoom min" -zoom minimum- change the number 41625 to work with your display
; "zoom w" - zoom window -
; "zoom p" - zoom previous -
; "zoom max"  I left this Rel 10 compatible (zoom V Rel 11 is better)
; "zoom All"  
; " YES" do you want a Regen? Only works with the zooms (not with Wblock etc.)
; " NO"  same as above

;*****************************************************************************

(defun *ERROR* (ERR)
(princ"\n")
(princ ERR)
(grtext)
(princ)
)


;Slash operator subroutine - changes menu foreslash to dos backslash
;pslash is from "Inside Autolisp" Smith & Gesner
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


;Subr to remove word from list.
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

;Subr to get path and present default.  Path is a global variable
(DEFUN getPATH ( / temp)
  (if (= path nil) (setq path ""))
  (if (AND (setq temp (getstring (strcat "\nPATH< " path " >: ")))
           (/= TEMP ""))
           (setq path (pslash temp)))
)


;Number of Screen Pages subroutine
(DEFUN PAG#S (FLS SCRLEN / PAGES)
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
 ))

;Subr to get list of all blocks (except hatch and dims) in drawing
(defun blklst ( / tmp )
  (SETQ BLKS (CDR (ASSOC 2 (TBLNEXT "BLOCK" T))))
  (SETQ BLKS (LIST (CDR (ASSOC 2 (TBLNEXT "BLOCK"))) BLKS))
  (WHILE (SETQ TMP (TBLNEXT "BLOCK")) 
         (if
            (not (equal (substr  (CDR (ASSOC 2 TMP)) 1 1) "*"))
             (SETQ BLKS (CONS (CDR (ASSOC 2 TMP)) BLKS))
 ) ) )

;Subr to truncate block names to 8 letters
(defun trunc8 (blk / tmp i)
(setq i 0 tmp "")
(repeat 8
(setq i (1+ i))
(setq tmp (strcat tmp (substr blk i 1)))
)
tmp
)

;Subr to obtain # of inserts of a block
(defun nsrt# (blk )
  (if (setq blkSET (ssget "x" (list (cons 2 blk))))
                               (setq blk# (sslength blkSET))
                               (setq blk# 0)
                         )
)

;Subr previous page
(defun prevpg ()
  (IF (/= PAGENO 0) (SETQ PAGENO (1- PAGENO))
		     (SETQ PAGENO PAGES)))
;Subr next page
(defun nextpg ()
   (IF (/= PAGENO PAGES) (SETQ PAGENO (1+ PAGENO))
		         (SETQ PAGENO 0)))


;Subr flash block
(defun flashb ()
      (if (and blk# (/= blk# 0))
               (progn
                  (SETQ CNT 0 SETLIST NIL)
                  (REPEAT blk#
                     (SETQ SETLIST (APPEND SETLIST (LIST(SSNAME BLKSET CNT)))
                           CNT (1+ CNT))
                  )

                    (repeat 3
                      (foreach name setlist
                         (repeat 2
                           (entdel name)(setq cnt 0)
                              (repeat 150 (setq cnt (+ 1 cnt)))
                         )
                      )
                    )

               )
           ))


;Subr Wblock
(defun wblk ()
        (IF BLK (PROGN
                (if (> (strlen blk) 8)
                         (progn
    (prompt  (strcat "\nSorry " BLK  " too long. Rename or <T>runcate? "))
                           (initget "R T")
                           (setq flagw (getkword))
                           (if (= flagw "R")
                                (setq newblk (getstring "New name "))
                                (setq newblk (trunc8 blk))))
                          (setq newblk blk))
                   (IF (not (FINDFILE (STRCAT PATH newBLK ".DWG")))
                     (PROGN
                         (COMMAND "WBLOCK" (STRCAT path newBLK) BLK )
                         (IF (NOT FLAGm)
                            (COMMAND "MSLIDE" (STRCAT path newBLK)))
                    ) ;progn
                     (progn
                       (initget "Yes No")
                       (if (= "Yes"
                            (getkword
    (strcat " File " PATH newBLK ".DWG " "exists - Overwrite? Y or <N>")))
                            (progn
                          (COMMAND "WBLOCK" (STRCAT path newBLK) "Y" BLK )
                              (IF (NOT FLAGm)
                                  (COMMAND "MSLIDE" (STRCAT path newBLK)))
                           ) ;progn
                            (prompt "Cancelled ")
                       ) ;if
                    ) ;progn 
                  ) ;if
                ) ;progn
         ) ;if
     ) ;end func


; Subr Rename Block 
; If we were always working with a complete block list
; we could easily check for a duplicate name. As it is the function will
; crash if you give an invalid or duplicate name.
(defun renblk ()
           (if blk (progn
              (prompt (strcat "\nOld Name " blk))
              (prompt "\nNew Name ")
              (setq oldblk blk blk (getstring))
              (COMMAND "RENAME" "BLOCK" OLDBLK BLK)
              (SETQ BLKS (SUBST BLK OLDBLK BLKS))
            )))




;The Main Function
(DEFUN C:BLKMGR ( /  blk blk# tmp pageno readno scrlen
                     pages cnt setlist blkset)
  (if (not blks) (blklst))
  (SETQ  PAGENO 0  READNO NIL SCRLEN 20)
  (SETQ PAGES (PAG#S BLKS SCRLEN))
(gc) (mem)
  (WHILE    (/= READNO (+ scrlen 3))
   (MENUCMD "S=BLANK")
   (PROMPT " PICK FROM SCREEN ")
             (GRTEXT)
             (GRTEXT (+ scrlen 1) "PREVIOUS")
             (GRTEXT (+ scrlen 2) "NEXT")
             (GRTEXT (+ scrlen 3) "EXIT")
             (GRTEXT (+ scrlen 5) (IF BLK BLK "BLK NAME"))
             (GRTEXT (+ scrlen 6) (IF blk# (itoa blk#) "# NSRTED"))
             (GRTEXT (+ scrlen 8) "WBLOCK")
             (GRTEXT (+ scrlen 9) (if flagm " MsldOff" " Msld On"))
             (GRTEXT (+ scrlen 10) " EnMasse")
             (GRTEXT (+ scrlen 11) "INSERT")
             (GRTEXT (+ scrlen 12) (if flage " ExitOff" " Exit On"))
             (GRTEXT (+ scrlen 13) "RENAME")
             (GRTEXT (+ scrlen 14) "NEW LIST")
             (GRTEXT (+ scrlen 15) (if flags "Save On"  "Save Off"))
	     (GRTEXT (+ scrlen 16) "BLK CYCL")
             (DSPFLS BLKS scrlen 1 pageno) 
             (grtext -1 (if blk blk ""))
             (grtext -2 (strcat "Page No. " (itoa pageno)))
             (SETQ READNO (NTH 1 (GRREAD)))
	     (COND
               (( AND (>= READNO 0)(< READNO scrlen)(NOT (LISTP READNO)))
                 (if (SETQ BLK (NTH (+ READNO (* PAGENO scrlen)) BLKS))
                       (progn
                         (PROMPT (STRCAT "\n" (EVAL BLK) ))
                         (nsrt# blk)
                       )
                 )
              )
                       

		((= READNO (+ scrlen 1))  ;previous
                     (prevpg))

		((= READNO (+ scrlen 2))   ;next
                     (nextpg))


                 ((= READNO (+ scrlen 5)) ;print blk name
                    (if blk (princ blk)))

                 ((= READNO (+ scrlen 6)) ;#nsrtd- makes inserts flash
                       (flashb))


         ((= READNO (+ scrlen 8)) ;wblock
        (IF BLK (PROGN
                  (grtext)
                  (MENUCMD "S=DIRS")
                  (GETPATH)(menucmd "S=blank")
                  (wblk))))


               ((= READNO (+ scrlen 9)) ;msld on/off
                   (if (not flagm) (setq flagm T) (setq flagm nil)))


               ((= READNO (+ scrlen 10))   ;en masse
                 (grtext)
                 (MENUCMD "S=DIRS")
                 (GETPATH) (menucmd "S=blank")
                 (IF (not flagm) (setq flagm "w"))
                 (FOREACH BLK BLKS (wblk))
                 (if (= flagm "w") (setq flagm nil))
                )



               ((= READNO (+ scrlen 11))             ;insert
                    (IF BLK
                       (PROGN
                 (if (not flage)      ;if flage is nil then we Insert and Exit
                       (progn                       ;nil flage is default
                         (COMMAND "INSERT" BLK )
                         (setq readno (+ scrlen 3)))           ;exit
                                ;else if flage is T we Insert and Stay
                          (COMMAND "INSERT" BLK pause pause pause pause)
                   )))
                   )

               ((= READNO (+ scrlen 12)) ;exit on/off
                 (if (not flage) (setq flage T) (setq flage nil)))

               ((= READNO (+ scrlen 13)) ;rename
                     (renblk))


	((= READNO (+ scrlen 14))  ;new list
;                (IF (NOT nwlst)(LOAD "C:/ACAD/LISP/nwlst"))
                 (nwlst)
;                (setq nwlst nil)
                 (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN)
                        blk nil blk# nil ))


                ((= READNO (+ scrlen 15))  ;save on/off
                  (if (not flags) (setq flags T) (setq flags nil)))


        	((= READNO (+ scrlen 16))  ;insert block cycle
;                (IF (NOT blkcyc)(LOAD "C:/ACAD/LISP/blkcycl"))
                 (blkcyc)
;                (setq blkcyc nil)
                 (setq blk nil blk# nil)
                )

	
              ) ;COND    
) ;WHILE
  (GRTEXT)
  (MENUCMD "S=DIRS" )
  (if (not flags) (setq blks nil))
) ;DEFUN 

;****************************************************************************

;Blkcycl.lsp by T.Priest - used with BLKMGR program
;Insert Block Cycle sub-menu.

(defun blkcyc ( / readno flager )
(setq blk nil blk# nil)
        (WHILE    (/= READNO (+ scrlen 3))
                   (GRTEXT)
                   (GRTEXT (+ scrlen 1) "PREVIOUS")
                   (GRTEXT (+ scrlen 2) "NEXT")
                   (GRTEXT (+ scrlen 3) "EXIT")
                   (GRTEXT (+ scrlen 5) (if flager "ER L OFF" "ER L ON"))
                   (GRTEXT (+ scrlen 6) (IF BLK BLK "BLK NAME"))
                   (GRTEXT (+ scrlen 7) (IF blk# (itoa blk#) "# NSRTED"))
                   (GRTEXT (+ scrlen 8) "WBLOCK")
                   (GRTEXT (+ scrlen 9) (if flagm " MsldOff" " Msld On"))
                   (GRTEXT (+ scrlen 10) "RENAME")
                   (GRTEXT (+ scrlen 11) "zoom min")
                   (GRTEXT (+ scrlen 12) "zoom w")
                   (GRTEXT (+ scrlen 13) "zoom p")
                   (GRTEXT (+ scrlen 14) "zoom max")
                   (GRTEXT (+ scrlen 15) "zoom All")
                   (GRTEXT (+ scrlen 16) " YES")
                   (GRTEXT (+ scrlen 17) " NO")
                   (GRTEXT (+ scrlen 18) "List Obj")
                   (DSPFLS BLKS scrlen 1 pageno)
                   (grtext -1 (if blk blk "")) 
                   (grtext -2 (strcat "Page No. " (itoa pageno)))
                   (SETQ READNO (NTH 1 (GRREAD)))
               (COND


       (( AND (>= READNO 0)(< READNO scrlen)(NOT (LISTP READNO)))
                 (if blk (if (not flager) (command "erase" "L" "")))
                 (if (SETQ BLK (NTH (+ READNO (* PAGENO scrlen)) BLKS))
                       (progn
                          (PROMPT (STRCAT "\n" (EVAL BLK) ))
                          (COMMAND "INSERT" BLK pause pause pause pause)
                          (nsrt# blk)
                       )
                 )
              )


		((= READNO (+ scrlen 1))  ;previous
                     (prevpg))

		((= READNO (+ scrlen 2))   ;next
                     (nextpg))


                ((= READNO (+ scrlen 5)) 
                  (if flager (setq flager nil) (setq flager T)))

                 ((= READNO (+ scrlen 6)) ;print blk name
                     (if blk (prompt (strcat " " blk))))

                 ((= READNO (+ scrlen 7)) ;#nsrtd- makes inserts flash
                     (flashb))


         ((= READNO (+ scrlen 8)) ;wblock
        (IF BLK (PROGN
                  (grtext)
                  (MENUCMD "S=DIRS")
                  (GETPATH)(menucmd "S=blank")
                  (wblk))))


               ((= READNO (+ scrlen 9)) ;msld on/off
                   (if (not flagm) (setq flagm T) (setq flagm nil)))


               ((= READNO (+ scrlen 10)) ;rename
                      (renblk))


                 ((= READNO (+ scrlen 11)) ;zoom min
                    (command "zoom" "C" PAUSE (/ (-(CAR (GETVAR "VSMAX"))
                         (CAR (GETVAR "VSMIN")))
                         (/ 41625.0 (CAR (GETVAR "SCREENSIZE"))))))

                ((= READNO (+ scrlen 12)) ;zoom w
                    (command "zoom" "w" pause pause))

                ((= READNO (+ scrlen 13)) ;zoom p
                    (command "zoom" "p"))

                ((= READNO (+ scrlen 14)) ;zoom max
                    (command "zoom" "W" (GETVAR "VSMIN")(GETVAR "VSMAX")))

                ((= READNO (+ scrlen 15)) ;zoom All
                    (command "zoom" "All" ))

                ((= READNO (+ scrlen 16)) ;YES
                   (COMMAND  "Y")) 
                ((= READNO (+ scrlen 17)) ;NO
                   (COMMAND "N"))

                ((= READNO (+ scrlen 18))
                    (command "select" "au" pause "list" "p" ""))

               ) ;cond
               ) ;while
               (if blk (if (not flager) (command "erase" "L" "")))
               ) ;defun

;****************************************************************************

;NWLST.lsp by T.PRIEST used with BLKMGR program
;Sub-menu "New list"

(DEFUN nwlst (  / tmp readno )
  (WHILE    (/= READNO (+ scrlen 3))
   (PROMPT " PICK FROM SCREEN ")
             (GRTEXT)
             (GRTEXT (+ scrlen 1) "PREVIOUS")
             (GRTEXT (+ scrlen 2) "NEXT")
             (GRTEXT (+ scrlen 3) "EXIT")
             (GRTEXT (+ scrlen 5) "   *")
             (GRTEXT (+ scrlen 6) "WILDCARD")
             (GRTEXT (+ scrlen 7) "PICK OUT" )
             (GRTEXT (+ scrlen 8)  "PICK IN" )
             (GRTEXT (+ scrlen 9) "NSRTDNLY")
             (GRTEXT (+ scrlen 10) "NON-NSRT")
             (GRTEXT (+ scrlen 11) "NAMELIST")
             (DSPFLS BLKS scrlen 1 pageno)
             (grtext -1 (if blk blk "")) 
             (grtext -2 (strcat "Page No. " (itoa pageno)))
             (SETQ READNO (NTH 1 (GRREAD)))

	     (COND

           (( AND (>= READNO 0)(< READNO scrlen)(NOT (LISTP READNO))) 
              (if (SETQ BLK (NTH (+ READNO (* PAGENO scrlen)) BLKS))
                      (PROMPT (STRCAT "\n" (EVAL BLK) )))
              )


		((= READNO (+ scrlen 1))  ;previous
                     (prevpg))

		((= READNO (+ scrlen 2))   ;next
                     (nextpg))


             ((= READNO (+ scrlen 5)) ;* RESET
                   (blklst)
                   (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN)))

;the Release 11 lisp command WCMATCH is required for this wildcard filter
                 ((= READNO (+ scrlen 6))  ;wildcard
                  (setq tmplst nil
                        pattrn (strcase (getstring " Pattern ? ")))
                  (foreach blk blks
                       (if (wcmatch blk pattrn)
                             (setq tmplst (cons blk tmplst))))
                  (setq blks tmplst  tmplst nil)
             (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN)))

;This (below - rem'd (;) out) would be the release 10 wildcard filter
;version for which you would need the (modified) file blk!.lsp by Wing Tong
;The (gc) (mem) are interesting to see how close you are to crashing.
;((= READNO (+ scrlen 6))  ;wildcard
 ;               (gc) (mem)(prompt " before loading c:blk!\n ")
  ;              (IF (NOT C:BLK!)(LOAD "C:/ACAD/LISP/BLK!"))
   ;             (gc) (mem) (prompt " after loading c:blk\n ")
    ;            (c:blk!) (gc) (mem) (prompt " after returning from c:blk\n ")
     ;      (setq c:blk! nil) (gc) (mem) (prompt " after setting c:blk nil\n ")
      ;       (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN)))



        ((= READNO (+ scrlen 7))  ; PICK OUT
          (SETQ FLAGP NIL)
;         (IF (NOT PICKIO) (LOAD "C:/ACAD/LISP/PICKIO"))
          (PICKIO)
;         (SETQ PICKIO NIL)
          (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN))
        )


        ((= READNO (+ scrlen 8))  ; PICK IN
         (SETQ FLAGP T)
;        (IF (NOT PICKIO) (LOAD "C:/ACAD/LISP/PICKIO"))
         (PICKIO)
;        (SETQ PICKIO NIL)
         (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN))
        )


          ((= readno (+ scrlen 9)) ;inserted only - this & next option can
                   (blklst)        ;take over a minute
                   (foreach blk blks
                     (nsrt# blk)
                     (prompt "  ")(princ blk#)(prompt " ") (princ blk) 
                     (if (= 0 blk#)
                           (setq blks (remove blk blks))))
                   (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN))
            )

          ((= readno (+ scrlen 10)) ;non-inserted only
                   (blklst)
                   (setq tmp nil)   
                   (foreach blk blks
                     (nsrt# blk)
                     (prompt "  ")(princ blk#)(prompt " ") (princ blk)
                     (if (= blk# 0)
                            (setq tmp (append (list blk) tmp)))
                     )
                    (setq blks tmp  tmp nil)
                  (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN))
             )

       ((= READNO (+ scrlen 11))  ; SAVE TO NAME OR USE NAMED LIST
;            (IF (NOT NAMLST) (LOAD "C:/ACAD/LISP/NAMLST"))
             (NAMLST)
;            (SETQ NAMLST NIL)
             (SETQ  PAGENO 0  PAGES (PAG#S BLKS SCRLEN))
        )

              ) ;COND    
) ;WHILE
;BLKS
) ;DEFUN 

;****************************************************************************

;PICKIO.lsp by T.PRIEST used with BLKMGR program
;Sub-menu "Pick in" (pick the blocks in the new list)
;         "Pick out" (pick the blocks out of the list)
(DEFUN PICKIO ( / TMPLST READNO)
         (WHILE (/= READNO (+ scrlen 3))
           (GRTEXT)
           (GRTEXT (+ scrlen 1) "PREVIOUS")
           (GRTEXT (+ scrlen 2) "NEXT")
           (GRTEXT (+ scrlen 3) "EXIT")
           (GRTEXT (+ scrlen 5) (IF FLAGP "PICK IN" "PICK OUT"))
           (DSPFLS BLKS SCRLEN 1 PAGENO)
           (grtext -1 (if blk blk ""))
           (grtext -2 (strcat "Page No. " (itoa pageno)))
           (IF TMPLST (PROGN
                       (SETQ I (+ scrlen 7))
                       (REPEAT (LENGTH TMPLST)
                        (GRTEXT I (NTH (- I (+ scrlen 7)) TMPLST))
                        (SETQ I (1+ I))
                  ) ) ) ;repeat progn and if 

           (SETQ READNO (NTH 1 (GRREAD)))
	     (COND

           (( AND (>= READNO 0)(< READNO scrlen)(NOT (LISTP READNO))) 
              (if (SETQ BLK (NTH (+ READNO (* PAGENO scrlen)) BLKS))
                      (PROGN
                       (PROMPT (STRCAT "\n" (EVAL BLK) ))
                       (SETQ BLKS (REMOVE BLK BLKS)
                             PAGES (PAG#S BLKS SCRLEN))
                       (IF FLAGP (SETQ TMPLST (CONS BLK TMPLST)))
              )       ) ;PROGN & IF
              ) ;COND


		((= READNO (+ scrlen 1))  ;previous
                     (prevpg))

		((= READNO (+ scrlen 2))   ;next
                     (nextpg))


               ) ;COND
           ) ;WHILE
(IF TMPLST (SETQ  BLKS TMPLST
                  TMPLST NIL
                  pageno 0
                  PAGES (PAG#S BLKS SCRLEN)))
) ;DEFUN

;****************************************************************************

;NAMLST.LSP by T.Priest
;NAMLST.LSP a sub-menu screen for BLKMGR.lsp in its embryonic form.
;This routine could be expanded to write and read from disk, and to add
;sets (block lists) together. 

(DEFUN NAMLST ( / READNO LSTNAM)  ; USE/SAVE LIST
         (WHILE (/= READNO (+ scrlen 3))
           (IF SAVLST (PROMPT "\nPICK LIST TO USE "))
           (GRTEXT)
           (GRTEXT (+ scrlen 3) "EXIT")
           (GRTEXT (+ scrlen 5) "SAVELIST")
           (IF SAVLST (PROGN
                       (SETQ I 0)
                       (REPEAT (LENGTH SAVLST)
                        (GRTEXT I (NTH I SAVLST))
                        (SETQ I (1+ I))
                   ) ) )
           (SETQ READNO (NTH 1 (GRREAD)))

	     (COND

              ((= READNO ( + scrlen 5)) ;SAVELIST
                 (SETQ LSTNAM (GETSTRING "NAME OF LIST "))
                 (if (not (member lstnam savlst))
                 (SETQ SAVLST (CONS LSTNAM SAVLST)))
                 (set (read lstnam) blks)
                )

               (( AND (>= READNO 0)(< READNO scrlen)(NOT (LISTP READNO)))
                   (IF (SETQ LSTNAM (NTH READNO SAVLST))
                         (PROGN
                            (SETQ BLKS (eval (read LSTNAM)))
                            (SETQ READNO (+ scrlen 3))
                       ) ) )

               ) ;COND
           ) ; WHILE
 ) ;DEFUN

;****************************************************************************
