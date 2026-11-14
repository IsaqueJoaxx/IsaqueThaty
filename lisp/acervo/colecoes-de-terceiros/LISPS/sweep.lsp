;==========================================================
; SWEEP.LSP Copyright 1994 by Looking Glass Microproducts
;==========================================================
; Run a script file for every .DWG file in a directory 
;=============================================================
(Defun c:sweep (/ dwgmod error notrans go_ahead dosdir get_sweep_list
                fprinc create_script_file sweep)
   ;==========================================================
   ; Display acad_dwgmod dialog box
   ;==========================================================
   (Defun dwgmod (/ dcl_id what_next)
      (Setq dcl_id (Load_Dialog "acad.dcl"))
      (If (Not (New_Dialog "acad_dwgmod" dcl_id))
         (Exit)
      )
      (Action_Tile "cancel" "(done_dialog 0)")
      (Action_Tile "save" "(done_dialog 1)")
      (Action_Tile
         "discard"
         "(done_dialog 2)"
      )
      (Setq what_next (Start_Dialog))
      (Unload_Dialog dcl_id)
      what_next
   )
   ;==========================================================
   ; Error Handler
   ;==========================================================
   (Defun error (s)
      (If (Not
             (Member
                S
                '("Function cancelled" "console break")
             )
          )
         (Princ s)
      )
      (If fhand (Close fhand))
      (Princ)
   )
   ;==========================================================
   ; Disallow transparent invocation of routine.
   ;==========================================================
   (Defun notrans ()
      (Cond
         ((Zerop
             (LogAnd
                (GetVar "cmdactive")
                (+ 1 2 4 8)
             )
          )
         )
         ((Alert
             "This command may not be invoked transparently."
          )
         )
      )
   )
   ;==========================================================
   ; Go ahead with command? 
   ;==========================================================
   (Defun go_ahead ()
      (Cond
         ((Zerop (GetVar "dbmod"))
            T ; nothing changed, go ahead
         )
         ((= 0 (Setq what_next (dwgmod)))
            Nil ; cancel command
         )
         ((= 1 what_next)
            ; save changes
            (Command "_qsave")
            (If (/= (GetVar "cmdactive") 0)
               (Command "~")
            )
            T
         )
         ((= 2 what_next)
            ; discard changes
            T
         )
      )
   )
   ;==========================================================
   ; Get Dos Directory
   ;==========================================================
   (Defun dosdir (filespec / dirfile dircmd fhand fnames lbuf)
      (Setq
         dirfile "_SWEEP_.DIR"
         dircmd  (Strcat
                    "dir /-p /-w /-a /-s /b /-l /o-n "
                    filespec
                    " >"
                    dirfile
                 )
      )
      (Command "_shell" dircmd)
      (Cond
         ((Null (Setq fhand (Open dirfile "r")))
            (Alert
               (Strcat dirfile ": Can't open file.")
            )
         )
         (T
            (While (Setq lbuf (Read-Line fhand))
               (Setq fnames (Cons lbuf fnames))
            )
            (Close fhand)
            (Setq fhand Nil)
         )
      )
      fnames
   )
   ;==========================================================
   ; Get Sweep List
   ;==========================================================
   (Defun get_sweep_list (/ sweepdir filelist)
      (Cond
         ((Null
             (Setq
                sweepdir (GetFileD
                            "Select Drawing Directory"
                            "_select_"
                            "dwg"
                            3
                         )
             )
          )
         )
         ((Progn
             ; =====================================--
             ; remove filename from directory
             (While (WCmatch sweepdir "~*`\\")
                (Setq
                   sweepdir (Substr
                               sweepdir
                               1
                               (1- (Strlen sweepdir))
                            )
                )
             )
             (Null
                (Setq
                   filelist (dosdir (Strcat sweepdir "*.dwg"))
                )
             )
          )
            (Alert
               (Strcat
                  "No drawing files found in "
                  sweepdir
               )
            )
         )
         (T
            (Setq
               filelist (MapCar
                           '(Lambda (x) (Strcat sweepdir x))
                           filelist
                        )
            )
         )
      )
      filelist
   )
   ;==========================================================
   ; Print to file
   ;==========================================================
   (Defun fprinc (s) (Princ s fhand))
   ;==========================================================
   ; Create Script File
   ;==========================================================
   (Defun create_script_file (sweep_scr sweep_list big_scr / fhand
                              lbuf lines fname line)
      (Cond
         ((Null (Setq fhand (Open sweep_scr "r")))
            (Alert
               (Strcat sweep_scr ": Can't read file.")
            )
         )
         ((Progn
             (While (Setq lbuf (Read-Line fhand))
                (Setq
                   lines (Cons (Strcat lbuf "\n") lines)
                )
             )
             (Setq
                lines (Apply 'Strcat (Reverse lines))
             )
             (Close fhand)
             (Setq fhand Nil)
             (Null (Setq fhand (Open big_scr "w")))
          )
            (Alert
               (Strcat big_scr ": Can't write file.")
            )
         )
         (T
            (ForEach fname sweep_list
               (fprinc "_point (getvar \"limmin\")\n")
               (fprinc (Strcat "open y " fname "\n"))
               (fprinc lines)
            )
            (fprinc "_point (getvar \"limmin\")\n")
            (fprinc "new y\n\n")
            (fprinc "shell erase _sweep_.*\n")
            (fprinc "(alert \"SWEEP Complete.\")(princ)\n")
            (Close fhand)
            (Setq fhand Nil)
            T
         )
      )
   )
   ;==========================================================
   ; Main Routine
   ;==========================================================
   (Defun sweep (/ sweep_list sweep_scr big_scr)
      (Setq big_scr "_SWEEP_.SCR")
      (Cond
         ((Null (go_ahead)))
         ((Null
             (Setq
                sweep_scr (GetFileD
                             "Select Script File"
                             ""
                             "scr"
                             10
                          )
             )
          )
         )
         ((Null
             (Setq sweep_list (get_sweep_list))
          )
         )
         ((Null
             (create_script_file
                sweep_scr
                sweep_list
                big_scr
             )
          )
         )
         (T (Command "_script" big_scr))
      )
   )
   ;==========================================================
   ; Body of c:sweep  
   ;==========================================================
   (If (notrans)
      (Progn
         (Setq old-error *Error* *Error* error)
         (Setvar "cmdecho" 0)
         (sweep)
      )
   )
   (Princ)
)
(Princ
   (Strcat
      "  SWEEP.LSP v1.0 (Copyright 1994 by "
      "Looking Glass Microproducts) loaded."
   )
)
(Princ)
