;;; Peditlt: pedit linetype regen of polylines 
;;;
;;; Guido M Haarmans, Neuchatel Switzerland, July 4, 1994.
;;; CIS account 73324,1133
;;;
;;; Peditlt, set linetype regen on/off for a selection set of polylines
;;; (non-polylines are automatically rejected)
;;;
;;; Freeware, permission to modify and distribute as long as you
;;; leave my credit in, and don't charge for this software.

;peditlt error handler
   (defun aesc_peditlt_myerror (msg)
    (princ (strcat "\nPeditlt: " msg))
    (setq *error* aesc_peditlt_olderror) 
    (list (setvar "cmdecho" aesc_peditlt_cmdo))   
    (princ "\n")(princ) 
   )

; ---------------------------------------------------------------

(defun c:peditlt ( / onoff sset setlen cnt 
                     entlist lt ltvalue newlt newlist ent)
   ;get cmdecho and set to 0
   (setq aesc_peditlt_cmdo (getvar "cmdecho"))
   (setvar "cmdecho" 0)
   
   ;error handler
   (setq aesc_peditlt_olderror *error*)
   (setq *error* aesc_peditlt_myerror)
   
   ;get selection set of polylines
   (initget 1 "on off")
   (setq onoff (getkword "\nSet linetype regen to ON or OFF > "))
   (setq onoff (strcase onoff))

   (setq sset (ssget))                     ;selecion set
   (setq setlen 0)                         ;initiate setlen
   (if sset (setq setlen (sslength sset))) ;number of entities in set
   (setq cnt 0)                            ;counter for selection set
       
   (princ "\nChanging polylines linetype regen, please wait. ")
   (while (< cnt setlen)
       ;get next entity in selection set
       (setq ent (ssname sset cnt))
       (setq cnt (1+ cnt))
       (setq entlist (entget ent))
       (if (= (cdr (assoc 0 entlist)) "POLYLINE") ; is a pline
         (progn
           'check current state of linetype regen setting
           (setq lt (assoc 70 entlist))
           (setq ltvalue (cdr lt))

           'change setting if necessary
           (if (= onoff "OFF") 
               ;off, turn of if currently on
               (if (= (logand ltvalue 128) 128)
                 (progn
                 (setq newlt (cons 70 (- ltvalue 128)))
                 (setq newlist (subst newlt lt entlist))
                 (entmod newlist)(entupd ent)
                 )
               );end if logand

               ;on, turn off if currently on
               (if (= (logand ltvalue 128) 0)
                 (progn
                 (setq newlt (cons 70 (+ ltvalue 128)))
                 (setq newlist (subst newlt lt entlist))
                 (entmod newlist) (entupd ent)
                 )
               );end if logand

            );end if onoff
          );end progn if pline                  
        );end if pline
    );end while
     
    (setvar "cmdecho" aesc_peditlt_cmdo)
    (prompt "\nDone.")
    (setq *error* aesc_peditlt_olderror)
    (princ)

); end defun peditlt
;--------------------------------------------------------------------

(prompt "\nPeditlt loaded, type PEDITLT to start command.")(princ)