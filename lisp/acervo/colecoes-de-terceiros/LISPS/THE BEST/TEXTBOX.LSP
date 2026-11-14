(princ "\n        Digite \"TB\"")
;;; TBOX2.LSP
;     Copyright (C) 1992 by Autodesk, Inc.
;
;     Permission to use, copy, modify, and distribute this software 
;     for any purpose and without fee is hereby granted, provided 
;     that the above copyright notice appears in all copies and that 
;     both that copyright notice and this permission notice appear in 
;     all supporting documentation.
;
;     THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED
;     WARRANTY.  ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR
;     PURPOSE AND OF MERCHANTABILITY ARE HEREBY DISCLAIMED.
;     ****************************************************************
;;;
;;; Demonstrates the (textbox) function by drawing a Polyline box around
;;;   a selected text entity.
;;; This example differs from TBOX.LSP by using the (sin) and (cos) functions
;;;   to calculate the required angles.
;;; See "Text Box Utility Function" in chapter 2 of the AutoLISP Programmer's
;;;   Reference (Release 12)
(defun C:TB ( / textent ang sinrot cosrot t1 t2 p0 p1 p2 p3 p4)
  (setq textent (entget (car (entsel "\nSelect text: "))))
  (setq p0 (cdr (assoc 10 textent))
        ang (cdr (assoc 50 textent))
        sinrot (sin ang)
        cosrot (cos ang)
        t1 (car (textbox textent))
        t2 (cadr (textbox textent))
        p1 (list (+ (car p0)
                    (- (* (car t1) cosrot) (* (cadr t1) sinrot)))
                 (+ (cadr p0)
                    (+ (* (car t1) sinrot) (* (cadr t1) cosrot))))
        p2 (list (+ (car p0)
                    (- (* (car t2) cosrot) (* (cadr t1) sinrot)))
                 (+ (cadr p0)
                    (+ (* (car t2) sinrot) (* (cadr t1) cosrot))))
        p3 (list (+ (car p0)
                    (- (* (car t2) cosrot) (* (cadr t2) sinrot)))
                 (+ (cadr p0)
                    (+ (* (car t2) sinrot) (* (cadr t2) cosrot))))
        p4 (list (+ (car p0)
                    (- (* (car t1) cosrot) (* (cadr t2) sinrot)))
                 (+ (cadr p0)
                    (+ (* (car t1) sinrot) (* (cadr t2) cosrot))))
  )
  (command "pline" p1 p2 p3 p4 "c")
  (princ)
)

