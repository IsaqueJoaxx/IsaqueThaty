;;; --------------------------------------------------------------------------
;;; File     : SORTTEST.LSP
;;; Author   : Steve Johnson
;;; Date     : 3 March 1994
;;; Purpose  : Performs simple tests on AutoLISP and ADS sort routines.
;;; Notes    : Designed to run in a simple test environment in a throwaway
;;;            drawing.  Draws a line to make sure it has an entity.
;;; Comment  : The purpose of this program is to fail.  That may seem
;;;            perverse, but such is the nature of testing.  A successful
;;;            test is one in which bugs are found, rather than one in which
;;;            the bugs go free.  Enough on the philosophy of testing!
;;;            The exact purpose of this program is to test one of the sort
;;;            routines (selsort) from my 2SORTS.LSP, and its translated
;;;            equivalent in 2SORTS.EXP, created using Lisp2C by BASIC
;;;            d.o.o. of Slovenia.  These files are available in the file
;;;            3SORTS.ZIP, which was created by BASIC d.o.o. and is not
;;;            documented as such.  The BASIC-modified 2SORTS.LSP from
;;;            3SORTS.ZIP should be used, rather than my original version.
;;;            No tests have been performed on the (qsort) or (msort)
;;;            functions which are also defined in the undocumented 2SORTS.EXP.
;;;
;;;            The history behind this test is as follows:
;;;          - The marketing of Lisp2C claims it translates ANY AutoLISP.
;;;          - I have consistently maintained that due to limitations in the
;;;            AutoLISP/ADS interface, it is impossible for any automatic
;;;            translation process to successfully translate ALL AutoLISP.
;;;          - In response to a request for a sorting routine, I uploaded
;;;            2SORTS.LSP (as a .ZIP), and included the comment that it
;;;            represents "an example of an AutoLISP routine which cannot be
;;;            successfully converted to ADS by any automatic conversion
;;;            software".
;;;          - This gave rise to a debate with Jure Spiler of BASIC d.o.o.
;;;            in which I challenged him to perform the translation, after
;;;            which I would test the results.
;;;          - This file is a formalised version of some simple command-line
;;;            tests I performed.  The AutoLISP tests pass OK, but some of the
;;;            ADS ones fail.  Success!  (In testing, not in translation).
;;;
;;;            Note that for my original assertion to be correct, I would need
;;;            to show one of two things:
;;;            - that the translation was not automatic, OR
;;;            - that the translation was not successful.
;;;            The first point is proven, because manual changes were required
;;;            to both the sort functions themselves and any code which calls
;;;            the sort functions.  The second point is proven by this test.
;;; Results  : 1, 2, 3, 4 - OK
;;;            5 - Fails due to a scoping problem.  Lisp2C .EXP files contain
;;;            an AutoLISP interpreter, which explains why the routines do as
;;;            well as they do, and why the files are so large.  This
;;;            interpreter does not have the same access to locally defined
;;;            symbols (such as the (compare) function) as the normal AutoLISP
;;;            interpreter.  This problem is compounded here by the ADS
;;;            routine failing to terminate the calling AutoLISP, but returning
;;;            nil instead.  A program could use that nil as a list and
;;;            perform some operations based on it without actually crashing,
;;;            leading to invalid or lost data.
;;;            6 - Performs the sort correctly, but performs undesirable
;;;            type conversions.  Reals with whole number values are returned
;;;            as integers.  This could cause a routine, which subsequently 
;;;            expects real numbers, to crash or perform incorrectly.
;;;            7 - Crashes when asked to sort an entity association list.
;;;            Although this seems simple enough, it is actually quite a
;;;            complex task (in C) to sort a list with many different data
;;;            types.  I was not at all surprised to see this one fail.
;;; Mitigating Circumstances:
;;;            These problems are largely caused by inherent restrictions in
;;;            the AutoCAD Release 12 AutoLISP/ADS interface.  Given that
;;;            these and other restrictions exist, Lisp2C actually does a
;;;            good job in providing a useful tool.  An AutoLISP compiler
;;;            has been on the wishlist for many years, and third party
;;;            products like Lisp2C and drcyak are the closest thing we have.
;;;            While it is true to say that the translation process is neither
;;;            automatic nor successful in the case of 2SORTS.LSP, it is
;;;            not a typical case.  I just want to have a fast, generic sort
;;;            routine to call from AutoLISP routines.  If I translated the
;;;            calling routines, there would be few, if any, problems.
;;;            For me, the file size and maintenance overheads involved in
;;;            that solution make it inappropriate, but that may not be true
;;;            for others. As it is, the effort involved in making the manual
;;;            changes is not major, although it could have been a problem if
;;;            there had been many calling routines.
;;; Conclusion:
;;;            The translated version of 2SORTS.LSP actually did better than I
;;;            expected.  However, it required manual code changes and comes
;;;            a long way short of my definition of a successful translation.
;;;            That 2SORTS.LSP is "an example of an AutoLISP routine which
;;;            cannot be successfully converted to ADS by any automatic
;;;            conversion software" is self-evident truth.  Marketing claims
;;;            which imply otherwise are not to be believed and should be
;;;            withdrawn.
;;; --------------------------------------------------------------------------
;;; Function definition structure:
;;;  C:SORTTEST
;;;  ÃÄÄdotest
;;;  ÀÄÄcompare
;;; -------------------------------------------------------------------------

