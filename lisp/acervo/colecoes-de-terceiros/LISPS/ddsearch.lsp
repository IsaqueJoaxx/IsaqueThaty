;this is a AutoLISP routine for implementing a search and replace
;command in AutoCAD Release 12. It takes advantage of the dialogs
;in Release 12 to make it easier to use. This is a good replacement
;for the old CHGTEXT command.
;written by:
;Michael Jenkins
;Gray Construction Co.
;Lexington, Kentucky

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
      (ai_abort "DDCHPROP"
         (strcat "Can't locate file AI_UTILS.LSP."
   "\n Check support directory.")))

   (  (eq "failed" (load "ai_utils" "failed"))            ; load it
   (ai_abort "DDCHPROP" "Can't load file AI_UTILS.LSP"))
)

(if (not (ai_acadapp))               ; defined in AI_UTILS.LSP
   (ai_abort "DDCHPROP" nil)        ; a Nil <msg> supresses
)                                    ; ai_abort's alert box dialog.

;;; ==================== end load-time operations ===========================


;this is a metering prompt
(defun gc_meter (gcm_pr gc_num gc_max)
   (prompt 
      (strcat "\r" gcm_pr " ("
         (rtos(*(/(float(1+ gc_num))(float gc_max))100)2 0)
         "%)"
      ) ;strcat
   ) ;prompt   
) ;defun

(defun C:DDSEARCH
   (/ mode replace_loc _accept _replace text text_string
      index pointer sset id text_string_length *olderror*
   )

   ;process an ok
   (defun _accept ()
      (cond  
         (
            (or 
               (= 
                  (get_tile "search")
                  ""
               )
               (= 
                  (get_tile "replace")
                  ""
               )
            )  
            (set_tile "error" "Empty or invalid input")
         )
         (
            (=
               (get_tile "search")
               (get_tile "replace")
            )
            (set_tile "error" "Search and replace are identical")
         )    
         (T
            (setq #search_string (get_tile "search"))
            (setq #replace_string (get_tile "replace"))
            (setq #case_sensitive (get_tile "case"))
            (setq #global (get_tile "global"))
            (done_dialog 1)
         )   
      )  
   )

   ;function for prompt to replace
   (defun _replace ()
      (new_dialog "ddsearch2" id "" replace_loc)
      (action_tile "cancel" "(done_dialog)(exit)")
      (action_tile "accept" "(setq replace_loc (done_dialog 1))")
      (action_tile "skip"   "(setq replace_loc (done_dialog 0))")
      (action_tile "auto"   "(done_dialog 2)")
      (set_tile
         "error" 
         (strcat 
            (rtos (1+ index) 2 0)
            " of "
            (rtos (sslength sset) 2 0)
         )
      )
      (start_dialog)
   )  

   ;set up the dialog identification
   (setq id (load_dialog "ddsearch"))

   ;open dialog and store location as a global
   (new_dialog "ddsearch" id)

   ;set those defaults
   (if
      #search_string
      (set_tile "search" #search_string)
   )
   (if 
      #replace_string
      (set_tile "replace" #replace_string)
   )
   (if
      #case_sensitive
      (set_tile "case" #case_sensitive)
   )
   (if
      #global
      (set_tile "global" #global)
   )

   ;set up callbacks
   (action_tile "accept"  "(_accept)")

   ;process the look for the callbacks
   (if
      ;make changes if ok is picked
      (=
         (start_dialog)
         1
      )
      (progn
         ;get a selection set if not global
         (if (/= #global "1")
            (while
               (=
                  (setq sset (ssget (list (cons 0 "TEXT"))))
                  nil
               )
               (prompt "\nNo entities selected")
            )
            (setq sset (ssget "X"(list(cons 0 "TEXT"))))
         )
         (setq index 0)
         ;go through the selection set
         (while
            (/=
               (setq text (ssname sset index))
            nil)
            ;go through each string
            (setq pointer 1)
            (setq text_string (cdr (assoc 1 (entget text))))
            (if 
               (<=
                  (strlen #search_string)
                  (strlen text_string)
               )
               (progn
                  (setq text_string_length (strlen text_string))
                  ;go until you reach the end of the string
                  (while
                     (< 
                        pointer
                        (+ (- text_string_length (strlen #search_string)) 2)
                     )
                     (if 
                        (= 
                           (if
                              (= #case_sensitive "1")
                              (substr
                                 text_string
                                 pointer
                                 (strlen #search_string)
                              )
                              (strcase 
                                 (substr
                                    text_string 
                                    pointer 
                                    (strlen #search_string)
                                 )
                              )
                           )
                           (if (= #case_sensitive "1")
                              #search_string
                              (strcase #search_string)
                           )
                        )
                        (progn
                           (redraw text 3)
                           (if (/= mode 2)
                              (if (/= #global "1")
                                 (setq mode (_replace))
                                 (setq mode 2)
                              )
                           )   
                           (if (> mode 0)
                              (progn
                                 (setq text_string
                                    (if (= pointer 1)
                                       (strcat
                                          #replace_string
                                          (substr
                                             text_string
                                             (+ pointer (strlen #search_string))
                                          )
                                       )
                                       (strcat 
                                          (substr 
                                             text_string
                                             1
                                             (- pointer 1)
                                          )
                                          #replace_string
                                          (substr
                                             text_string
                                             (+ pointer (strlen #search_string))
                                          )
                                       )
                                    )
                                 )
                                 (entmod 
                                    (subst 
                                       (cons 1 text_string)
                                       (assoc 1 (entget text))
                                       (entget text)
                                    )
                                 )
                                 (setq 
                                    pointer 
                                    (+ pointer (strlen #replace_string))
                                 )
                                 (setq
                                    text_string_length
                                    (strlen text_string)
                                 )
                              )
                           )
                           (redraw text 4)
                           (setq pointer (1+ pointer))
                        )
                        (setq pointer (1+ pointer))
                     )
                  )
               )
            )
            (gc_meter "Changing text" index (sslength sset))
            (setq index (1+ index))
         )
      )
   )
   (unload_dialog id)
   (setq *error* *olderror*)
   (princ)
) ;defun ddsearch 
(princ "DDSEARCH Loaded.")                   
(princ)
