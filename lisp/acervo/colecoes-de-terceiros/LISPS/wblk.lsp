;;; -*-  Mode: LISP -*- (C) Ben Olasov 1991
;;; Writes all blocks references in drawing to specified directory.
;;; DOS version

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; File: WBLCK.LSP     Copyright (C) Ben Olasov 1991  All Rights Reserved  ;;;
;;; Inquiries:                                                              ;;;
;;;                                                                         ;;;
;;;       Ben Olasov     Lispenard Technologies                             ;;;
;;;                      New York, NY                                       ;;;
;;;                                                                         ;;;
;;;                      Voice:    (212) 274-8506                           ;;;
;;;                      FAX:      (212) 979-3686                           ;;;
;;;                      Arpanet:  olasov@cs.columbia.edu                   ;;;
;;;                      Internet: ben@syska.com                            ;;;
;;;                                                                         ;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

(VMON)
(gc)

(princ "\nLoading- please wait...")

;; creates wblocks in user-specified path of all blocks in drawing
(defun c:wblk (/ dwgpfx blks tmp foo)
      (setq cmdecho (getvar "cmdecho")
            dwgpfx (getvar "dwgprefix")
            output_path (parse_path (userstr (if output_path output_path dwgpfx)
                                         "\nOutput blocks to which directory")))
      (setvar "cmdecho" 0)
      (setq blks (cdr (assoc 2 (tblnext "BLOCK" T)))
            blks (list (cdr (assoc 2 (tblnext "BLOCK"))) blks))
      (while (setq tmp (tblnext "BLOCK")) 
             (setq blks (cons (cdr (assoc 2 tmp)) blks)))
      (foreach X (clean_blklist blks)
               (if (and (<= (strlen X) 8) (/= (substr x 1 1) "*"))
                   (progn (setq foo (open (strcat output_path x ".dwg") "r"))
                          (if foo (progn (close foo)
                                         (princ (strcat "\nDrawing "
                                                        output_path
                                                        X
                                                        " already exists!")))
                                  (progn (princ (strcase (strcat "\nWriting " output_path X ".dwg") t))
                                         (command "wblock" (strcat output_path X) X))))))
      (setvar "cmdecho" cmdecho)
       'done)

;; get a user string with default
(defun userstr (dflt prmpt / var) ;;DFLT and PRMPT are strings
       (setq var (getstring (if (and dflt (/= dflt ""))
                                (strcat prmpt " <" dflt ">: ")
                                (strcat prmpt ": "))))
       (cond ((/= var "") var)
             ((and dflt (= var "")) dflt)
             (T "")))

;; parse a user's path response
(defun parse_path (s / STRL FIRSTC SECONDC LASTC)
       (cond ((null s) nil) ;; is S bound?
             ((= s "") s)   ;; is S an empty string?
             (T (setq STRL (strlen s)
                      FIRSTC (substr s 1 1)
                      SECONDC (substr s 2 1)
                      LASTC (substr s STRL 1))
                (cond ((= STRL 1)  ;; if S has only one character
                       (if (or (= FIRSTC "/")   ;; and the 1st char is "/"
                               (= FIRSTC "\\")) ;; or "\\"
                           "\\"                 ;; return the 1st char
                           (strcat DWGPFX S "\\"))) ;; otherwise prepend DWGPFX
                                                   ;; and append a "\\"
                      ((or (and (= FIRSTC "/")  ;; if the user pathname
                                (= LASTC "/"))  ;; looks superficially
                           (and (= FIRSTC "\\") ;; well-formed, return it.
                                (= LASTC "\\"))) S)
                      ((and (/= FIRSTC "/")
                            (/= FIRSTC "\\"))  ;; the 1st char isn't /
                       (cond ((= SECONDC ":")  ;; is it a drive spec?
                              (if (and (/= LASTC "/") ;; make sure there's
                                       (/= LASTC "\\")) ;; a slash on the end
                                  (strcat S "\\")
                                  S))
                             ((and (/= LASTC "/")
                                   (/= LASTC "\\"))
                              (strcat DWGPFX S "\\"))))
                      (T s)))))

;; removes atom ATM from list of unique atoms LST
(defun aux_remove (atm lst) 
     (cond ((null lst) NIL) 
           ((null (member atm lst)) lst) 
           ((equal atm (car lst)) 
            (cdr lst)) 
           (t (append (reverse (cdr (member atm (reverse lst))))
                      (cdr (member atm lst)))))) 

;; removes HATCH references and blocks with names longer than 8 chars
(defun clean_blklist (blklist / bl)
       (setq bl blklist)
       (if (and bl (listp bl))
           (foreach blk bl
                    (if (or (null blk)
                            (= (substr blk 1 1) "*")
                            (> (strlen blk) 8))
                        (progn (princ (strcat
             "\nRemoving " blk " from block list."))
                               (setq bl (aux_remove blk bl))))))
       bl)

(princ "\nType WBLK to write out all block references to a user-specified directory.")
(princ)
