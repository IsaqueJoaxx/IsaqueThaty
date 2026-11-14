;;; -------------------------------------------------------------------------
;;; File    : 2SORTS.LSP
;;; Purpose : Defines two generic sort functions, (selsort) and (qsort).
;;; Author  : Steve Johnson, some portions by Stephen Bradshaw.
;;; Date    : 10 February 1994 (compiled from earlier files)
;;; Notes   : In theory, (qsort) should be fastest, particularly on larger
;;;           lists.  In practice, exactly the opposite is true.  My tests
;;;           indicate (selsort) is typically twice as fast as (qsort).
;;;           For serious speed, an ADS routine should be used.  The Release
;;;           12 (acad_strlsort) function can be used for strings, but nasty
;;;           restrictions, automatic conversions and a bug in the AutoLISP to
;;;           ADS interface make it impossible to write a comprehensive generic
;;;           AutoLISP sort function in ADS.  This file also serves as an
;;;           example of an AutoLISP routine which cannot be successfully
;;;           converted to ADS by any automatic conversion software.
;;; -------------------------------------------------------------------------
;;; Function definition structure:
;;;  selsort
;;;  ÀÄÄdelnth
;;;  qsort
;;;  ÃÄÄsort
;;;  ÀÄÄsubnth
;;; -------------------------------------------------------------------------


;; --------------------------------------------------------------------------
;; Function: SELSORT
;; Method  : Straight selection sort, creates an enlarging copy of list as
;;           original list has highest elements removed from it.
;; Call    : (selsort LST FUN)
;; Params  : LST: list to sort
;;           FUN: name of function to apply to test for "lower": could be a
;;                built-in function (e.g. >) or a user defined function,
;;                possibly defined on the spot using lambda.
;; Local   : size:   number of elements in list
;;           #:      element counter
;;           pos:    position of highest element
;;           high:   value of highest element
;;           item:   current element
;;           tmplst: temporary list, used to build final list
;; Examples: (selsort '(4 3 5 2 1 0) '<) ; ascending sort
;;         => (0 1 2 3 4 5)
;;           (selsort '(4 3 5 2 1 0) '>) ; descending sort
;;         => (5 4 3 2 1 0)
;;           (selsort                    ; ascending sort on 1st elements
;;             '((4 "z") (3 "a") (5 "q") (2 "s") (1 "g") (0 "w"))
;;             '(lambda (X Y) (> (car X) (car Y))) )
;;         => ((0 "w") (1 "g") (2 "s") (3 "a") (4 "z") (5 "q"))
;;           (selsort
;;             '((2 "a") (3 "a") (1 "z") (2 "c") (2 "b") (3 "w"))
;;             '(lambda (X Y)
;;               (if (= (car X) (car Y)) ; if 1st elements are equal
;;                 (> (cadr X) (cadr Y)) ;  then descending sort on 2nd elem's
;;                 (< (car X) (car Y))   ;  else ascending sort on 1st elem's
;;               )
;;             )
;;           )
;;         => ((1 "z") (2 "c") (2 "b") (2 "a") (3 "w") (3 "a"))
;; --------------------------------------------------------------------------

