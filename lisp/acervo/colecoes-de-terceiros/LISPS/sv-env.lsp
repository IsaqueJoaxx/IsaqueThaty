;;; -*-  Mode: LISP -*- Syntax: AutoLISP (C) Benjamin Olasov 1988
;;;      environment writing function **Release 9**

;;; -*-  Mode: LISP -*- Syntax: AutoLISP (C) Benjamin Olasov 1988, 1989

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; File: SV-ENV.LSP Copyright (C) Benjamin Olasov    Graphic Systems, Inc. ;;;
;;; Inquiries:                                                              ;;;
;;;                                                                         ;;;
;;;     Benjamin Olasov                                                     ;;;
;;;     Graphic Systems, Inc.:                                              ;;;
;;;                                                                         ;;;
;;;                    New York, NY:   PH (212) 725-4617                    ;;;
;;;                    Cambridge, MA:  PH (617) 492-1148                    ;;;
;;;                    MCI-Mail:       GSI-NY   344-4003                    ;;;
;;;                    Arpanet:        olasov@cs.columbia.edu               ;;;
;;;                                                                         ;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;; This program is provided 'as is' without warranty of any kind, either 
;; expressed or implied, including, but not limited to the implied warranties of
;; merchantability and fitness for a particular purpose.  The entire risk as to
;; the quality and performance of the program is with the user.  Should the 
;; program prove defective, the user assumes the entire cost of all necessary 
;; servicing, repair or correction. 
;;
;; AutoLisp and AutoCad are registered trademarks of AutoDesk, Inc.

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; This function C:SV-ENV saves the current LISP environment into a file with
;; the name of the drawing and with the extension .LSP.  For example, if the
;; name of the current drawing whose LISP environment you wish to save is
;; FOOBAR.DWG, then the name of the LISP file to which the environment is saved
;; will be FOOBAR.LSP.
;; For this function to work at all, you must have placed a marker in your
;; ATOMLIST (AutoLISP's record of all LISP variables, functions, etc.).
;; To do this, simply place the following line at the end of your ACAD.LSP file:
;; (setq MARK nil)
;; That's all.  Then the SV-ENV function will not attempt to record 
;; AutoLISP's primitive functions, or the functions automatically loaded with
;; your ACAD.LSP.
;;
;; To restore your previous environment of variables and user functions, just
;; type (load "dwgname") at the command prompt, where "dwgname" is the name
;; of the drawing whose LISP environment you saved.

;; This is a beta version, which I hope to improve soon.
;; If you have ideas on how to improve this, please don't just munge this file
;; and redistribute it- let me hear your ideas.  If they would create errors
;; of which I'm aware, I'll let you know- if not, I'll include them in a 
;; revision. 

(defun C:SV-ENV (/ usr-atoms dwgname file-spec)
      (if (setq usr-atoms (cdr (member 'MARK (reverse ATOMLIST))))
          (progn
              (princ usr-atoms) (terpri)
              (setq dwgname (getvar "dwgname") counter 0)
              (setq file-spec (open (strcat dwgname ".lsp") "w"))
              (foreach atm usr-atoms
                       (write-atom atm file-spec))
              (close file-spec)
              (princ "\nWrote ")
              (princ counter)
              (princ " expressions to ")
              (princ (strcase dwgname))
              (princ ".LSP.")
              (princ))
         (if (null ATOMLIST)
             (prompt "\nCan't evaluate ATOMLIST.")
             (prompt "\nNo marker present in atomlist."))))

(defun write-atom (atm filespec)                   ;; beta version
       (cond  ((or (= (type (eval atm)) 'LIST)
                   (= (type (eval atm)) 'ENAME))
               (princ (strcat (chr 40) "setq ") filespec)
               (prin1 atm filespec)
               (princ (strcat " " (chr 39)) filespec)
               (prin1 (eval atm) filespec)
               (princ (strcat (chr 41) "\n") filespec)
               (setq counter (1+ counter)))
              ((or (= (type (eval atm)) 'SYM)
                   (= (type (eval atm)) 'INT)
                   (= (type (eval atm)) 'REAL)
                   (= (type (eval atm)) 'STR)
                   (= (type (eval atm)) 'PAGETB))
               (princ (strcat (chr 40) "setq ") filespec)
               (prin1 atm filespec)
               (princ " " filespec)
               (prin1 (eval atm) filespec)
               (princ (strcat (chr 41) "\n") filespec)
               (setq counter (1+ counter)))
              ((null (eval atm))
               (princ "\nIgnoring null expression ") (princ atm))
              (T (princ "\nI don't know how to handle ")
                 (princ atm))))

(setq MARK 'T)    ;; this places a marker in ATOMLIST for 
                   ;; later saving LISP environment

(setvar "cmdecho" 0)
(command "textscr")
(repeat 24 (terpri))
(princ "\nFor this function to work at all, you must have placed a marker in your")
(princ "\nATOMLIST (AutoLISP's record of all LISP variables, functions, etc.).")
(princ "\nTo do this, simply place the following line at the end of your ACAD.LSP file:")
(princ "\n\(setq MARK 'T\)")
(princ "\nTo use SV-ENV, type SV-ENV at the command prompt.")
(princ)