;; --------------------------------------------------------------------------
;; Function: C:SORTTEST
;; Purpose : Provides AutoCAD command which performs the tests
;; Uses    : (dotest): local
;; --------------------------------------------------------------------------

(defun C:SORTTEST (/ dotest compare)


;; --------------------------------------------------------------------------
;; Function: dotest
;; Purpose : Actually performs the tests.
;; Uses    : (selsort): global
;;           (compare): local to C:SORTTEST
;; --------------------------------------------------------------------------

  (defun dotest ()
; 1 - Ascending sort, using internal function
    (print (selsort '(4 3 5 2 1 0) "<"))
; => (0 1 2 3 4 5)

; 2 - Descending sort
    (print (selsort '(4 3 5 2 1 0) ">"))
; => (5 4 3 2 1 0)

; 3 - Descending sort on 1st elements, using lambda function
    (print (selsort
     '((4 "z") (3 "a") (5 "q") (2 "s") (1 "g") (0 "w"))
     "(lambda (X Y) (> (car X) (car Y)))"
    ))
; => ((5 "q") (4 "z") (3 "a") (2 "s") (1 "g") (0 "w"))

; 4 - Ascending sort on 1st elements: if 1st elements are equal then descending
; sort on 2nd elements
    (print (selsort
      '((2 "a") (3 "a") (1 "z") (2 "c") (2 "b") (3 "w"))
      "(lambda (X Y) (if (= (car X) (car Y)) (> (cadr X) (cadr Y)) (< (car X) (car Y))))"
    ))
; => ((1 "z") (2 "c") (2 "b") (2 "a") (3 "w") (3 "a"))

; 5 - As above, but use separate AutoLISP function for the comparison
    (print (selsort
      '((2 "a") (3 "a") (1 "z") (2 "c") (2 "b") (3 "w"))
      "compare"
    ))
; => ((1 "z") (2 "c") (2 "b") (2 "a") (3 "w") (3 "a"))

; 6 - Ascending sort on a mix of integers and reals, placing the integers first
;     and the reals second.
    (print (selsort
      '(3 -1 0 5.0 2.1 2 -3.4 4 1.0)
      (strcat
        "(lambda (X Y)"
          "(cond"
            "((= (type X) (type Y)) (< X Y))"
            "((= (type X) 'INT) T)"
          ")"
        ")"
      )
    ))
; => (-1 0 2 3 4 -3.4 1.0 2.1 5.0)

; 7 - Ascending sort on 1st elements, using lambda function, on a list
;     containing different data types (an entity association list).
    (print (selsort (entget (entlast)) "(lambda (X Y) (< (car X) (car Y)))"))
; => ((-1 . <Entity name: 60000044>) (0 . "LINE") (5 . "8") (8 . "0")
;     (10 0.0 0.0 0.0) (11 1.0 1.0 0.0) (210 0.0 0.0 1.0))

  ) ; end dotest


;; --------------------------------------------------------------------------
;; Function: compare
;; Purpose : Performs a comparison: used by one of the tests in (dotest).
;; --------------------------------------------------------------------------

  (defun compare (X Y)
    (if (= (car X) (car Y)) (> (cadr X) (cadr Y)) (< (car X) (car Y)))
  ) ; end compare


;; Start C:SORTTEST proper --------------

  (setvar "CMDECHO" 0)
  (command "LINE" "0,0" "1,1" "") ; Make sure there is an entity for (entlast)
  (if (= (type selsort) 'EXSUBR)  ; If the routine has been run before, the
    (xunload "2SORTS.EXP")        ; ADS version will need unloading.
  )
  (getstring "\n\n[Enter] to run AutoLISP sort tests: ")
  (if (or (not selsort) (/= (type selsort) 'LIST))
    (load "2SORTS.LSP")
  )
  (dotest)

  (getstring "\n\n[Enter] to run ADS sort tests: ")
  (if (or (not selsort) (/= (type selsort) 'EXSUBR))
    (xload "2SORTS.EXP")
  )
  (dotest)

; The program will not get this far, so I won't bother tidying up...

) ; End C:SORTTEST


;;; -------------------------------------------------------------------------
;;;  Copyright: (c) Water Authority of Western Australia 1994.
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
;;; End SORTTEST.LSP --------------------------------------------------------
