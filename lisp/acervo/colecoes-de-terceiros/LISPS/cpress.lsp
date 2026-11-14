; Program : CPRESS.LSP
; Purpose : Shows how AutoCOMPRESS can extract drawings from a slide
;           library, then un-compress the drawing and insert it into the
;           drawing.
; =======================================================================
;
(PRINC "Loading AutoCOMPRESS Demo......")
(DEFUN grab( symbol / errCode )
    (IF (TBLSEARCH "BLOCK" symbol)
        (PROGN
            (PRINC "Block already exists in drawing\n")
            (intoDrawing symbol)
        )
        (PROGN
            (PRINC "\nExtracting from Library.....")
            (SETQ errCode (libext "VALVES.SLB" symbol) )
            (IF errCode
                (PROGN  ; OK, we now know symbol has been extracted from lib
                    (PRINC "Complete\nUn-compressing file.....")
                    (IF (ef symbol (STRCAT symbol ".DWG"))
                        (PROGN
                            (PRINC "Drawing un-compressed\n")
                            (intoDrawing symbol)
                            (UNLINK symbol) ; Erase library file
                            (UNLINK (STRCAT symbol ".DWG")) ; Erase drawing file
                        )
                        (PROGN
                            (PRINC "AutoCOMPRESS ERROR in un-compress\n")
                        )
                    )
                )
                (PROGN
                    (PRINC "Symbol not in Slide Library\n")
                )
            )
        )
    )
    (PRINC)
)

(DEFUN intoDrawing( symbol / )
    (COMMAND "INSERT" symbol )
)
(PRINC "Loaded.\nUse (GRAB <blockname> ) where block name is V1, V2 or V3" )
(PRINC)
