;;========================== Load-time error checking ========================

  (defun ai_abort (app msg)
     (defun *error* (s)
        (if old_error (setq *error* old_error))
        (princ)
     )
     (if msg
       (alert (strcat " Application error: "
                      app
                      " \n\n  "
                      msg
                      "  \n"
              )
       )
     )
     (exit)
  )

;;; Check to see if AI_UTILS is loaded, If not, try to find it,
;;; and then try to load it.
;;;
;;; If it can't be found or it can't be loaded, then abort the
;;; loading of this file immediately, preserving the (autoload)
;;; stub function.

  (cond
     (  (and ai_dcl (listp ai_dcl)))          ; it's already loaded.

     (  (not (findfile "ai_utils.lsp"))                     ; find it
        (ai_abort "DDLIST"
                  (strcat "Can't locate file AI_UTILS.LSP."
                          "\n Check support directory.")))

     (  (eq "failed" (load "ai_utils" "failed"))            ; load it
        (ai_abort "DDLIST" "Can't load file AI_UTILS.LSP"))
  )

  (if (not (ai_acadapp))               ; defined in AI_UTILS.LSP
      (ai_abort "DDLIST" nil)         ; a Nil <msg> supresses
  )                                    ; ai_abort's alert box dialog.

;;======================== End load-time error checking ======================

(defun C:DDLIST ()
   (setq *olderror* *error*)
   (defun *error* (msg)
      (princ msg)
      (setq 
         *error* *olderror*
         *olderror* nil
      )
      (princ)
   )
   (ddlist nil)
   (princ)
)   

