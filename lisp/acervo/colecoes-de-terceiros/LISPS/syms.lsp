; License
; -------
;
; Copyright (c) 1989 Discovery Systems.  All Rights Reserved.
;
; You are free to use, copy and distribute Syms for noncommercial use IF:
;
;        NO FEE IS CHARGED FOR USE, COPYING OR DISTRIBUTION.
;
;        IT IS NOT MODIFIED IN ANY WAY.
;
;
; Clubs and user groups may charge a nominal fee for expenses and
; handling while distributing Syms.
;
; Site licenses, commercial licenses and custom versions of Syms
; are available.  Write to the address below for more information.
;
; This program is provided AS IS without any warranty, expressed or
; implied, including but not limited to fitness for a particular
; purpose.
;
; Leif Eriksen
; Discovery Systems
; 34 Autumnleaf
; Irvine, CA 92714
;-----------------------------------------------------------------------------
;				A D D S Y M
;
; Description: Adds a symbol into the symbol grid area of the 
;              tablet drawing file.
;
; Parameters:  synName  (String)  Name of symbol to add.  Symbol must
;                       have been WBLOCKed to a file with the same name.
;              symPos	(String)  Position of symbol from A1 to H25.  
;                       Symbol position should have a dash between letter
;                       and letter (i.e., "B-15").
;
; Calls: None
;
; Returns: nil

