;;ษออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออป
;;บProgram name:       DDTIME.LSP                                            บ
;;บInitial Author:     Michael Jenkins                                       บ
;;บDescription:        This is a dialog box for setting the time elapsed     บ
;;บ                    between automatic saves. There is no provision for    บ
;;บ                    disabling automatic saves to encourage use of this    บ
;;บ                    feature.                                              บ
;;ศออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออผ
;;
;

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
      (ai_abort "DDTIME"
         (strcat "Can't locate file AI_UTILS.LSP."
   "\n Check support directory.")))

   (  (eq "failed" (load "ai_utils" "failed"))            ; load it
   (ai_abort "DDTIME" "Can't load file AI_UTILS.LSP"))
)

(if (not (ai_acadapp))               ; defined in AI_UTILS.LSP
   (ai_abort "DDTIME" nil)        ; a Nil <msg> supresses
)                                    ; ai_abort's alert box dialog.

;;; ==================== end load-time operations ===========================

(defun C:DDTIME (/ timer timer_update id ddtime_list)
   
   (setq ddtime_list '(15 30 45 60 120))

   ;update when OK
   (defun timer_update()
      (done_dialog)
      (setvar "savetime" timer)
   )

   ;initialize dialog
   (setq id (load_dialog "ddtime.dcl"))
   (new_dialog "ddtime" id)

   ;set default time if no time is set
   (set_tile "15" "1")

   ;set default time if it matches
   (if (/= (member (getvar "savetime") ddtime_list) nil)
      (progn
         (set_tile (itoa (getvar "savetime")) "1")
         (setq timer (getvar "savetime"))
      )   
      (setq timer 15)
   )

   ;set callback actions
   (mapcar
      '(lambda (x)
         (action_tile (itoa x) (strcat "(setq timer " (itoa x) ")"))
       )
       ddtime_list
   )
   (action_tile "accept" "(timer_update)")
   (action_tile "help" "(acad_helpdlg \"ddtime\" \"ddtime\")")

   (start_dialog)
   (unload_dialog id)
   (prin1)
)
;;============================ THE END =======================================
(princ "DDTIME Loaded.")
(princ)










