;ATTEXTED.LSP
;Program by Rolando Padron 3/19/91 Revised 5/2/91
;
;This routine allows the user to create a selection set of blocks 
;containing attribute text and edit the text using an ASCII text editor.
;The program will work with all, but constant attributes.
;
;You must modify your ACAD.PGP file prior to starting up AutoCAD as follows:
;EDATXT,<EDIT.EXE> ATTEXTED.FIL,XXXXXX,,4
;where <EDIT.EXE> is the command to start your  ASCII text editor and XXXXXX
;is the amount of memory required in the shell to load and run the editor.
;
(defun c:attexted ( / ss-ctr at-sslgth etemp elist etemp2 elist2 etemp3
                      elist3 bk-str ctr etemp-bk bk-name bk-ctr att-prmt
                      att-str at-val ss1 at-fil cmd-ex olderr)
   (errset)
   (f-test)
   (s1)
   (at-ss)
   (bk-tst)
   (setq ss-ctr 0)
   (prompt "\nWriting file. . .")
   (while (< ss-ctr at-sslgth)
      (setq etemp-bk nil)
      (prompt " .")
      (bk-info)
      (setq bk-ctr 0
            etemp3 (entnext etemp)
            elist3 (entget (entnext etemp))
      )
      (while (and (= "ATTRIB" (dxf 0 elist3)) (/= "SEQEND" (dxf 0 elist3)))
         (setq bk-ctr (1+ bk-ctr)
               etemp3 (entnext etemp3)
               elist3 (entget etemp3)
         )
          (if (= bk-ctr 1) (bk-prn))
          (if (= ctr nil) (setq ctr 0))
          (while (< ctr bk-ctr)
             (at-info)
             (at-value)
             (at-file)
             (setq ctr (1+ ctr))
          )
      )
      (setq ss-ctr (1+ ss-ctr)
            ctr nil
            etemp2 nil
      )
   )
   (command "EDATXT")
   (prompt "\nUpdating text . . .")
   (at-fix)
   (command "DEL" "attexted.fil")
   (graphscr)
   (setq ss-ctr 0)
   (while (< ss-ctr at-sslgth)
      (prompt " .")
      (entupd (ssname ss1 ss-ctr))
      (setq ss-ctr (1+ ss-ctr))
   )
   (s2)
   (err-b4)
   (princ)
)
;---------------------------------AT-SS---------------------------------------
(defun at-ss ()
   (prompt "\n Select BLOCKS with attributes for editing:  ")
   (setq ss1 (ssget)
         at-sslgth (sslength ss1)
         ss-ctr 0
   )
)
;--------------------------------BK-INFO--------------------------------------
(defun bk-info ()
   (setq etemp (ssname ss1 ss-ctr)
         elist (entget etemp)
         bk-name (dxf 2 (entget etemp))
         bk-str (strcat "***Block Name:  " bk-name)
   )
)
;--------------------------------BK-PRN---------------------------------------
(defun bk-prn ()
   (setq at-fil (open "attexted.fil" "a"))
   (write-line bk-str at-fil)
   (close at-fil)
)
;--------------------------------AT-INFO--------------------------------------
(defun at-info ()
   (if (= etemp-bk nil)
      (setq etemp-bk (dxf -2 (tblsearch "BLOCK" bk-name))
      )
   )
   (while (/= (dxf 0 (entget etemp-bk)) "ATTDEF")
      (setq etemp-bk (entnext etemp-bk))
   )      
   (setq att-prmt (dxf 3 (entget etemp-bk))
         att-str (strcat "***Attribute Prompt:  " att-prmt)
         etemp-bk (entnext etemp-bk)
   )
) 
;-------------------------------AT-VALUE--------------------------------------
(defun at-value ()
   (if (= etemp2 nil)
      (setq etemp2 (entnext etemp)
            at-val (dxf 1 (entget etemp2))
      )
      (setq etemp2 (entnext etemp2)
            at-val (dxf 1 (entget etemp2))
      )
   )
)
;-------------------------------AT-FILE---------------------------------------
(defun at-file ()
   (setq at-fil (open "attexted.fil" "a"))
   (write-line att-str at-fil)
   (write-line at-val at-fil)
   (close at-fil)
)
;-------------------------------AT-FIX----------------------------------------
(defun at-fix ( / etemp elist etemp2 elist2 str-test at-new old new at-fil)
   (setq ss-ctr 0
         at-fil (open "attexted.fil" "r")
   )
   (while (< ss-ctr at-sslgth)
      (setq etemp (ssname ss1 ss-ctr)
            elist (entget etemp)
            etemp2 (entnext etemp)
            elist2 (entget etemp2)
      )
      (while (and (= "ATTRIB" (dxf 0 elist2)) (/= "SEQEND" (dxf 0 elist2)))
         (setq str-test (read-line at-fil))
         (while str-test
            (if (= (substr str-test 1 3) "***")
               (setq str-test (read-line at-fil))
               (setq at-new str-test
                     str-test nil
               )
            )
         )
         (setq old (assoc 1 elist2)
               new (cons 1 at-new)
               elist2 (subst new old elist2)
         )
         (entmod elist2)
         (setq etemp2 (entnext etemp2)
               elist2 (entget etemp2)
         )
      )
      (setq ss-ctr (1+ ss-ctr))
   )
   (close at-fil)
)
;-----------------------------------------------------------------------------
;*****************************U T I L I T I E S*******************************
;-----------------------------------------------------------------------------
(defun s1 ()
   (setq cmd-ex (getvar "cmdecho"))
   (setvar "cmdecho" 0)
)
;-----------------------------------------------------------------------------
(defun s2 ()
   (setvar "cmdecho" cmd-ex)
   (prompt "\nProgram Completed...")
)
;-----------------------------------------------------------------------------
(defun dxf (code e-list)
   (cdr (assoc code e-list))
)
;-----------------------------------------------------------------------------
(defun f-test ()
   (if (/= (findfile "attexted.fil") nil)
      (progn
         (prompt "\nErasing existing file...\n")
         (command "DEL" "attexted.fil")
      )
   )
)
;-----------------------------------------------------------------------------
;*************************E R R O R   H A N D L I N G*************************
;-----------------------------------------------------------------------------
;---------------------------------BK-TST--------------------------------------
(defun bk-tst ( / b-el b-en blk-ok)
   (setq ss-ctr 0
         blk-ok 0
   )
   (prompt "\nChecking selection set . . .")
   (while (< ss-ctr at-sslgth)
      (bk-info)
      (setq etemp-bk (dxf -2 (tblsearch "BLOCK" bk-name))
            b-en etemp-bk
      )
      (while b-en
         (setq b-el (entget b-en))
         (if (and (= (dxf 70 b-el) 2)               
                (= (dxf 0 b-el) "ATTDEF")
             )
            (setq blk-ok (1+ blk-ok))
         )
         (setq b-en (entnext b-en))
      )
      (if (/= blk-ok 0) (out)		;causes null function for error
         (prompt " .")
      )
      (setq ss-ctr (1+ ss-ctr))  
   )
   (prompt " O.K.")
   (princ)
)
;---------------------------------AT-ERR--------------------------------------
(defun at-err (m)
   (cond
      ((= m "null function")
         (prompt "\nerror:  Blocks cannot contain CONSTANT attributes")
         (setq *error* olderr)
         (princ)
      )
      ((= m "Function cancelled")
      (prompt "\n\n\nUser cancelled function")
      (setq *error* olderr)
      )
      ((/= m nil)
         (prompt "\nerror:  ")
         (princ m)
         (prompt "\nObjects selected possibly not blocks or don't have attributes")
         (setq *error* olderr)
         (princ)
      )
   )
   (princ)
)
;---------------------------------ERR-SET-------------------------------------
(defun errset ()
   (setq olderr *error*
         *error* at-err
   )
)
;---------------------------------ERR-B4--------------------------------------
(defun err-b4 ()
   (setq *error* olderr)
)

