; S.Kowalski
; Wardrop Eng. Inc. Winnipeg, MB. Canada R3C 3H1
;
;       GLOBAL VARIABLES
;       =============================================================
;       fname           - drawing LST file name
;       fn              - drawing LST file handle
;       fhandle         - SCRIPT file name
;       fh              - SCRIPT file name handle
;       cmd             - Script type of selection & name of script file
;                         either PRPLOT or OTHER

;SCRNINFO FUNCTION - TELL USER HOW TO USE SCRIPT COMMAND & FILE
(defun scrninfo ()
   (prompt "\n\n\n\tTo use the script file ") (princ fhandle)
   (prompt "\n\tType in at the command prompt SCRIPT")
   (prompt "\n\tThen the script file name omitting the .SCR")
   (prompt "\n\twhile in the drawing editor.")
   (prompt "\n\tThe script file will END the current drawing")
   (prompt "\n\tand proceed with processing the drawings you")
   (prompt "\n\thave listed in the ") (princ fname) (prompt ".LST file")
   (prompt "\n\tWhen completed the script file exit AutoCAD")
   (prompt "\n\tand return you to the DOS prompt or menu")
);end of function scrn info

;--------------------------------------------------------------------

;SCRPINFO FUNCTION - TELL USER HOW TO INPUT COMMANDS IN SCRIPT FILE
(defun scrpinfo ()
  (prompt "\nNow enter the operations you wish to perform")
  (prompt "\non the drawings you selected for the script file.")
  (prompt "\nUse the semicolon < ; > for a return. The semicolon must be")
  (prompt "\nthe only character entered after the - Enter: prompt")
  (prompt "\n\neg:")
  (prompt "\n\tEnter:MOVE               do the MOVE command")
  (prompt "\n\tEnter:W                  using the Window option")
  (prompt "\n\tEnter:0,0                first corner of window")
  (prompt "\n\tEnter:100,50             second corner of window")
  (prompt "\n\tEnter:;                  end selecting items to move")
  (prompt "\n\tEnter:10,10              base point")
  (prompt "\n\tEnter:50,40              second point to move items to")
  (prompt "\n\tEnter:END                end the drawing")
  (prompt "\n\tEnter:<ENTER>            pressing ENTER will terminate ")
  (prompt "\n\t                         request for command entry in the")
  (prompt "\n\t                         SCRIPT file.")
  (prompt "\n\nUse the END command to end the drawing, the script file")
  (prompt "\nwill automatically apply the above sequence of commands to")
  (prompt "\nall the drawing files you have selected.")
  (prompt "\n\n")
);end of function scrpinfo

;--------------------------------------------------------------------

;FUNCTION TO CREATE DRAWING.LST FILE
(defun dwglstfile ()
   (setq operate "DIR *.DWG | SORT > DRAWING.TMP")
   (command "SHELL" operate)
   (prompt "\n\n")
   (prompt "\nType in either Y or N to include line into drawing list.\n")
   (princ)
   (setq yesno " <y/N>:")
   (setq ftemp "DRAWING.TMP")
   (setq ft (open ftemp "r"))
   (setq fn (open fname "w"))
   (while (not (null ft))               ;while still reading tmp file
     (setq line (read-line ft))
     (if (/= line nil)
        (progn
          (if (and
                (> (substr line 1 1) (chr 32))  ;ASCII char 32 'space'
                (< (substr line 1 1) (chr 127)) ;ASCII char 127 'Ctrl del'
              )
            (progn
              (setq subline (substr line 1 8))       ;get 1st 8 characters
              (princ (strcat "Include - " subline " <Y/N>:"))
              (setq ans (strcase (getstring)))  ;ask to include file
              (if (= ans "Y")                ;if yes process string
                (progn
                   (setq exit 1 n 1 word "")
                   (while (= exit 1)
                     (setq ch (substr subline n 1))
                     (setq c (ascii ch))
                     (if (or (= c 32) (= c 0))
                       (setq exit nil)
                       (setq word (strcat word ch) n (+ 1 n))
                     );end of if in while
                   );end of while
                   (write-line word fn)
                );end of progn
              );end of if
            );end of progn
          );end of if
        );end of main progn - 1st
        (progn
           (close ft)
           (setq ft nil)
           (close fn)
           (setq fn nil)
           (setq operate "DEL DRAWING.TMP")
           (command "shell" operate)
        );end of main progn - 2nd
     );end of main if
   );end of while
);end of function dwglstfile

;--------------------------------------------------------------------