(defun DDLIST 
   (
      ddlist_flag / ddlist_str dxf gc_pts ddlist_ent ddlist_name
      ddlist_etype 
   )

   ;;; Point to string conversion
   ;;;
   ;;; Purpose: Takes a point list and converts it to a string. It can
   ;;;          handle 2-D or 3-D points.             
   ;;;
   ;;; Returns: The point as a string utilizing commas as separators.   
   ;;;
   ;;;    Note: No built-in error checking, this expects a valid point list.

   (defun gc_pts (pt)
      (strcat
         (rtos (car pt))
         ","
         (rtos (cadr pt))
         (if (/= (caddr pt) nil)
            (strcat
               ","
               (rtos (caddr pt))
            )
            ""
         )   
      )
   )

   (defun DXF (code lst) (cdr (assoc code lst)))
   
   (defun ddlist_str (dxfcode)
      (if ddlist_flag
         (if dxfcode
            (strcat
               (substr "   " 1 (- 3 (strlen (itoa dxfcode))))
               "("
               (itoa dxfcode)
               ")"
            )
            "     "
         )
         ""
      )
   )

   ;allow selection based on global variable
   (if (= #ddlist_n "1")
      (while (=
            (setq ddlist_ent (nentsel))
            nil
         )
         (prompt "\nNo entity selected.")
      )
      (while (=
            (setq ddlist_ent (entsel))
            nil
         )
         (prompt "\nNo entity selected.")
      )
   )

   ;reassign for entity list
   (setq ddlist_ent
      (entget
         (setq ddlist_name (car ddlist_ent))
      )
   )

   ;highlight the entity
   (redraw ddlist_name 3)

   ;set up the dialog identification
   (setq id (load_dialog "ddlist"))

   (new_dialog "ddlist" id "" #ddlist_loc)

   ;start the list
   (start_list "list_box")

   ;check entity type
   (setq ddlist_etype (dxf 0 ddlist_ent))

   ;change insert to block
   (add_list 
      (strcat "        Entity" (ddlist_str 0) ": "
         (if (/= ddlist_etype "INSERT")
            (dxf 0 ddlist_ent)
            "BLOCK"
         )
      )
   )

   ;add the common dxf groups
   (add_list (strcat "         Layer" (ddlist_str 8) ": " (dxf 8 ddlist_ent)))
   (add_list
      (strcat "         Color" (ddlist_str 62) ": "
         (if (dxf 62 ddlist_ent)
            (itoa (dxf 62 ddlist_ent))
            "BYLAYER"
         )
      )
   )
   (add_list
      (strcat "     Thickness" (ddlist_str 39) ": "
         (if (dxf 39 ddlist_ent)
            (rtos (dxf 39 ddlist_ent))
            "0"
         )
      )
   )
   (add_list
      (strcat "      Linetype" (ddlist_str 6) ": "
         (if (dxf 6 ddlist_ent)
            (dxf 6 ddlist_ent)
            "BYLAYER"
         )
      )
   )
   (add_list
      (strcat "      Location" (ddlist_str 67) ": "
         (if 
            (or 
               (= (dxf 67 ddlist_ent) nil)
               (= (dxf 67 ddlist_ent) 0)
            )
            "MODEL SPACE"
            "PAPER SPACE"
         )
      )
   )

   ;process the dxf groups specific to each entity
   (cond 
      ((or (= ddlist_etype "TRACE") (= ddlist_etype "3DFACE") (= ddlist_etype "SOLID"))
         (add_list (strcat "  First Corner" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (add_list (strcat " Second Corner" (ddlist_str 11) ": " (gc_pts (dxf 11 ddlist_ent))))
         (add_list (strcat "  Third Corner" (ddlist_str 12) ": " (gc_pts (dxf 12 ddlist_ent))))
         (add_list (strcat " Fourth Corner" (ddlist_str 13) ": " (gc_pts (dxf 13 ddlist_ent))))
      )
      ((or (= ddlist_etype "ATTDEF") (= ddlist_etype "ATTRIB"))
         (add_list (strcat "   Start Point" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (add_list (strcat "        Height" (ddlist_str 40) ": " (rtos (dxf 40 ddlist_ent))))
         (add_list (strcat " Default Value" (ddlist_str 1) ": " (dxf 1 ddlist_ent)))
         (if (= ddlist_etype "ATTDEF")
            (add_list (strcat " Prompt String" (ddlist_str 3) ": " (dxf 3 ddlist_ent)))
         )   
         (add_list (strcat "    Tag String" (ddlist_str 2) ": " (dxf 2 ddlist_ent)))
         (add_list
            (strcat "  Field Length" (ddlist_str 73) ": "
               (if (dxf 73 ddlist_ent)
                  (itoa (dxf 73 ddlist_ent))
                  "0"
               )
            )
         )
         (add_list
            (strcat "Rotation Angle" (ddlist_str 50) ": "
               (if (dxf 50 ddlist_ent)
                  (angtos (dxf 50 ddlist_ent))
                  (angtos 0)
               )
            )
         )
         (add_list
            (strcat "X Scale Factor" (ddlist_str 41) ": "  
               (if (dxf 41 ddlist_ent)
                  (rtos (dxf 41 ddlist_ent))
                  (rtos 0)
               )
            )
         )
         (add_list
            (strcat " Oblique angle" (ddlist_str 51) ": "
               (if (dxf 51 ddlist_ent)
                  (angtos (dxf 51 ddlist_ent))  
                  (angtos 0)
               )
            )
         )
         (add_list 
            (strcat "    Text Style" (ddlist_str 7) ": "
               (if (dxf 7 ddlist_ent)
                  (dxf 7 ddlist_ent)
                  "STANDARD"
               )
            )
         )
      )
      ((or (= ddlist_etype "ARC") (= ddlist_etype "CIRCLE"))
         (add_list (strcat "        Center" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (add_list (strcat "        Radius" (ddlist_str 40) ": " (rtos (dxf 40 ddlist_ent))))
         (if (= ddlist_etype "ARC")
            (progn
               (add_list (strcat "   Start Angle" (ddlist_str 50) ": " (angtos (dxf 50 ddlist_ent))))
               (add_list (strcat "     End Angle" (ddlist_str 51) ": " (angtos (dxf 51 ddlist_ent))))
            )
         )
      )
      ((= ddlist_etype "DIMENSION")
         (add_list (strcat "    Block Name" (ddlist_str 2) ": " (dxf 2 ddlist_ent)))
         (add_list (strcat "    Style Name" (ddlist_str 3) ": " (dxf 3 ddlist_ent)))
         (add_list (strcat " Definition Pt" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (add_list (strcat "  Middle Point" (ddlist_str 11) ": " (gc_pts (dxf 11 ddlist_ent))))
         (if (/= (dxf 1 ddlist_ent) "")
            (add_list (strcat "          Text" (ddlist_str 1) ": " (dxf 1 ddlist_ent)))
         )
         (if (dxf 40 ddlist_ent)
            (add_list (strcat " Leader Length" (ddlist_str 40) ": " (rtos (dxf 40 ddlist_ent))))   
         )
         (if (dxf 50 ddlist_ent)
            (add_list (strcat "Rotation Angle" (ddlist_str 50) ": " (angtos (dxf 50 ddlist_ent))))
         )
         (if (dxf 52 ddlist_ent)
            (add_list (strcat "    Ext. Angle" (ddlist_str 52) ": " (angtos (dxf 52 ddlist_ent))))
         )
         (if (dxf 53 ddlist_ent)
            (add_list (strcat "    Text Angle" (ddlist_str 53) ": " (angtos (dxf 53 ddlist_ent))))
         )

      )
      ((= ddlist_etype "INSERT")
         (add_list (strcat "    Block Name" (ddlist_str 2) ": " (dxf 2 ddlist_ent)))
         (add_list (strcat "  Insertion Pt" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (if (dxf 41 ddlist_ent)
            (add_list (strcat "X Scale Factor" (ddlist_str 41) ": " (rtos (dxf 41 ddlist_ent))))
         )   
         (if (dxf 42 ddlist_ent)
            (add_list (strcat "Y Scale Factor" (ddlist_str 42) ": " (rtos (dxf 42 ddlist_ent))))
         )   
         (if (dxf 43 ddlist_ent)
            (add_list (strcat "Z Scale Factor" (ddlist_str 43) ": " (rtos (dxf 43 ddlist_ent))))
         )   
         (if (dxf 50 ddlist_ent)
            (add_list (strcat "Rotation Angle" (ddlist_str 50) ": " (angtos (dxf 50 ddlist_ent))))
         )   
         (if (dxf 70 ddlist_ent)
            (add_list (strcat "  Column Count" (ddlist_str 70) ": " (itoa (dxf 70 ddlist_ent))))
         )   
         (if (dxf 71 ddlist_ent)
            (add_list (strcat "     Row Count" (ddlist_str 71) ": " (itoa (dxf 71 ddlist_ent))))
         )   
         (if (dxf 44 ddlist_ent)
            (add_list (strcat "Column Spacing" (ddlist_str 44) ": " (rtos (dxf 44 ddlist_ent))))
         )   
         (if (dxf 45 ddlist_ent)
            (add_list (strcat "   Row Spacing" (ddlist_str 45) ": " (rtos (dxf 45 ddlist_ent))))
         )   
      )
      ((= ddlist_etype "LINE")
         (add_list (strcat "   Start Point" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (add_list (strcat "     End Point" (ddlist_str 11) ": " (gc_pts (dxf 11 ddlist_ent))))
         (add_list
            (strcat "  Angle Formed" (ddlist_str nil) ": "
               (angtos
                  (angle
                     (dxf 10 ddlist_ent)
                     (dxf 11 ddlist_ent)
                  )
               )
            )
         )   
         (add_list
            (strcat "        Length" 
               (ddlist_str nil)
               ": "
               (rtos
                  (distance
                     (dxf 10 ddlist_ent)
                     (dxf 11 ddlist_ent) 
                  )
               )
            )
         )
      )
      ((= ddlist_etype "POINT")
         (add_list (strcat "    Coordinate" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (if (dxf 50 ddlist_ent)
            (add_list (strcat "  X Axis Angle" (ddlist_str 50) ": " (angtos (dxf 50 ddlist_ent))))
         )
      )
      ((= ddlist_etype "POLYLINE")
         (add_list (strcat "     Elevation" (ddlist_str 10) ": " (rtos (caddr (dxf 10 ddlist_ent)))))
         (if (dxf 40 ddlist_ent)
            (add_list (strcat "Starting Width" (ddlist_str 40) ": " (rtos (dxf 40 ddlist_ent))))
         )
         (if (dxf 41 ddlist_ent)
            (add_list (strcat "  Ending Width" (ddlist_str 41) ": " (rtos (dxf 41 ddlist_ent))))
         )
      )
      ((= ddlist_etype "SHAPE")
         (add_list (strcat "  Insertion Pt" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (add_list (strcat "          Size" (ddlist_str 40) ": " (rtos (dxf 40 ddlist_ent))))
         (add_list (strcat "    Shape Name" (ddlist_str 2) ": " (dxf 2 ddlist_ent)))
         (if (dxf 50 ddlist_ent)
            (add_list (strcat "Rotation Angle" (ddlist_str 50) ": " (angtos (dxf 50 ddlist_ent))))
         )
         (if (dxf 41 ddlist_ent)
            (add_list (strcat "X Scale Factor" (ddlist_str 41) ": " (rtos (dxf 41 ddlist_ent))))
         )
         (if (dxf 51 ddlist_ent)
            (add_list (strcat " Oblique Angle" (ddlist_str 51) ": " (angtos (dxf 51 ddlist_ent))))
         )
      )
      ((= ddlist_etype "TEXT")
         (add_list (strcat "  Insertion Pt" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (add_list (strcat "        Height" (ddlist_str 40) ": " (rtos (dxf 40 ddlist_ent))))
         (add_list (strcat "          Text" (ddlist_str 1) ": " (dxf 1 ddlist_ent)))
         (if (dxf 50 ddlist_ent)
            (add_list (strcat "Rotation Angle" (ddlist_str 50) ": " (angtos (dxf 50 ddlist_ent))))
         )
         (if (dxf 41 ddlist_ent)
            (add_list (strcat "X Scale Factor" (ddlist_str 41) ": " (rtos (dxf 41 ddlist_ent))))
         )                                     
         (if (dxf 51 ddlist_ent)
            (add_list (strcat " Oblique Angle" (ddlist_str 51) ": " (angtos (dxf 51 ddlist_ent))))
         )
         (add_list
            (strcat "    Text Style" (ddlist_str 7) ": "
               (if (dxf 7 ddlist_ent)
                  (dxf 7 ddlist_ent)
                  "STANDARD"
               )
            )
         )
      ) 
      ((= ddlist_etype "VERTEX")
         (add_list (strcat "   Location Pt" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (if (dxf 40 ddlist_ent)
            (add_list (strcat "Starting Width" (ddlist_str 40) ": " (rtos (dxf 40 ddlist_ent))))
         )
         (if (dxf 41 ddlist_ent)
            (add_list (strcat "  Ending Width" (ddlist_str 41) ": " (rtos (dxf 41 ddlist_ent))))
         )
         (if (dxf 42 ddlist_ent)
            (add_list (strcat "         Bulge" (ddlist_str 42) ": " (rtos (dxf 42 ddlist_ent))))
         )
      )
      ((= ddlist_etype "VIEWPORT")
         (add_list (strcat "     Center Pt" (ddlist_str 10) ": " (gc_pts (dxf 10 ddlist_ent))))
         (add_list (strcat "         Width" (ddlist_str 40) ": " (rtos (dxf 40 ddlist_ent))))
         (add_list (strcat "        Height" (ddlist_str 41) ": " (rtos (dxf 41 ddlist_ent))))
      )  
   )

   ;stop the list box
   (end_list)

   ;highlight the nested option if present
   (if (= #ddlist_n "1")
      (set_tile "nested" "1")
   )

   ;set the list again to nil
   (setq ddlist_again nil)

   (action_tile "accept" "(setq #ddlist_loc (done_dialog))")
   (action_tile "nested" "(setq #ddlist_n $value)")
   (action_tile "again"  "(setq ddlist_again T #ddlist_loc (done_dialog))")

   (start_dialog)
   (unload_dialog id)
   (redraw ddlist_name 4)

   ;do it again if necessary
   (if ddlist_again
      (if ddlist_flag
         (ddlist T)
         (C:DDLIST)
      )
   )

   (setq *error* *olderror*)

   (prin1)
)  
(princ "DDLIST Loaded.")
(princ)