(defun selsort (LST FUN / delnth size # pos high item tmplst)


;; --------------------------------------------------------------------------
;; Function: DELNTH
;; Purpose : Returns a list with the Nth item deleted
;; Params  : N:    index number of item to delete (0 is first item in list)
;;           LST:  list to delete from
;; Local   : #:    item counter
;;           tlst: temporary list
;; Examples: (delnth 9 '(1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6))
;;           returns => (1 2 3 4 5 6 7 8 9 1 2 3 4 5 6)
;;           (delnth 9 '(1 2 3 4))  => (1 2 3 4)
;;           (delnth 9 nil)         => nil
;; --------------------------------------------------------------------------

  (defun delnth (N LST / # tlst)
    (setq # 0)
    (while (and LST (/= # N))
      (setq
        tlst (cons (car LST) tlst)
        LST (cdr LST)
        # (1+ #)
      )
    )
    (reverse (append (reverse (cdr LST)) tlst))
  ) ; End selsort.delnth


;; Start selsort ----------------------

;;; ********************************
  (if (equal (type FUN) 'STR)
    (setq FUN (read FUN)))

  (repeat (1- (setq size (length LST)))
    (setq
      #   1             ; Position of 1st item to compare with highest item
      pos 0             ; Position of initial highest item
      high (nth 0 LST)  ; Value of initial highest item
    )
    (while (< # size)   ; Walk through the whole list, finding the highest item
      (if (apply FUN (list high (setq item (nth # LST)))) ; Test for higher
        (setq           ; Found a higher item, so record its:
          high item     ;  value,
          pos  #        ;  position
        )
      )
      (setq # (1+ #))   ; Test next item
    )
    (setq
      tmplst (cons high tmplst)  ; Stick highest item at start of temp list
      LST    (delnth pos LST)    ; Delete highest item from main list,
      size   (- size 1)          ;  and reduce size of main list
    )
  )
  (cons (car LST) tmplst)        ; Add last (lowest) item to start and return
) ; End selsort


;; --------------------------------------------------------------------------
;; Function: QSORT
;; Method  : Quicksort.
;; Params  : See (selsort).
;; Examples: As per (selsort).
;; --------------------------------------------------------------------------

(defun qsort (LST FUN / sort subnth)


;; --------------------------------------------------------------------------
;; Function: SORT
;; Purpose : Recursive part of quicksort routine.
;; Params  : L, R: index numbers indicating left and right ends of sublist
;;                 of LST to sort using FUN.
;; Locals  : i, j: index numbers of "end" elements to sort
;;           x:    index number of "middle" element
;;           loop: boolean - T while sorting still required
;; --------------------------------------------------------------------------

  (defun sort (L R / i j x loop)
    (setq
      i L
      j R
      x (nth (/ (+ L R) 2) LST)
      loop T
    )
    (while loop
      (while (apply FUN (list (nth i LST) x))
        (setq i (1+ i))
      )
      (while (apply FUN (list x (nth j LST)))
        (setq j (1- j))
      )
      (if (<= i j)
        (setq
          LST (subnth (nth i LST) j (subnth (nth j LST) i LST))
          i (1+ i)
          j (1- j)
        )
      )
      (setq loop (<= i j))
    )
    (if (< L j) (sort L j))
    (if (< i R) (sort i R))
  ) ; End qsort.sort


;; --------------------------------------------------------------------------
;; Function: SUBNTH
;; Purpose : Returns a list with the item at the Nth position substituted
;;           with a new item.
;; Params  : ITEM: item to insert
;;           N:    index number of item position (0 is first item in list)
;;           LST:  list to act on
;; Local   : #:    item counter
;;           tlst: temporary list
;; Examples: (subnth 999 9 '(1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6))
;;           returns     => (1 2 3 4 5 6 7 8 9 999 1 2 3 4 5 6)
;;           (subnth 999 9 '(1 2 3 4))  => (1 2 3 4 999)
;;           (subnth 999 9 nil)         => (999)
;; --------------------------------------------------------------------------

  (defun subnth (ITEM N LST / # tlst)
    (setq # 0)
    (while (and LST (/= # N))
      (setq
        tlst (cons (car LST) tlst)
        LST (cdr LST)
        # (1+ #)
      )
    )
    (append (reverse tlst) (list ITEM) (cdr LST))
  ) ; End qsort.subnth


;; Start qsort ------------------------

;;; ********************************
  (if (equal (type FUN) 'STR)
    (setq FUN (read FUN)))

  (sort 0 (1- (length LST)))
  LST
) ; End qsort

;;; -------------------------------------------------------------------------
;;;  Copyright: (c) Water Authority of Western Australia 1990, 1994.
;;;
;;;  Disclaimer: This software is provided "as is" without express or
;;;  implied warranty.  All implied warranties of fitness for any particular
;;;  purpose and of merchantability are hereby disclaimed.  If it eats your
;;;  drawings, tough luck.
;;;
;;;  Permission notice: Permission to use, copy, modify and distribute this
;;;  software for any purpose and without fee is hereby granted, provided
;;;  that it is distributed with the above copyright notice, disclaimer and
;;;  this permission notice.
;;;
;;; End 2SORTS.LSP ----------------------------------------------------------
