;;ษออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออป
;;บProgram name:       DDALIAS.LSP                                           บ
;;บInitial Author:     Michael Jenkins @ Gray Construction Company           บ
;;บDescription:        This is a dialog box for creating command aliases     บ
;;บ                    within AutoCAD. This routine actually evaluates the   บ
;;บ                    existing PGP file and will build a new one based on   บ
;;บ                    the users input. This allows the user to assign       บ
;;บ                    aliases on the fly without editing the ACAD.PGP file. บ
;;ศออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออผ
;;========================== Load-time error checking ========================
(defun ai_abort(app msg)
   (defun *error*(s)
      (if old_error(setq *error* old_error))
      (princ)
   ) ;defun *error*
   (if msg               
      (alert(strcat " Application error: "app" \n\n  "msg"  \n"))
   ) ;if
   (exit)
) ;defun
;;
;;Check to see if AI_UTILS is loaded, If not, try to find it, and then try  
;;to load it. If it can't be found or it can't be loaded, then abort the    
;;loading of this file immediately, preserving the (autoload) stub function.
;;
(cond
   ((and ai_dcl(listp ai_dcl)))            ; it's already loaded.
   ((not(findfile "ai_utils.lsp"))         ; find it
      (ai_abort func_name
         (strcat "Can't locate file AI_UTILS.LSP.")
      ) ;ai_abort
   ) ;not
   ((eq "failed"(load "ai_utils" "failed")); load it
   (ai_abort func_name "Can't load file AI_UTILS.LSP"))
) ;cond
;;
;;If acad_app is loaded
;;

(if(not(ai_acadapp))           
   (ai_abort func_name nil)    
) ;if                          
;;======================== End load-time error checking ======================