;MAKPRPLOT FUNCTION - CREATES PRINTER PLOT SCRIPT FILE
(defun makprplot (/ size units origin max rotate hide fit stuff line)
  (setq size (strcase (getstring "\nEnter Display, Extents or Limits <E>: ")))
  (if (= size "") (setq size "E"))
  (setq units (strcase (getstring "\nEnter units Millimeters or Inches <M>: ")))
  (if (= units "") (setq units "M"))
  (setq origin (strcase (getstring "\nOrigin <0,0>:")))
  (if (= origin "") (setq origin "0,0"))
  (prompt "\nPlotting size is automatically set to MAX.")
  (setq max "MAX")
  (setq rotate (strcase (getstring "\nRotate Yes or No <N>: ")))
  (if (= rotate "") (setq rotate "N"))
  (setq hide (strcase (getstring "\nRemove hidden lines <N>?: ")))
  (if (= hide "") (setq hide "N"))
  (prompt "\nDrawing will automatically be plotted to FIT on paper.")
  (setq fit "FIT")
  (setq fn (open fname "r"))
  (if (null fn)
     (prompt "\nDrawing list file not found.")
     (progn
        (setq fh (open fhandle "w"))
        (setq r (chr 10))
        (setq stuff
          (strcat r size r "Y" r "N" r units r origin r max r rotate r hide r fit r r)
        )
        (write-line "END" fh)
        (while (not (= fn nil))
          (setq line (read-line fn))
          (if (= line nil)
            (progn
              (write-line "0" fh)
              (close fh)
              (close fn)
              (setq fn nil fh nil)
              (scrninfo)        ;tell user how to use script file
            )
            (progn
              (setq info (strcat "4 " line stuff))
              (write-line info fh)
            )
          )
        )
     )
  )
  (princ)
);end of makprplot

;--------------------------------------------------------------------

;OTHER FUNCTION - INPUTTING COMMANDS OTHER THAN PRPLOT INTO SCRIPT FILE
(defun other (/ exit stuff txt line info)
  (scrpinfo)
  (setq Exit 1 stuff "")
  (while (= Exit 1)
    (prompt "Enter: ")
    (setq txt (read-line))
    (if (= txt "")
       (setq Exit 0)
       (progn
          (if (= txt ";")
             (setq stuff (strcat stuff "\n"))
             (setq stuff (strcat stuff "\n" txt))
          );end of nest if in progn
       );end of progn
    );end of if in while
  );end of while
  (prompt "\nEnd of script command line.")
  (setq fn (open fname "r"))
  (if (null fn)
     (prompt "\nDrawing list file not found.")
     (progn
        (setq fh (open fhandle "w"))
        (write-line "END" fh)
        (while (not (= fn nil))
          (setq line (read-line fn))
          (if (= line nil)
            (progn
              (write-line "0" fh)
              (close fh)
              (close fn)
              (setq fn nil fh nil)
              (scrninfo)        ;tell user how to use script command
            )
            (progn
              (setq info (strcat "2 " line stuff))
              (write-line info fh)
            )
          )
        )
     )
  )
  (princ)
);end of function other

;--------------------------------------------------------------------

;MAKESCR COMMAND - MAIN COMMAND ROUTINE
(defun c:makescr (/ ans)
  (setvar "cmdecho" 0)
  (textscr)
  (prompt "\n\n")
  (prompt "\nMAKESCR requires a file containing the names of drawings")
  (prompt "\nto be included in the script file you are about to make.\n")
  (prompt "\nNote: file name extensions are not required when asked for file")
  (prompt "\nnames.")
  (setq ans nil)
  (setq ans (strcase (getstring "\nCreate LST file <y/N>? ")))
  (if (= ans "Y")               ;YES, create new LST file
     (progn
       (setq ans nil)           ;reset variable ans to nil
       (prompt "\nName for LST file <DRAWING.LST>: ")
       (setq ans (strcase (getstring)))
       (if (= ans "")           ;if ENTER is pressed - user wants DRAWING.LST
         (setq fname "DRAWING.LST")
         (setq fname ans)       ;else use user input
       )
       (dwglstfile)             ;function to create LST file
     );end of progn to get new LST file
     (progn                     ;NO use existing LST file
       (setq ans nil)           ;reset variable ans to nil
       (prompt "\nName of existing LST file to use <DRAWING.LST>: ")
       (setq ans (strcase (getstring)))
       (if (= ans "")           ;if ENTER is pressed - user wants DRAWING.LST
         (setq fname "DRAWING.LST")
         (setq fname ans)       ;else use user input
       )
     );end of progn to get LST file name
  )
  (prompt "\nAutoCAD commands currently supported are:")
  (prompt "\n\n\n\tPRPLOT\t\tOTHER")
  (prompt "\n\nType in command as listed above to use MAKESCR.")
  (setq cmd
     (strcase
       (getstring "\n\nEnter Command: ")
     )
  )
  (setq ans nil)
  (princ (strcat "\nUse " cmd ".SCR or type in different SCRIPT file name: "))
  (setq ans (strcase (getstring)))
  (if (= ans "")        ;if ENTER is pressed - user wants default
      (setq fhandle (strcat cmd ".SCR"))
      (setq fhandle (strcat ans ".SCR"))
  )
 (cond                                 ;use conditional statement in case
    ((= cmd "PRPLOT") (makprplot))     ;more selections are added at a
    ((= cmd "OTHER") (other))          ;later date
  )
);end of command