(defun AddSym (symName symPos / row col snn sll sur 
               xScale yScale scale zoomScale)
;
;  Check if the layer INSERT-SYM exists.  If not, create the layer.
;  Set the current layer to INSERT-SYM.
;
  (if (not (tblsearch "layer" "insert-sym"))
    (command "layer" "n" "insert-sym" "")
    )
  (command "layer" "s" "insert-sym" "")

; Move to area below menu drawing to insert the symbol.  Insert symbol.

  (command "zoom" "c" '(10.8285 -6.3801) 13.5)
  (command "undo" "group")
  (princ "\nInsert symbol near center of screen ")
  (command "insert" symName PAUSE "" "" 0)

; Find the scaling factor so that the symbol may be placed in a 0.4" by
; 0.4" box.  Reduce by 20% to allow some room around the symbol in the box.

  (initget 1) ;Disable nil input
  (setq sll (getpoint "\nPick lower left corner of symbol (use 'ZOOM first if necessary) "))
  (initget 1) ;Disable nil input
  (setq sur (getcorner sll "\nPick upper right corner of symbol "))
  (setq xScale (* (/ 0.4 (- (car sur) (car sll))) 0.8))
  (setq yScale (* (/ 0.4 (- (cadr sur) (cadr sll))) 0.8))
  (setq xScale (abs xScale) yScale (abs yScale)) ;in case ll & ur were 
  (setq scale (min xScale yScale))		 ;reversed

; Remove the symbol inserted in the drawing.

  (command "undo" "end")
  (command "u")

; Zoom to move insertion box to the center of the screen.  Drag in symbol
; and allow user to place.

  (setq row (- (ascii (substr symPos 1 1)) 65))
  (setq col (1- (atoi (substr symPos 3))))
  (setq zoomCenter (list (+ 1.28125220 (* col 0.4))
                         (- 10.79526968 (* row 0.4))))
  (command "zoom" "c" zoomCenter 0.5)
  (princ "\nPlace symbol in box at the center of the screen ")
  (command "insert" symName "s" scale PAUSE "")
  )

;				C H G S T R
;
; Description: Examines a string for certain character(s) and if found
;              replaces the character(s) with new character(s).  The 
;              resulting string is returned.  
;
; Parameters:  str     (String)  String to be checked and changed.
;              oldChrs (String)  One or more characters to checked for in
;                       str.
;              newChrs (String)  String to replace oldChrs in str.
;
; Calls: None
;
; Returns: (String)  The new string.

(defun chgstr (str oldChrs newChrs)
  (setq idx 1)
  (setq str# (strlen str))
  (setq oc# (strlen oldchrs))
  (setq newstr "")
  (while (<= idx str#)
    (if (= (substr str idx oc#) oldchrs)
      (progn
      (setq newstr (strcat newstr newchrs))
      (setq idx (+ idx oc#))
      )
      (progn
      (setq newstr (strcat newstr (substr str idx 1)))
      (setq idx (1+ idx))
    ))
  )
  (eval newstr)
)


;				D E L S Y M
;
; Description: Deletes a symbol from the symbol grid area of the 
;              tablet drawing file.
;
; Parameters:  symPos   (String)  Position of symbol from A1 to H25.  
;                       Symbol position should have a dash between letter
;                       and letter (i.e., "B-15").
;
; Calls: None
;
; Returns: nil

(defun DelSym ( symPos / en row col ll ur)
  (setq row (- (ascii (substr symPos 1 1)) 65))
  (setq col (1- (atoi (substr symPos 3))))
  (setq ll (list (+ 1.081255220 (* col 0.4)) (- 10.59526968 (* row 0.4))))
  (setq ur (list (+ (car ll) 0.4) (+ (cadr ll) 0.4)))
  (command "zoom" "c" '(6 11) 6.5)
  (command "erase" "w" ll ur "")
  )
;				F N D P O S
;
; Description: Requests and verifies position for symbol Add, Delete, or
;              Replace.
;
; Parameters:  type     (String)  Action being preformed in association with
;                       with finding the position.  These include "Add", 
;                       "Delete", or "Replace".
;
; Calls: None
;
; Returns: (String)  Position in the form Letter-Number (i.e., "C-12").

(defun FndPos (type / row col pos)
  (initget 1 "A B C D E F G H")
  (setq row (getkword (strcat type " symbol at row (A..H): ")))
  (while (or (< col 1) (> col 25))
    (initget (+ 1 2 4))
    (setq col (getint (strcat type " symbol at column (1..25): ")))
    )
  (setq pos (strcat row "-" (itoa col)))
  )
;				F N D S Y M
;
; Description: Requests a symbol name and verifies that it exists.
;
; Parameters:  None.
;
; Calls: ChgStr
;
; Returns: (String)  nil

(defun FndSym ( / snn sn)
  (while (not snn)
    (setq sn (strcase (getstring "\nEnter name of symbol to insert: ")))
    (if (/= (substr sn (- (strlen sn) 4)) ".DWG")
      (setq sn (strcat sn ".DWG"))
      )
    (setq snn (findfile sn))

    (if (not snn)
      (princ (strcat "\nError: Cannot find symbol " sn))
      )
    ) ; End while

  (setq snn (chgstr snn "\\" "/"))
  (eval snn)
  )


;				M A K E G R I D
;
; Description: Builds a grid of lines in 0.4 inch square.
;
; Parameters:  None.
;
; Calls: None
;
; Returns: nil

(defun MakeGrid ()

; Create insert-grid layer, if it does not exist.

  (if (not (tblsearch "layer" "insert-grid"))
    (command "layer" "n" "insert-grid" "")
    )

  (command "layer" "s" "insert-grid" "")
  (command "line" '(1.08125220 8.19526968) '(11.08125220 8.19526968) "")
  (command "array" "l" "" "r" 7 1 0.4)
  (command "line" '(1.48125220 7.79526968) '(1.48125220 10.99526968) "")
  (command "array" "l" "" "r" 1 24 0.4)
  )
;				U P D M E N U
;
; Description: Update the menu file by adding, deleting, or replacing
;              an insert instruction in the appropriate box.
;
; Parameters:  updList  (List)  List of position, block name pairs used
;                       to update the menu file.
;
; Calls: SrchFile
;
; Returns: nil

(defun UpdMenu (updList / mnu sav lineCnt modFlag inLine 
                          srchList pos srchStr srchStrLen)

; Search for ACAD.MNU.  If not found ask where it can be found.

  (setq mnu (srchfile "acad.mnu"))
  (while (not mnu)
    (setq mnu (strcase (getstring "Enter menu file to use: ")))
    (if (/= (substr mnu (- (strlen mnu) 4)) ".MNU")
      (setq mnu (strcat mnu ".MNU"))
      )
    (setq mnu (srchfile mnu))
    )

; Copy menu file to .SAV file so that change may be reversed.

  (setq sav (strcat (substr mnu 1 (- (strlen mnu) 4)) ".SAV"))
  (command "sh" (strcat "copy" " " mnu " " sav " > nul:"))
  (setq inFile (open sav "r"))	; Keep inFile & outFile global so that
  (setq outFile (open mnu "w"))	; that error handling routine can use.
  (setq lineCnt 1)
  (setq modFlag nil)
  (princ "\nWorking...\n")
  (setq inLine (read-line inFile))

; Loop to read through menu file.

  (while inLine

; Only check lines that have a chance of being changed (saves lots of time
; during update).

    (if (= (substr inLine 1 1) "[")
      (progn
      (setq srchList updList)
      (while srchList
        (setq pos (car srchList))
        (setq srchStr (strcat "[" pos "]"))
        (setq srchStrLen (strlen srchStr))
        (if (= (substr inLine 1 srchStrLen) srchStr)
          (progn
          (if (> (strlen (cadr srchList)) 0)
            (write-line (strcat srchStr
                                "^C^CINSERT " (cadr srchList) 
                                " \\\\\\\\") outFile) ;Add
            (write-line srchStr outFile) ;Delete
          )
          (setq modFlag T)
          ))

        (setq srchList (cddr srchList))
        ) ;End of while SrchList

; If no change then write line as read.

      (if (not modFlag)
        (progn
        (write-line inLine outFile)
        ))
      (setq modFlag nil)

      ) ;End of progn inline 1 1
      (write-line inLine outFile)
      )

; Update screen display to show progress of update.

    (if (= (rem lineCnt 100) 0)
      (princ (strcat (itoa (fix (* (/ lineCnt 3104.0) 100))) "% done\r"))
      )
    (setq lineCnt (1+ lineCnt))
    (setq inLine (read-line inFile))
    ) ;End of while inline
  (princ "100% done")
  (close inFile)
  (close outFile)
  )

  
;				S R C H F I L E
;
; Description: Search for the given file name in the 
;                1. Current directory
;                2. Drawing directory
;                3. Directory specified by the ACAD environment variable
;                   (if any).
;                4. AutoCAD program directory.
;
;              If not found return nil.  If found verify OK to use.  If not
;              OKed then return nil.  if OK return full file name.
;
; Parameters:  file  (String)  File name to for search.
;
; Calls: None
;
; Returns: The full file name (as a string) or nil if not found.

(defun SrchFile (file / fn OK)
  (setq fn (findfile file))
  (if fn
    (progn
    (initget "Yes No")
    (setq OK (getkword (strcat "\nFound file " fn 
                               ".  Use this file? No/<Yes>: ")))
    (if (= OK "No")
      (eval nil)
      (eval fn)
      )
    ))
 )

;                                S Y M S E R R
;
; Description: Error routine that takes control in case of an error.
;              Resets system variables, closes files.
;
; Parameters:  None.
;
; Calls: None
;
; Returns: nil.

(defun symserr (s)

; Restore System variables and zoom out to restore frozen layers

  (command "zoom" "c" '(10.8285 6.75) 13.5)
  (command "layer" "t" "text,red,symbols,black,marks" "")

  (setvar "attreq" ar)
  (setvar "cmdecho" ce)

; If files are open then close and give warning

  (if (or inFile outFile)
    (progn
    (close inFile)
    (close outFile)
    (princ "\nWARNING: Menu file was open.  Check that menu files are OK")
    ))

  (princ)
  ) ;End SYMS

;                                  S Y M S
;
; Description: Main routine in symbol update program.  Calls necessary 
;              update routines.  Continues in loop until exit is requested.
;
; Parameters:  None.
;
; Calls: FndSym, FndPos, AddSym, DelSym, MakeGrid, UpdMenu
;
; Returns: Quite exit.

(defun c:syms ( / action symName symPos updList)

; Set error handling routine to restore in case of an error

(setq olderr *error*)
(setq *error* symserr)

; Save system variables, then change to new value.

  (setq ce (getvar "cmdecho")) ;Keep global so symserr can use
  (setvar "cmdecho" 0)
  (setq ar (getvar "attreq"))  ;Keep global so symserr can use
  (setvar "attreq" 0)

; Freeze unnecessary layers to save time on regens

  (command "layer" "f" "text,red,symbols,black,marks" "")

; Program keeps running until Exit is given

  (while (/= action "Exit")
    (initget "Add Delete Replace Grid Exit")
    (setq Action (getkword "\nSymbol library action /Delete/Replace/Grid/Exit/<Add>: "))

; Check for and do Add, Delete, Replace, or Grid

    (cond

      ((or (= action "Add")
           (= action nil)
       )
        (setq symName (FndSym))
        (setq symPos (FndPos "Add")) ;symPos is a string Row-Col
        (AddSym symName symPos)
       	(setq updList (cons symName (cons symPos updList)))
       	)

      ((= action "Delete")
       	(setq symPos (FndPos "Delete"))
        (DelSym symPos)
       	(setq updList (cons "" (cons symPos updList)))
      )

      ((= action "Replace")
       	(setq symName (FndSym))
       	(setq symPos (FndPos "Replace"))
        (DelSym symPos)
        (AddSym symName symPos)
       	(setq updList (cons symName (cons symPos updList)))
     	)

      ((= action "Grid")
        (MakeGrid)
     	)

      )	;End cond
    ) ;End while

; Check is menu file update is desired.  If so do it.

  (initget "Yes No")
  (setq action (getkword "Update Menu file? No/<Yes>: "))
  (if (or (= action nil)
          (= action "Yes"))
    (progn
    (setq updList (reverse updList))
    (UpdMenu updList)
    ))

; Zoom out to see whole menu. Restore original layers and system variables.

  (command "zoom" "c" '(10.8285 6.75) 13.5)
  (command "layer" "t" "text,red,symbols,black,marks" "")
  (setvar "attreq" ar)
  (setvar "cmdecho" ce)

; Restore old error handling

  (setq *error* olderr)
  (princ)
  ) ;End SYMS
