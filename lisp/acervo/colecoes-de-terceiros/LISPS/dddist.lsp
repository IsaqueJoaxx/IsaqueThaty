;;ษออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออป
;;บProgram name:       DDDIST.LSP                                            บ
;;บInitial Author:     Michael Jenkins                                       บ
;;บDescription:        This is a dialog box for obtaining a distance. It     บ
;;บ                    improves on the DIST commands in three ways. First, itบ
;;บ                    checks each point and does not restart if an invalid  บ
;;บ                    point is input. Second, it avoids having to flip to   บ
;;บ                    the text screen to get a distance. Finally, it allows บ
;;บ                    several distances to be obtained without having to    บ
;;บ                    reissue the command.                                  บ
;;ศออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออผ

;;; ===================== load-time error checking ============================
;;;

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
      (ai_abort "DDDIST"
         (strcat "Can't locate file AI_UTILS.LSP."
   "\n Check support directory.")))

   (  (eq "failed" (load "ai_utils" "failed"))            ; load it
   (ai_abort "DDDIST" "Can't load file AI_UTILS.LSP"))
)

(if (not (ai_acadapp))               ; defined in AI_UTILS.LSP
   (ai_abort "DDDIST" nil)         ; a Nil <msg> supresses
)                                    ; ai_abort's alert box dialog.


(defun C:DDDIST (/ dddist_pt1 dddist_pt2 id ddlist_again)

   (setq *olderror* *error*)
   (defun *error* (msg)
      (princ msg)
      (setq 
         *error* *olderror*
         *olderror* nil
      )
      (princ)
   )
   
   ;go until they pick first point
   (while (=
         (setq
            dddist_pt1
            (getpoint "\nFirst point: ")
         )
         nil
      )
      (prompt "\nInvalid point.")
   )

   ;get second point with rubberband from first
   (while (=
         (setq
            dddist_pt2
            (getpoint dddist_pt1 "\nSecond point: ")
         )
         nil
      )
      (prompt "\nInvalid point.")
   )

   ;set up the dialog identification
   (setq id (load_dialog "dddist"))

   ;open dialog and store location as a global
   (new_dialog "dddist" id "" #dddist_loc)

   ;set up the 2-D option
   (if (/= #dddist_2d "1")
      (set_tile "distance"
         (strcat "         Distance: "
            (rtos (distance dddist_pt1 dddist_pt2))
         )
      )
      (set_tile "distance"
         (strcat "    Distance (2D): "
            (rtos (distance 
                  (list (car dddist_pt1) (cadr dddist_pt2) 0)
                  (list (car dddist_pt2) (cadr dddist_pt2) 0)
               )
            )
         )
      )
   )

   ;tile setup
   (set_tile "angle"
      (strcat "Angle in XY Plane: "
         (angtos (angle
               (list (car dddist_pt1) (cadr dddist_pt1) 0)
               (list (car dddist_pt2) (cadr dddist_pt2) 0)
            )
         )
      )
   )
   (set_tile "deltax"
      (strcat "          Delta X: "
         (rtos (distance
               (list (car dddist_pt1) 0 0)
               (list (car dddist_pt2) 0 0)
            )
         )
      )
   )
   (set_tile "deltay"
      (strcat "          Delta Y: "
         (rtos (distance
               (list 0 (cadr dddist_pt1) 0)
               (list 0 (cadr dddist_pt2) 0)
            )
         )
      )
   )
   (set_tile "deltaz"
      (strcat "          Delta Z: "
         (rtos (distance
               (list 0 0 (caddr dddist_pt1))
               (list 0 0 (caddr dddist_pt2))
            )
         )
      )
   )

   ;reset to nil for check later
   (setq dddist_again nil)

   ;highlight the 2D option if present
   (if (= #dddist_2D "1")
      (set_tile "2d" "1")
   )

   ;set up callbacks
   (action_tile "accept"  "(setq #dddist_loc (done_dialog))")
   (action_tile "2d"      "(setq #dddist_2d $value)")
   (action_tile "again"   "(setq dddist_again T #dddist_loc (done_dialog))")

   (start_dialog)
   (unload_dialog id)

   ;do it again if necessary
   (if dddist_again(C:DDDIST))
   (prin1)
) ;defun dddist  
;;================================ The End ===================================
(princ "DDDIST Loaded.")                   
(princ)