(defun c:ddalias (/ list_pair index command_list)
	 ;;
	 ;;                            ฺฤฤ Change this to reflect the full path
	 ;;                            ณ	 to your ACAD.PGP file.
	 ;;             ฺฤฤฤฤฤฤฤฤฤฤฤฤฤฤมฤฤฤฤฤฤฤฤฤฤฤฤฟ
	 ;;             ณ                           ณ
	 (setq pgp_name "c:/acadr12/support/acad.pgp")
   ;;

 	 (setq cmdecho (getvar "cmdecho"))
   (setvar "cmdecho" 0)

	 ;error handler
	 (setq *olderror* *error*)
	 (defun *error* (msg)
	    (princ msg)
			(if cmdecho (setvar "cmdecho" cmdecho))
			(setq *error* *olderror*)
			(princ)
	 )

   ;; This function takes a string and a delimiter and returns
   ;; the parsed strings as a list.
   ;;
   (defun _parsestr (string del / count return gp)
      (if (/= string "")
         (progn
            (setq
               count 1
               return '()
               string (strcat string del)
               new_string ""
            )
            (while (< count (1+ (strlen string)))
               (if (= (substr string count 1) del)
                  (progn
                     (setq return (append return (list new_string)))
                     (setq new_string "")
                  )
                  (setq new_string (strcat new_string (substr string count 1)))
               )
               (setq count (1+ count))
            )     
            return
         ) 
         nil
      )   
   )

   ;;
   ;; This function checks the input box for illegal characters 
   ;; and duplicate aliases
   ;;
   (defun _good (alias / alias good_pos)
      (setq
         good_pos 1
         gp 0
      )
      ;check for illegal characters in the text input box
      (while (< good_pos (1+ (strlen alias)))
         (if
            (not
               (member
                  (strcase (substr alias good_pos 1))
                  '(
                     "A" "B" "C" "D" "E" "F" "G" "H" "I" "J" "K" "L" "M"
                     "N" "O" "P" "Q" "R" "S" "T" "U" "V" "W" "X" "Y" "Z"
                     "1" "2" "3" "4" "5" "6" "7" "8" "9" "0" ","
                  )
               )
            )  
            (setq
               good_pos (1+ (strlen alias))
               gp 1                        
            )
         )
         (setq good_pos (1+ good_pos))
      )
      ;check for duplicate aliases
      (if (and (/= alias "") (/= gp 1))
         (foreach n (_parsestr (strcase alias) ",")
            (foreach cmd list_pair
               (foreach al (_parsestr (strcase (cdr cmd)) ",")
                  (if (= al n)
                     (if (/= (nth index command_list) (car cmd))
                        (setq gp (car cmd))
                     )   
                  )
               )
            )
         )
      )
      gp
   )

   ;process the pop-up list
   (defun _command ()
      (setq is_it_good (_good (get_tile "alias")))
      (cond ((= is_it_good 0)
            (_change)
            (setq index (atoi (get_tile "command")))
            (_update)
            (set_tile "error" "")
         )
         ((= is_it_good 1)
            (set_tile "error" "Empty or Invalid Input")
            (set_tile "command" (itoa index))
            (mode_tile "alias" 2)
         )
         (T
            (set_tile "error" (strcat "Duplicate Alias Found in " (strcase is_it_good)))
            (set_tile "command" (itoa index))
            (mode_tile "alias" 2)
         )
      )   
      is_it_good
   )   

   ;change the value in the list
   (defun _change ()
      (if (assoc (nth index command_list) list_pair)
         (setq list_pair
            (subst
               (cons (nth index command_list) (get_tile "alias"))
               (assoc (nth index command_list) list_pair)
               list_pair
            )
         )
         (setq 
            list_pair
            (append
               (list (cons (nth index command_list) (get_tile "alias")))
               list_pair
            )
         )
      )
   )

   ;clear the value to empty string
   (defun _clear ()
      (setq list_pair
         (subst
            (cons newline "")
            (assoc newline list_pair)
            list_pair
         )
      )
   )

   ;update the alias box
   (defun _update ()
      (set_tile "alias" (_getalias (nth index command_list)))
   )   

   ;step thru the existing pgp file
   (defun _stepthru ()
      (if 
         (setq
            pgp 
            (open pgp_name "r")
         )
         (progn
            (setq list_pair nil)
            (while
               (/= (setq line (read-line pgp)) nil)
               (setq pos 1)
               (setq comma 0)
               ;check to see if it is an alias
               (while (/= (setq char (substr line pos 1)) "")
                  (cond
                     ((= char ";")
                        (setq pos (+ (strlen line) 1))
                     )
                     ((= char ",")
                        (setq comma (1+ comma))
                     )
                  )
                  (setq pos (1+ pos))
               )         
               ;append it to the list if it is
               (if (= comma 1)
                  (progn
                     (setq pos 1)
                     (setq string "")
                     (while (/= (setq char (substr line pos 1)) ",")
                        (setq string (strcat string char))
                        (setq pos (1+ pos))
                     )
                     (setq line (_ltrim (substr line (1+ pos))))
                     (if (assoc line list_pair)     
                        (setq list_pair
                           (subst
                              (cons line (strcat (cdr (assoc line list_pair)) "," string))
                              (assoc line list_pair)
                              list_pair
                           )
                        )
                        (setq list_pair (_addlist line string))
                     )
                  )    
               )
            )
            (close pgp)
         )
      )
   )  

   ;adds an alias to the list
   (defun _addlist (alias command)
      (if (= list_pair nil)
         (list (cons alias command))
         (cons (cons alias command) list_pair)
      )
   )   

   ;trims spaces off of the command
   (defun _ltrim (string / string)
      (setq pos 1)
      (while (/= (setq char (substr string pos 1)) "*")
         (setq pos (1+ pos))
      )
      (substr string (1+ pos))
   )      

   ;get the alias from the list
   (defun _getalias (command / command)
      (if (assoc command list_pair)
         (cdr (assoc command list_pair))
         ""
      )
   )

   ;get all of the commands from the atoms-family
   (defun _addlisp ()
      (foreach cmd (atoms-family 1)
         (if (= (substr cmd 1 2) "C:")
            (setq command_list (append command_list (list (substr cmd 3))))
         )
      )
   )

   ;process an okay
   (defun _accept (/ pgpin pgpout pos comma char string newline)
      (if (= (_command) 0)
         (progn
            (set_tile "error" "One Moment Please...")
            (_change)
            (setq
               pgpin (open pgp_name "r")
               file_list nil
            )
            (if pgpin
               (progn
                  (while
                     (/= (setq line (read-line pgpin)) nil)
                     (setq pos 1)
                     (setq comma 0)
                     ;check to see if it is an alias
                     (while (/= (setq char (substr line pos 1)) "")
                        (cond
                           ((= char ";")
                              (setq pos (+ (strlen line) 1))
                           )
                           ((= char ",")
                              (setq comma (1+ comma))
                           )
                        )
                        (setq pos (1+ pos))
                     )         
                     ;append it to the list if it is
                     (if (= comma 1)
                        (progn
                           (setq pos 1)
                           (setq string "")
                           (while (/= (setq char (substr line pos 1)) ",")
                              (setq string (strcat string char))
                              (setq pos (1+ pos))
                           )      
                           (setq newline (_ltrim (substr line (1+ pos))))
                           (if (assoc newline list_pair)
                              (if (/= (cdr (assoc newline list_pair)) "")
                                 (progn
                                    (setq cmdlist (_parse (cdr (assoc newline list_pair))))
                                    (foreach cmd cmdlist
                                       (_append
                                          (strcat
                                             (strcase cmd)
                                             ",*"
                                             newline
                                          )
                                       )   
                                    )
                                    (_clear) 
                                 )
                              )
                              (_append line)
                           )   
                        )
                        (_append line)
                     )    
                  )
                  (close pgpin)
               )
            )
            (foreach cmnd list_pair 
               (if (/= (cdr cmnd) "")
                  (progn
                     (setq cmdlist (_parse (cdr cmnd)))
                     (foreach cmd cmdlist
                        (_append
                           (strcat
                              (strcase cmd)
                              ",*"
                              (car cmnd)
                           )
                        )
                     )
                  )   
               )      
            )         
            (setq pgpout (open pgp_name "w"))
            (foreach line (reverse file_list)
               (write-line line pgpout)
            )
            (close pgpout)
            (set_tile "error" "")
            (done_dialog)
            (setvar "re-init" 16)
         )
      )
   )  

   (defun _parse (string / parse_loc cmd)
      (setq
         parse_loc 1
         cmd_list nil
         cmd ""
      )
      (while (< parse_loc (1+ (strlen string))) 
         (if (/= (substr string parse_loc 1) ",")
            (setq cmd (strcat cmd (substr string parse_loc 1)))
            (progn
               (if (= cmd_list nil)
                  (setq cmd_list (list cmd))
                  (setq cmd_list (append (list cmd) cmd_list))
               )
               (setq cmd "")
            )
         )
         (setq parse_loc (1+ parse_loc))
      )   
      (if (= cmd_list nil)
         (setq cmd_list (list cmd))
         (setq cmd_list (append (list cmd) cmd_list))
      )
      cmd_list
   )

   (defun _append (add)
      (if (= file_list nil)
         (setq file_list (list add))
         (setq file_list (append (list add) file_list))
      )
   )   

   (defun ddalias_main ()
      ;set up the dialog identification
      (if(not(new_dialog "ddalias" dcl_id))(exit))
      (set_tile "error" "One Moment Please...")
      (start_list "command")
      (add_list "")
      (end_list)
      (setq
         command_list
         '(
            "ABOUT" "APERTURE" "ARC" "AREA" "ARRAY" "ATTDEF"
            "ATTDISP" "ATTEDIT" "ATTEXT" "AUDIT" "BASE" 
            "BLIPMODE" "BLOCK" "BREAK" "BOX" "CHAMFER" "CHANGE"
            "CHPROP" "CIRCLE" "COLOR" "COMPILE" "CONE" "CONFIG" "COPY"
            "DBLIST" "DDATTE" "DDEDIT" "DDEMODES" "DDLMODES" "DDRMODES" 
            "DDUCS" "DELAY" "DIM" "DIM1" "DISH" "DIST" "DIVIDE"
            "DOME" "DONUT" "DRAGMODE" "DTEXT" "DVIEW" "DXBIN" "DXFIN"
            "DXFOUT" "EDGESURF" "ELEV" "ELLIPSE" "END" "ERASE"
            "EXPLODE" "EXTEND" "FILES" "FILL" "FILLET" "FILMROLL"
            "GRAPHSCR" "GRID" "HANDLES" "HATCH" "HELP" "HIDE" "ID"
            "IGESIN" "IGESOUT" "INSERT" "ISOPLANE" "LAYER" "LIMITS"
            "LINE" "LINETYPE" "LIST" "LOAD" "LTSCALE" "MEASURE" "MENU" "MESH"
            "MINSERT" "MIRROR" "MOVE" "MSLIDE" "MSPACE" "MULTIPLE" 
            "MVIEW" "NEW" "OFFSET" "OOPS" "OPEN" "ORTHO" "OSNAP" "PAN"
            "PEDIT" "PFACE" "PLAN" "PLINE" "PLOT" "POINT" "POLYGON"
            "PSOUT" "PSPACE" "PURGE" "PYRAMID" "QSAVE"
            "QTEXT" "QUIT" "RECOVER" "REDEFINE" "REDO" "REDRAW" "REDRAWALL"
            "REGEN" "REGENALL" "REGENAUTO" "REINIT" "RENAME" "RESUME" 
            "REVSURF" "ROTATE" "RSCRIPT" "RULESURF" "SAVE" "SAVEAS" "SCALE"
            "SCRIPT" "SELECT" "SETVAR" "SH" "SHADE" "SHAPE" "SHELL" "SKETCH"
            "SNAP" "SOLID" "SPHERE" "STATUS" "STRETCH" "STYLE" "TABLET" "TABSURF"
            "TEXT" "TEXTSCR" "TIME" "TORUS" "TRACE" "TREESTAT" "TRIM" "U" "UCS"
            "UCSICON" "UNDEFINE" "UNDO" "UNITS" "VIEW" "VIEWPORTS" "VIEWRES"
            "VPLAYER" "VPOINT" "VSLIDE" "WBLOCK" "WEDGE" "XBIND" "XREF" "ZOOM"
            "3DFACE" "3DMESH" "3DPOLY"
         )
         index 0
      )      

      (_addlisp)
      (setq command_list(acad_strlsort command_list))

      (_stepthru)
      (_update)

      (action_tile "accept" "(_accept)")
      (action_tile "command" "(_command)")

      (start_list "command")
      (foreach cmd command_list (add_list cmd))
      (end_list)

      (set_tile "error" "")
      (start_dialog)                                                      
   )

   ;;
   ;;Check and setup for function.
   ;;
   (cond
      ((not(ai_transd)))                       ; transparent OK
      ((not(ai_acadapp)))                      ; ACADAPP.EXP xloaded?
      ((not(setq dcl_id(ai_dcl "ddalias"))))    ; is .DCL file loaded?
      (t(if(and(/= 1(logand 1(getvar "cmdactive")))
               (/= 8(logand 8(getvar "cmdactive")))
            ) ;and
            (ai_undo_push)
         ) ;if
         ;;
         ;;Start function
         ;;
         (ddalias_main)
         (if(and(/= 1(logand 1(getvar "cmdactive")))
               (/= 8(logand 8(getvar "cmdactive")))
            ) ;and
            (ai_undo_pop)
         ) ;if        
      ) ;T
   ) ;cond
	 (setvar "cmdecho" cmdecho)
	 (setq cmdecho nil)
   (prin1)
)   


