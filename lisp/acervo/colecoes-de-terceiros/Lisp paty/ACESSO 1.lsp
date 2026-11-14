;;; -*- Mode: AutoLisp -*-
;;; Plain AutoLISP Project of all STDLIB modules.
;;; Don't modify anything in here! This file is created dynamically
;;; out of all modules, any changes will be lost!

;;; Files:
;;; STD%PRJ.LSP STDINIT.LSP STDMODUL.LSP STDSTR.LSP STDLIST.LSP STDLISP.LSP
;;; STDINIT2.LSP STDMATH.LSP STDPOINT.LSP STDTIME.LSP STDINPUT.LSP STDENT.LSP
;;; STDMISC.LSP STDERROR.LSP ENTMAKE.LSP DICT.LSP STDLIB.LSP

;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STD%PRJ.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Dealing with projects
;;; This code is needed for starting a project definition
;;; Included in a collection of all modules only!

;;; Copyright (c) 1992,93,94,98,99,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;;
;;; Permission to use, copy, modify and distribute this software and its
;;; documentation for any purpose is hereby granted without fee, provided
;;; 1) that the above copyright notice appear in all copies,
;;; 2) that the copyright notice, this permission notice and the pointer
;;;    where to download the source code for free appear in the
;;;    supporting documentation of source code distributions,
;;; 3) that the name of Reini Urban not be used in advertising or
;;;    publicity pertaining to distribution of the software and
;;; 4) that modifications without changing the defined function and
;;;    symbol names may not be published, distributed nor copied
;;; without specific, written prior permission.
;;;
;;; No Warranty
;;; Reini Urban makes no representations about the suitability of this
;;; software for any purpose, without even the implied warranty of
;;; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. It is provided
;;; "as is" without express or implied warranty.
;;; See the full Disclaimer for all detailed warranty exclusions.
;;; --------------------------------------------------------------------

;;; STDLIB.FAS, STDLIB.BI4, STDLIB.VLX, STDALL.LSP or seperate packages
;;; This delays std-require, omit all further STDLIB requires
;;; because all files are loaded in one step.
;;; The first dangerous top-level call must the called after having
;;; loaded everything, not to hinder correct VL initialisation.

(setq *STD:%PROJECT* t)

;;; VLIDE convenience: unprotect all to avoid re-assignment warnings
(if (and *STDLIB-SYMS* std-%unprotect-allsyms)
  (std-%unprotect-allsyms))
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDINIT.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:32:26 rurban>
;;; Copyright (c) 1994,98,99,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Initialization and error handling functions for the STDLIB
;;;
;;; This module may be used in two major ways:
;;; 1) If only some functions of the STDLIB are needed,
;;;    this module must be loaded first to able to use std-require
;;; 2) As simple module for STDLIB, which loads all modules.

;;; STATUS:
;;;   Fully tested.
;;;   From 0.4 on we seperate autolisp system specific functions
;;;     into special versions to be easier to understand at load-time,
;;;     and to improve compiler optimization (linkable functions).
;;;   We use special markers for external processing.
;;;   There are still some functions pending, in particular:
;;;     INPUT functions for tablenames and SSGET,
;;;   *STDLIB-DATE* now in DATLST format
;;;

;;; --------------------------------------------------------------------
;;; $Log: STDINIT.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; 2000-10-02 22:37:38 rurban
;;;   added STDLIB-BETA-P, STDLIB-RELEASE-P
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; 2000-09-19 16:06:24 rurban
;;;   fixed STD-%VLAX-GET-ACAD-OBJECT to support multiple copies of ACAD
;;;   added STD-%VLAX-VLA-OBJECT->ENAME
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; 2000-06-18 09:27:36 rurban
;;;  renamed *STDLIB-DATE* to *STDLIB-CDATE*
;;;  changed *STDLIB-DATE* format from CDATE to DATLST format
;;;    The old cdate format has some weird precision loss in VLISP 5,
;;;    so we change to DATLST format. This might require some changes 
;;;    on the user side. The < comparable number is now *STDLIB-CDATE*, 
;;;    but this might be unreliable, until the cause of the problem is found.
;;;     (fix *STDLIB-CDATE*) => 20000600 WRONG!
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;; 2000-04-26 21:02:49 rurban
;;;   renumbered MSG ID's (static only)
;;; 2000-04-09 18:32:34 rurban
;;;   changed "*MODULE-PATH* not set" verbosity
;;;
;;; Revision 0.4009  2000/04/07 17:29:20  rurban
;;; v0.4009, see Changes
;;; 2000-04-07 16:21:49 rurban
;;;   fixed STD-VLA-P: added vl-load-com, 
;;;   r15-vlx without activex rts => nil
;;; 2000-03-14 22:15:10 rurban
;;;   changed STD-VLA-P: check for (STD-%VLAX-GET-ACAD-OBJECT)
;;;
;;; Internal functions start with std-%
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDINIT-SYMBOLS*
    '(STDLIB-VERSION STRINGP CONSP
      STD-VL-P		    STD-FUNCTIONP	  STD-SYMBOL-VALUE
      STD-VERBOSE-PRINT	    STD-VLIDE-P		  STD-DOSLIB-P
      STD-VLA-P		    STD-ACAD-CONNECTION-P STD-VL-AVAILABLE-P
      STD-VILL-P	    STD-VLISP-P		  STD-VLISP-BETA1-P
      STD-ACOMP-P	    STD-VLRTS-P		  STD-SYS-DOSBASED-P
      STD-SYS-DOS-P	    STD-SYS-MSWIN-P	  STD-SYS-UNIX-P
      STD-SYS-MAC-P	    STD-SYS-OS2-P	  STD-SYS-LONGFNAME-P
      STD-VER-R12-P	    STD-VER-R13-P	  STD-VER-R14-P
      STD-VER-R2000-P	    STD-VER-WIN-P	  STD-VER-INT-P
      STD-VER-FCAD-P	    STD-VER-ICAD-P	  STD-VER-TCAD-P
      STD-VER-LANGUAGE	    STD-ACADVER		  STD-ACADPLATFORM
      STD-VER-ACADSUBVERSION			  STD-VER-NUM
      STD-VER-NAME	    STD-PROBE-FILE	  STD-BITSETP
      STD-SYMBOLP	    STD-SYMBOL-NAME	  STD-SYMBOL-VALUE
      STD-PRINC-TO-STRING   STD-PRIN1-TO-STRING	  STD-POSITION
      STD-REMOVE	    STD-REMOVE-IF	  STD-REMOVE-IF-NOT
      STD-MEMBER-IF	    STD-MEMBER-IF-NOT	  STD-SORT
      STD-FAST-SORT
      STD-%VLAX-GET-ACAD-OBJECT
      STD-%VLAX-ENAME->VLA-OBJECT
      STD-%VLAX-VLA-OBJECT->ENAME
      STD-EVERY
      STD-SOME		    STD-MSG))))


;;; Postprocessing markers: (since v0.3015)
;;; The markers are similar to the Lisp reader-macros #+ and #- but
;;; they have to be embedded into inline comments, and need an end marker
;;;
;;; 
;;; 
;;;
;;; #+ <feature>  : if feature is defined
;;; #- <feature>  : if feature is not defined
;;; <feature>     : VL | ACOMP | ALISP | VLISP | VILL | VLA | SMALL |
;;;		    STANDALONE | DYNAMIC | FCAD | ICAD | TCAD
;;; VL		  : if VILL, VLISP, VLA or STANDALONE
;;; DYNAMIC       : always false postprocessed
;;;
;;; no nesting allowed yet, but I work on it.

;;; Version format: major.minor[1]built[3] such as 0.1001
;;; Builts are incremented every single published update.
;;; Minors at minor functionality change or bugfixes
;;; Majors at larger functionality changes, no bugfixes
(setq *STDLIB-VERSION* 0.5006)
(defun STDLIB-VERSION () *STDLIB-VERSION*)

;;; added 2000-10-02 22:36:31 rurban
(defun STDLIB-RELEASE-P () T)
(defun STDLIB-BETA-P () (not (stdlib-release-p)))

;;; Date of version in readable CDATE format.
;;; Comparable with <
;;; This variable gets updated automatically with stdinit-upd.pl
;;; Recently a y2k bugfix (just perl, not lisp!)
;;; FIXME:
;;; There's a numeric problem with VLISP 5 (A2000) somewhere
;;; which truncates some decimal places: fix and rtos
;;;   (fix *STDLIB-CDATE*) => 20000600 WRONG!
;;; only workaround: change to DATE or DATLST format
;;; (std-cdate->date-string (std-datlst->cdate *STDLIB-DATE*))
(setq *STDLIB-CDATE* 20010106.0141)
(setq *STDLIB-DATE* '(2001  1  6  1 41 0))
;(setq *STDLIB-DATE*  (STD-CDATE->DATLST *STDLIB-CDATE*))

(setq *std:%old-verbose*      *VERBOSE*
      *std:%old-load-verbose* *LOAD-VERBOSE*)
;;; For early versions we set verbosity automatically.
;;; A user/developer can set this also.
(if (< *STDLIB-VERSION* 0.5)
  (progn
    (setq *VERBOSE*      T
          *LOAD-VERBOSE* T)
    (if *STD:%PROJECT*
      ;; We save/restore this variable in *std:%old-load-verbose*
      ;; Note: VLISP uses it as well internally.
      ;; Tools->E.Options->General Options->Diagnostics
      ;;  ->"Print notification message after load"
      (setq *LOAD-VERBOSE* nil))))

;;; *MODULE-PATH* should be set by the calling application,
;;; but with projects we don't need it yet, top-level loads are delayed
(if (and (null *MODULE-PATH*) (not *STD:%PROJECT*) *LOAD-VERBOSE*)
  (princ (strcat "* Warning: *MODULE-PATH* not set!"
		 "\n** STDLIB will not find its modules")))

;; This will be redefined in STDFILE
(setq *SLASH-CHAR* "/")
(setq *std:ori-error* *error*)	; just to be on the safe side

;;; -------------------------------------------------------------------73
;;; Basic requirements for std-require, overridden in later modules.
;;; Yes, this is defined somewhere else too.

;;; Running under Vital or Visual Lisp?
;;; This must be dynamic to be able to decide if the function
;;; was called from AutoCAD (AutiLISP mode) or from the RTS or the IDE.
;;; Not to be used to decide if native extensions may be used or
;;; workarounds. Use (or (std-vlrts-p)(std-vlide-p)) instead.
;;; Usage of unquoted lambda is illegal in acomp.

(defun STD-VL-P ()
  (not (listp (lambda () T))))



  ; acomp has SPECIAL
  (defun special (x) nil)	; ACOMP declarations
 


(if (not (std-vl-p))
     ; VL has PRAGMA instead
  (defun pragma (x) nil)	; VL declarations
 
)



;;; Avoid reload warnings in VL,
;;; The not "std-" prefixed functions are protected.
;;; Moved behind the pragma definition, thanks to David Kozina.
(eval (list 'pragma
	    (list 'quote
	      (list (cons 'unprotect-assign
                          '(stringp consp))))))


(defun STRINGP (s)  (= (type s) 'STR))	; EQ may cause problems under ACOMP
;;; VL has a native predicate vl[x]-consp, but we don't use it
(defun CONSP (lst) (not (atom lst)))

; (defun STD-SYMBOLP (x)
;  (= (type x) 'SYM))

;;; ADS resp. AutoCAD independant function version are supported
;;; (with some needed workarounds), to be able to use the Vital Lisp
;;; Editor standalone (vill 1 and vill 2).
;;; AutoLISP, Vill 3 and Vlisp can only be called from within AutoCAD
;;; so far (no arx stub yet), so we can ignore the special ADS
;;; independent functions.



;;; Acomp break (ctrl-c) enabler.
;;; Without calling this somewhere acomp will never break on <Esc>
;;; or <Ctrl-C>
;;; But this must be defined in an uncompiled lisp!
;;; Best in the MNL file or in ACAD.LSP before S::STARTUP
;;; (defun ACOMP-CTRL-C-INTERCEPTOR () (princ))


;;; STD-FUNCTIONP returns T if the argument is an internal function
;;; or a lambda list (lambda (args) body)
;;; or list representation of a function ((args) body)
;;; or a symbol of a named function, the return value of (function name)
(defun STD-FUNCTIONP (f)
  ;; fixed 0.3006 for VL compiled '(lambda..) and
  ;; unquoted AutoLISP defun lists (forbidden)
  (or
    (not (null (member (type f) 
		       '(USUBR SUBR EXSUBR EXRXSUBR)))) ;just boolean
    ;; ( function (lambda (..) ..)) => (quote #<usubr ..>)
    (and (listp f)
       ;;(eq (car f) 'quote)	; eq fails!
      (eq (type (cadr f)) 'USUBR)  ;  There are two quotes in VL.
      ;; this tests (function (lambda..))
      (eq (car f) (car (function (lambda () T))))
    )
    (and (listp f)
      ;; '(lambda (..)..)
      (eq (car f) 'LAMBDA))
    ;; plain AutoLISP accepts lists too. This may be too dangerous
    ;; as functional argument!
    (and (not (std-vl-p))
	 (not (std-acomp-p))
	 (consp f)
         ;; ((..) ..)
         (listp (car f))
	 (consp (cdr f)))
    ;; (function func-name) => SYM
    (and (eq (type f) 'SYM)
      (or (member (type (std-symbol-value f)) '(USUBR SUBR EXSUBR EXRXSUBR))
	  (STD-FUNCTIONP (std-symbol-value f))))
  )
)



  ; VL has native definitions

;;; if not ViLL: almost the same
;;; to be redefined at the end of this file if VL
(setq STD-SYMBOL-VALUE eval)

;;; considered too dirty, relying on too much
;;; undocumented behaviour and uses invalid lisp syntax,
;;; but valid AutoLISP and scheme syntax






;;; princ on *VERBOSE*
;;; requires STDLISP
(defun STD-VERBOSE-PRINT (x)
  (if *VERBOSE* (std-princ x)))

(defun std-%load-verbose-print (msg)
  (if *LOAD-VERBOSE* (mapcar (function princ) msg)))

;;;  princ on *DEBUG*
;(defun std-debug-print (x)
;  (if *DEBUG* (std-princ x)))


;;; -------------------------------------------------------------------73
;;; Lisp version predicates




(defun std-%defun-check (func)
  (if (or (not (eq (car func) 'defun))
	  (/= (type (cadr func)) 'SYM)		; function name
	  (not (listp (caddr func)))		; argument list
      )
    (progn
      (princ "\n; ***ERROR: wrong defun")
      (exit)
    )
    T
  )
)

;;; "conditional defun"
;;; Define the function only if not already defined
;;; hmm, this fools the VL linker
(defun std-%cond-defun (func)
  (if (std-%defun-check func)
    (if (not (std-functionp (std-symbol-value (cadr func))))
      (eval func))))

;;; Memoized versions

;;; What is memoization?
;;; For every argument store the result value in the function itself
;;; or in a global variable.
;;; So the function is "self-modifiying" or "learning".
;;; vlisp/vill don't store the function as list so we have to
;;; to store the actual function definition in a special symbol,
;;; returned by (std-memoizated-funcname).
;;; The term and usage of memoization is widely used in Common Lisp, but
;;; mostly with hash-tables, not self-modifying functions.

;;; For simple function with no arguments we don't use
;;; self-modifying functions, we use global symbols instead.
;;; These memoizing functions were used to create the expanded versions.
;;; VL and acomp cannot optimize quoted DEFUN's, that's why.

;;; At first the simple ones without arguments

;;; Predicates without Arguments
;;; Cache first return value, only works with predicates
;;; 1 is true, 0 is false
;;; looks awful as long as no backquote/comma mechanism is supported
;;; =>
;;; `(defun ,funcname ,args
;;;    (if (boundp ',memoized-var)
;;;      (= ,memoized-var 1)
;;;      (= 1 (setq ,memoized-var
;;;           (if ,body 1 0))))




 ;;; with linked-VL provide direct defun's

;;; Not cached! it may change dynamically.
;;; under the IDE we may not use (exit)!
(defun STD-VLIDE-P ()
  (or (and (std-vill-p)  (= "IDE" (substr (_vill-version)  1 3)))
      (and (std-vlisp-p) (= "IDE" (substr (_vlisp-version) 1 3)))
      (and (std-ver-r2000-p) (eq (type vlisp-make-project-fas) 'SUBR))
      ))

;;; Doslib loaded
(defun STD-DOSLIB-P ()
  (member (type dos_ver) '(EXSUBR EXRXSUBR)))

;;; Some more system specific stuff, but they need an acad connection!

 ;;; with linked-VL provide direct defun's

;;; Another possibility. Make a guess from (ver)
;;; This seems to be ISO 639-1 conformant.
;;; There's also a (getvar "LOCALE") in R13 and a VLA method in R14
;;; see STDLOCAL (std-acad-locale)
(defun std-%ver-language-from-ver (/ s)
  (std-%simple-require "STDSTR")
  (setq s (std-lastchars 4 (ver)))
  (cond
    ((= s "(en)") ':ENGLISH)
    ((= s "(de)") ':GERMAN)
    ((= s "(fr)") ':FRENCH)
    ((= s "(it)") ':ITALIAN)
    ((= s "(sp)") ':SPANISH)
    ((= s "(ja)") ':JAPANESE)
    ((= s "(ru)") ':RUSSIAN)	; dos (cp866) version only
    ((= s "(cz)") ':CZECH)
    ))


;;; Temp. workaround for versions which return english commands
;;; but are marked as International = Locked
(defun std-%ver-weird-language ()
  (std-%ver-language-from-ver)
  ;;(if (= (std-lastchars 4 (ver)) "(ja)")
  ;;  ':JAPANESE
  ;;  nil
  ;;)
)


 ;;; with linked-VL provide direct defun's


;;; VL specific feature: Test stand-alone VL mode without ACAD.
;;; Should almost always return T,
;;; Only under VL stand-alone (ViLL 2) it is nil.
;;; So far I assume no connection ViLL.EXE IDE 2.0 (which works with
;;; R13c4 only)
;;;
;;; The reported feature to load VisualLISP alone with EBATCHP.EXE
;;; or any future seperate loader is not detected if FILEEXT.FAS is
;;; not loaded. I cannot repeat stand-alone VLisp for myself.
;;; AutoCAD is probably connected, only invisible.
;;;
;;; Also with vill.exe ver 2 without FILEEXT.FAS it always returns T
;;; which may be wrong, but FILEEXT.FAS should have been loaded so it's
;;; academic.
 ;;; with linked-VL provide direct defun's

;;; Provided to disallow loading FILEEXT.FAS to avoid the stupid
;;; warnings printed.
;;; (setq *DISALLOW-FAS2-EXTENSIONS* t) to disallow loading
;;; fas2 extensions, like FILEEXT.FAS, BINIO.FAS, INIFILE.FAS and
;;; REGISTRY.FAS
;;; We'll use the ads stdlib<ver>.<ext> module instead.
(defun std-%accept-fas2-extensions-p ()
  (and (std-vl-p)
       (not *DISALLOW-FAS2-EXTENSIONS*)	; defaults to nil, so it's ok
       (or (not (std-acad-connection-p))
           (<= 12 (atoi (std-acadver)) 14))))

;;; -------------------------------------------------------------------73
;;; not-memoized versions needed here to help VL link the functions.

(defun STD-VLA-P ()
  (and (std-vl-p)
       (progn (and (std-ver-r2000-p)(vl-load-com)) 1) ;2000-04-07
       ;; it also depends on the runtime system
       ;; (STD-%VLAX-GET-ACAD-OBJECT)
       (std-functionp vla-Move)))

;;; with linked-VL provide direct defun's

;;;    (if (boundp ',memoized-var)
;;;      (= ,memoized-var 1)
;;;      (= 1 (setq ,memoized-var
;;;           (if ,body 1 0))))

(defun STD-ACAD-CONNECTION-P ()
  (if (boundp '*ACAD-CONNECTED*)	; only for ViLL 2
    (= 1 *ACAD-CONNECTED*)
    (= 1 (setq *ACAD-CONNECTED* (if

    (if (eq (type std-%acad-connection-p) 'SUBR)
      (std-%acad-connection-p)
      (if (std-vill-p)
        (/= "IDE v.2.0" (substr (_vill-version) 1 9))	; fixed
        T
      ))
    1 0)
))))
(defun STD-VL-AVAILABLE-P ()
  (if (boundp '*DEF-VL-AVAILABLE*)
    (= 1 *DEF-VL-AVAILABLE*)
    (= 1 (setq *DEF-VL-AVAILABLE* (if

    (or (std-vlide-p)(std-vlrts-p))
    1 0)
))))
(defun STD-VILL-P ()
  (if (boundp '*DEF-VILL*)
    (= 1 *DEF-VILL*)
    (= 1 (setq *DEF-VILL* (if

    (and (std-vl-p) (boundp '_vill-version))
    1 0)
))))
(defun STD-VLISP-P ()
  (if (boundp '*DEF-VLISP*)
    (= 1 *DEF-VLISP*)
    (= 1 (setq *DEF-VLISP* (if

    (and (std-vl-p) (std-functionp vl-file-size))
    1 0)
))))
(defun STD-VLISP-BETA1-P ()
  (if (boundp '*DEF-VLISP-BETA1*)
    (= 1 *DEF-VLISP-BETA1*)
    (= 1 (setq *DEF-VLISP-BETA1* (if

    (and (std-vl-p) (std-functionp file-size))
    1 0)
))))
(defun STD-ACOMP-P ()
  (if (boundp '*DEF-ACOMP*)
    (= 1 *DEF-ACOMP*)
    (= 1 (setq *DEF-ACOMP* (if

    (and (not (std-vl-p)) (eq (type special) 'USUBR))
    1 0)
))))
(defun STD-VLRTS-P ()
  (= 1 *DEF-VLRTS*)
)
(defun STD-SYS-DOSBASED-P ()
  (if (boundp '*SYS-DOSBASED*)
    (= 1 *SYS-DOSBASED*)
    (= 1 (setq *SYS-DOSBASED* (if

    (getenv "COMSPEC")
    1 0)
))))
(defun STD-SYS-DOS-P ()
  (if (boundp '*SYS-DOS*)
    (= 1 *SYS-DOS*)
    (= 1 (setq *SYS-DOS* (if

    (and (getenv "COMSPEC") (not (std-sys-mswin-p)))
    1 0)
))))
(defun STD-SYS-MSWIN-P ()
  (if (boundp '*SYS-MSWIN*)
    (= 1 *SYS-MSWIN*)
    (= 1 (setq *SYS-MSWIN* (if

    (STD-VER-WIN-P)
    1 0)
))))
(defun STD-SYS-UNIX-P ()
  (if (boundp '*SYS-UNIX*)
    (= 1 *SYS-UNIX*)
    (= 1 (setq *SYS-UNIX* (if

    (and (not (std-sys-dos-p))
	 ;; This might fail on NT/Win2000
	 ;; We should better check for a filesystem specific
	 ;; or analyse the platform string (or use uname)
	 (getenv "USER"))
    1 0)
))))
(defun STD-SYS-MAC-P ()
  (if (boundp '*SYS-MAC*)
    (= 1 *SYS-MAC*)
    (= 1 (setq *SYS-MAC* (if

    (wcmatch (std-acadplatform) "*Mac*")
    1 0)
))))
(defun STD-SYS-OS2-P ()
  (if (boundp '*SYS-OS2*)
    (= 1 *SYS-OS2*)
    (= 1 (setq *SYS-OS2* (if

    (wcmatch (std-acadplatform) "*OS2*")
    1 0)
))))
(defun STD-SYS-LONGFNAME-P ()
  (if (boundp '*SYS-LONGFNAMES*)
    (= 1 *SYS-LONGFNAMES*)
    (= 1 (setq *SYS-LONGFNAMES* (if

    (or (std-ver-r14-p)
        (and (std-ver-r13-p)
	     (> (getvar "LONGFNAME") 0))
        (std-sys-os2-p)
        (not (std-sys-dosbased-p)))
    1 0)
))))
(defun STD-VER-R12-P ()
  (if (boundp '*SYS-R12*)
    (= 1 *SYS-R12*)
    (= 1 (setq *SYS-R12* (if

   (= (atoi (std-acadver)) 12)
    1 0)
))))
(defun STD-VER-R13-P ()
  (if (boundp '*SYS-R13*)
    (= 1 *SYS-R13*)
    (= 1 (setq *SYS-R13* (if

    (>= (atoi (std-acadver)) 13)
    1 0)
))))
(defun STD-VER-R14-P ()
  (if (boundp '*SYS-R14*)
    (= 1 *SYS-R14*)
    (= 1 (setq *SYS-R14* (if

    (>= (atoi (std-acadver)) 14)
    1 0)
))))
(defun STD-VER-R2000-P ()
  (if (boundp '*SYS-R15*)
    (= 1 *SYS-R15*)
    (= 1 (setq *SYS-R15* (if
    ;; internal debug builds:
;    (or (< 0.60 (atof (substr (std-acadver) 3)) 1.0)
	(> (atoi (std-acadver)) 14);)
    1 0)
))))
(defun STD-VER-WIN-P ()
  (if (boundp '*SYS-MSWIN*)
    (= 1 *SYS-MSWIN*)
    (= 1 (setq *SYS-MSWIN* (if
   (wcmatch (std-acadplatform) "*Win*")
    1 0)
))))
(defun STD-VER-INT-P ()
  (if (boundp '*SYS-LOCKED*)
    (= 1 *SYS-LOCKED*)
    (= 1 (setq *SYS-LOCKED* (if
    ;; new "h" postfix to be checked
    (wcmatch (std-acadver) "*Int*,*Lock*,*h")
    1 0)
))))
(defun STD-VER-FCAD-P ()
  (if (boundp '*SYS-FCAD*)
    (= 1 *SYS-FCAD*)
    (= 1 (setq *SYS-FCAD* (if
      (getvar "FCVERSION")
      1 0)
))))
(defun STD-VER-ICAD-P ()
  (if (boundp '*SYS-ICAD*)
    (= 1 *SYS-ICAD*)
    (= 1 (setq *SYS-ICAD* (if
      ;; This works only for Icad 98, Icad 2000 not tested yet
      (= (substr (std-acadver) 1 3) "14i")
      1 0)
))))
(defun STD-VER-TCAD-P ()
  (if (boundp '*SYS-TCAD*)
    (= 1 *SYS-TCAD*)
    (= 1 (setq *SYS-TCAD* (if
      (= (std-ver-name) "TCAD") 1 0)
))))

(defun STD-VER-LANGUAGE (/ cmd)
 (if *SYS-LANGUAGE*
   *SYS-LANGUAGE*
   (setq *SYS-LANGUAGE*
    (cond
     ((not (boundp 'getcname))
       (if (std-ver-int-p)
         (std-%ver-language-from-ver)
         ':ENGLISH))
     ((= (setq cmd (getcname "_CIRCLE")) "CIRCLE")
      (if (std-ver-int-p)
       (std-%ver-weird-language)
      ':ENGLISH))
     ((= cmd "KREIS")   ':GERMAN)
     ((= cmd "CERCLE")  ':FRENCH)
     ((= cmd "CERCHIO") ':ITALIAN)
     ((= cmd "CIRCULO") ':SPANISH)
     ((= cmd "KRU\216NICE") ':CZECH)
     ((= cmd "\312\320\323\303") ':RUSSIAN)
     ;; to be added more below, like
     ;((= cmd "xy\200z") ':newlanguage)
     ((std-ver-fcad-p)
      (setq cmd (getvar "LANGUAGE"))	; => 1-5
      (cond ((= cmd 1) ':GERMAN)
	    ((= cmd 2) ':ENGLISH)	; what is 3?
	    ((= cmd 4) ':HUNGARIAN)
	    ((= cmd 5) ':POLISH)
	    ))
     (T nil)))))

(defun STD-ACADVER ()
  (if *ACADVER* *ACADVER*
    (setq *ACADVER*
      (cond ((getvar "FCVERSION"))
            ((getvar "ACADVER"))))))

;;; FCAD returns "Win32"
;;; R14 eg. "Microsoft Windows NT Version 4.0 (x86)"
(defun STD-ACADPLATFORM ()
  (cond (*ACADPLATFORM*)
        ((setq *ACADPLATFORM* (getvar "PLATFORM")))))

;;; Returns the fractional part as float, like 0.1
;;; "10 c2"  => 0.2
;;; "10_c10" => 0.1
;;; "11_c2"  => 0.2
;;; "13_c1" - "13_c4" => 0.1 - 0.4
;;; "13_c4a" => 0.41
;;; "14.0"   => 0.0
;;; "14.01"  => 0.01
;;; ICAD 98:   13.9
;;; ICAD a-e:  13.91 - 13.95
;;; FCAD 3:    14.3
;;; FCAD 4:    14.4
(defun STD-VER-ACADSUBVERSION (/ s)
  (std-%simple-require "STDSTR")
  (cond
    ((= (substr (setq s (std-acadver)) 1 6) "13_c4a")  ;13_c4a or 13_c4ah
      0.41)
    ((wcmatch s "##[ _]c#*")      ;r10,11,13; fixed
      (atof (strcat "0." (substr s (+ 1 (std-strpos "c" s))))))
    ((wcmatch s "##c*#*")                              ;r10-12 (not sure)
      (atof (strcat "0." (substr s (+ 1 (std-strpos "c" s))))))
    ((wcmatch s "##.#*")                               ;r14, fixed
      (atof (strcat "0." (substr s (+ 1 (std-strpos "." s))))))
    ((wcmatch s "#.#*")                                ;<=r9??
      (atof (strcat "0." (substr s (+ 1 (std-strpos "." s))))))
    ((= "14i")
      -0.1)
    ((std-ver-fcad-p)
      (/ (atof (getvar "FCVERSION")) 10))
    (T 0)))

;;; Comparable unique float for acad or clones main_release.sub_version
;;; R10c10: 10.10
;;; R12:    12.0
;;; R13:    13.0 c1: 13.1 c2: 13.2 c4: 13.4 c4a: 13.41
;;; R14:    14.0, 14.01
;;; A2000:  15.0
;;; ICAD 98:   13.9
;;; ICAD a-e:  13.91 - 13.95
;;; FCAD 3:    14.3
;;; FCAD 4:    14.4
(defun STD-VER-NUM ()
  (cond (*ACADVER-NUM*)
	((std-ver-fcad-p)
	 (+ 14 (std-ver-acadsubversion)))
        (T (+ (atoi (std-acadver))
              (std-ver-acadsubversion)))))

;;; (STD-VER-NAME) => "ACAD" | "ICAD" | "FCAD" | "TCAD"
(defun STD-VER-NAME (/ s)
  (cond (*ACADVERNAME*)
        ((setq *ACADVERNAME*
	 (cond ((not (std-acad-connection-p)) "VILL")
	       ((= (setq s (substr (ver) 1 5)) "AutoL")
		"ACAD")
	       ((= s "Visua") 	 "ACAD")
	       ((std-ver-fcad-p) "FCAD")
	       ((std-ver-icad-p) "ICAD")
	       ;; TurboCAD?
	       )))))

;;; -------------------------------------------------------------------73
;;; Initialization, Error handling
;;; moved to STDERROR.LSP

;;; -------------------------------------------------------------------73
;;; Loading stuff

;;; Supported compiled file extension or nil.
;;; Depricated!
;;; This is not the whole story since we could also check if a vill
;;; or vlisp runtime module is present and (vload) it then.
;;; see STDLISP
(setq *COMP-EXT*
  (cond ((std-acomp-p) ".BI4")
        ((not (std-%accept-fas2-extensions-p)) nil)
        ((std-vl-p)    ".FAS")	; consider also ".VLX"
	(T nil)))

;;; Needs basename only, like (std-%simple-load "STDINIT")
(defun std-%simple-load (fname / lst path)
  (std-%load-verbose-print (list "\nLoading " fname " ..."))
  (setq lst *MODULE-PATH*)
  ;; first search it in the module path, findfile needs acad
  (while (and lst (not path))

    (if *COMP-EXT*
      (setq path (findfile (strcat (car lst) fname *COMP-EXT*))))

    (or path	; break if found
	(setq path (findfile (strcat (car lst) fname ".LSP"))))
    (setq lst (cdr lst)))
  ;; either found it in *MODULE-PATH*, or if not
  ;; search in the acad library path
  (setq path (cond (path) (fname)))
  (if (= "error" (load path "error"))
    (mapcar 'princ (list " Error loading " fname "!"))
    T
  )
)

;;; readable file? return the filename
;;; with linked-VL provide direct defun's
(defun STD-PROBE-FILE (fname / f)
  (if (and (stringp fname)
           (setq f (open fname "r")))	; fixed
    (progn (close f) fname)))

;;; extended for arx and ads modules
(defun std-%simple-load-external (fname / lst path)
  (std-%load-verbose-print (list "\nLoading " fname " ..."))
  (setq lst *MODULE-PATH*)
  ;; first search it in the module path
  (while (and lst (not path))
    (setq path
      (if (std-acad-connection-p)
	   (cond
             ((findfile (strcat (car lst) fname
                (if *COMP-EXT* *COMP-EXT* ".LSP"))))
	     ((findfile (strcat (car lst) fname ".LSP")))
	     ((findfile (strcat (car lst) fname ".ARX")))
	     ((findfile (strcat (car lst) fname ".EXE")))
	     ((findfile (strcat (car lst) fname ".EXP")))
	   )
	   (cond
             ((std-probe-file (strcat (car lst) fname
                (if *COMP-EXT* *COMP-EXT* ".LSP"))))
	     ((std-probe-file (strcat (car lst) fname ".LSP")))
	   ))
          lst (cdr lst)))
  (if path
    ;; found it in *MODULE-PATH*
    (load path "error")
    ;; or search in the acad library path
    ;; try lisp, arx and ads
    (if (= "error" (load fname "error"))
      (if (std-acad-connection-p)	; fixed 0.2013
	(if (or (not (std-functionp arxload))
		(= (arxload fname "error") "error"))
          (if (or (not (std-functionp xload))
                  (= (xload fname "error") "error"))
  	    "error"
	  )
        )
        "error"
      )
    )
  )
)

;;; Moved from STDMATH. We don't need the whole Maths just for this
;;; basic function.
;;; T if all bitvalues in flag are set or nil
(defun STD-BITSETP (val flag)
  (= (logand val flag) val))

;;; Bugfix for VLAX-GET-ACAD-OBJECT resp. VLAX-GETACADOBJECT
;;; VLAX-GETACADOBJECT will fail with multiple ACAD.EXE copies otherwise.
(defun std-%vlax-get-acad-object ()
  (cond 
   (*IAcadApplication*)			; cache
   (T (setq *IAcadApplication*
	(vlax-get (std-%vlax-ename->vla-object 
	  (namedobjdict)) "Application")))))

;;; -------------------------------------------------------------------73
;;; Extension mappings
;;; Only acomp, vital lisp and Visual Lisp
;;; External extensions are handled by extra files:
;;; BINIO.FAS, REGISTRY.FAS, INIFILE.FAS
;;; and extra version numbering.


(cond
  ((std-acomp-p)
   	;;; see STDACOMP.LSP



  )
  ((std-vill-p)		; Vital Lisp


	(defun std-symbolp (x)	; already defined above
	  (vlx-symbolp x))
	(defun std-symbol-name (x)
	  (vlx-symbol-name x))
	(defun std-symbol-value (x)
          (vlx-symbol-value x))
	(defun std-princ-to-string (x)
	  (vlx-princ-to-string x))
	(defun std-prin1-to-string (x)
	  (vlx-prin1-to-string x))
	(defun std-remove (x lst)
	  (vlx-remove x lst))
	(defun std-remove-if (f lst)
	  (vlx-remove-if f lst))
	(defun std-remove-if-not (f lst)
	  (vlx-remove-if-not f lst))
	(defun std-member-if (f lst)
	  (vlx-member-if f lst))
	(defun std-member-if-not (f lst)
	  (vlx-member-if-not f lst))
	(defun std-sort (x f)
	  (vlx-sort x f))
	(defun std-fast-sort (x f)
	  (vlx-sort x f))
        (if (std-functionp vlx-position)	;introduced with vill 3
	  (defun std-position (x lst)
	    (vlx-position x lst)))
;;; OLD:
;;;     (defun std-%vlax-get-acad-object ()
;;;    	  (vlax-GetAcadObject))
;;; Fix: VLAX-GETACADOBJECT will fail with multiple acad.exe copies otherwise.
;;; See cached version above instead.
;        (defun std-%vlax-get-acad-object ()
;	  (vlax-get (vlax-entname->vlaobj (namedobjdict)) "Application"))
        ;; 7-Apr-00 for backwards compatibility with VILL
        (defun std-%vlax-ename->vla-object (ele)
	  (vlax-entname->vlaobj ele))
        (defun std-%vlax-vla-object->ename (ele)
	  (vlax-vlaobj->entname ele))

        (if (std-functionp vlx-every)	;introduced when?
	  (progn
          (defun std-every (pred lst)
            (vlx-every pred lst))
          (defun std-some (pred lst)
            (vlx-some pred lst))))


  )
  ((std-vlisp-p)	; Visual Lisp


	(defun std-symbolp (x)	; already defined above
	  (vl-symbolp x))
	(defun std-symbol-name (x)
	  (vl-symbol-name x))
	(defun std-symbol-value (x)
          (vl-symbol-value x))
	(defun std-princ-to-string (x)
	  (vl-princ-to-string x))
	(defun std-prin1-to-string (x)
	  (vl-prin1-to-string x))
	(defun std-position (x lst)
	  (vl-position x lst))
	(defun std-remove (x lst)
	  (vl-remove x lst))
	(defun std-remove-if (f lst)
	  (vl-remove-if f lst))
	(defun std-remove-if-not (f lst)
	  (vl-remove-if-not f lst))
	(defun std-member-if (f lst)
	  (vl-member-if f lst))
	(defun std-member-if-not (f lst)
	  (vl-member-if-not f lst))
	(defun std-sort (x f)
	  (vl-sort x f))
	(defun std-fast-sort (x f)
	  (vl-sort x f))
        ;; Those three funcs were renamed from ViLL to VLISP
        ;; so we provide unified names

;;; R14 note: 
;;; vlax-get-acad-object will fail with multiple acad.exe copies.
;;; We either need acadunsup.arx 1.0 or this hack by Albert Szilvasy
;        (defun std-%vlax-get-acad-object ()
;	  (vlax-get (vlax-ename->vla-object (namedobjdict)) "Application"))
    	  ;; to fix (vlax-Get-Acad-Object)
   
        ;; 7-Apr-00
        (defun std-%vlax-ename->vla-object (ele)
	  (vlax-ename->vla-object ele))
        (defun std-%vlax-vla-object->ename (ele)
	  (vlax-vla-object->ename ele))

        (defun std-every (pred lst)
          (vl-every pred lst))
        (defun std-some (pred lst)
          (vl-some pred lst))


  )

  (T 	; no vill nor vlisp, use workarounds instead


        ;; already needed earlier
	;; (setq STD-SYMBOL-VALUE eval)

;;; symbol name as string
(defun STD-SYMBOL-NAME (symb / x)
  (if (not (boundp symb))
    (progn
      (set symb T)
      (setq x (std-%bound-symbol-name symb))
      (set symb nil)
      x
    )
    (std-%bound-symbol-name symb)
  )
)

;;; Must be bound, means must be non-nil!
;;; AutoLISP has a wrong implementation of bound,
;;; something like (defun boundp (x)(not (null x)))
;;; Normally bound symbols may have the value nil.
;;; In autolisp not! aargh
(defun std-%bound-symbol-name (symb)
  (std-%simple-require "STDLIST")
  ;; needed: workaround for atomlist, <= R11
  (nth (std-position symb (atoms-family 0))
       (atoms-family 1)))

(setq FUNCTION quote)

(defun STD-SYMBOLP (x)	; already defined above
  (eq (type x) 'SYM))


  )
)


;;; Translate to localized message
(defun STD-MSG (x)
  (if (std-module-defined-p "STDLOCAL")
    (progn
      (if (listp x)
        (mapcar (function std-%translate-string) x)
        (std-%translate-string x)))
    x))

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDINIT-SYMBOLS*))
(setq *STDINIT-SYMBOLS* nil)


;;; Module dependencies:

;;; STDINIT needs modules to work:



;;; If all required STDLIB modules are contained in one project,
;;; we don't have to load it explicitly.
;;; STDLIB.FAS already contains it!
(if (not *STD:%PROJECT*)	; set by STD%PRJ.LSP which is only
  (progn			; part of the project
    (if (not (std-functionp std-%simple-require))
      (std-%simple-load "STDMODUL")
      (std-provide "STDINIT")
)))

;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDMODUL.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:30:35 rurban>
;;; Copyright (c) 1998 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Modules
;;;
;;; Load a file if it is not already loaded.
;;; Delay loading if *STD:%PROJECT* is not-nil, to avoid unneccessary
;;; and unpredictable top-level load calls.
;;; The project using the stdlib needs to manage the globals
;;; *STD:%PROJECT* and *STD:MODULES-DELAYED* then!
;;;
;;; STATUS:
;;;  Tested, the interface is stable.
;;;  We use a list of uppercase strings as modules.
;;;  Intermediate global constants:
;;;    *STD:UNBIND-SYMBOLS* -boolean- decides if to unbind variables too.
;;;    Depricated:
;;;    *MODULES-AS-STRINGS* -boolean- if the modules are stored as strings
;;;      or symbols. Symbols cannot be handled by the ADS resbuf.
;;;      Now: As if T
;;;   *MODULES-IN-OBARRAY* -boolean- is to test the two implementations.
;;;      I don't know yet if to keep the IN-OBARRAY version
;;;      or the simplier *MODULES* list version.
;;;      Now: nil

;;;  Features:
;;;   Some feature code will probably be added. Features are not exactly
;;;   modules. Just features provided by the system or by a module.
;;;   This is another way to do run-time detection of some features
;;;   provided by the systemm as with #+
;;;   Features can be set and checked.
;;;   Typical features would be (VL, VLISP, VILL, VLA, Acomp, registry,
;;;   inifile, binio, Int32, strong-random, protected-eval, SageCLOS,
;;;   setf, array, struct, mvalues, ...)



;;; $Log: STDMODUL.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:50:35  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; internal functions start with std-%

;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDMODUL-SYMBOLS*
    '(STD-%SIMPLE-REQUIRE     STD-%SIMPLE-PROVIDE
      STD-REQUIRE	      STD-PROVIDE
      STD-MODULE-DEFINED-P    STD-MODULES
      STD-MODULE-UNBIND))))


;;; May be set by the calling application and is overridden in
;;; STDLIB.LSP. We need just the path of the STDLIB files. In STDLIB.LSP
;;; -the main module- at the very end of the file we define all other
;;; paths.
;;; Change this to your requirements if you intend to load STDMODUL
;;; alone without STDLIB
;;; (if (not (boundp '*MODULE-PATH*))
;;;  ;; just my default
;;;   (setq *MODULE-PATH* '("l:\\src\\STDLIB\\")))

;;; Simple modules
;;; Versions not using STDFILE, blindly assuming pathnames
;;; ending with slashes and extensions prefixed with a dot.
;;; We need it to load STDMODUL,STDSTR,STDLISP,STDFILE and STDINIT2
;;; Then we can use the full versions, which are safer.
;;;


;;; Needs basename only, like (std-%simple-load "STDINIT")
;;; (defun STD-%SIMPLE-LOAD (fname / lst path)
;;;   (std-%load-verbose-print (list "\nLoading " fname " ..."))
;;;   (setq lst *MODULE-PATH*)
;;;   ;; first search it in the module path, findfile needs acad
;;;   (while (and lst (not path))
;;;     (if *COMP-EXT*
;;;       (setq path (apply (if (std-acad-connection-p)
;;;                    'findfile
;;;                    'std-probe-file)
;;; 	         (list (strcat (car lst) fname *COMP-EXT*)))))
;;;     (or path	; break if found
;;; 	(setq path (apply (if (std-acad-connection-p)
;;;                    'findfile
;;;                    'std-probe-file)
;;; 	         (list (strcat (car lst) fname ".LSP")))))
;;;     (setq lst (cdr lst)))
;;;   ;; found it in *MODULE-PATH*
;;;   ;; or search in the acad library path
;;;   (setq path (cond (path) (fname)))
;;;   (if (and (std-functionp 'vl-load) (std-vlrts-p))
;;;     (vl-load path)
;;;     (load path 'error)
;;;   )
;;; )

;;; (std-%cond-defun
;;; ;;; extended for arx and ads modules
;;; '(defun STD-%SIMPLE-LOAD-EXTERNAL (fname / lst path)
;;;   (std-%load-verbose-print (list "\nLoading " fname " ..."))
;;;   (setq lst *MODULE-PATH*)
;;;   ;; first search it in the module path
;;;   (while (and lst (not path))
;;;     (setq path
;;; 
;;;       (if (std-acad-connection-p)
;;; 	   (cond
;;; 	     ((findfile (strcat (car lst) fname
;;; 			 (if *COMP-EXT* *COMP-EXT* ".LSP"))))
;;; 	     ((findfile (strcat (car lst) fname ".LSP")))
;;; 	     ((findfile (strcat (car lst) fname ".ARX")))
;;; 	     ((findfile (strcat (car lst) fname ".EXE")))
;;; 	     ((findfile (strcat (car lst) fname ".EXP")))
;;; 	   )
;;; 
;;; 	   (cond
;;; 	     ((std-probe-file (strcat (car lst) fname
;;; 			       (if *COMP-EXT* *COMP-EXT* ".LSP"))))
;;; 	     ((std-probe-file (strcat (car lst) fname ".LSP")))
;;; 	   )
;;; 
;;;           )
;;; 
;;;           lst (cdr lst)))
;;;   (if path
;;;     ;; found it in *MODULE-PATH*
;;;     (if vl-load
;;;       (vl-load path)
;;;       (load path 'error))
;;;     ;; or search in the acad library path
;;;     ;; try lisp, arx and ads
;;;     (if vl-load
;;;       (vl-load fname)
;;;       (if (eq 'error (load fname 'error))
;;; 
;;;         (if (std-acad-connection-p)	; fixed 0.2013
;;;           (if (eq 'error (arxload fname 'error))
;;;             (xload fname 'error)
;;;   	    'error
;;; 	  )
;;;         )
;;; 
;;;         'error
;;;       )
;;;     )
;;;   )
;;;  )
;;; )

(setq *MODULES-AS-STRINGS* T)
(setq *MODULES-IN-OBARRAY* nil)

;;; simple require, only strings, no module-path
(defun STD-%SIMPLE-REQUIRE (s)
  (cond ((std-%simple-module-string-defined-p s) s)
	(*STD:%PROJECT*
	  (setq *STD:MODULES-DELAYED*
		(cons s *STD:MODULES-DELAYED*))
	  (std-%load-verbose-print
            (list "\n..." "Module" " " 
		  s
		  " delayed... "))
	  T)
        ((= "STD" (substr s 1 3))
          (std-%simple-load s))
        (T (std-%simple-load-external s))))

;;; Simple provide: must take uppercase strings only
(defun STD-%SIMPLE-PROVIDE (s)
  (if *STD:MODULES-DELAYED*
    (setq *STD:MODULES-DELAYED*
      (std-remove s *STD:MODULES-DELAYED*)))
  (if (std-%simple-module-string-defined-p s)
    s
    (progn
      (std-%load-verbose-print 
       (list "\n" "Module" " " s
	          " provided. "))
      (setq *MODULES* (cons s *MODULES*))
    )))

;;;---------------------------------------------------------------------
;;; Modules
;;; Not defined in the X3J13 ANSI CL standard but in most lisps and
;;; scheme libraries.

;;; All STD-% functions are implementation specific hacks
;;; and may be replaced by other functions and functionality.
;;; e.g. a global list *MODULES* of all loaded modules.
;;;  *MODULES-IN-OBARRAY* -boolean- is to test the two implementations.
;;;    I don't know yet if to keep the IN-OBARRAY version
;;;    or the simplier *MODULES* list version.

;;; (STD-REQUIRE	str|sym)	; loads a module if necessary
;;; This is the actual full definition of std-require, the simple
;;; version requires a string without path and extension.
;;;
;;; This version accepts symbols and strings or filenames.
;;; Works best with strings. (std-load) is defined in STDLISP
(defun STD-REQUIRE (path / name)
  (setq name (std-%force-module-string-dot path))
  (cond ((std-%module-string-defined-p name)
          ;; "REGISTRY" needs special treatment
          (if (and (= name "REGISTRY") (not REGISTRY-ACAD-PRODUCT-KEY))
            (std-load "REGISTRY"))
          (if *STD:MODULES-DELAYED*
            (setq *STD:MODULES-DELAYED*
                 (std-remove name *STD:MODULES-DELAYED*)))
          name)
	(*STD:%PROJECT*
	  (setq *STD:MODULES-DELAYED*
 	    (cons name *STD:MODULES-DELAYED*))
	  (std-%load-verbose-print
            (list "\n..." (std-msg "Module") " " name 
		  (std-msg " delayed... ")))
	  'delayed)
        ((stringp path)
         (if (std-load path)
           (progn
             (if *STD:MODULES-DELAYED*
               (setq *STD:MODULES-DELAYED*
                 (std-remove name *STD:MODULES-DELAYED*)))
             path
           )))
        (T
         (if (std-load name)
           (progn
             (if *STD:MODULES-DELAYED*
               (setq *STD:MODULES-DELAYED*
                 (std-remove name *STD:MODULES-DELAYED*)))
             name
           )))
        ))

;;; (STD-PROVIDE	str|sym)	; define a module
;;; This version accepts symbols and strings, and may use
;;; specially prefixed symbols in the atomtable.
;;; Works best with strings.
(defun STD-PROVIDE (name)
  ;; We won't provide dots in the module name
  (std-%simple-provide (std-%force-module-string-dot name)))

(defun STD-MODULE-DEFINED-P (name)
  (cond ((stringp name)
	 (std-%module-string-defined-p name))	; fixed for strings
        ((std-symbolp name)
      	  (std-%module-string-defined-p (std-%force-module-string name)))
        (T
	 (std-error 
	  (list "Module name must be a string or symbol -- "
		name)))))

;; Assuming upcase strings
(defun STD-%SIMPLE-MODULE-DEFINED-P (name)
   (std-%simple-module-string-defined-p name))

;;; Therefore we provide a special method for strings too.
;;; Used internally only. Must be not destructive!
;;;  if (boundp (std-%module-string->symbol "x")) => nil
;;;  then all subsequent (boundp (std-%module-string->symbol "x"))
;;;    must return nil also!
;;; The other way is the simple global *MODULES* list
(defun STD-%MODULE-STRING-DEFINED-P (s)
  ;; temp. workaround for the ADS version
  (if (= s "STDLIB-ADS")
    (or (numberp *STDLIB-ADS-VERSION*)
        ;; fix for versions before 0.400013
	(numberp *STDLIB-ADSRX-VERSION*))


      ;; T or nil only
      (and (member (strcase s) *MODULES*))

    ))

;; assume upcase strings
(defun STD-%SIMPLE-MODULE-STRING-DEFINED-P (s)
  (if (= s "STDLIB-ADS")
    (or (numberp *STDLIB-ADS-VERSION*)
	(numberp *STDLIB-ADSRX-VERSION*))
      ;; T or nil only
      (and (member s *MODULES*))))


;;; Module string->symbol
;;; We may use a magic (hopefully unique) name to avoid nameconflicts
;;; in the atoms hashtable.
(defun STD-%MODULE-STRING->SYMBOL (s)
  (read s))

;;; Cannot be used for module names with dots.
;;; Use std-%force-module-string-dot for these.
(defun STD-%FORCE-MODULE-STRING (name)
  (cond ((stringp name)
	 (if (std-acad-connection-p)
	  (strcase name)
	  (std-strcase name)))
        ((std-symbolp name)
          (std-symbol-name name))  	; we use uppercase
        (T (std-error
	     (list "Module name must be a string or symbol -- "
		   name)))))

;;; Removes the extension in filenames given to require
;;; as in (std-require "FILEEXT.FAS")
;;; => "FILEEXT"
(defun STD-%FORCE-MODULE-STRING-DOT (name)
;  (std-%simple-require "STDSTR")
  (std-%string-without-dots (std-%force-module-string name)))

;;; Returns list of loaded modules as strings or symbols.
;;; Re Strings VL RTS troubles as outlined by Serge Pashkov:
;;;  (vl-eval-str "(std-modules)" will print and return nothing, but
;;;  (vl-eval-str "(princ (std-modules))" will work.
;;;
;;;  (setq m (vl-eval-str "(std-modules)")) not.
;;;  (vl-eval-str "(setq m (std-modules))") and
;;;  (vl-eval-str "(vlisp-export-symbol 'm)") will not work with symbols!
;;;  Instead use:
;;;  (vl-eval-str "(setq m (mapcar 'vl-princ-to-string (std-modules)))")
;;;  (vl-eval-str "(vlisp-export-symbol 'm)")
;;;
;;; These constructs are only needed if you need vl symbols in autolisp
;;; but normally you work just in one system, not in both.


;;; List of all define modules
;;; Unbind all modules with (mapcar 'std-module-unbind (std-modules))
(defun STD-MODULES (/ s missing)
  (if (and *STD:MODULES-DELAYED*
           (setq missing (std-set-difference *STD:MODULES-DELAYED* *MODULES*)))
    (std-verbose-print
	(list "" missing "\n")))
  *MODULES*
)

;;; Undefine a module and unbind according symbols
;;;  *STD:UNBIND-SYMBOLS* -boolean- decides if to unbind variables too.
;;; Default: nil (not yet)
(defun STD-MODULE-UNBIND (name / s global func)
  (setq *MODULES* (std-remove
        (cond
	  ((stringp name) name)
	  ((std-symbolp name)
            (std-%force-module-string name))
	)
        *MODULES*))
  (if *STD:UNBIND-SYMBOLS*
    (progn
	;; possibly unbind all symbols with the module prefix, such as
	(setq func   (strcat (std-%force-module-string name) "-"))
	(setq global (strcat "*" (std-%force-module-string name)))
	(foreach s (atoms-family 1)
	    (if (or (std-strcmp func s)
	            (and (std-strcmp global s)
			 (= "*" (std-lastchar s))))
	      (set (read s) nil)
	    )
	)
    )
  )
)
;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDMODUL-SYMBOLS*))
(setq *STDMODUL-SYMBOLS* nil)


;;; This is no seperate module, just an extension for STDINIT
(std-%simple-provide "STDINIT")


;;; If all required STDLIB modules are contained in one project,
;;; we don't have to load it explicitly.
;;; STDLIB.FAS already contains it!
(if (not *STD:%PROJECT*) (progn

(std-%simple-require "STDSTR")  ; std-modules: std-str-1,
				; std-module-unbind: std-lastchar
				; std-%force-module-string-dot:
				;  std-%string-without-dots
(std-%simple-require "STDLISP") ; std-require: std-load-external

))

;;; >>cut here  ---------------------------------------------- <<cut here
;;; $Id: STDSTR.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $-*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:30:50 rurban>
;;; Copyright (c) 1998,99,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; String functions for the STDLIB
;;; This can be loaded standalone.



;;; STATUS:
;;;   Not fully tested but they should be okay.
;;;   Some names have been renamed lately to long versions, and some
;;;   zero-based functions added, for downwards compatibilities sake.
;;;
;;; The naming scheme is twofold. There are short STRxxx
;;; versions and long STRING-xxx versions. The short names match
;;; CLIB names, the long Common Lisp.
;;;
;;; $Log: STDSTR.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; 2000-10-02 15:28:49 rurban
;;;   STD-STRING-WC-REPLACE: added "i", "x",
;;;     added not-nested functional string splice at "e"
;;; 2000-10-02 14:23:34 rurban
;;;   STD-STRING-WC-REPLACE: 
;;; 2000-10-01 14:01:27 rurban
;;;   added STD-STRING-WC-REPLACE
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;; 2000-03-16 10:46:35 rurban
;;;   changed STD-%STRING-8BIT->OCTAL to put the double-quote around it.
;;;   so it's a fully portable string-unexpand now, reverse to princ
;;;   needed for the STDLOCAL message tables.
;;;   not destructive anymore
;;; 2000-04-07 19:20:22 rurban
;;;   Boyer-Moore method for STD-STRPOS, VLISP method added
;;;   DCLSTRING<->STRLIST docs added to STDLIB.HLP
;;; 2000-04-10 13:32:20 rurban
;;;   fixed STD-STRPOS for sys VLISP
;;;
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDSTR-SYMBOLS* 
    '(STD-STRING-NOT-EMPTY-P STD-ISCHAR
      STD-ISDIGIT	     STD-ISCONTROL
      STD-ISUPPER	     STD-ISLOWER
      STD-TOLOWER	     STD-TOUPPER
      STD-STRING=	     STD-STRING/=
      STD-STRING<	     STD-STRING<=
      STD-STRING>	     STD-STRING>=
      STD-STRING-EQUAL	     STD-STRING-NOT-EQUAL
      STD-STRING-LESSP	     STD-STRING-NOT-LESSP
      STD-STRING-GREATERP    STD-STRING-NOT-GREATERP
      STD-STRCASE	     STD-STRING->LIST
      STD-LIST->WSTRING	     STD-LIST->STRING
      STD-STRING-ELT	     STD-FIRSTCHAR
      STD-LASTCHAR	     STD-LASTCHARS
      STD-STR-1		     STD-STR-N
      STD-STRPOS	     STD-STRIPOS
      STD-STRING-POSITION    STD-STRCMP
      STD-STRICMP	     STD-STRING-MISMATCH
      STD-STRCOUNT	     STD-WCIMATCH
      STD-STRMEMBER	     STD-STRIMEMBER
      STD-STRLIST-MATCH	     STD-STRLIST-IMATCH
      STD-STRING-UPCASE	     STD-STRING-DOWNCASE
      STD-STRING-CAPITALIZE  STD-STRTOK
      STD-STRSPLIT	     STD-STRJOIN
      STD-STRING->STRLIST    STD-CSSTRING->STRLIST
      STD-STRLIST->STRING    STD-STRLIST->CSSTRING
      STD-STRLIST->DCLSTRING STD-DCLSTRING->STRLIST
      STD-STRING-LEFT-TRIM   STD-STRING-RIGHT-TRIM
      STD-STRING-TRIM	     STD-TRIM
      STD-STRING-REMOVE-COMMENTS
      STD-STRING-RIGHT-PAD-CHAR
      STD-STRING-LEFT-PAD-CHAR
      STD-RPAD		     STD-LPAD
      STD-STRCENTER	     STD-STRCHG
      STD-STRING-SUBST	     STD-STRING-TRANSLATE
      STD-STRING-WC-REPLACE
      STD-ENTAB		     STD-ENTAB8
      STD-DETAB		     STD-DETAB8
      STD-RTOS		     STD-MAKE-STRING))))


;;; Predicates
;;;already defined in STDINIT
;(defun STRINGP (s) (= (type s) 'STR))  ; eq has errors with BI4 (acomp)

;;;  is non-empty string?
(defun STD-STRING-NOT-EMPTY-P (s)
  (and (stringp s) (/= s "")))

;; For the character predicates we use clib names
;; in CL named: alpha-char-p
(defun STD-ISCHAR (char)
  ;; So far we use only the Windows (ANSI) charset.
  (or (std-isupper char)
      (std-islower char)))

;;; in CL named: digit-char-p
(defun STD-ISDIGIT (char)
  (if (eq (type char) 'INT)
    (<= 48 char 57)
    (<= "0" char "9")))

;;; The class of chars that are not correctly printed in DCL
;;; edit_boxes or list_boxes.
;;; Checks for the class of non-graphic characters,
;;; also named formatting characters or control characters.
;;; In DCL control chars are not printed well.
(defun STD-ISCONTROL (char)
  (if (eq (type char) 'INT)
    (< char 32)
    (< char " ")))


;;; Special 8 bit characters and
;;; Codepages and upper-lower ranges and translation tables
;;;---------------------------------------------------------------------
;;; See STDLOCAL.
;;; With ISO cp's or Unicode we have no problems.
;;; upcase <=> downcase == +-32 or 0x20 on ISO cp's and Unicode,
;;; but the range of upcase/downcase letters is different.
;;; Old dos charsets like dos437, dos850, ... are completely different
;;; and unique.
;;; We set and get our codepage with STD-SYS-CODEPAGE from STDLOCAL
;;; This affects so far:
;;;   std-isupper 	  (range)
;;;   std-islower 	  (range)
;;;   std-toupper 	  (low-level int translation)
;;;   std-tolower 	  (low-level int translation)
;;; Note also that these names are clib based, not common lisp!

;;; Subject to redefinition in STDLOCAL
;;; in CL named: upper-case-p
(defun STD-ISUPPER (char)
  (if (eq (type char) 'INT)
    (setq char (chr char)))
  ;; This is only iso8859-1, also named ANSI or Latin-1
  ;; This is standard in MSWIN and most unix
  (or (<= "A" char "Z")
;;;   (<= "À" char "ß")
      (<= "\300" char "\336"))) ;CM

;(defun STD-ISLOWER (char)
;  (and (std-ischar char) (not (std-isupper char))))

;;; Subject to redefinition in STDLOCAL
;;; in CL named: lower-case-p
;;; fixed by MASAMI Chikahiro
(defun STD-ISLOWER (char)
  (if (eq (type char) 'INT)
    (setq char (chr char)))
  ;; This is only iso8859-1
  (or (<= "a" char "z")
;;;      (<= "à" char "ÿ")
      (<= "\340" char "\377"))) ;CM

;;; low-level modifier, takes only an integer number
;;; in CL named: char-downcase
(defun STD-TOLOWER (i)
  (if (or (<= 65 i 90)
	  (<= 192 i 223))
    (+ i 32)
    i))

;;; low-level modifier, takes only an integer number
;;; in CL named: char-upcase
(defun STD-TOUPPER (i)
  (if (or (<= 97 i 122)
	  (<= 224 i 255))
    (- i 32)
    i))

;;; -------------------------------------------------------------------73
;;; Comparison


;;; "stronger" comparisons: case-Sensitive
(defun STD-STRING=  (s1 s2)(= s1 s2))
(defun STD-STRING/= (s1 s2)(/= s1 s2))
(defun STD-STRING<  (s1 s2)(< s1 s2))
(defun STD-STRING<= (s1 s2)(<= s1 s2))
(defun STD-STRING>  (s1 s2)(> s1 s2))
(defun STD-STRING>= (s1 s2)(>= s1 s2))

;;; "weaker" comparisons: case-Insensitive
(defun STD-STRING-EQUAL (s1 s2)
  (= (std-strcase s1) (std-strcase s2)))
(defun STD-STRING-NOT-EQUAL (s1 s2)
  (/= (std-strcase s1) (std-strcase s2)))
(defun STD-STRING-LESSP (s1 s2)
  (< (std-strcase s1) (std-strcase s2)))
(defun STD-STRING-NOT-LESSP (s1 s2)
  (>= (std-strcase s1) (std-strcase s2)))
(defun STD-STRING-GREATERP (s1 s2)
  (> (std-strcase s1) (std-strcase s2)))
(defun STD-STRING-NOT-GREATERP (s1 s2)
  (<= (std-strcase s1) (std-strcase s2)))


;;; -------------------------------------------------------------------73
;;; String Conversion

;;; AutoCAD independent strcase
;;; Affected by redefinition of STD-TOUPPER in STDLOCAL
(defun STD-STRCASE (s)
  (cond
    ((std-list->string
     (mapcar
      (function std-toupper)
      (std-string->list s))))
    (T "")))


;;; Safe redefinition:
;;; (defun std-%force-module-string (name)
;;;   (cond ((stringp name) (std-strcase name))
;;;         ((std-symbolp name) (std-symbol-name name))
;;;      (T (std-error (list "Module name must be a string or symbol -- "
;;;                             name)))))

;;; numbers need less memory space and housekeeping
;;; "12" => (49 50)

(defun STD-STRING->LIST (s / lst)
  (if (stringp s)
    (while (/= s "")
      (setq lst (cons (ascii (substr s 1 1)) lst)
            s   (substr s 2))))
  (reverse lst)
)
 

;;; numbers need less memory space and housekeeping
;;; unicode support
;;; (49 50 9600) => "12€"
(defun STD-LIST->WSTRING (lst / s)
  (if (listp lst)	; fixed
    (progn
      (setq s "")
      (while lst
        (setq s   (strcat s (chr (car lst)))
              lst (cdr lst)))))
  s
)

;(if (eq (type vl-list->string) 'SUBR)


(defun std-list->string (lst) (std-list->wstring lst))
 

;)

;;; -------------------------------------------------------------------73
;;; Accessors, Searching, Matching


;;; ascii int of i'th char (zero-based)
(defun STD-STRING-ELT (s i) (std-elt (std-string->list s) i))
 


;;; return the first char
(defun STD-FIRSTCHAR (s)
  (substr s 1 1))

;;; return the last char
(defun STD-LASTCHAR (s)
  (if (std-string-not-empty-p s)
    (substr s (strlen s) 1)
    ""))

;;; return the last n chars
(defun STD-LASTCHARS (n s)
  (cond
    ((>= n (strlen s)) s)
    ((std-string-not-empty-p s)
      (substr s (1+ (- (strlen s) n)) n))
    (T "")))

;;; "string minus one"
;;; return the string without the last char
(defun STD-STR-1 (s)
  (if (STD-STRING-NOT-EMPTY-P s)
    (substr s 1 (1- (strlen s))) s))

;;; "string minus n"
;;; return the string without the last n chars
(defun STD-STR-N (n s)
  (cond
    ((>= n (strlen s)) "")
    ((STD-STRING-NOT-EMPTY-P s)
      (substr s 1 (- (strlen s) n)))))

;;; Returns first position of match in str or nil
;;; case-sensitive, no regex, plain substring, first index = 1
;;; Boyer-Moore method by Serge Pashkov, faster than old strpos below
;;; Lisp translation of Modula-2 program from the book
;;; "Algorithms and data structure", N.Wirth, Prentice-Hall, Inc. 1986
;;; see contrib/boyer-moore.lsp for benchmarks and samples
(defun STD-STRPOS (match s / m n i d p c mc mm1)

  (setq m (strlen match)
        n (strlen s))
  (if (<= 1 m n)
    (progn
      (setq mc (substr match m 1) mm1 (1- m))
      (setq d nil i 0)
      ;;build table of shifts d
      (while (< (setq i (1+ i)) m)
        (setq d (cons (cons (substr match i 1) (- m i)) d))) 
      ;;search match within string
      (setq i m)
      (while (<= 1 i n)
        (setq i (if (or (/= (setq c (substr s i 1)) mc)
                        (/= (substr s (- i mm1) m) match))
                  (+ i (if (setq p (assoc c d)) (cdr p) m))
                  (- mm1 i)))   ;negative value for ending
      )
      ;;return position or nil
      (if (< i 0) (- i))
    )
  )


)

;;; Return first position of match in str or nil
;;; case-sensitive, no regex, plain substring
;;; (std-strpos substr string)
;;;   (std-strpos "" "abc") => 1
;;; See Boyer-Moore by Serge Pashkov above
;;; (improvable by using the Knuth-Morris-Pratt method, 
;;;  backing up optimally?)
(defun STD-%OLD-STRPOS (match s / i1 l1 l2 pos)
  (setq l2 (strlen match)
        l1 (1+ (- (strlen s) l2))
        i1 1
        pos nil)
  (if (> l2 0)
    (while (<= i1 l1)
      (if (= (substr s i1 l2) match)
        (setq pos i1
              i1 (1+ l1))
        (setq i1 (1+ i1))
      )
    )
  )
  pos
)
;;; case-insensitive std-strpos
(defun STD-STRIPOS (match s)

  (std-strpos (strcase match) (strcase s))


)



;;; remove the following for linked VLISP:

;;; as the new vlisp function, searches for one integer char
;;; zero-based!
;;;   (std-string-position (ascii "A") "ABC") => 0
;;;   (std-string-position 50 "ABC") 	      => nil
;;; could be implemented better.
(defun STD-STRING-POSITION (i s / pos)
  (if (setq pos (std-strpos (chr i) s))
    (1- pos)))
 

;;; if the substring is the first part of the string line
;;; (std-strcmp "debug" "debugging") => T
(defun STD-STRCMP (sub line)
  (= (substr line 1 (strlen sub)) sub))

;;; case insensitive
(defun STD-STRICMP (sub line)
  (std-strcmp (strcase sub)(strcase line)))
;;; remove the following for linked VL:


;;; remove the following for linked VLISP:

;;; length of the longest common prefix
;;;   (std-string-mismatch "STD-FUN" "STD-VAR")   => 4
;;;   (std-string-mismatch "STD-VAR" "*STD-VAR*") => 0
(defun STD-STRING-MISMATCH (s1 s2 / i l j)
  (setq j nil i 1 l (min (strlen s1) (strlen s2)))
  (while (< i l)
    (if (= (substr s1 i 1)(substr s2 i 1))
      (setq i (1+ i))
      (setq j i i (1+ l))	; fixed
    )
  )
  (if j (1- j) 0)
)
 



;;; (std-strcount substr string) counts substring in string, 
;;; case-sensitive, no regex!
(defun STD-STRCOUNT (match s / i p lst)
  (setq i 0
	lst (strlen match))
  (while (setq p (std-strpos match s))
    (setq s (substr s (+ p lst))
	  i (1+ i)
    )
  )
  i	; fixed
)


(defun STD-WCIMATCH (s match)
  (wcmatch (strcase s) (strcase match)))

;;; 
;;; linking is disturbed by double definition, but wcmatch needs 
;;; ACAD anyway
;;; (if (not (std-acad-connection-p))
;;;   ;; wcmatch needs acad too...
;;;   (defun STD-WCIMATCH (s match)
;;;     (wcmatch (std-strcase s) (std-strcase match)))
;;; )
;;; 

;;; match in stringlist lst?
;;; (member) with (wcmatch) comparison
;;; returns the list from the first found match on.
;;; see also STD-STRLIST-MATCH which returns all found matches
(defun STD-STRMEMBER (match lst / okay)
  (if (setq okay (member match lst))		; exact match
    okay					; or
    (progn					; try wcmatch
      (setq okay nil)
      (while (and lst (not okay))
        (if (and (stringp (car lst))		; string
                 (wcmatch (car lst) match))	; and matches
          (setq okay lst)			; then okay
        )
        (setq lst (cdr lst))
      )
      okay
    )
  )
)

;;; (std-strmember) case insensitive
(defun STD-STRIMEMBER (match lst / okay)
  (if (setq okay (member match lst))		; exact match
    okay					; or
    (progn					; try wcmatch
      (while (and lst (not okay))
        (if (and (stringp (car lst))      	; string
                 (std-wcimatch (car lst) match)) ; and matches
          (setq okay lst)                       ; then okay
        )
        (setq lst (cdr lst))
      )
      okay
    )
  )
)

;;; Returns all matching elements in string list with wcmatch-comparison
;;; like std-strmember but returns only all matching elements.
(defun STD-STRLIST-MATCH (match lst / rslt s)
  (setq rslt nil)
  (foreach s lst
    (if (wcmatch s match) (setq rslt (cons s rslt))))
  (reverse rslt))

;;; case insensitive version
(defun STD-STRLIST-IMATCH (match lst / rslt s)
  (setq rslt nil match (std-strcase match))
  (foreach s lst
    (if (wcmatch (std-strcase s) match)
      (setq rslt (cons s rslt))))
  (reverse rslt))


;;;also:
;;;(defun STD-STRLIST-MATCH (_match lst)
;;;  (std-remove-if-not 'null
;;;    (mapcar (function (lambda (_s) (if (wcmatch _s _match) _s)))
;;;	    lst)))
;;; or
;;;(defun STD-STRLIST-MATCH (s lst / out)
;;;  (while (setq lst (std-strmember s lst))
;;;    (setq out (cons (car lst) out)
;;;          lst (cdr lst))
;;;  )
;;;  (reverse out)
;;;)

;;; -------------------------------------------------------------------73
;;; Conversion ("destructive")

;;; (STD-STRING-UPCASE   'str) => str
;;; Affected by redefinition of STD-TOUPPER in STDLOCAL
;;; STRCASE does not process foreign special 8-bit characters in
;;; earlier releases.
;;; We use std-strcase instead and provide destructive modification.
;;; Accepts and modifies symbols, "destructive"
(defun STD-STRING-UPCASE (_$s / _$tmp)
  (if (= (type _$s) 'SYM) (setq _$tmp _$s _$s (std-symbol-value _$s)))
  (setq _$s (std-strcase _$s))
  (if _$tmp (set _$tmp _$s) _$s)
)

;;; (STD-STRING-DOWNCASE 'str)
;;; Affected by redefinition of STD-TOUPPER in STDLOCAL
;;; STRCASE does not process foreign special 8-bit characters in
;;; earlier releases.
;;; Accepts and modifies symbols, "destructive"
(defun STD-STRING-DOWNCASE (_$s / _$tmp _$i)
  (if (= (type _$s) 'SYM) (setq _$tmp _$s _$s (std-symbol-value _$s)))
  (setq _$s (std-list->string
    (mapcar
      (function std-tolower)
      (std-string->list _$s))))
  (if _$tmp (set _$tmp _$s) _$s)
)


;;; (STD-STRING-CAPITALIZE 'str)
;;; Splits strings at defined word boundaries, upcases the first char
;;; and lowers the rest.
;;; Accepts and modifies symbols, "destructive"
(defun STD-STRING-CAPITALIZE (_$s / _$tmp lst new _seps _ch _up)
  ;(std-error "STD-STRING-CAPITALIZE is broken")
  (if (= (type _$s) 'SYM) (setq _$tmp _$s _$s (std-symbol-value _$s)))
  (setq lst  (std-string->list _$s)
	new  nil
	_up T	; fixed
	_seps (std-string->list " \r\n\t!?\"\'\\=+-,.;/\(\)"))
  (setq _$s (std-list->string (mapcar
    (function (lambda (_ch)
       (cond ((member _ch _seps) (setq _up t) _ch)
	     (_up (setq _up nil) (std-toupper _ch))
	     (T  (std-tolower _ch)))))
     lst)))
  (if _$tmp (set _$tmp _$s) _$s)
)

;;; split a string at word boundaries into lines
;;; with maximal length len.
;;; FIXME: "," is converted to " "
(defun std-%line-split (s len / words line lst)
  (setq words (std-strtok (std-detab8 s) " \r\n\t,")
	line "")
  (while words
    (if (> (strlen line) len)
      (setq lst  (cons (std-str-1 line) lst)
	    line (strcat (car words) " "))
      (setq line (strcat line (car words) " ")))
    (setq words (cdr words)))
  (reverse (cons (std-str-1 line) lst))
)

;;; Portable Stringification (STRING-UNEXPAND)
;;; We provide a unique and transportable way to represent
;;; special extended characters not in the ASCII character set 0-127
;;; Internet transport may be 7-bit only. mime-types may be ignored
;;; or configured wrong. So we use octal, 3 nums prefixed with "\".
;;; unicode and multibyte character strings are represented differently
;;; so there should be no problem. (\U+xxxx and \M+nxxxx)
;;; To be used for expansion with princ (not prin1)
;;;
;;; Note on printing and converting:
;;;  The result of the conversion is not the same as the original string!
;;;  Only the printed result with escape expansion (princ or prompt)
;;;  may be taken as input for strings which internally use the local
;;;  character table.
;;;
;;; (setq s (std-%string-8bit->octal "äüßç")) => "\"\\344\\374\\337\\347\""
;;; =>
;;; (progn (princ s) (prin1)) prints "\344\374\337\347"
;;;  which may be pasted as string or used in external
;;;  message files portably read in with read-line.
;;; 2000-03-15: put double-quotes around "\\..."
(defun STD-%STRING-8BIT->OCTAL (s)
  (std-%simple-require "STDMATH")
  (apply 'strcat
    (mapcar (function (lambda (i)
	 (if (> i 127)
	  ;; 92 or \134 is thanksfully the same on PC-DOS (Ascii) and
	  ;; Latin-1 (ANSI)
	  (strcat (chr 92) (std-num->oct i))
	  (chr i))))
    (append '(34) (std-string->list s) '(34)))))

;;; Accepts and modifies symbols, "destructive"
;;;(defun STD-%STRING-8BIT->OCTAL (_$s / _$s1 _$lst  _$i _$tmp)
;;;  (if (= (type _$s) 'SYM) (setq _$tmp _$s _$s (std-symbol-value _$s)))
;;;  (std-%simple-require "STDMATH")
;;;  (setq _$s1 ""
;;;	_$lst (std-string->list _$s))
;;;  (while _$lst
;;;    (setq _$i (car _$lst)
;;;          _$s1 (strcat _$s1
;;;                       (if (> _$i 127)
;;;			 ;; 92 or \134 is thanksfully the same on PC-DOS
;;;			 ;; (Ascii) and Latin-1 (ANSI)
;;;			 (strcat (chr 92) (std-num->oct _$i))
;;;			 (chr _$i)))
;;;          _$lst (cdr _$lst)))
;;;  (if _$tmp (set _$tmp _$s1) _$s1)
;;;)
;;;



;;; -------------------------------------------------------------------73
;;; AutoLISP workarounds for VLX extensions


;;; -------------------------------------------------------------------73
;;; Tokenizers
;;; These might be renamed to the long versions:
;;;   std-string-tokenize, std-string-split and std-string-join

;;; Converts string with delimiters into string list
;;; Ignore repeated delims such as white space.
;;; The order of chars in delim is not important.
;;; Might be renamed to std-string-tokenize
;;; Also named lex-string in some Common Lisps.
;;;   (std-strtok " 2   3 " " ") => ("2" "3")
;;;   (std-strtok "f 1,3" ", ")  => ("f" "1" "3")
;;; Same as std-string->strlist
(defun STD-STRTOK (s delims / len s1 i c lst)
  (setq delims (std-string->list delims)
   	len (strlen s)
	s1 ""
	i (1+ len))
  (while (> (setq i (1- i)) 0)
    (setq c (substr s i 1))
    (if (member (ascii c) delims)
      (if (/= s1 "")	; no null tokens
        (setq lst (cons s1 lst)
	      s1 "")
      )
      (setq s1 (strcat c s1))
    )
  )
  (if (/= s1 "")
    (cons s1 lst)   	; no ("" "1" "2")!
    lst
  )
)

;;; The order of chars in delim is not important.
;;; keeping null tokens, not as with std-strtok.
;;; Might be renamed to std-string-split
;;; by Vladimir Nesterowsky
(defun STD-STRSPLIT (s delims / len s1 i c lst)
  (setq delims (std-string->list delims) ; fixed
   	len (strlen s)
	s1 ""
	i (1+ len))
  (while (> (setq i (1- i)) 0)
    (setq c (substr s i 1))
    (if (member (ascii c) delims)
      (if (/= i len) 	; "1,2," -> ("1" "2") and not ("1" "2" "")
        (setq lst (cons s1 lst)
	      s1 "")
      )
      (setq s1 (strcat c s1))
    )
  )
  (cons s1 lst)   	; ",1,2" -> ("" "1" "2")
)

;;; Joins strings from list together again.
;;; Takes all types and converts them to strings (safer).
;;; Opposite to std-strsplit.
;;; Might be renamed to std-string-join
;;;  (std-strjoin '("1" "2" "3") ",") => "1,2,3"
;;;  (std-strjoin '(1 2 3) ",")       => "1,2,3"
(defun STD-STRJOIN (lst _delim)
  (cond
    ((std-string-list-p lst)
      (substr
        (apply (function strcat) 		; force strings
          (mapcar
            (function (lambda (s) (strcat _delim s)))
	    lst))
        (1+ (strlen _delim))))
    (T
      ;(if (not std-princ-to-string) ; VL optimizer
      (std-%simple-require "STDLISP")
      ;)
      (substr
        (apply (function strcat) 		; force strings
          (mapcar
            (function (lambda (s)
	      (strcat _delim (std-princ-to-string s))))
	    lst))
        (1+ (strlen _delim))))))

;;; Converts string with "delimiters into string list"
;;; Ignore repeated delims such as white space, ",,2,3,," -> '("2" "3")
;;;   (std-string->strlist "f 1,3" ", ") -> ("f" "1" "3")
;;; Same as std-strtok.
;;; This name is questionable and may be removed from the release version
(defun STD-STRING->STRLIST (s delims)
  (std-strtok s delims))

;;; "comma list into string list"
;;; Needs only one argument, the string, to be mappable.
;;; "INIT,INIT.LOG" -> '("INIT" "INIT.LOG")
;;; ",,2,3,," -> '("" "" "2" "3" "" "")
(defun STD-CSSTRING->STRLIST (s)
  (std-strtok s ",\t "))

;;; (defun STD-CSSTRING->STRLIST (s / p lst)
;;;   (setq lst nil)
;;;   (while (setq p (std-strpos "," s))
;;;     (setq lst (cons (substr s 1 (1- p)) lst)
;;;           s   (substr s (1+ p))
;;;     )
;;;   )
;;;   (setq lst (cons s lst))
;;;   (reverse lst)
;;; )

;;; This is similar to STD-STRJOIN, but more type restrictive
(defun STD-STRLIST->STRING (strlst _delim)
  (substr
    (apply (function strcat) 		; force strings
      (mapcar
        (function (lambda (s) (strcat _delim s)))
	    strlst))
      (1+ (strlen _delim))))

;;; "Stringlist to comma seperated string"
;;; Needs only one argument, the list, to be mappable
(defun STD-STRLIST->CSSTRING (lst)
  (std-strlist->string lst ","))

;;; "Stringlist to space seperated string" as for dcl list tiles
;;; Needs only one argument, the list, to be mappable
(defun STD-STRLIST->DCLSTRING (lst)
  (std-strlist->string lst " "))

;;; "Space seperated string to stringlist" as for dcl list tiles
;;; Needs only one argument, the string, to be mappable
(defun STD-DCLSTRING->STRLIST (s)
  (std-strtok s "\t "))

;;; -------------------------------------------------------------------73
;;; Formatting:
;;; Destructive versions must not use typical symbol names to avoid
;;; binding conflicts: STD-STRCENTER and STD-STRING-REMOVE-COMMENTS
;;; Most of it is defined in VLISP also.

;;; Remove leading characters in baglist

(defun STD-STRING-LEFT-TRIM (bag s)
  (setq bag (std-string->list bag))
  (while (and (/= s "")(member (ascii (substr s 1 1)) bag))
    (setq s (substr s 2)))
  s
)
 

;;; Remove terminating characters in baglist

(defun STD-STRING-RIGHT-TRIM (bag s / lst)
  (setq bag (std-string->list bag))
  (while (and (/= s "")(member (ascii (std-lastchar s)) bag))
    (setq s (std-str-1 s)))
  s
)




(defun STD-STRING-TRIM (bag s)
  (std-string-left-trim bag (std-string-right-trim bag s)))
 

;;; Remove leading and following whitespace
(defun STD-TRIM (s)
  (std-string-trim " \t\n\r" s))

;;; Returns or modifies a string without inline  and end of
;;; line comments ";"
;;; Inside strings don't remove it!
;;; Preserve "(test \"test\")"
;;; Accepts and modifies symbols, "destructive"
(defun STD-STRING-REMOVE-COMMENTS (_$s / i j _$tmp _evenp)
  (if (= (type _$s) 'SYM) (setq _$tmp _$s _$s (std-symbol-value _$s)))
  (defun _evenp (n) (zerop (rem n 2)))
  (if (and (setq i (std-strpos "\;\|" _$s))
	   (setq j (std-strpos "\|\;" (substr _$s i)))
	   (and (_evenp (std-strcount "\"" (substr _$s 1 i)))	; \"
	        (_evenp (std-strcount "\"" (substr _$s j)))))
    (setq _$s (strcat (substr _$s 1 (1- i))
		      (substr _$s (+ 1 i j)))))
  (if (and (setq i (std-string-position 59 _$s))     ; ";"
	   (/= "\|" (substr _$s (+ 2 i) 1))	     ; "" v0.3020
	   (and (_evenp (std-strcount "\"" (substr _$s 1 i)))
	        (or (zerop i) 
		    (_evenp (std-strcount "\"" (substr _$s i))))))
     (setq _$s (substr _$s 1 i)))
  (if _$tmp (set _$tmp _$s) _$s)
)

;;; Fill string on the right side with char up to length n
;;; Longer strings are ignored, not cut
(defun STD-STRING-RIGHT-PAD-CHAR (s n char)
  (while (< (strlen s) n) (setq s (strcat s char)))
  ;;(substr s 1 n)
  s
)

;;; Fill string on the left side with char up to length n
;;; Longer strings are ignored, not cut
(defun STD-STRING-LEFT-PAD-CHAR (s n char)
  (while (< (strlen s) n) (setq s (strcat char s)))
  ;;(substr s 1 n)
  s
)
;;; Right padding, cuts string or fills string rightwise with spaces.
;;; Name from Intellicad
(defun STD-RPAD (s n)
  (while (< (strlen s) n) (setq s (strcat s " ")))
  (substr s 1 n))

;;; Left padding, cuts string or fills string leftwise with spaces
;;; Name from Intellicad
(defun STD-LPAD (s n)
  (while (< (strlen s) n) (setq s (strcat " " s)))
  (substr s 1 n))


;;; Center string s to length len
;;; Accepts and modifies symbols, "destructive"
(defun STD-STRCENTER (_$s len / i _$tmp)
  (if (= (type _$s) 'SYM) (setq _$tmp _$s _$s (std-symbol-value _$s)))
  (setq _$s (if (> (setq i (- len (strlen _$s))) 0)
    (strcat (std-rpad " " (/ (- len (strlen _$s)) 2)) _$s)
    _$s))
  (if _$tmp (set _$tmp _$s) _$s)
)



;;; -------------------------------------------------------------------73
;;; Miscellaneous: Replace

;;; Change EVERY occurance of old in string s to new, iterative version.
;;; No regular expressions, just a simple case-sensitive substring
;;; replacement. See also std-string-subst
;;; Check for degenerate case when old="" => add only first new
(defun STD-STRCHG (s old new / i ls lold)
  (if (= old "")
    (strcat new s)
    (progn
      (setq lold (strlen old)	    ; length of substr to search
    	    ls (1+ (- (strlen s) lold)) ; max. position to search to
    	    i 1)
      (while (<= i ls)
        (if (= (substr s i lold) old)   ; found
          (setq s (strcat (if (> i 1)   (substr s 1 (1- i)) "")
                	  new
			  (if (<= i ls) (substr s (+ i lold)) ""))
    	    	i (+ i (strlen new)))   ; next position to search
          (setq i (1+ i))
        )
      )
      s
)))


;;; Using subst-alike argument ordering, changes only the FIRST
;;; occurance of old to new (not as subst).
;;; This is to match vl-string-subst which works this way.
;;; degenerate case when old="" works okay
(defun STD-STRING-SUBST (new old s / i ls lold)
  (setq	lold (strlen old)	    ; length of substr to search
	    ls (1+ (- (strlen s) lold)) ; max. position to search to
	    i 1)
  (while (<= i ls)
    (if	(= (substr s i lold) old)   ; found
      (setq s (strcat
        	(if (> i 1)   (substr s 1 (1- i)) "")
        	new
        	(if (<= i ls) (substr s (+ i lold)) ""))
	    i (1+ ls))   ; quit loop
      (setq i (1+ i))
    )
  )
  s
)
 


;;; Replace all occurances of some characters in a string.
;;; same as VL-STRING-TRANSLATE
;;; (std-string-translate "abcdefgh" "ABCDEFGH" "my house is yellow")
;;;       => "my HousE is yEllow"
(defun STD-STRING-TRANSLATE (old new _s / _o _n)
  (mapcar
    (function (lambda (_o _n)
      (setq _s (std-strchg _s (chr _o) (chr _n)))))
    (std-string->list old)
    (std-string->list new)
  )
  _s
)
 



;;; Beta - "String Wildcard Replace"
;;; In STRING, replace occurences matching REGEXP with REPLACEMENT,
;;; COUNT times. If COUNT is nil, replace all.
;;;
;;; Very slow and limited, using the weird wcmatch syntax, compared 
;;; to regex. We should better write a more perl-style replacer.
;;; This syntax is familiar to wcmatch users, similar to shell globbing.
;;;
;;; Special replacements characters:
;;;   $<  pre-match       $&  match         $>  post-match
;;;   %<  delete to left  %   delete        %>  delete to right
;;;   ^   set cursor (not yet)
;;; No grouping by (), so we have no positional matches: $1, $2, ...
;;;
;;; Option:
;;;   e     eval functions in replacement
;;;   i     case insensitive (not yet)
;;;   cNUM  count NUM, replace the first NUM matches
;;;   x     maximal matches rather than minimal. longest found substring.
;;;
;;; Examples:
;;;   (std-string-wc-replace "L:/src/stdlib/STDSTR.lsp"
;;;      "src*/" "new$&" nil)             => "L:/newsrc/stdlib/STDSTR.lsp"
;;;   (std-string-wc-replace "L:/src/stdlib/STDSTR.lsp"
;;;      "/src*/" "%>\\$&\\%<" nil)      => "L:\\src\\stdlib/STDSTR.lsp"
;;;   (std-string-wc-replace "L:/src/stdlib/STDSTR.lsp"
;;;     "L*`." '(std-strcase $&) "ec10") => "L:/SRC/STDLIB/STDSTR.lsp"
(defun STD-STRING-WC-REPLACE (s regexp replacement option
			      / i l len new count
			        $& $< $>
			        expand-p eval-p case-p max-p notend-p
			      )
  (if (and (stringp option)
	   (setq case-p (std-strpos "i" option)))
    (setq regexp (std-strcase regexp)))
  (if
    (if case-p
      (wcmatch (std-strcase s) (strcat "*" regexp "*"))
      (wcmatch s (strcat "*" regexp "*")))
    (progn
      ;; options
      (if (stringp replacement)
        (setq expand-p (or (= "$" (std-firstchar replacement))
		           (wcmatch replacement "*[~``]$[&``']*")))) ; no `$&
      (if (stringp option)
	(progn
      	  (setq eval-p (std-strpos "e" option))
          (if (setq i (std-strpos "c" option))
	    (setq count (atoi (substr option (1+ i)))))
      	  ;;(if (setq case-p (std-strpos "i" option))
	  ;;  (setq regexp (std-strcase regexp)))
      	  (setq max-p  (std-strpos "x" option))))
      (if (or (null count) (zerop count)) (setq count 32767))
      
      ;; expand the substring from left until we find a match
      ;; i: cursor position, l: cursor length
      (setq i 1 len (strlen s))
;     (if max-p 			; end predicate
	;; l is the length of the matched substring
;	(defun notend-p () (>= l 1))
;	(defun notend-p () (< l len)))
      (while (and (< i len) (> count 0))
	;; on "x" minimize from right to left, find the longest match
	;; without x expand from left to right, find the shortest match
	(setq l (if max-p (1+ (- len i)) 1))
	(while (and (not
		      (if case-p
			(wcmatch (std-strcase (substr s i l)) regexp)
			(wcmatch (substr s i l) regexp)))
		    (if max-p (>= l 1) (< l len)))
	  (setq l (if max-p (1- l) (1+ l))))
	(if (and (if max-p (>= l 1) (< l len))
		 (> count 0))	; found
	  (progn
	    ;; bind pseudovariables
	    (setq $<  (substr s 1 (1- i))   ; pre-match
		  $&  (substr s i l)        ; match
		  $>  (substr s (+ i l)))   ; post-match
	    
	    (if eval-p
	      ;; expand pseudovariables in function
      	      (if (stringp replacement)
		(if (and (= "(" (std-firstchar replacement))
			 (not (std-strpos "(" (substr replacement 2))) ;)
			 (= ")" (std-lastchar replacement)))
	          (setq new (eval (read replacement)))
		  ;; FIXME: only not-nested "splice"
		  ;; "..(..()).." will fail
		  (setq new (std-%string-splice-wc replacement)))
	        (setq new (eval replacement)))
	      ;; no "e": expand the string below
	      (setq new replacement))
	    
	    ;; "%.." delete-at-front: eat pre-match or match?
	    (while (= "%" (std-firstchar new))
	      (cond
	        ((= ">" (substr new 2 1))  ; "%>..." eat match
	          (setq $&  (substr $& 2)
		        new (substr new 3)))
		((= "<" (substr new 2 1))  ; "%<..." eat pre-match
	          (setq $<  (std-str-1 $<)
		        new (substr new 3)))
		(T 
	          (setq $<  (std-str-1 $<) ;"%..." eat pre-match
		        new (substr new 2)))))
	    ;;  "..%" delete-at-end:
	    (while (and (= "%"  (std-lastchar new))
			(/= "`" (std-lastchar (std-str-1 new)))) ; unquoted
	      (setq $>  (substr $> 2)	    ; "...%" eat post-match
	      	    new (std-str-1 new)))
	    ;;  "..%[<>]" delete-at-end:
	    (while (or (wcmatch new "*[~``]%[<>]")
		       (wcmatch new "%[<>]"))
	      (cond
	        ((= "%<" (std-lastchars 2 new))  ; "...%<" eat match
	          (setq $&  (std-str-1 $&)
		        new (std-str-n 2 new)))
		((= "%>" (std-lastchars 2 new))   ; "...%>" eat post-match
	          (setq $>  (substr $> 2)
		        new (std-str-n 2 new)))))
	    ;; "..%.." a delete inside replacement is not expanded
	    
	    (if	expand-p
	      ;; expand pseudovariables in string
	      ;; wcmatch supports no grouping (), so we have no $1, $2, ...
	      ;; FIXME: quote $ with `$
	      (setq new (std-strchg new "$&" $& )  ; FIXME: quoted specials!
		    new	(std-strchg new "$<" $< )  ; pre-match
		    new	(std-strchg new "$>" $> )  ; post-match
		    ))
	    ;(std-debug-print (list "$& \"" $& "\" => \"" new "\"\n"))
	    (setq s	(strcat	$< new $>)
		  i	(1+ (strlen (strcat $< new))) ; new cursor
		  count	(1- count)
		  len   (strlen s)))
	  ;; else not found
	  (setq i (1+ i))))
      s)
    s))

;;; eval each function in the string and strcat the results
;;; $< $& and $> are bound
;;; (setq $& "match" s "pre(strcase (substr $& 1 1))(substr $& 2)post")
;;; (setq $& "match" s "pre(strcase $&)(substr $& 2)post")
;;; (std-%string-splice-wc s) => 
;;; Warning: no nested functions yet!
(defun std-%string-splice-wc (s / i new)
  ;; (std-warn "STD-STRING-WC-REPLACE: no function splice yet.")
  (setq new "")
  (while (setq i (std-strpos "(" s))
    (setq new (strcat new
		      (substr s 1 (1- i))	  ; pre
		      (eval (read (substr s i)))) ; func
          s   (substr s (1+ (std-strpos ")" s))))); error handling?
  ;; the rest
  (setq new (strcat new s))
  new)


;;; Convert free spaces in a string to tabs, modulo n, see std-entab8
;;; "x       x1" -> "x<tab>x1"
;;;  1       9
;;; Not destructive, fixed by Serge Pashkov
(defun STD-ENTAB (s n / c i len p sn _$tmp ie)
  ;(if (= (type s) 'SYM) (setq _$tmp s s (std-symbol-value s)))
  (setq sn "" p nil i 1 ie 0 len (strlen s))     ; p: first space
  ;;ie - effective position - takes into account possible \t character
  (while (<= i len)
    (if (= (setq c (substr s i 1)) " ")
      (progn
        (if (not p)(setq p i))
        (if (= (setq ie (rem (1+ ie) n)) 0)        ;fixed
          (setq sn (strcat sn (if (< p i) "\t" c)) ;fixed
                p nil)
        )
      )
      (setq sn
        (if (and p (/= c "\t"))
          (strcat sn (substr s p (1+ (- i p))))
          (strcat sn c)
        )
        ie (if (= c "\t") 0 (1+ ie))
        p nil
      )
    )
    (setq i (1+ i))
  )
  ;(if _$tmp (set _$tmp sn) sn)
  sn
)

;;; Useful default, insert tabs at every 8th place
(defun STD-ENTAB8 (s) (std-entab s 8))

;;; STD-DETAB convert tabs to spaces, modulo n
;;;  "x<tab>x1" -> "x       x1"
;;;                 1       8
;;; Not destructive, fixed by Serge Pashkov
(defun STD-DETAB (_$s n / i _$tmp)
  ;(if (= (type _$s) 'SYM) (setq _$tmp _$s _$s (std-symbol-value _$s)))
  (while (setq i (std-strpos "\t" _$s))
    (setq _$s (strcat
      (substr _$s 1 (1- i))
      (std-rpad "" (- n (rem (1- i) n))) ;fixed
      (substr _$s (1+ i)))))
  ;(if _$tmp (set _$tmp _$s) _$s)
  _$s
)
;;; Useful default, replaces tabs assuming modulo 8
(defun STD-DETAB8 (s) (std-detab s 8))

;;; Removes extension from string
;;; Needed for (std-load-external "FILEEXT.FAS")
;;; This will not work with dots in directory names!
;;; something like std-rposition should be used instead then.
(defun std-%string-without-dots (s / i)
  (if (setq i (std-strpos "." s))
    (substr s 1 (1- i))		; fixed
    s))

;;; "rtos to the max" without "ADS connection to acad" (acad independent)
;;; rtos with maximal digit accuracy. Try it with the largest number of
;;; fractional digits. If the fix conversion does not allow this number,
;;; try it with less digits accuracy.
(defun STD-RTOS (r / n s)
  (if (std-acad-connection-p)
    (progn
      (std-var-push '(("DIMZIN" . 8)))
      (setq s (rtos r 2 14))
      (std-var-pop)
      s
    )

    (progn
      (std-%simple-require "STDMATH")
      (setq n 14)
      (while (and (>= n 0) (not (setq s (std-%rtos-n r n))))
        (setq n (1- n)))
      (setq s (if (std-strpos "." s) (std-string-right-trim "0" s) s))
      (if (= (std-lastchar s) ".")
        (strcat s "0")
        s
    ))

))


;;; rtos to n digits or nil if n is too high,
;;; (if the fractional part is too large)
(defun std-%rtos-n (r n / x sign)
  (cond
    ((and (> r 2147483647)
	  (std-acad-connection-p))
     (rtos r 2 n))

    ;; fractional part too big?
    ;; EXPT is completely broken in larger numbers, so we have
    ;; to use std-expt and load STDMATH...
    ((eq 'REAL (type (setq x (fix (* (std-expt 10 n)
				     (- r (fix r)))))))
      nil)	; to break loop in std-rtos

    ;; integer still too big so that itoa will fail.
    ;; We have problems with overlarge reals which fail above
    ;; but would not fail with lower n
    ((> (abs r) 2147483647)
      ;; print number by number to avoid exponential printing
      (setq sign r r (abs r))
      (setq x
        (if (zerop (- r (fix r)))
	  ""
	  (substr (std-rtos (- r (fix r))) 1)))
      ;; mimic itoa...
      (setq r (fix r))
      (while (> r 0)
	(setq x (strcat (itoa (fix (rem r 10))) x)
	      r (/ r 10))
        (if (and (eq (type r) 'REAL) (< r 2147483647))
	  (setq r (fix r)))
      )
     (if (minusp sign) (strcat "-" x) x)
    )
    (T
      (strcat (itoa (fix r)) "."
        (std-string-left-pad-char (itoa x) n "0")))))


;;; Creates a string of n chars with default value def
;;;   (std-make-string 10 " ")	=> "          "
;;;   (std-make-string 2 ":")	=> "::"
;;; def is coerced to a string.
;;; Support dynamic defaults from functions like:
;;;   (std-make-string 100 (lambda () (std-random 10)))
;;;       => 100 random digits
;;;   Don't create n times def then, only the string of length n
(defun STD-MAKE-STRING (n _def / s)		; like STD-MAKE-LIST
  (setq s "")
  (substr
    (cond
      ((std-functionp _def)
        ;(if (not std-princ-to-string)
        (std-%simple-require "STDLISP") ;)
        (while (< (strlen s) n)
          (setq s (strcat s (std-princ-to-string
                            (apply _def nil))))))
      ((stringp _def)
        (while (< (strlen s) n)
          (setq s (strcat s _def))))
      (T
        ;(if (not std-princ-to-string)
        (std-%simple-require "STDLISP");)
    	(while (< (strlen s) n)
          (setq s (strcat s (std-princ-to-string _def))))))
    1 n))

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDSTR-SYMBOLS*))
(setq *STDSTR-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDSTR")  ; provide it

;;; Removed all ad hoc dependencies

;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDLIST.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:33:04 rurban>
;;; Copyright (c) 1991,1998,1999 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; List functions for the STDLIB

;;; STATUS:
;;; Tested and stable.
;;; added STD-SETNTH, STD-RPLACE is depricated and will be removed.
;;; std-%every-n and std-%some-n don't work yet.
;;; Points moved to seperate STDPOINT module.
;;; Sets and esp. STD-INTERSECTION are incorrect with duplicates.
;;; Recursive functions are almost completely replaced by iterative ones
;;;   not to crash on large lists. (stability)
;;; STD-POSITION takes no strings => STD-STRING-POSITION
;;; STD-CENTROID moved to STDPOINT and renamed to GEOM-CENTROID2D
;;; added EVERY and SOME functionality
;;;
;;; There's no function to search for a list in a list.
;;; You might want to try
;;;   http://xarch.tu-graz.ac.at/autocad/lisp/whmatch.lsp

;;; $Log: STDLIST.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; 2000-10-02 14:23:34 rurban
;;;   improved STD-RANDOMIZE (Fisher-Yates algorithm)
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;; 2000-04-14 16:56:00 rurban
;;; added (STD-SETNTH new i lst)
;;;
;;; ===================================================================73

;;; avoid reload warnings in VL
(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDLIST-SYMBOLS*
;;;(eval (list 'pragma
;;;	    (list 'quote
;;;	      (list (cons 'unprotect-assign
        '(first second	     third		fourth
	  fifth		     sixth		seventh
	  eighth	     ninth		tenth
	  rest		     STD-DOTTED-PAIR-P	STD-NOT-PROPER-LIST-P
	  STD-LIST-LENGTH    STD-STRING-LIST-P	STD-NUMBER-LIST-P
	  STD-POSITION-IF    STD-POSITION-IF-NOT
	  STD-RPOSITION	     STD-RASSOC		STD-NTHCDR
	  STD-FIRSTN	     STD-SUBSEQ		STD-BUTLAST
	  STD-ELT	     STD-SELECT		STD-SETNTH
	  STD-RPLACE	     STD-DELPOS		STD-ADJOIN
	  STD-REMOVE-DUPLICATES			STD-SPLIT-LIST
	  STD-FLATTEN	     STD-ROTATE-RIGHT	STD-ROTATE-LEFT
	  STD-PUSH	     STD-PUSHNEW	STD-PUSHMAX
	  STD-POP	     STD-MAPATOM	STD-EVERY
	  STD-EVERY-N	     STD-SOME		STD-SOME-N
	  STD-INTERSECTION   STD-SET-DIFFERENCE	STD-UNION
	  STD-ORDERED-UNION  STD-SET-EXCLUSIVE-OR
	  STD-SUBSETP	     STD-SET-EQUAL-P	STD-INT-LIST
	  STD-MAKE-LIST	     STD-ISEQ		STD-RSEQ
	  STD-NXTCYC	     STD-COUNT		STD-COUNT-IF
	  STD-COUNT-IF-NOT   STD-STABLE-SORT	STD-MERGE
	  STD-COPY-TREE	     STD-RANDOMIZE	STD-RANDOM-ELT
	  STD-RANDOM-PT))))
;;;)))


;;; FIRST   - first element of the list
(defun first (x) (car x))
;;; REST    - the rest of the list
(defun rest (x)  (cdr x))
;;; Other readable accessors, the element or nil, better than nth
(defun second (lst) (cadr lst))
(defun third (lst)  (caddr lst))
(defun fourth (lst) (cadddr lst))
(defun fifth (lst)  (car (cddddr lst)))

(defun sixth (lst)  (cadr (cddddr lst)))
(defun seventh (lst)(caddr (cddddr lst)))
(defun eighth (lst) (cadddr (cddddr lst)))
(defun ninth (lst)  (car (cddddr (cddddr lst))))
(defun tenth (lst)  (car (cdr (cddddr (cddddr lst)))))


;;;(std-%cond-defun
;;;  '(defun consp (lst) (not (atom lst)))
;;;)

;or
;(defun consp (lst) (and lst (listp lst)))

(defun STD-DOTTED-PAIR-P (lst)
  (and (consp lst) (not (listp (cdr lst)))))

;;; This definitely has to converted to iteration!
;;; but (std-dotted-pair-p (last lst)) will fail!


;;; iterative version
;;; checks if the last cons cell is dotted or not.
;;; improvement over the highly-recursive version above, loops to the
;;; last cons (last is forbidden!) and checks this.
;;; not-proper-lists will fail ("bad list" error) with:
;;;   MEMBER, LENGTH, LAST, FOREACH, APPLY, MAPCAR, REVERSE, ...
;;;
;;;   (std-not-proper-list-p '(0 1 2))     => nil
;;;   (std-not-proper-list-p '(0 0 0 . 1)) => T
;;; but (std-not-proper-list-p '(0 . '(0 0 1))) => nil
;;;   because it is read as '(0 0 0 1)
;;;   (std-not-proper-list-p 0)            => nil
(defun STD-NOT-PROPER-LIST-P (lst)
  (if (consp lst)
    (progn
      (while (and lst (listp (cdr lst)))
        (setq lst (cdr lst)))
      (std-dotted-pair-p lst)
      )))

;;;(std-list-length 1) 		=> nil
;;;(std-list-length nil) 	=> 0
;;;(std-list-length '(0 1 2 0 1))  	=> 5
;;;(std-list-length '(0 1 2 0 . 1))  	=> 4
;;;(std-list-length '(0 1 2 (0 . 1))) 	=> 4
;;;(std-list-length '(0 1 2 0 . (1))) 	=> 5
(defun STD-LIST-LENGTH	(lst / l)
  (cond
    ((null lst) 0)
    ((atom lst) nil)
    (T
     (setq l 1)
     (while lst
       (if (atom (cdr lst))
         (setq lst nil l l)
         (setq lst (cdr lst) l (1+ l)))))))


(defun STD-STRING-LIST-P (lst)
  (and (consp lst)			;a cons?
       (not (std-not-proper-list-p lst))
       (<= (length lst) *MAX-ARGS-LIMIT*)
       (apply (function and) (mapcar (function stringp) lst))))

(defun STD-NUMBER-LIST-P (lst)
  (and (consp lst)			;a cons?
       (not (std-not-proper-list-p lst))
       (<= (length lst) *MAX-ARGS-LIMIT*)
       (apply (function and) (mapcar (function numberp) lst))))


;;; STD-POSITION - returns the index of the first element in the list,
;;; base 0, or nil if not found
;;;   (std-position x '(a b c))   => nil
;;;   (std-position b '(a b c d)) => 1
;;; Ansi CL supports strings too but VL not. If we want to support
;;; strings, then the workaround is not just a simple
;;; (setq std-position vl-position) anymore
;;; ? (std-position (ascii "t") "Test")   => 3   ; zero based!
;;; or (std-position "t" "Test")   => 3   ; zero based!
;;; vlx-position was introduced with vill 3.0
;;; see also: std-string-position which searches for an intchar
;;; and is zero-based.
(if (not STD-POSITION)
 (defun STD-POSITION (x lst / ret)
   ;;(cond ((stringp lst)
   ;;       (std-string-position (if (stringp x) (ascii x) x) lst))
   ;;      (T
	  (if (not (zerop (setq ret (length (member x lst)))))
             (- (length lst) ret))
   ;;    ))
 )
)


;;; -------------------------------------------------------------------73
;;; AutoLISP workarounds for VLX extensions


(if (listp (lambda () T)) (progn

;;; (STD-MEMBER-IF pred lst)
;;; pred requires exactly 1 arg, by Serge Pashkov
;;;   (std-member-if 'numberp '(b a 2 3)) -> (2 3)
(defun STD-MEMBER-IF (pred lst)
  (while (and lst (not (apply pred (list (car lst)))))
    (setq lst (cdr lst)))
  lst
)
;;; highly recursive, may fail on large lists!


;;; (STD-MEMBER-IF-NOT pred lst)
;;; pred requires exactly 1 arg, by Serge Pashkov
;;;   (std-member-if-not 'numberp '(b a 2 3)) -> (b a 2 3)
(defun STD-MEMBER-IF-NOT (pred lst)
  (while (and lst (apply pred (list (car lst))))
    (setq lst (cdr lst)))
  lst
)
;;; highly recursive, may fail on large lists!


;;; STD-REMOVE - Removes all items from a list, also all duplicate elements
;;;   (std-remove 0 '(0 1 2 3 0)) -> (1 2 3)
(defun STD-REMOVE (ele lst)      ; by Serge Volkov, Tony also claims 
;  (if (> (length lst) *MAX-ARGS-LIMIT*)          ; authorship for this
;    (std-error (std-msg "maximal number of arguments exceeded"))
;; FCAD fails with (subst x y nil)
    (if lst
      (apply (function append)
	     (subst nil (list ele)
	       (mapcar (function list) lst)))))
;)

;;; (STD-REMOVE-IF pred lst)     - Conditional remove
;;;   pred requires exactly 1 arg
(defun STD-REMOVE-IF (_pred lst)
;  (if (> (length lst) *MAX-ARGS-LIMIT*)
;    (std-error (std-msg "maximal number of arguments exceeded"))
    (apply (function append)
      (mapcar
        (function (lambda (e) (if (not (apply _pred (list e))) (list e))))
        lst)))	;)

;;; STD-REMOVE-IF-NOT  - keeps all elements to which the predicate
;;;   returns not-nil, say: "keep if"
;;;   pred requires exactly 1 arg
(defun STD-REMOVE-IF-NOT (_pred lst)	;by Vladimir Nesterowsky
;  (if (> (length lst) *MAX-ARGS-LIMIT*)
;    (std-error (std-msg "maximal number of arguments exceeded"))
    (apply (function append)
      (mapcar
        (function (lambda(e) (if (apply _pred (list e)) (list e))))
        lst)))	;)



)) ; eof vl list workarounds

;;; -------------------------------------------------------------------73


;;; (std-position-if 'numberp '(0 1 2 3))  => 0
(defun STD-POSITION-IF (pred lst / ret)
  (if (not (zerop (setq ret (length (std-member-if pred lst)))))
    (- (length lst) ret)))

;;; (std-position-if-not 'stringp '("0" "1" 2 "3"))  => 2
(defun STD-POSITION-IF-NOT (pred lst / ret)
  (if (not (zerop
      (setq ret (length (std-member-if-not pred lst)))))
    (- (length lst) ret)))

;;; STD-RPOSITION  returns the index of the last found element or nil
;;;   (std-rposition 2 '(2 1 2 3)) => 2
(defun STD-RPOSITION (x lst / ret)
  (if (setq ret (member x (reverse lst)))
    (1- (length ret))))



(defun STD-RASSOC (key alst / i)
  (if (setq i (std-position key (mapcar (function cdr) alst)))
    (nth i alst)))


;;; "list behind (including) the nth element"
;;;   (std-nthcdr 1 '(0 1 2 3) => '(1 2 3)
;;; O(i)
(defun STD-NTHCDR (i lst)
  (repeat i
    (setq lst (cdr lst)))
  lst				; fixed
)

;;; "list of first n elements", iterative version
;;; with safety check:
;;;   (std-firstn 1 '(0 1 2)) => '(0)
;;;   (std-firstn 0 '(0 1 2)) => nil
;;;   (std-firstn 4 '(0 1 2)) => '(0 1 2)
;;; O(n+i) if i<n, O(2n) if i>=n
(defun STD-FIRSTN (i lst / out)
  (setq out nil)	; possible VL lsa compiler bug
  (repeat (min i (length lst))
    (setq out (cons (car lst) out) lst (cdr lst)))
  (reverse out))

;;; without safety check, only usable internally
;;;   (std-%firstn 4 '(0 1 2)) => '(0 1 2 nil)
;;; O(i) even if i>n
(defun std-%firstn (i lst / out)
  (setq out nil)	; possible VL lsa compiler bug
  (repeat i
    (setq out (cons (car lst) out) lst (cdr lst)))
  (reverse out))


;;; slower version:
;;; O(4n+i)



;;; STD-SUBSEQ - subsequence including start excluding end
;;;   (std-subseq '(0 1 2 3) 1 3) => '(1 2)
(defun STD-SUBSEQ (lst start endp)
  (if (<= 0 start endp (length lst))	; fixed
    (std-firstn (- endp start) (std-nthcdr start lst))
    (std-error (list "STD-SUBSEQ: " 
		     (std-msg "index out of range") 
		     " - " start " " endp))))

;;; (STD-BUTLAST lst) - list without (last lst)
;;; iterative version
(defun STD-BUTLAST (lst)
  (reverse (cdr (reverse lst))))

;;; recursive version


;;; safe NTH version with reverse order of arguments
;;; Throws an index out of range error.
;;; NTH throws just a bad argument type error
;;; (from LISP)
;;;   (STD-ELT '(0 1) 0) => 0
;;;   (STD-ELT lst -1)   => ERROR
;;;   (STD-ELT lst (length lst)) => ERROR
(defun STD-ELT (lst i)
  (if (< -1 i (length lst))	; fixed
    (nth i lst)
    (std-error (list "STD-ELT: "
		     (std-msg "index out of range")
		     " - " i))))

;;; std-select is NTH expanded for multiple indices. (from xlisp-stat)
;;;   (std-select lst '(0 2 4)) or (std-select lst 0)
(defun STD-SELECT (lst i / x)
  (cond
    ((numberp i) (std-elt lst i))
    ((consp i)
      (while i	; looping over i is faster than mapping over lst
	(setq x (cons (std-elt lst (car i)) x)
	      i (cdr i)))
      (reverse x))))

;;; STD-SETNTH - replace by position, 0 based
;;; If the index is invalid return the unchanged list.
;;; (Sorry, this changed v0.3003)
(defun STD-SETNTH (new i lst)
  (std-%setnth (list new) i lst))

;;; Depricated, same as STD-SETNTH, only reversed argument ordering
;;; STD-RPLACE - replace by position, 0 based. will be removed soon.
;;; This was a bad name. The argument ordering was also
;;; questionable: possible is also (std-rplace new i lst) as in subst
;;; (std-setnth new i lst) or (std-nth<- new i lst)
;;;   (std-rplace '(0 1 2) 0 'NEW) => (NEW 1 2) but
;;;   (std-rplace '(0 1 2) 3 'NEW) => (0 1 2)
(defun STD-RPLACE (lst i new)
  (std-%setnth (list new) i lst))

;;; STD-DELPOS - list without i-th element
;;;   (std-delpos 2 '(0 1 2)) => (0 1)
;;;   (std-delpos 4 '(0 1 2)) => (0 1 2)
;;; Expanded for multiple indices:
;;;   (std-delpos '(2 1) '(0 1 2 3)) => (0 3)
(defun STD-DELPOS (i lst / new n j)
  (if (listp i)
    (progn
      (setq i (std-fast-sort i '<)
	    new '()
	    j 0)
      (foreach n i
        (setq new (cons (- n j) new)
	      j   (1+ j)))
      (foreach i (reverse new)
        (setq lst (std-%setnth nil i lst)))
      lst)
    (std-%setnth nil i lst)))

;;; Fast helper for DELPOS and RPLACE by Serge Pashkov.
;;; The new argument must be a list!
;;; Improved by Daniele Piazza, who thanks Fausto Azzoni. ~45% faster.
;;; Avoid firstn and nthcdr, loop only once through the list.
;;; Tests shows that actual gain is near 3 times at n << N,
;;; 2 times at (N-n) << N and no gain when n ~ N/2 to the old
;;; versions below.
;;; "very weak recursive", worst for elements in the middle.
;;; stack-overflow at about 2^stack-size (~log n) which is
;;; practically infinite: (std-expt 2 994) => 1.67423e+299
(defun std-%setnth (new i lst / fst len)
  (cond
    ((minusp i) lst)
    ((> i (setq len (length lst))) lst)
    ((> i (/ len 2))
      (reverse (std-%setnth new (1- (- len i)) (reverse lst))))
    (T
      (append
        (progn
          (setq fst nil)	; ; possible VL lsa compiler bug
          (repeat (rem i 4)
            (setq fst (cons (car lst) fst)
                  lst (cdr lst)))
          (repeat (/ i 4)
            (setq fst (cons (cadddr lst)
		            (cons (caddr lst)
			          (cons (cadr lst)
				        (cons (car lst) fst))))
                  lst (cddddr lst)))
          (reverse fst)
        )
        (if (listp new) new (list new))	; v0.4001
        (cdr lst)))))



;;; STD-ADJOIN - cons if not in list
;;; (std-adjoin 0 '(1 2))  => (0 1 2)
(defun STD-ADJOIN (x lst)
  (if (member x lst)
    lst
    (cons x lst)))

;;; by Serge Pashkov
;;; The below mentioned lsa compiler bug couldn't be isolated yet.
(defun STD-REMOVE-DUPLICATES (lst / ele new)
  (setq new nil)	; possible VL lsa compiler bug (??)
  (foreach ele lst
    (if (not (member ele new))
      (setq new (cons ele new))))
  (reverse new))

;;; removed: v0.3015
;;; STD-APPEND-JOIN appends only new elements from new to old,
;;; like append but only elements which are not already in old.
;;; use STD-ORDERED-UNION instead.
;;;   (std-append-join '(0 1 2) '(2 3)) => (0 1 2 3)
;;; (defun STD-APPEND-JOIN (_old new)
;;;   (append _old (std-remove-if
;;;  (quote     ;)
;;; 
;;;                 (lambda (x)
;;;                    (member x _old)))
;;;                 new)))

;;; STD-SPLIT-LIST splits list into sublists of maximal length n
;;; n must be > 0!
;;; Iterative version by Serge Pashkov, safer than recursive version
;;;   (std-split-list 2 '(1 2 3 4 5 6)) => ((1 2) (3 4) (5 6))
(defun STD-SPLIT-LIST (n lst / ret out cnt)
  (setq ret nil)	; possible VL lsa compiler bug
  ;; adjust cnt to set incomplete number of elements (if any) for the
  ;; last segment
  (setq cnt (- n (rem (length lst) n)) lst (reverse lst))
  (while lst
    (setq ret (cons (car lst) ret) lst (cdr lst))
    (if (zerop (rem (setq cnt (1+ cnt)) n))
      (setq out (cons ret out) ret nil)))
  (if ret (cons ret out) out))

;;; recursive version by Serge Volkov



;;; Flattens a tree to a plain list, by Serve Pashkov
;;; iterative version, also processes not-proper-lists.
;;;   (std-flatten  '((1 2 3 (3 4)))) => (1 2 3 3 4)
(defun STD-FLATTEN (lst / pend new curr)
  (cond
    ((null lst) nil)
    ((atom lst) lst)
    (T
     (setq pend lst new nil)
     (while pend
       (if (atom pend)                 ; for processing '(1 2 . 3)
         (setq curr pend pend nil)
         (setq curr (car pend) pend (cdr pend)))
       (while (consp curr)
         (if (cdr curr)
           (setq pend (cons (cdr curr) pend)))
         (setq curr (car curr)))
       ;;now curr is atom
       (setq new (cons curr new)))
     (reverse new))))



;;; old, wrong and highly recursive, may fail on large lists!


;;; std-rotate-right rotates a list rightwise
;;; put the last element to the front, 2x slower
;;;   (std-rotate-right '(0 1 2 3)) => '(3 0 1 2)
(defun STD-ROTATE-RIGHT (lst)
  (cons (last lst) (std-butlast lst)))

;;; std-rotate-left rotates a list leftwise
;;; put the first element to the end, 2x faster
;;;   (std-rotate-left '(0 1 2 3)) => '(1 2 3 0)
(defun STD-ROTATE-LEFT (lst)
  (append (cdr lst) (list (car lst))))

;;; The following local symbols may look obfuscated. It is to prevent
;;; binding of symbols with the same name.
;;; All symbols in functions which call eval and which requires symbol
;;; values not defined locally should use special prefixes (here "_$")
;;; not to harm earlier defined symbols.  (prevent "shadowing")

;;; to: lst or 'lst
;;; returns new stack
;;;   (std-push 1 '(2 3)) -> (1 2 3)
(defun STD-PUSH (_$what _$lst / _$tmp)
  (if (= (type _$lst) 'SYM)
    (setq _$tmp _$lst _$lst (std-symbol-value _$lst)))
  (setq _$lst (cons _$what _$lst))
  (if _$tmp (set _$tmp _$lst) _$lst))

;;; Stack argument may be quoted: lst or 'lst
;;; Returns new stack.
;;;   (std-pushnew 1 '(2 3))   => (1 2 3)
;;;   (std-pushnew 1 '(1 2 3)) => (1 2 3)
(defun STD-PUSHNEW (_$what _$lst / _$tmp)
  (if (= (type _$lst) 'SYM)
    (setq _$tmp _$lst _$lst (std-symbol-value _$lst)))
  (setq _$lst (std-adjoin _$what _$lst))
  (if _$tmp (set _$tmp _$lst) _$lst))

;;; with maximal stack size
;;; to: lst or 'lst
;;; returns new stack
;;;   (std-pushmax 1 '(2 3 4 5) 4) -> (1 2 3 4)
(defun STD-PUSHMAX (_$what _$lst nmax / _$tmp)
  (if (= (type _$lst) 'SYM)
    (setq _$tmp _$lst _$lst (std-symbol-value _$lst)))
  (if (>= (length _$lst) nmax)
    (setq _$lst (reverse (cdr (reverse _$lst)))))
  (setq _$lst (cons _$what _$lst))
  (if _$tmp (set _$tmp _$lst) _$lst))

;;; to: lst or 'lst
;;; returns what if SYM or new stack
;;;   (setq lst '(1 2 3))
;;;   (std-pop lst)  -> (2 3), lst = (1 2 3)
;;;   (std-pop 'lst) -> 1, lst = (2 3)
(defun STD-POP (_$lst / _$what _$tmp)
  (if (= (type _$lst) 'SYM)
    (setq _$tmp _$lst _$lst (std-symbol-value _$lst)))
  (setq _$what (car _$lst)
        _$lst  (cdr _$lst))
  (if _$tmp (progn (set _$tmp _$lst) _$what) _$lst))

;;; -------------------------------------------------------------------73


;;; std-mapatom maps a function to every symbol in a tree,
;;; keeping the tree structure intact.
;;; Iterative version by Serge Pashkov.
(defun STD-MAPATOM (func lst / new pend curr pendlst newlst)
  (cond
    ((atom lst)
     (apply func (list lst)))
    (T
     ;; loop until last element, for '(1 2 . 3) too
     (while (consp lst)
       (setq pend (cons (car lst) pend) lst (cdr lst)))
     (setq new (if lst (apply func (list lst)))
           pendlst nil newlst nil)
     (while pend
       (while pend
         (setq curr (car pend) pend (cdr pend))
         (if (atom curr)
           (setq new (cons (apply func (list curr)) new))
           (progn
             (setq pendlst (cons pend pendlst)
                   newlst (cons new newlst) pend nil)
             (while (consp curr)
               (setq pend (cons (car curr) pend) curr (cdr curr)))
             (setq new (if curr (apply func (list curr)))))))
       (while (and pendlst (null pend))
         (setq new (cons new (car newlst)) newlst (cdr newlst)
               pend (car pendlst) pendlst (cdr pendlst))))
     new)))



;;; The old readable recursive version:
;;; highly recursive, may fail on large lists!
;;; by Vladimir Nesterovsky <vnestr@netvision.net.il>


;;; -------------------------------------------------------------------73
;;; Added with v0.4001, 7/21/99
;;; SOME and EVERY for AutoLISP backwards compatibility.
;;; VL takes multiple arguments, so divide them into an one-arg version
;;; and a n-args version for alisp.
;;; These are the optimized versions, the simple one is just
;;;   (apply 'and|or (mapcar 'pred lst))


;;; T if pred is non-nil for every element
;;; or nil if some elements are nil when applied to pred
;;; (apply 'and (mapcar 'pred lst))
;;;
;;; one-arg optimized alisp version
;;;   don't process the whole list,
;;;   break at the first nil
(defun STD-EVERY (pred lst / res)
  (setq	res (apply pred (list (car lst)))
	lst (cdr lst))
  (while (and res lst)
    (setq res (apply pred (list (car lst)))
	  lst (cdr lst)))
  res)

;;; T if pred is non-nil for some elements
;;; or nil if every element is nil when applied to pred
;;; (apply 'or (mapcar 'pred lst))
;;;
;;; one-arg optimized alisp version
;;;   don't process the whole list,
;;;   return T at the first non-nil
;;; (std-some 'numberp '(0 x 1 3)) => T
;;; (std-some 'zerop   '(1 2 3)) => nil
(defun STD-SOME  (pred lst / res)
  (setq	res (not (apply pred (list (car lst))))
	lst (cdr lst))
  (while (and res lst)
    (setq res (not (apply pred (list (car lst))))
	  lst (cdr lst)))
  (not res))


;;; lisp OR
;;; Return the value of the first non-nil value.
(defun STD-%OR  (lst)
  (while (not (car lst))
    (setq lst (cdr lst)))
  (car lst))

;;; for predicates taking more args
(defun STD-SOME-N  (pred lists)
  (cond ((std-vlisp-p)
         (apply (function vl-some) (cons pred lists)))
	((std-vill-p)
         (apply (function vlx-some) (cons pred lists)))
	(T (std-%some-n pred lists))))

;;; for predicates taking more args
(defun STD-EVERY-N  (pred lists)
  (cond ((std-vlisp-p)
         (apply (function vl-every) (cons pred lists)))
	((std-vill-p)
         (apply (function vlx-every) (cons pred lists)))
	(T (std-%every-n pred lists))))

;;; n-args optimized alisp version
(defun std-%every-n  (pred lists / res)
  (std-warn "std-%every-n fails sometimes. ")
  (setq	res   (apply pred (mapcar (function car) lists))
	lists (mapcar (function cdr) lists))
  (while (and res (car lists))
    (setq res   (apply pred (mapcar (function car) lists))
	  lists (mapcar (function cdr) lists)))
  res)

;;; n-args optimized alisp version
(defun std-%some-n  (pred lists / res)
  (std-warn "std-%some-n fails sometimes. ")
  (setq res (not (apply pred (mapcar (function car) lists)))
	lists (mapcar (function cdr) lists))
  (while (and res (car lists))
    (setq res (not (apply pred (mapcar (function car) lists)))
	  lists (mapcar (function cdr) lists)))
  (not res))

;;; -------------------------------------------------------------------73
;;; Sets (operations on unordered lists)
;;; Our sets are unordered, most operations are not stable, but duplicates
;;; are ignored or removed.

;;; The operations are very simple, not optimized for any data.

;;; STD-INTERSECTION returns all elements that are in x and y.
;;; Iterative, stable version by Serge Pashkov.
;;; Duplicates are handled incorrectly yet.
;;;   (std-intersection '(0 1 2 3 4 5) '(2 0 6 7)) => (0 2)
;;; Problem: (std-intersection '(0 0 0 1) '(0 0 1)) => (0 0 0 1)
;;;  but it should be (0 0 1) instead
(defun STD-INTERSECTION (x y / new)
  (setq new nil)	; possible VL lsa compiler bug
  (foreach ele x
    (if (member ele y)
      (setq new (cons ele new))))
  (reverse new))

;;; highly recursive, may fail on large lists!


;;; STD-SET-DIFFERENCE returns all elements of x without elements
;;; also in y, iterative, stable version by Serge Pashkov
;;;   (std-set-difference '(0 1 2 3 4 5) '(2 0 6 7)) => (1 3 4 5)
;;; Big lists as second argument are faster:
;;;   (setq big (std-int-list 1000) small (std-int-list 10))
;;;   (std-time '(std-set-difference small big))
;;;   => (STD-SET-DIFFERENCE SMALL BIG) 	: 0 ms
;;;   (std-time '(std-set-difference big small))
;;;   (STD-SET-DIFFERENCE BIG SMALL) 	: 50 ms
(defun STD-SET-DIFFERENCE (x y / new)
  (setq new nil)	; to avoid possible VL lsa compiler bug (?)
  (foreach ele x
    (if (not (member ele y))
      (setq new (cons ele new))))
  (reverse new))

;;; highly recursive, may fail on large lists!


;;; STD-UNION returns a list of all elements in both lists
;;; Iterative version by Serge Pashkov
;;;   (std-union '(0 1 2 3 4 5) '(2 0 6 7)) => like (7 6 0 1 2 3 4 5)
;;; Ignore duplicates in y are ignored, in x not.
;;; Simplified version without any ordering.
;;; See STD-ORDERED-UNION which keeps the orginal order intact.
;;; Big lists as first argument are faster:
;;;   (setq big (std-int-list 1000) small (std-int-list 10))
;;;   (std-time '(std-union small big))
;;;   => (STD-UNION SMALL BIG) 	: 1222 ms
;;;   (std-time '(std-union big small))
;;;   (STD-UNION BIG SMALL) 	: 0 ms
(defun STD-UNION (x y)
  (foreach ele y
    (if (not (member ele x))
      (setq x (cons ele x))))
  x)


;;; We use reversing only for appending missing elements
;;; after the end of x. Iterative version by Serge Pashkov
(defun STD-ORDERED-UNION (x y / new)
  (setq new (reverse x))
  (foreach ele y
    (if (not (member ele new))
      (setq new (cons ele new))))
  (reverse new))




;;; highly recursive, may fail on large lists!


;;; Elements that appear in exactly one of lst1 and lst2
(defun STD-SET-EXCLUSIVE-OR (x y)
  (append (std-set-difference x y)
          (std-set-difference y x)))

;;; std-subsetp returns T if every element of x is a member of y,
;;; ignoring order and duplicates, iterative version by Serge Pashkov
;;;   (std-subsetp '(7 0) '(2 0 6 7)) -> T
;;;   (std-subsetp '(1 7) '(2 0 6 7)) -> nil
;;; Note: in CL the default comparison is with EQL not with EQUAL!
;;;   (std-subsetp '((1) (2)) '((1) (2))) => T
;;;   but in CL it would return nil
(defun STD-SUBSETP (x y)
  (while (and x (member (car x) y))
    (setq x (cdr x)))
  (null x))

;;; highly recursive, may fail on large lists!


;;; have both sets the same elements? ignoring order and duplicates
;;;  (std-set-equal-p '(a b c) '(c b a))   => T
;;;  (std-set-equal-p '(a b c) '(c b a a)) => T
(defun STD-SET-EQUAL-P (x y)
  (and (std-subsetp x y)
       (std-subsetp y x)))

;;; -------------------------------------------------------------------73
;;; List Creation

;;; std-int-list creates '(0 1 2 ... <n-1>) of length n
(defun STD-INT-LIST (n / lst)
  (setq lst nil)	; possible VL lsa compiler bug
  (repeat n
    (setq lst (cons (setq n (1- n)) lst))))

;;; Creates a list of n elements with initial value def
;;;   (std-make-list 100 nil)
;;; Support dynamic defaults from functions like:
;;;   (std-make-list 100 'std-%random)
;;;   (std-make-list 100 '(lambda () (std-random 10)))
;;;   (std-make-list 100 ( function (lambda () (std-random 10))))
;;; but not: (std-make-list 100 '(std-random 10))
;;; which would create a list of 100 lists
(defun STD-MAKE-LIST (n def / lst)
  (setq lst nil) 	; possible VL lsa compiler bug
  (if (std-functionp def)
    ;; dynamic invocation
    (repeat n
      (setq lst (cons (apply def nil) lst)))
    ;; dynamic list of n def's
    (repeat n
      (setq lst (cons def lst)))))

;;; Tip: To ensure a list of fixed length:
;;; (std-%firstn 4 '(0 1 2)) => '(0 1 2 nil)

;;; integer sequence including start and end element
;;;   (std-iseq 0 1) => (0 1)	; idea from xlisp-stat
(defun STD-ISEQ (start endp / lst)
  (repeat (1+ (- endp start))
    (setq lst (cons endp lst)
	  endp (1- endp)))
  lst)

;;; real sequence, list of n equally spaced real numbers between
;;; start and end (both including)	; idea from xlisp-stat
;;;   (std-rseq 0 1 3) => (0.0 0.5 1.0)
(defun STD-RSEQ (start endp n / lst d)
  (cond
    ((= n 1) start)
    ((< n 1) nil)
    ((< endp start) nil)
    (T
     (setq d (/ (float (- endp start)) (1- n))
	   lst nil)	; possible VL lsa compiler bug
     (repeat n
       (setq lst (cons (+ start (* (setq n (1- n)) d)) lst))))))


;;; "next cyclic number"
;;;   (std-nxtcyc 0 3 '1+) => 1
;;;   (std-nxtcyc 1 3 '1+) => 2
;;;   (std-nxtcyc 2 3 '1+) => 3
;;;   (std-nxtcyc 3 3 '1+) => 0
(defun STD-NXTCYC (i n func)
  (cond
    ((>= (setq i (apply func (list i))) n) 0)
    ((< i 0) (1- n))
    (T i)))


;;; Note: equal is too weak and eq is too strong.
;;; = is the AutoLISP eql alike
(defun STD-COUNT (ele lst / i)
  (setq i 0)
  (foreach x lst
    (if (= ele x) (setq i (1+ i))))
  i)
(defun STD-COUNT-IF (pred lst / i)
  (setq i 0)
  (foreach x lst
    (if (apply pred (list x)) (setq i (1+ i))))
  i)
(defun STD-COUNT-IF-NOT (pred lst / i)
  (setq i 0)
  (foreach x lst
    (if (not (apply pred (list x))) (setq i (1+ i))))
  i)

;; more elegant but more inefficient:



;;; -------------------------------------------------------------------73
;;; Sorting:
;;; vlx-sort introduced a new unwanted behaviour. (also in vl-sort)
;;; It removes duplicate elements, which does not conform to ANSI CL
;;; That's why we define 3 different sort functions for the user and
;;; several algorithms to acomplish it.
;;; STD-SORT is like ANSI CL, should have about O(n logn) average
;;;   complexity of long lists
;;; STD-FAST-SORT uses vlx-sort or same as above,
;;;   it may delete duplicate entries!
;;; STD-STABLE-SORT garantees order of elements
;;;   should be stable, but could be slower or use more stack than
;;;   std-sort.
;;; The algorithms tested were QUICK-SORT, STABLE-QSORT,
;;; MERGE-SORT and INSERTION-SORT and various (previously unstable)
;;; combinations. See the timings and all the algos in SORTTST.LSP

;;; Non-recursive stable merge sort
;;; Overall the best method in AutoLISP. By Serge Pashkov
;;; Slightly improved 3% by using Danieles trick. see std-%setnth
;;;   (setq l '((1 1) (2 1) (1 2) (3 1) (1 3) (3 2) (3 3) (4 1) (1 4)))
;;;   (std-%merge-sort l ( function (lambda (x y)(< (car x) (car y)))))
;;;     => ((1 1) (1 2) (1 3) (1 4) (2 1) (3 1) (3 2) (3 3) (4 1))
;;; It's supposed that used version of std-%insertion-sort is stable.
(defun std-%merge-sort (lst _cmp / mlst rst len)
  ;; split by short lists sorted by insertion to decrease overhead
  ;; length 4 is near optimum for sorted and random lists.
  (setq rst (mapcar (function (lambda (e) (std-%insertion-sort e _cmp)))
                    (std-split-list 4 lst)))
  ;; repeat while leave only 1 sequence
  (while (> (setq len (length rst)) 1)
    ;; Merge every 2 adjacent sorted sequences
    (repeat (/ len 2)
      (setq mlst (cons (std-merge (car rst) (cadr rst) _cmp) mlst)
            rst (cddr rst)))
    ;; Add last sorted sequence (if any)
    (setq rst (reverse (if rst (cons (car rst) mlst) mlst)) mlst nil))
  (car rst))

;;; old version


;;; stable insertion sort, fast for already ordered and short lists
;;; very slow for reversed or random lists O(n^2)
;;; by Serge Pashkov
(defun std-%insertion-sort (lst cmp / M N O cO)
  (setq O (reverse lst) M nil N (list (car O)))
  (while (setq O (cdr O))
    (setq cO (car O))
    (while (and N (apply cmp (list (car N) cO)))
      (setq M (cons (car N) M)
            N (cdr N)))
    (setq N (cons cO N))
    (while M
      (setq N (cons (car M) N) M (cdr M))))
  N)

;;; use merge-sort for everything
(defun STD-STABLE-SORT (lst cmp)
  (std-%merge-sort lst cmp))


(if (not (boundp 'STD-SORT))
 (defun STD-SORT (lst cmp)
  (std-%merge-sort lst cmp))
)

(if (not (boundp 'STD-FAST-SORT))
 (defun STD-FAST-SORT (lst cmp)
  (std-%merge-sort lst cmp))
)


;;; iterative stable version, (see test/SORTTST.LSP)
;;; by Serge Pashkov
;;; This version is ~2 times faster on nearly sorted and nearly
;;; backwards sorted lists, but 5-7% slower on truly random lists,
;;; so we take this one. It has some testing overhead.
;;; hmm, error! no time to check it for now.


(defun std-merge (l1 l2 cmp / lst cl1 cl2)
  (setq cl1 (car l1) cl2 (car l2)
	lst nil)		; possible VL lsa compiler bug
  (while (and l1 l2)
    (if (apply cmp (list cl2 cl1))
      (setq lst (cons cl2 lst) l2 (cdr l2) cl2 (car l2))
      (setq lst (cons cl1 lst) l1 (cdr l1) cl1 (car l1))))
  (append (reverse lst) l1 l2))


;;; This version is 5-7% faster on truly random lists, so we
;;; support it too.
(defun std-%merge-random (l1 l2 cmp / lst cl1 cl2)
  (setq cl1 (car l1) cl2 (car l2)
	lst nil)		; possible VL lsa compiler bug
  (while (and l1 l2)
    (if (apply cmp (list cl2 cl1))
      (setq lst (cons cl2 lst) l2 (cdr l2) cl2 (car l2))
      (setq lst (cons cl1 lst) l1 (cdr l1) cl1 (car l1))))
  (append (reverse lst) l1 l2))

;;; Some more recursion homework: freshly cons each car and cdr part
;;; this would only be useful if there are some destructive functions,
;;; but so far there are none.
;;; This function has no higher meaning, it is included just for fun.
;;; Well, some sort implementations were destructive in the past, but
;;; I cannot remember which.
;;; Highly recursive, may fail on large lists!
(defun STD-COPY-TREE (tree)
  (cond ((atom tree) (car (cons tree nil)))
	(T (cons (std-copy-tree (car tree))
		 (std-copy-tree (cdr tree))))))


;;; std-randomize arranges all list elements randomly.
;;; Fisher-Yates Algorithm. 10/2/00
;;; This will be re-defined by std-%randomize from FILEEXT 0.5 in
;;; STDINIT2, because this performs poorly.
;;;   (std-randomize '(0 1 2 3 4)) => (2 4 0 3 1)
(defun STD-RANDOMIZE (lst / j i tmp)
  (setq i (1- (length lst)))
  (while (> i 0)
    (if (/= i (setq j (std-random (1+ i))))
      (setq tmp (nth i lst)
	    lst (std-setnth (nth j lst) i lst) ; new i lst
	    i   (1- i)
	    lst (std-setnth tmp j lst)))))

;;; This doesn't create evenly distributed arrangements.
;;; I should have known, Serge Pashkov already told me.




;;; std-random-elt returns a random list element.
;;; elt is in LISP the normal safe sequence accessor.
(defun STD-RANDOM-ELT (l)
  (if (null (cdr l))
    (car l)
    (nth (std-random (length l)) l)))

;;; Returns a point in the 3d-box (0 0 0) - (1 1 1)
(defun STD-RANDOM-PT ()
  (list (std-random nil)
        (std-random nil)
        (std-random nil)))

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDLIST-SYMBOLS*))
(setq *STDLIST-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDLIST") ; first provide it, but before you may
				; call it, be sure to have all
				; supporting modules
(std-%simple-require "STDINIT")	; all dependent top-level calls removed
				; consp, std-functionp, function
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDLISP.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:32:56 rurban>
;;; Copyright (c) 1998 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Lisp specific functions for the STDLIB


;;; STATUS:
;;;   still incomplete yet
;;;   certain size-critical functions (mainly STD-PRINC) are not called 
;;;     from other functions to reduce cross-dependencies.
;;;   some are not tested
;;;
;;; $Log: STDLISP.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;; 2000-04-07 15:16:20 rurban
;;;   removed std-princ dependency from std-error

;;; Internal functions are prefixed with std-%
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDLISP-SYMBOLS*
    '(STD-GENSYM	 STD-INTEGERP	    STD-REALP
      STD-FILEP		 STD-ENAMEP	    STD-PICKSETP
      STD-PRINC		 STD-PRINC-TO	    STD-TOSTR
      STD-EQUALP	 STD-DEBUG-PRINT    STD-PRINT-WARN
      STD-WARN		 STD-ERROR	    STD-CERROR
      STD-ASSERT	 STD-DEFAULT	    STD-DEFAULT-TYPE
      STD-DEFCONSTANT	 STD-DEFKEYWORD	    STD-KEYWORDP
      STD-MODULE-PATHNAME		    STD-REQUIRE-PATH
      STD-REQUIRE-VERSION		    STD-LOAD
      STD-LOAD-EXTERNAL))))




;;; Used globals: Not defined here. Their defaults are NIL.
;;; *VERBOSE*			; prints verbose messages
;;; *LOAD-VERBOSE*              ; prints load messages
;;; *DEBUG*                     ; enables debugging
;;; *BREAK*                     ; enables break
;;; *BREAK-ON-WARNINGS*         ; user-interactive dialog on STD-WARN
                                ;  needs keypress to continue
;;; *GENSYM-COUNTER*		; actual (std-gensym) index
				; (if bound or unbound)

(if (not (numberp *GENSYM-COUNTER*))
(setq *GENSYM-COUNTER* 0))	; first col to include it in lspdoc.hlp

;;; Create a new unbound symbol.
;;; Changed behaviour: The counter is always incremented to
;;;   simplify macros
;;;  (setq x (std-gensym)) => G0
;;;  (eval x)		   => nil
;;;  (set (std-gensym) 1)  => 1	(sets G1 to 1)
;;;  (eval x)		   => nil (x is G0)
(defun STD-GENSYM (/ x)
  (while (boundp (setq x (read (strcat "G"
    (itoa (1- (setq *GENSYM-COUNTER* (1+ *GENSYM-COUNTER*)))))))))
  x
)

;;; -------------------------------------------------------------------73
;;; Workarounds for not VL funcs:
;;; Some of them were already defined in STDINIT because they
;;; are needed at load-time. We redefine them here to have them
;;; all together.

;;; already defined in STDINIT
;;;(std-%cond-defun
;;;'(defun STD-SYMBOLP (x)
;;; (eq (type x) 'SYM))
;;;)

;;; already defined in STDINIT
;;;(if (not (boundp 'STD-SYMBOL-VALUE))
;;;;;; if not ViLL: almost the same
;;;(setq STD-SYMBOL-VALUE eval)
;;;)

;;;(std-%cond-defun
;;;;;; if not ViLL: very very tricky! I replaced it with a more stable
;;;;;; version in STDINIT. boundp and the implicit evaluation of lists
;;;;;; is not quite lisp conform.
;;;;;; from a CIS forum, Author unknown
;;;'(defun STD-SYMBOL-NAME (name)
;;;   (if (std-symbolp name)
;;;     (
;;;       (list
;;;         (if (boundp name)
;;;           (cons '/ (atoms-family 0))
;;;           (cons '/ (cons name (atoms-family 0)))
;;;         )
;;;         (list setq name 0)
;;;         (list car (list atoms-family 1))
;;;       )
;;;     )
;;;     nil
;;;   )
;;; )
;;;)
;;;


(std-%cond-defun
;;; if not ViLL: very slow!
'(defun STD-PRIN1-TO-STRING (x / f str tmp)
  (setq tmp (std-filename-mktemp nil)
        f   (std-fopen tmp "w"))
  (prin1 x f)
  (std-fclose f)
  (setq f   (std-fopen tmp "r")
	str (read-line f)
	f   (std-fclose f))
  str
 )
)

(std-%cond-defun
;;; if not ViLL: very slow!
'(defun STD-PRINC-TO-STRING (x / f str tmp)
  (if (stringp x)
    ;; avoid costly conversion if already string!
    x
    (progn
      (setq tmp (std-filename-mktemp nil)
            f   (std-fopen tmp "w"))
      (princ x f)
      (std-fclose f)
      (setq f   (std-fopen tmp "r")
	    str (read-line f)
	    f   (std-fclose f))
      str
     )
   )
 )
)


;;; More Type Predicates
(defun STD-INTEGERP  (x)	; is it a INTEGER number? (type INT)
  (eq (type x) 'INT))
(defun STD-REALP (x)		; is it a REAL number?
  (eq (type x) 'REAL))
(defun STD-FILEP (x)          	; is it a FILE object?
  (eq (type x) 'FILE))
(defun STD-ENAMEP (ele)
  (eq (type ele) 'ENAME))
(defun STD-PICKSETP (ss)
  (eq (type ss) 'PICKSET))

;;; Note: Other defined type predicates are CONSP and STRINGP in
;;; the STDLIB and LISTP, ATOM in AutoLISP.

;;; redefine std-princ to support global printing variables:

;;; princ extended to take lists too
;;; using *PRINT-LENGTH*, *PRINT-LEVEL* in the list argument only.
;;; speed it up if none defined.
(defun STD-PRINC (x)
  (if (consp x)

    (if (and (not *PRINT-LENGTH*) (not *PRINT-LEVEL*))

      (mapcar (function princ) x)

      (mapcar (function (lambda (s)(std-%write s nil 0))) x)
    )

    (princ x)))

;;; std-princ extended to print to a file, console or string
;;; if to files we need exactest information as possible, no omissions
;;; on long lists or deep nesting, and exactest number representation!
;;; not just (1 2 3 ...) or 1.345+e008
(defun STD-PRINC-TO (x _to / *tmpstr*)
  (cond
    ;; with files we don't need dots!
    ((and (std-filep _to) (std-open-file-p _to))	; fixed
      (if (consp x)
	;; todo: convert numbers
        (mapcar (function (lambda (_s)(princ _s _to))) x)
        (princ x _to)))
    ((std-symbolp _to)	; assume a string
      (setq *tmpstr* "")
      (if (consp x)

        (if (and (not *PRINT-LENGTH*) (not *PRINT-LEVEL*))

          (setq *tmpstr* (std-strjoin x ""))

	  ;; use self-evaluating T _to pass symbols recursively
          (mapcar (function (lambda (_s)(std-%write _s 'T 0))) x)
	)

      )
      (set _to *tmpstr*))
    ((not _to)		; nil for console
      (std-princ x))
    (T (std-error (list (std-msg "invalid output argument - ") _to)))
))


;;; first level call for std-%write-to
;;; (defun std-%write (x to)
;;;   (std-%write-to x to 0)
;;;   (princ))

;;; processor for std-princ and std-princ-to
;;; honoring global print variable settings
;;; may be extended for more objects
;;; improvement: overlarge integers are printed as integers,
;;;   not as abbrevated reals,
;;;   (princ 10000000000) not 1.0e+10
;;;   (princ 10000000000) => 10000000000
;;; take care with dotted lists!
(defun std-%write (x to level)
  (if (and (numberp x)
           (> x *MAX-INT*)
	   (zerop (std-fraction x)))
     ;;; it seems to be a overlarge integer so print it as one
     (setq x (std-rtos x)))
  (cond
    ((and (numberp *PRINT-LEVEL*)
          (> level *PRINT-LEVEL*))
     (std-%princ-to "#" to))
    ((consp x)
      (if (and (numberp *PRINT-LENGTH*)
	       (> (std-list-length x) *PRINT-LENGTH*))
        (std-%write-list (append (std-firstn *PRINT-LENGTH* x) '("..."))
	  	         to (1+ level))
        (std-%write-list x to (1+ level))
      )
    )
    ((std-filep to)
      (princ x to))
    ((not to)		; nil for console
      (princ x))
    ((std-symbolp to)	; assume a string
      (setq *tmpstr* (strcat *tmpstr* (std-princ-to-string x))))
    (T (std-error (list (std-msg "invalid output argument - ") to)))
    ))

;;; treat lists special, dotted pairs!
(defun std-%write-list (x to level / y)
  (if (and (numberp *PRINT-LEVEL*)
           (> level *PRINT-LEVEL*))
    (std-%princ-to "#" to)
    (progn
      (std-%princ-to "(" to)
      (std-%write (car x) to level)
      (setq x (cdr x))
      (while (and (consp x) (listp (cdr x)))
        (std-%princ-to " " to)
        (std-%write (car x) to level)
        (setq x (cdr x)))
      (if (and x (atom x))
	(progn
          (std-%princ-to " . " to)
          (std-%princ-to x to))
      )
      (std-%princ-to ")" to)
    )
  )
  nil
)


;;; faster version for x of type string
(defun std-%princ-to (x to)
  (cond
    ((not to)
      (princ x))
    ((std-filep to)
      (princ x to))
    ((std-symbolp to)	; assume a string
      (setq *tmpstr* (strcat *tmpstr* (std-princ-to-string x))))
  )
)

;;; Convert expression to user readable format.
;;; Portable and quite accurate definition.
;;; Faster without VL, but slower with VL.
;;; Used for user prompts. Some expressions are READ incompatible.
;;; nil is printed as "" and points as (0,0)
(defun STD-TOSTR (x / typ)	; accepts any expression
  (cond
    ((not x) "")			; "" = NIL
    ((eq (setq typ (type x)) 'STR) x)	; String
    ((eq typ 'INT)  (itoa x))		; Number
    ((eq typ 'REAL) (std-rtos x))	; Float with max.accuracy
    ((eq typ 'SYM)  (std-symbol-name x))
    ((std-dotted-pair-p x)
      (strcat
        "("
          (std-tostr (car x))
	  " . "
          (std-tostr (cdr x))
	")"
      )
    )
    ;; there's a dot somewhere
    ((std-not-proper-list-p x)
      ;; for now print it only in the recursive dotted notation
      ;; (1 2 . 3) => "(1 . (2 . 3))"
      ;; will be fixed later
      (strcat "("
              (std-tostr (car x))
	      " . "
              ;(if (consp (cdr x)) " " " . ")
              (std-tostr (cdr x))
              ")"
      )
    )
    ;; print numbers with commas, points: (1, 2, 3)
    ;; this is incompatible to read, so it may be changed later.
    ((std-number-list-p x)
      (strcat
        "("
        (if (> (length x) 1)
          (std-strlist->csstring (mapcar (function std-tostr) x))
          (std-tostr (car x))           ; single entry within brackets
	)
	")"
      )
    )
    ;; Not empty list, print it with spaces
    ((eq typ 'LIST)
      (strcat
        "("
        (if (> (length x) 1)		; error with dotted pair lists
          (std-strlist->string (mapcar (function std-tostr) x) " ")
          (std-tostr (car x))           ; single entry within brackets
	)
	")"
      )
    )
    ;; all other types (FILE, ENAME, PICKSET, SUBR, ...)
    (T (std-princ-to-string x))
  )
)

;;; Special (weaker) case for
;;; points (number-lists) with numerical tolerance (usually 1e-6),
;;; sets (unordered same elements) and strings (case-insensitive)
;;; Note: numeric lists are no sets!
(defun STD-EQUALP (x y)
  (cond
    ((std-number-list-p x) (equal x y *NUM-TOL*))
    ((consp x)   (std-set-equal-p x y))
    ((stringp x) (std-string-equal x y))
    ((std-realp x) (equal x y *NUM-TOL*))
    (T (equal x y))))

;;; -------------------------------------------------------------------73
;;; Simple error and debugging support:

;;; also defined in STDINIT
;;; see the error handling in STDERROR.LSP
;;; see user-interactive debugging in STDDEBUG.LSP

;(defun STD-VERBOSE-PRINT (msg)
;  (if *VERBOSE* (std-princ msg)))

(defun STD-DEBUG-PRINT (msg)
  (if *DEBUG* (std-%print-error-prefix (std-msg "; DEBUG") msg)))

;;; Prints warning and optionally waits for a keypress on
;;; *BREAK-ON-WARNINGS*
;;; Takes a string or lists of any type.
;;; This is similar to STD-WARN but never calls STD-BREAK.
(defun STD-PRINT-WARN (msg)
  (if (and (std-acad-connection-p) *BREAK-ON-WARNINGS*)	; changed
    (alert (std-tostr msg))
    (std-%print-error-prefix (std-msg "; * Warning") msg)
  )
  (princ)
)

;;; Print formatted msg on command line
(defun std-%print-error-prefix (prefix msg / s)
  (if (not prefix)(setq prefix (std-msg "; *** ERROR")))
  (terpri)
  (princ prefix)
  (princ ": ")
  (if (listp msg)
    (foreach s msg (princ s))
    (princ msg))
  ;;(std-princ msg)
  (princ)
)

;;; May break or just prints the msg, dependent on the global *BREAK*
(defun STD-WARN (msg)
  (if *BREAK*
    (std-cerror msg)
    (std-print-warn msg)))

;;; Force not continuable (severe) error.
;;; Prints the error message and breaks, don't mix it up with *STD-ERROR*
;;; This functions forces an error but is not the error handler.
(defun STD-ERROR (msg)
  (std-%print-error-prefix nil msg)
  (princ " ")
  ;(if (std-vlide-p)	; see below
  ;  (std-%forced-exit)
    (exit)		; just exits the running lisp
  ;)
)
;;; only for VL IDE
;;; (exit) may closes the whole IDE, that's why we need a USUBR
(defun std-%forced-exit ()(exit))

;;; Continuable error
;;; So far the conditioning system in AutoLISP is not capable to
;;; continue on errors, so it's more or less a call to break with a
;;; different message. The real functionality is reserved for the
;;; future, when jumps or restarts are supported.
;;; Fixed by Serge Pashkov not to depend on STDDEBUG anymore.
(defun STD-CERROR (msg / *BREAK*)
  (std-%print-error-prefix (std-msg "; ** Continuable Error") msg)
  (setq *BREAK* T)
  (if (std-%simple-require "STDDEBUG")
    (std-break msg nil)))

;;; Throw error if 'test evaluates to nil
;;; The test argument must be used with quotes!
;;;   (std-assert '(= x y) "x /= y")
(defun STD-ASSERT (_$test _$msg / _$obreak)
  (if (not (eval _$test))
    (progn
      (std-%print-error-prefix
        (std-msg "; *** Assertion Failure")
	(if _$msg
	  _$msg
          (list (std-prin1-to-string _$test) " => nil")))
      ;; force break or just exit?
      (if (or *DEBUG* *BREAK*)
        (if (std-%simple-require "STDDEBUG")
          (std-break nil nil))
	(exit)
      )
    )
  )
)


;;; -------------------------------------------------------------------73
;;; Initializing variables:

;;; If nil set sym to def
;;; The second argument is always evaluated (a function, no macro)
;;; so better use it only on simple functions or default variables.
;;; Similar to the CL defvar.
;;;   (std-default 'my:int 100)
(defun STD-DEFAULT (symb value)
  (if (and value (not (std-symbol-value symb)))
    (set symb value)))

;;; Default with type checking.
;;; If not of type def set sym to def.
;;; Better than std-default which only check s for nil. This checks for
;;; a required type.
;;; The second argument is always evaluated (it's a function not a macro)
;;; so better use it only on simple functions or default variables
;;;   (std-default-type 'my:int 100)
(defun STD-DEFAULT-TYPE (ele def / _$tmp)
  (if (= (type ele) 'SYM) (setq _$tmp ele ele (std-symbol-value _$tmp)))
  (if (/= (type ele) (type def))
    (setq ele def)
  )
  (if _$tmp (set _$tmp ele) ele)
)

;;; With VL it protects the constant from assigning.
;;; It really should be macro to avoid the quote.
;;; Usage: (std-defconstant '*X* 0)
;;;        (setq *X* 1)
;;; prints: *U* WARNING: assignment to protected symbol: *X* <- 1
;;; Without VL there's no warning.
(defun STD-DEFCONSTANT (symb value)
  (std-%unprotect-assign (list symb))
  (set symb value)
  ;(setq *CONSTANTS* (std-adjoin symb *CONSTANTS*))
  (std-%protect-assign (list symb))
  (std-symbol-value symb)	; don't evaluate value twice!
)

(defun STD-DEFKEYWORD (symb)
  (std-defconstant symb symb))

;;; If it is in the keyword package is not decidable, so we must use
;;; tricks: Self-evaluating symbol and ":" prefix.
;;; Usage: (std-keywordp :error) => T or (std-keywordp ':error) => T
(defun STD-KEYWORDP (symb)
  (and (std-symbolp symb)
       (= ":" (std-firstchar (std-symbol-name symb)))
       (eq symb (std-symbol-value symb))))

;;; Self-evaluating objects like numbers, strings
;;; Constant variables like keywords, defconstant'ed symbols (NOT YET!)
;;; Quoted forms
;;; Don't rely in this so far. defconstant'ed symbols are not detected!
(defun STD-%CONSTANTP (x)
  (or (numberp x)
      (stringp x)
      (and (std-symbolp x)
           (eq x (std-symbol-value x)))
      ;(member x *CONSTANTS*)   ; symbols may be shadowed so this may be
                                ; wrong. It depends on the environment
      (and (consp x)
	   (eq (car x) 'QUOTE))))

;;; -------------------------------------------------------------------73
;;; VL specific stuff


;;; Prints warning on redefinition of symbol, in VL only.
;;; Used for defconstant
;;; As side-effect they are blue in the IDE.
;;; Removed STDSTR dependency
;;;(defun std-%protect-assign (syms)
;;;  (if (std-vl-p)
;;;    (eval (read
;;;      (strcat "(pragma '((protect-assign "
;;;        (apply (function strcat)	; v0.4001
;;;          (mapcar
;;;            (function (lambda (s)
;;;	      (strcat " " (std-princ-to-string s))))
;;;	    syms))
;;;        ")))")))))

(defun std-%protect-assign (syms)
  (if (std-vl-p)
    (eval
      (list 'pragma
	    (list 'quote
	      (list (cons 'protect-assign syms)))))))
(defun std-%unprotect-assign (syms)
  (if (std-vl-p)
    (eval
      (list 'pragma
	    (list 'quote
	      (list (cons 'unprotect-assign syms)))))))

;;;(defun std-%unprotect-assign (syms)
;;;  (if (std-vl-p)
;;;    (eval (read
;;;      (strcat "(pragma '((unprotect-assign "
;;;        (apply (function strcat)	; v0.4001
;;;          (mapcar
;;;            (function (lambda (s)
;;;	      (strcat " " (std-princ-to-string s))))
;;;	    syms))
;;;        ")))")))))

;;; predicate if the symbol is protected
;;; better don't use the extension std-%protectedp directly
(defun std-%protect-assigned-p (symb)
  (if (and (std-vl-p)
           (>= *FILEEXT-VERSION* 0.54)
           (std-functionp std-%protectedp)
           )
    (std-%protectedp symb)))


;;; -------------------------------------------------------------------73
;;; Modules:

;;; Basic code is in STDINIT

;;; list of acceptable extensions, in the correct order for searching
(defun std-%loadable-lisp-extensions  ()
  (if (/= (std-ver-name) "ACAD")
   '(".LSP")
    (append (if	(std-vlisp-p)
	     '(".VLX"))
	    (if	(std-vl-p)
	     '(".FAS")
	      (if (std-acomp-p)
		'(".BI4")))
	   '(".LSP")
	    )))

(defun std-%loadable-ads-extensions ()
  (cond
    ((/= (std-ver-name) "ACAD")
     '(".DLL"))
    ((std-acad-connection-p)
      (append (if (>= (std-ver-num) 13)
		'(".ARX")		; arx or dll with clones
		(if (std-ver-win-p) 	; ads
		  '(".EXE")
		  '(".EXP")))))))

;;; return the loader function as well? (load, xload or arxload)
(defun std-%loadable-extensions ()
  (append (std-%loadable-lisp-extensions)
	  (std-%loadable-ads-extensions)))

(defun STD-MODULE-PATHNAME (name / ext extlst fname path)
  (setq fname (std-%force-module-string name))
  (if (setq ext (std-filename-extension-or-nil fname))
    ;; forced extension
    (setq extlst (list ext))
    (setq extlst (std-%loadable-extensions))
  )
  ;; with explicit path skip path search
  (if (std-filename-directory-or-nil fname)


    (findfile fname)

    (std-%load-try-paths fname
			 (append
			   ;; search order:
			   (list (std-current-path))
			   *MODULE-PATH*
			   (std-acad-library-paths))
      			 extlst)
  )
)

;;; Used if the module name is different from the filename or
;;; if the path is known, not to search all other paths.
(defun STD-REQUIRE-PATH (name path)
  (setq name (std-%force-module-string-dot name))
  (if (std-%module-string-defined-p name)
    (if (std-load-external path)
      name
    )
  )
)

;;; (STD-REQUIRE-VERSION <module-name> <version-number>)
;;; Used to search for the required version number which is
;;; the value of the special symbol * <module-name> -VERSION*
;;; such as *BINIO-VERSION* (currently: 2.2)
;;; Returns the version number or nil.
;;; Also used to get the actual version number with
;;; (std-require-version name -1)
(defun STD-REQUIRE-VERSION (module version / actver name)
  ;(std-%simple-require "STDFILE")
  (setq name (std-%force-module-string-dot module))
  (if (and (std-%simple-module-string-defined-p name)
	   (numberp (setq actver
	     (std-symbol-value (read (strcat "*" name "-VERSION*")))))
	   (>= actver version))
    actver
    (if (null (std-load-external (std-%force-module-string module)))
      nil
      (std-symbol-value (read (strcat "*" name "-VERSION*")))
    )
  )
)

;;; Search file in given pathnames
;;; Depricated. To be replaced by std-%findfile-exts, STDFILE
(defun std-%load-try-paths (fname pathlst extlst / path found)
  (while (and pathlst (not found))
    (setq path (std-force-path (car pathlst) fname))
    (setq found (if (std-filename-extension-or-nil path)
        ;; explicit extension given?
        (std-probe-file path)
        (std-%load-try-extensions path extlst)
      )
    )
    (setq pathlst (cdr pathlst))
  )
  found
)

(defun std-%load-try-extensions (path extlst / found)
  (while (and extlst (not found))
    (setq found (std-probe-file (strcat path (car extlst)))
          extlst (cdr extlst))
  )
  found
)

;;; Accepts any pathname, using STDFILE functions
;;; possible extension: check for vl-rts and use vl-load then
(defun STD-LOAD (fname / extlst path retval)
  (setq extlst (std-%loadable-lisp-extensions))
  ;; with explicit path skip *module-path*
  (if (not (std-string-not-empty-p (std-filename-directory fname)))
    (setq path (std-%load-try-paths fname *MODULE-PATH* extlst)))
  ;; or in the acad library path
  (if path (setq fname path))
  (setq retval (load fname "error"))
  (if (= retval "error")
    (if path
      (load fname)	; found, but loading error, break here!
      (std-print-warn (list (std-msg " Error loading ") fname
			    "\n"
			    (std-msg "Search paths: ")
			    *MODULE-PATH* "\n  "
			    (std-acad-library-paths)
			    "\n")))
    (std-%load-verbose-print 
      (list (std-msg " Loaded ") fname " \n"))
  )
  (/= retval "error")
)


;;; Try to load it from the acad library path.
;;; fname is the simple filebase, path the found path or nil
;;; Deprecated. Better search the file, try to load then.
;;; If it was found provide a different error messages than if it
;;; was not found. 
(defun std-%load-external-acad (fname path / retval ext)
  (if path
    (progn
      (std-%load-verbose-print 
        (list "\n" (std-msg "Loading ") path " ..."))
      (setq ext (std-strcase (std-filename-extension path)))
      (setq retval
        (cond ((= ext ".ARX")
	       (arxload path "error"))
	      ;; FelixCAD,... xload DLL's
	      ((member ext '(".EXE" ".EXP" ".DLL"))
	       (xload path "error"))
	      ;;((and vl-load (std-vlrts-p)) (vl-load path)) ;fixed
	      (T
               (load path "error"))
        )
      )
    )
    (progn
      (setq ext (std-strcase (std-filename-extension fname)))
      (setq retval
        (cond ((= ext ".ARX")(arxload fname "error"))
	      ((member ext '(".EXE" ".EXP" ".DLL"))
	       (xload fname "error"))
	      (T (load fname "error"))))
      (if (and (std-acad-connection-p)
               (= retval "error"))
	(if (or (not (std-functionp arxload))
		(= (arxload fname "error") "error"))
          (if (or (not (std-functionp xload))
                  (= (xload fname "error") "error"))
	    nil
	    (std-%load-verbose-print 
	      (list (std-msg " xload'ed ") fname " ")))
	  (std-%load-verbose-print 
	    (list (std-msg " arxload'ed ") fname " ")))
	(std-%load-verbose-print 
	  (list (std-msg " Loaded ") fname " ")))
      retval
    )
  )
  (/= retval "error")
)

;;; STD-LOAD-EXTERNAL tries to load external ads/adsrx/arx modules,
;;; such as DLL, EXP, EXE or ARX, dependent on the platform.
(defun STD-LOAD-EXTERNAL (fname / path extlst ext)
  (setq extlst (if (setq ext (std-filename-extension-or-nil fname))
    (list ext)
    (std-%loadable-ads-extensions)))
  (setq path (std-%load-try-paths fname *MODULE-PATH* extlst))
  (std-%load-external-acad fname path)
)

;;; we'll need a ADS independent version for std-ver-fcad-p here
;;; for the standalone mode.
(defun std-%ver-ext-adssuffix ()
  (cond ((std-ver-fcad-p) "fc")
	((std-ver-icad-p) "ic")
	((std-ver-tcad-p) "tc")
	(T (itoa (fix (std-ver-num))))))

;;; Load the version specific stdlib ADS module.
;;; For now support also all seperate "BINIO<xx>.<ext>",... versions as
;;; well. This might change to the fixed name STDLIB<xx>.<ext> only.
;;; Note that other apps may use a different filename and contain
;;; additional funcs.
;;; Later we will need a strategy for R15 to go without any arx.
(defun std-%require-external-ads (name / defname)
  (setq defname (strcat "STDLIB" (std-%ver-ext-adssuffix)))
  ;; Note: we cannot safely upcase name on case-sensitive filesystems
  (cond
    ((and (/= (std-strcase name) "STDLIB")
          (std-%simple-module-string-defined-p (std-strcase name))))
    ((member (std-strcase name)
                 '("STDLIB" "STDLIB-ADS"
		   "REGISTRY" "INIFILE" "BINIO" "FILEEXT"
		   "STDLOCAL-ADS"))
     (or (std-%simple-module-string-defined-p "STDLIB-ADS")
	 (and (std-load-external defname)
              (if (not stdlib-ads-version) (std-%import-stdlib-arx name) 1))))
    ((std-load-external (strcat name (std-%ver-ext-adssuffix)))
     (setq name (strcat name (std-%ver-ext-adssuffix)))
     ;; if loaded before or outside, import it.
     (if (not stdlib-ads-version) (std-%import-stdlib-arx name) 1))
    ;; we really should avoid recursive (load "stdlib15") invocations
    ((and (not std-ver-r2000-p)		; 11-Feb-00
	  (std-load-external defname))
      (if (not stdlib-ads-version) (std-%import-stdlib-arx name) 1))
    ))

;;; unify VILL/VLISP syntax
(defun std-%vl-import-exsubr (lst)
  (cond ((std-vlisp-p)  (vlisp-import-exsubrs lst))
	((std-vill-p)   (vill-import-exsubrs lst))
	))

;;; If this was the stdlib-ads we know the names to import
;;; Note: if any adsrx app uses the stdlib ads funcs it must only
;;;       import the additional funcs
;;; TODO: use versioning for the list
(defun std-%import-stdlib-arx (name / x fn var)
  (if (std-ver-r2000-p)
    (vl-arx-import name)
    (progn
      (foreach x
         '((stdlib-version 0 std-print-errno)
	   (stdlib-ads-version 0.400018 std-print-errno)
	   (fileext-version 0
	       std-%file-directory-p
	       std-%file-readable-p	  std-%file-systime
	       std-%file-modtime	  std-%file-size
	       std-%directory-files	  std-%mkdir
	       std-%rmdir		  std-%file-attrib
	       std-%fullpath		  std-%dos-filename
	       std-%current-path	  std-%get-drives-string
	       std-%internal-time->cdate  std-%get-internal-run-time
	       std-%acad-connection-p	  std-%open-file-p
	       std-%protectedp		  std-sys-version)
	   (random-ads-version 0
	       std-%sys-rand
	       std-%random-state-p	std-%make-random-state
	       std-%randomize)
	   (stdlocal-ads-version 0 ; fixed 19-Apr-00
	       std-%sys-codepage
	       std-%sys-oemcp std-%sys-locale
	       std-user-locale std-get-locale-info)
	   (binio-version 0 
	       binio-fopen binio-fclose binio-fcloseall
	       binio-fseek binio-ftell binio-rewind binio-fputc
	       binio-fputint binio-fputlong binio-fputreal
	       binio-fputs binio-fputstring binio-fgetc binio-fgetint
	       binio-fgetlong binio-fgetreal binio-fgets binio-fgetstring
	       binio-read-char binio-unread-char binio-peek-char
	       binio-%double->2long binio-%2long->double)
	   (registry-version 0
	       registry-read registry-write
	       registry-delete registry-descendents)
	   (inifile-version 0
	     inifile-get inifile-set)
	   (stdwin-version 0
	     std-select-folder std-getfilew)
	   (arxstuff-version 0.11 std-massprop)
      	   )
	(setq fn  (car x)
	      var (read (strcat "*" (std-princ-to-string fn) "*")))
	(std-%vl-import-exsubr (list name fn))
	(if (and (std-symbol-value fn)
		 (apply fn nil)
		 (>= (apply fn nil) (cadr x)))
	  (progn
	    (std-%vl-import-exsubr (cons name (cddr x)))
	    (std-%unprotect-assign (list var))
	    ;; try to avoid vlisp-import-symbol and adsrx export
	    (set var (apply fn nil))
	    (std-%protect-assign (list var))
	    )
	  (progn
	    (std-%unprotect-assign (list var fn))
	    (set fn nil)
	    (set var nil)
	    )
      )))))

;;; TODO: (vlisp-import-exsubr) for the external stdlib funcs

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDLISP-SYMBOLS*))
(setq *STDLISP-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDLISP") ; first provide it. But before you may
				; call it, be sure to have all supporting
				; functions.

(std-%simple-require "STDINIT")  ; various
(if (not *STD:%PROJECT*)
  ;; after STDLISP in the project loading order
  (std-%simple-require "STDFILE")  ; various
)
(std-%simple-require "STDSTR")   ; various
; (std-%simple-require "STDMISC") ; std-break: std-dclactive-p
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDFILE.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:32:19 rurban>
;;; Copyright (c) 1998,99,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; File functions for the STDLIB
;;;
;;; Filenames 		(just strings: splitting, ...)
;;; Directories 	(on existing files: checking, globbing)
;;; System
;;;   OS Specific 	(copy, delete, ...)
;;;   Text Files
;;;   Binary Files
;;;   INI Files
;;;   Registry
;;; User Input 		(file dialogs, ...)

;;; Also the top-level calls for all earlier modules.

;;; STATUS:
;;;   Almost complete, but not fully tested yet.
;;;   The whole module is quite dynamic because it checks various
;;;   loaded external functions. Only as last resort use the plain alisp
;;;   versions.
;;;   RBarlow reported a std-directory-files bug.




;;; -------------------------------------------------------------------73
;;; $Log: STDFILE.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; 2000-11-14 15:16:21 rurban
;;;   fixed STD-FILE-DIRECTORY-P warning on FILEEXT.FAS loading
;;; 2000-11-10 16:07:34 rurban
;;;   fixed STD-CURRENT-PATH to use STD-%CURRENT-PATH
;;;   added *STD-SHELL-TIMEOUT*
;;;   fixed std-%command-shell-wait to wait milliseconds and not loops
;;; 2000-09-23 16:00:22 rurban
;;;   improved STD-GET-DRIVE-LIST without ARX
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; 2000-09-20 12:57:04 rurban
;;;   fixed STD-FILE-COPY for VLISP
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; 2000-04-26 18:19:49 rurban
;;;   renumbered MSG ID's
;;;
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDFILE-SYMBOLS*
    '(STD-FPSLASH	       STD-MAKE-PATHNAME
      STD-FILENAME-BASE	       STD-FILENAME-EXTENSION-OR-NIL
      STD-FILEBASE	       STD-FILENAME-DIRECTORY
      STD-FILENAME-SPLIT       STD-FILENAME-EXTENSION
      STD-FILE-READABLE-P      STD-FILE-DIRECTORY-P
      STD-FILENAME-DIRECTORY-OR-NIL
      STD-FILENAME	       STD-FORCE-PATH
      STD-DEF-EXTENSION	       STD-FORCE-EXTENSION
      STD-LONGFNAMEP	       STD-FILENAME-UPCASE
      STD-TRUENAME	       STD-CURRENT-DRIVE
      STD-CURRENT-PATH	       STD-SAME-FILES-P
      STD-FOPEN		       STD-FCLOSE
      STD-FCLOSEALL	       STD-OPEN-FILE-P
      STD-OPEN-FILENAME-P      STD-OPEN-FILENAME->FILE
      STD-FILE->FILENAME       STD-DIRECTORY-FILES
      STD-GLOB-FILES	       STD-GLOB-DIRS
      STD-FINDFILE	       STD-FINDFILE-PATHS
      STD-FINDFILE-EXTS	       STD-ACAD-LIBRARY-PATHS
      STD-FT-BACKUP	       STD-FT-COPY
      STD-FT-APPEND	       STD-FT-EQUAL
      STD-FT-NTH-LINE	       STD-FT-LINES
      STD-FT-INSERT	       STD-SHOWFILE-DIALOG
      STD-SHOWFILE	       STD-READ-FILE-STRINGS
      STD-READ-FILE-LIST       STD-FILE-EXIST-P
      STD-FILE-WRITABLE-P      STD-FILENAME-MKTEMP
      STD-DOS-FILENAME	       STD-FILE-BACKUP
      STD-FILE-NEWER-P	       STD-GET-DRIVE-LIST
      STD-FILE-MODTIME		STD-FILE-SYSTIME
      STD-FILE-RENAME	       STD-FILE-COPY
      STD-FILE-APPEND	       STD-FILE-DELETE
      STD-FILE-ATTRIB
      STD-%DIRECTORY-FILES     STD-FILE-SIZE
      STD-MKDIR STD-RMDIR))))


(std-%simple-require "STDINIT")  ; for TOP-Level predicates
(std-%simple-require "STDLIST")  ; various

;;; Globals:
;;; Possible slashchars, improved later in STDINIT2: std-%top-level-1
(setq *STD:SLASH-CHARS* '(":" "/" "\\"))

;;; Maximal time in milliseconds to wait for the shell to complete 
;;; a simple builtin I/O command. Typically 'cd', 'pwd', 'dir', ...
;;; This is highly system dependent. In doubt use a higher number.
(setq *STD:SHELL-TIMEOUT* 30) ;[ms]


(if (not special)
  (defun special (x)
    (std-%load-verbose-print (append '("declared special: ") x))))



;;; Filesystem independent functions

;;; STD-FPSLASH
;;; Filepath-Slash converts "\\" or "/" in path strings to whichever is
;;; needed by operating system (Unix or DOS and OS/2),
;;; and forces trailing slash. Only "\\" and "/" supported so far!
;;; On the mac ending : "slashes" have a different meaning, hmm..
;;; Thanksfully acad never supported VMS with its awful [] naming.
;;; NT accepts "/" too, even in shells
(defun STD-FPSLASH (path)
  (if (= path "")
    ""
    (progn
      ;(if (not (stringp *SLASH-CHAR*))
      ;  (setq *SLASH-CHAR* (if (std-sys-dosbased-p) "\\" "/")))
      (setq path (std-%slash-unify path))
      (if (/= (std-lastchar path) *SLASH-CHAR*)
        (setq path (strcat path *SLASH-CHAR*))
        path
      )
    )
  )
)

;;; hmm, what to do with the mac ":" path delimiter?
;;; <joke>Do MAC folks do (AutoLISP) programming at all?</joke>
(defun std-%slash-unify (s)
;  (if (std-sys-mac-p)
;    (std-strchg (std-strchg s "/" ":") "\\" ":")
    (if (= *SLASH-CHAR* "/")
      (std-strchg s "\\" *SLASH-CHAR*)
      (std-strchg s "/" *SLASH-CHAR*)))
;)

;;; It's safer to use this than the simple strcat.
;;; It corrects slashes on paths. The extension must have a leading dot.
;;;
;;; This may be expanded in the future to support more systems.
;;; It may be used to convert between different filesystems,
;;;   DOS 8.3 <==> NTFS <==> unix for example
;;;   or to convert upper<->lowercase
;;; We will see if acad will support linux in the next years.
;;; Then this will be topic. MFC already runs on linux. windows
;;; emulators also. But that depends on the development of the desktop
;;; market.
(defun STD-MAKE-PATHNAME (pathnames / ext name)
  ;;; try to guess what's the path, name and extension:
  (if (stringp pathnames) (setq pathnames (list pathnames)))
  (if (= "." (std-firstchar (last pathnames)))
    (setq ext       (last pathnames)
	  pathnames (std-butlast pathnames))
    (setq ext ""))
  (if (member (std-lastchar (last pathnames)) *STD:SLASH-CHARS*)
    (setq name "")
    (setq name (cond ((last pathnames)) (T ""))
	  pathnames (std-butlast pathnames)))
  (strcat (std-strjoin (mapcar (function std-fpslash) pathnames) "")
	  name ext))

;;; Those 5 have sometimes problems to be loaded from fileext.fas

;;; avoid reload warnings in VL
;;;(if std-%unprotect-assign
;;;  (std-%unprotect-assign
;;;    '(;STD-FILENAME-BASE STD-FILENAME-EXTENSION-OR-NIL STD-FILEBASE
;;;      ;STD-FILENAME-DIRECTORY  STD-FILENAME-SPLIT
;;;      ;STD-FILENAME-EXTENSION
;;;      STD-FILE-MODTIME STD-FILE-SYSTIME
;;;      STD-FILE-READABLE-P STD-FILE-DIRECTORY-P)))

;;; Filename without path and extension
;;; Commonly called basename, hmm.. there's a problem with
;;; the vl extension which returns the filebase and not the basename
;;; name confusion!
;;; "l:/src/test.lsp" => test
(defun STD-FILENAME-BASE (fname)
  (cadr (std-filename-split fname)))

;;; File extension or nil. needed sometimes for cond
;;; "l:/src/test.lsp" 	=> ".lsp"
;;; "l:/src/test." 	=> "."
;;; "l:/src/test" 	=> nil
(defun STD-FILENAME-EXTENSION-OR-NIL (fname / s)
  (setq s (std-filename-extension fname))
  (if (/= "" s) s))

;;; Filename without extension, Ex: "L:\\SRC\\ACAD"
;;; For confusing stuff see STD-FILENAME-BASE
;;; Unifies path slashes
;;; "l:/src/test.lsp" 	=> "l:\\src\\test"
(defun STD-FILEBASE (fname)
  (std-make-pathname (std-firstn 2 (std-filename-split fname))))

;;; Only path of std-filename
;;; Unifies path slashes.
;;; "l:/src/test.lsp" 	=> "l:\\src\\"
(defun STD-FILENAME-DIRECTORY (fname)
  (car (std-filename-split fname)))

;;; Improved by FILEEXT, first load the normal ones then the extended
;;; but be sure that then the normal ones are not reloaded.

;;; Splits fspec in path, filename and extension
;;; Unifies path slashes.
;;; "C:\acad\lisp.doc"  => ("C:\acad\" "lisp" ".doc")
;;; "I:XX" 		=> ("I:" "XX" "")
;;; "L:/GERMAN"		=> ("L:/" "GERMAN" "")
;;;but
;;; "L:/GERMAN/"	=> ("L:/GERMAN" "" "")
;;; "acad/../acad/lsp"  => ("acad\\..\\acad\\" "lsp" "")
;;; "acad/../lisp.lsp" => ("acad\\..\\" "lisp" ".lsp")
;;; "acad/../.lsp" => ("acad\\..\\" "" ".lsp")
;;; "./acad/"	   => ("acad\\" "" "")
(defun STD-FILENAME-SPLIT (fspec / fn lst ext name path)
  (if (= fspec (car *STD:CACHED-FSPLIT*))
    (cdr *STD:CACHED-FSPLIT*)
    (progn
      ;; fixed v0.3020 for "./"
      (setq fn (std-strchg (std-%slash-unify fspec)
			   (strcat "." *SLASH-CHAR*) ""))
      (setq lst (reverse (std-string->list fn)))
      (setq ext (member 46 lst))		; the last dot
      (if (and ext (/= 46 (cadr ext)))		; fixed v0.3020 for ".."
        (setq ext (substr (std-list->string (reverse lst)) (length ext))
              lst (reverse (std-string->list
                      (substr fn 1 (- (strlen fn) (strlen ext))))))
        (setq ext  "")
      )
      (setq path (member (ascii *SLASH-CHAR*) lst))
      (if (not path) (setq path (member 58 lst)))	; (ascii ":")
      (setq name (std-list->string (reverse
    	(std-firstn (- (length lst) (length path)) lst))))

      (setq path (if path (std-list->string (reverse path)) ""))

      ;(setq name (substr (std-list->string lst) 1
      ;                   (- (length lst) (strlen path))))
      ;(list path (std-list->string (reverse (std-string->list name)))
      ;           ext)
      (cdr (setq *STD:CACHED-FSPLIT* (cons fspec (list path name ext))))
    )
  )
)

;;; file extension, Ex: ".DOC" or "", may not return nil!
;;; "l:/src/test.lsp" 	=> ".lsp"
;;; "l:/src/test." 	=> "."
;;; "l:/src/test" 	=> ""
(defun STD-FILENAME-EXTENSION (fname)
  (caddr (std-filename-split fname)))

;;; Readable file? Return the filename
(defun STD-FILE-READABLE-P (fname / f)	; => fname or nil
  (if (and (stringp fname)
           (setq f (open fname "r")))	; avoid std-remove
    (progn (close f) fname)))

;;; Is this a directory?
;;; This is quite dirty and won't probably work on all filesystems.
;;; On some clibs it failed with NTFS, but for now it should work
;;; with FAT and NTFS.
;;; Note: (findfile "<dir>/.") doesn't work,
;;;       (findfile "<dir>/NUL") neither
;;; Returns T or nil
;;; 2000-12-09 psw: It will fail with UNC pathnames
;;;  (std-%file-directory-p "D:\\prog\\") => nil
;;;  (std-%file-directory-p "D:\\prog") => T
;;;  (std-%file-directory-p "D:/prog") => T
;;;  (std-%file-directory-p "D:/prog/") => T
;;; 2000-12-31 05:55:35 rurban on NTFS5
;;;  (std-file-directory-p "\\\\reini\\install\\cad") =>  nil
;;;  (std-file-directory-p "//reini/install/cad/") => nil
;;;  (load "fileext.fas")
;;;  (std-%file-directory-p "\\\\reini\\install\\cad") => T
;;;  (std-%file-directory-p "//reini/install/cad/") => T
;;;  (std-load-external "stdlib14.arx")
;;;  
(defun STD-FILE-DIRECTORY-P (path)
  (if (std-functionp std-%file-directory-p) ; fixed since adsrx 0.5005
    (std-%file-directory-p path)
    (and (std-probe-file (strcat (std-fpslash path) "NUL")))))

;;; Directory name or nil. Needed sometimes for cond
(defun STD-FILENAME-DIRECTORY-OR-NIL (fname / s)
  (setq s (std-filename-directory fname))
  (if (/= "" s) s))

;;; Filename and extension without path, Ex: "TEST.DOC"
(defun STD-FILENAME (fname / lst)
  (strcat (cadr (setq lst (std-filename-split fname)))
	  (caddr lst)))

;;; Path must exist?
(defun STD-FORCE-PATH (path fname)
  (strcat (cond ((std-file-directory-p path) (std-fpslash path))
                ((std-filename-directory-or-nil path))
                (T (std-fpslash path)))
          (std-filename fname)))

;;; When extension is in fname return this,
;;; else append extension
(defun STD-DEF-EXTENSION (fname defext / lst)
  (setq lst (std-filename-split fname))
  (if (/= "." (std-firstchar defext))
    (setq defext (strcat "." defext)))
  (strcat (car lst) (cadr lst)
    (if (= "" (caddr lst)) defext (caddr lst))))

;;; Append or replace extension
(defun STD-FORCE-EXTENSION (fname ext)
  (if (/= "." (std-firstchar ext))
    (setq ext (strcat "." ext)))
  (strcat (std-filebase fname) ext))

;;; Is it a long filename?
;;; At least longer than allowed by MSDOS 8.3
;;; check also (STD-SYS-LONGFNAME-P)
;;; It must not be just a too long name, it may only be a
;;; illegal dos name but legal filename in other filesystems.
;;; embedded spaces e.g.
(defun STD-LONGFNAMEP (fname)
  (or (> (strlen (std-filename-base fname)) 8)
      (> (strlen (std-filename-extension fname)) 4)
      (std-strpos " " fname)
      ))

;;; (STD-FILENAME-UPCASE  fname)
;;; Upcase of filename, but ignore long filenames.
;;; Long filenames must be case-sensitive! Fixes "/.." paths
;;; This is for aesthetics only.
;;; Don't use it under unix as you probably know.
;;; Path slashes must be unified before?
(defun STD-FILENAME-UPCASE (fname / paths path p)
  ;; upcase all paths seperately
  (if (/= "" (setq path (std-filename-directory fname)))
    (progn
      (setq paths (std-strsplit (std-fpslash path) *SLASH-CHAR*))
      ;; resolve in between relative ".."
      ;; ("dir1" "dir2" ".." "dir3") => ("dir1" "dir3")
      (while (and (setq p (std-position ".." paths))
                  (> p 0))
	(setq paths (append (std-firstn (1- p) paths)
			    (std-nthcdr (1+ p) paths))))
      (setq path
	(std-strjoin
	  (if (= ":" (std-lastchar (car paths))) ; drive only
            (cons (std-strcase (car paths))
	          (mapcar (function std-filename-upcase) (cdr paths)))
	    (mapcar (function std-filename-upcase) paths))
          *SLASH-CHAR*))
      (setq fname (std-force-path path fname))))
  (cond
    ((not (stringp fname)))
    ((std-longfnamep fname)
     ;; if at least the extension is <= 3 + dot, upcase the ext
     (if (<= (strlen (std-filename-extension fname)) 4)
       (strcat (std-filename-directory fname)
	       (std-filename-base fname)
	       (strcase (std-filename-extension fname)))
       fname))
    ((std-acad-connection-p) (strcase fname))
    (T (std-strcase fname))))

;;; Path slashes must be unified before!
;;; (STD-%FILENAME-FIX-PATH "dir1/dir2/../dir3/") => "dir1/dir3"
;;; "./"  => "", "/./" => "/"
;;; Fixed by Serge Pashkov
(defun std-%filename-fix-path (path / p paths)
  (setq path (std-strchg path (strcat "." *SLASH-CHAR*) ""))
  (if (std-strpos (strcat *SLASH-CHAR* "..") path)
    (progn
      (setq paths (std-strsplit (std-fpslash path) *SLASH-CHAR*))
      ;; ("dir1" "dir2" ".." "dir3") => ("dir1" "dir3")
      (while (> (setq p (std-position ".." paths)) 0)
	(setq paths (append (std-firstn (1- p) paths)
			    (std-nthcdr (1+ p) paths))))
      (setq path (std-strjoin paths *SLASH-CHAR*)))
    path
  )
)
;;; Path slashes need not to be unified here
(defun std-%filename-abspath-p (path)
  (or (= (substr path 2 1) ":")
      (member (substr path 1 1) *STD:SLASH-CHARS*)))

;;; Returns full absolute filepath.
;;; Doesn't work yet, only on dos based systems.
;;; (std-truename "acad/../acad/test.lsp") => "C:/acad/test.lsp"
(defun STD-TRUENAME (fname / lst path drive cur)
  (if (std-module-defined-p "STDLIB-ADS")
    (std-%fullpath fname)
  (progn
  (setq lst (std-filename-split fname)
	path (std-%filename-fix-path (car lst))
	drive "")
  ;; check for drive (dos, win only)
  (if (std-sys-dosbased-p)
    (if (= ":" (substr path 2 1)) ; drive given
      (setq drive (substr path 1 2)
	    path (substr path 3))
      (setq drive (std-current-drive))))
  ;; check for absolute path
  (if (/= (substr path 1 1) *SLASH-CHAR*)
    (setq path (std-%filename-fix-path
		 (strcat (std-current-path) path)))) ; add current path
  (if (/= ":" (substr path 2 1)) ; no drive added?
    (setq path (strcat drive path)))
  (std-force-path path fname))
))

;;; cache this? only on DOS it was possible to change the workdir,
;;; r13 dos and unix also.
(defun STD-CURRENT-DRIVE ()
  (if (and (std-ver-r14-p) *STD:CURRENT-DRIVE*)
     *STD:CURRENT-DRIVE*
    (substr (std-current-path) 1 2)))

;;; Warning! This is poor code!
;;; Should be overridden by external code (FILEEXT)
;;; Returns current DOS, OS/2 or Unix path: "d:\\path\\...\\" or
;;; "/path/.../".
;;; WARNING: On DOS or OS/2 results may be FALSE UNPREDICTABLY.
;;; If SH (shell) fails with insufficient memory, increase its size
;;; in ACAD.PGP. Uses cached paths cached paths on R14 or later.
;;; Previous versions allowed the change of the workdir.
(defun STD-CURRENT-PATH (/ path f temp n)
  (cond 
   ((std-functionp std-%current-path)
    (std-%current-path))
   ((and (std-ver-r14-p) *STD:CURRENT-PATH*)
    *STD:CURRENT-PATH*)
   (T
    (setq temp (std-filename-mktemp nil))
    (close (open temp "w"))
    (setq path (car
		(std-%command-shell-wait
		 (cond
		  ((std-sys-dosbased-p) "CD")
		  ((std-sys-unix-p)     "pwd")
		  (T
		   (std-cerror
		    (std-msg 
             "unknown operating system for STD-CURRENT-PATH"))))
		     temp)))
		  (if path
		      (setq *STD:CURRENT-DRIVE* (substr path 1 2)
			    *STD:CURRENT-PATH* (std-fpslash path))
		      (std-cerror (std-msg "SHell failed"))
				   ))))


;;; without directory access and absolute path resolving yet
;;; "acad/test.lsp" <==> "../<actdir>/acad/test.lsp"    -cross-relative
;;; "acad/test.lsp" <==> "<drive>:/<path>/acad/test.lsp"-absolute
(defun STD-SAME-FILES-P (fname1 fname2)
  (cond
     ((= (setq fname1 (std-filename-upcase fname1))
         (setq fname2 (std-filename-upcase fname2))))
     ;; one absolute and one relative: resolve
     ((apply (function and)
	     (mapcar (function std-%filename-abspath-p)
		     (list fname1 fname2)))
       (= (std-truename fname1)
	  (std-truename fname2)))
     ;;??
     ;;((equal (std-filename-split fname1)
     ;;	       (std-filename-split fname2)))
     ;; to do: check actual filesystem
     ;; relative pathnames must be compared
     (T nil)
     ))


;;; -------------------------------------------------------------------73
;;; Directory (filesystem dependent) functions

;;; Open files are kept in the global list *FOPEN* and for
;;; non-VL systems also in the alist *std:fopen<->fname* to be able to
;;; the filename from the file object and vice versa.
;;; (for std-file-backup on open files and similar problems)

;;; We must keep track of open files for error handling
(defun STD-FOPEN (fname ch / f)
  (if (/= "" fname)
    (setq f (open fname ch)))
  (if (member f *FOPEN*)
    nil
    (if f (setq *FOPEN* (cons f *FOPEN*))))
  ;; for (std-%alisp-file->filename f) we also carry a global alist
  (if (and f (not (std-vl-p)))
    (std-push (cons f fname) '*std:fopen<->fname*))
  f
)

;;; What to do on nil?
;;;  1) raise an error or
;;;  2) do nothing (good on the user side) or
;;;  3) raise an continuable error only on *debug* (good for developers)
;;; Either treat it as programming error, or
;;; simplify loops like this:
;;; (if (setq f (std-fopen fname "r"))
;;;   (while (setq s (read-line f))
;;;     ...
;;; ))
;;; (std-fclose f)	; which means to accept nil
;;; this way another PROGN indentation is avoided
;;; we chose 2 and 3 and let already closed files pass also.
(defun STD-FCLOSE (f)
  (if (and f (std-open-file-p f))
    (progn
      (close f)
      ;; The following may be turned off for performance reasons.
      ;; Only useful if the number of closed files will get too large.
      (if (not (std-vl-p))
        (setq *std:fopen<->fname*
	       (std-remove (assoc f *std:fopen<->fname*)
		 *std:fopen<->fname*)))
      (setq *fopen* (std-remove f *fopen*))
    )
    (if *DEBUG*
      (if (not f)
	(std-cerror (list "(STD-FCLOSE nil)" *fopen*))
	(std-cerror
	 (list (std-msg "STD-FCLOSE on already closed file - ")
	       f *fopen*))))))

;;; close all my open files in case of errors,
;;; is placed in the error handler
(defun STD-FCLOSEALL ()
  (mapcar (function (lambda (f)
    	    (if (and (std-filep f) (std-open-file-p f))
              (close f))))
    	  *FOPEN*)
  (setq *FOPEN* nil *std:fopen<->fname* nil)
)

;;; Is the file (filepointer) already open?
;;; We have no predicate if the file was already closed.
;;; (close) with a closed file will throw an error. This predicate
;;; is needed for error handling cleanup, in unpredictable code areas
;;; where we don't know if the file was not already closed.
;;; => T or nil
(defun STD-OPEN-FILE-P	(f)
;;;  (if (and (std-vl-p)
;;;	   (not (std-module-defined-p "STDLIB-ADS"))
;;;	   (numberp *FILEEXT-VERSION*)
;;;	   (>= *FILEEXT-VERSION* 0.53)
;;;	   )
;;;    ;; this detects all opened files (binio-open, open, std-fopen).
;;;    ;; in visual lisp <R15 or vital lisp via FILEEXT.FAS>
       ;; this fails now...
;;;    (std-%open-file-p f)
    ;; simply this:
    ;; look up the file pointer in *FOPEN*
    (and (member f *FOPEN*)))
;;;)

;;; Return T if the filename belongs to an open file.
(defun STD-OPEN-FILENAME-P (fname)
  (and (stringp fname) (std-open-filename->file fname)))

;;; Return nil or the file object which belongs to the filename,
;;; by inspecting the *fopen* list plus *std:fopen<->fname* in non-VL systems.
(defun STD-OPEN-FILENAME->FILE (_fname)
  (if (findfile _fname)
    (car (std-member-if
	   (function (lambda (x)
	     (and x (std-same-files-p x _fname))))
	   (mapcar (function std-file->filename) *FOPEN*)))))

;;; The reverse: get filename for the open file object
(defun STD-FILE->FILENAME (f)
  (if (std-vl-p)
    (std-%vl-file->filename f)
    (std-%alisp-file->filename f)))

;;; The VL fileobject contains the filename in the printed object name
;;; #<file "filename">
(defun std-%vl-file->filename (f)
  (if (std-filep f)
    (std-str-1 (substr (std-princ-to-string f) 8))))

;;; Alisp not #<File: #3f322446> so we use a global assoc list.
(defun std-%alisp-file->filename (f)
  (cdr (assoc f *std:fopen<->fname*)))

;;; -------------------------------------------------------------------73

;;; recursive cycles in R12DOS! already defined in STDINIT
;;;(defun STD-PROBE-FILE (fname / f)
;;;  (if (std-acad-connection-p)
;;;    (if (std-filename-directory-or-nil fname)
;;;      (findfile fname)
;;;      (findfile (std-force-path "./" fname))
;;;    ))
;;;    (if (and (stringp fname)
;;;             (setq f (open fname "r")))	; avoid std-remove
;;;      (progn (close f) fname)))

;;; Return all matching and existing filenames.
;;; search fspec in lst (dirs also), which defaults to "*.*"
;;; Note: using wcmatch and NOT console globbing style
;;; (defun STD-GLOB (_fspec lst)
;;;   (if (not lst) (setq lst (std-directory-files "*")))
;;;   (if (std-acad-connection-p)
;;;     (std-remove-if-not
;;;  (quote    
;;; 	; )
;;;       (lambda (fn)
;;;        (and (or (= _fspec "")
;;; 	        (wcmatch fn _fspec))
;;; 	    (std-file-readable-p fn))))
;;;       lst)
;;;     (std-
;;;       (std-remove-if-not 'std-file-readable-p
;;;                          (std-directory-files _fspec))
;;;       lst)
;;;   )
;;; )

;;; List of files matching fspecs and dirs.
;;; GLOB-style wildcards allowed. Returns filenames without path prefix.
;;; A path should have either a "/" or "\\" as last char or explicitly
;;; use "/*". UNC names are allowed. (tested under NT)
;;;   "C:/ACAD/*" => ("." ".." "ACAD.LSP" ...)
;;;   "C:/ACAD/"  => ("." ".." "ACAD.LSP" ...)
;;;but
;;;   "C:/ACAD"   => ("ACAD")
(defun STD-DIRECTORY-FILES (fspec / base)
  (std-%directory-files
    (std-filename-directory fspec)
    (cond ((/= "" (setq base (std-filename-base fspec)))
	   base)
	  (T nil))
    0))

;;; as STD-DIRECTORY-FILES, but no dirs, includes the given path prefix.
;;; => ("C:/ACAD/SUPPORT/ACAD.LSP" "C:/ACAD/SUPPORT/ACADR14.LSP"...)
;;; TODO: GLOB-style wildcards, but we'll better change to wcmatch regex.
(defun STD-GLOB-FILES (fspec / _path)
  (setq _path (std-filename-directory fspec))
  (mapcar (function (lambda (fn) (std-force-path _path fn)))
    (std-%directory-files
      (cond ((std-filename-directory-or-nil fspec)) (T nil))
      (cond ((/= "" (std-filename fspec))(std-filename fspec)) (T nil))
      1)))

;;; All dirs matching fspec, but no files.
;;; no path prefixes, wildcards allowed
;;; "C:/ACAD" => ("." ".." "SUPPORT" "SAMPLE"...)
;;; TODO: GLOB-style wildcards, but we'll better change to wcmatch regex.
;;; we'll use STD-FINDFILE-PATHS then
(defun STD-GLOB-DIRS (fspec)
  (std-%directory-files fspec nil -1))

;;; Convert glob-style patterns to regular wcmatch patterns
;;; since wcmatch already uses most of the GLOIB semantics it makes no sense
;;; * -> .*, . -> \., ? -> ., 
;;; *.* -> .*\..*
;(defun std-%glob-regexp (pattern)
;  (setq pattern (std-strchg pattern "." "\."))
  ;; (setq pattern (std-strchg pattern "?" "."))  ; wcmatch has this
  ;; (setq pattern (std-strchg pattern "*" ".*")) ; wcmatch has this
;)

;;; Return the first found file in the user-defined module-path
;;; or the standard acad library path. No wildcards!
;;; Cache the unified list and remove duplicates.
;;; Cache re-init problem when the user changes the search-path setting!
;;;   (setq *STD:SEARCH-PATH* nil)
(defun STD-FINDFILE (fname)
  (if *STD:SEARCH-PATH*
    (std-findfile-paths fname *STD:SEARCH-PATH*)
    (std-findfile-paths fname
      (setq *STD:SEARCH-PATH*
	(std-ordered-union *MODULE-PATH*
			   (std-acad-library-paths))))))

;;; first found fspec in the given path list, no wildcards!
(defun STD-FINDFILE-PATHS (_fname pathlst / found)
  (if (std-%filename-abspath-p _fname)
    (findfile _fname)	; absolute path given
    (progn		; relative or no path
      (while (and (not found) pathlst)
	;; use findfile instead of std-probe-file?
        (setq found (std-probe-file (std-force-path (car pathlst)
						    _fname))
	      pathlst (cdr pathlst)))
      found)))

;;; Search file with all extensions in given pathnames, extensions first.
;;; First try the first extension in all paths,
;;; then for each other extension all paths. (opposite to std-%findfile-exts)
;;; Return first found filename.
;;; No wildcards yet
;;;   (std-findfile-exts "ACAD" '("MNS" "MNU") (STD-ACAD-LIBRARY-PATHS))
(defun STD-FINDFILE-EXTS (path extlst pathlst / found)
  (while (and extlst (not found))
    (setq path  (std-force-extension path (car extlst)))
    (setq found (if (std-filename-directory-or-nil path)
        (std-probe-file path)	; explicit directory given
        (std-findfile-paths path pathlst)
      )
    )
    (setq extlst (cdr extlst))
  )
  found
)

;;; Search file with all extensions in given pathnames, paths first.
;;; First try path, then in each path all extensions.
;;; return first found filename.
;;; no wildcards yet
(defun std-%findfile-exts (path extlst pathlst / fname)
  ;; fast version
  (while (and pathlst (not fname))
    (setq path (std-force-path (car pathlst) fname))
    (setq fname (if (std-filename-extension-or-nil path)
        (std-probe-file path)	; explicit extension given
        (std-%findfile-try-exts path extlst)
      )
    )
    (setq pathlst (cdr pathlst))
  )
  fname
)

;;; Try all extensions in the path (=filebase),
;;; no extension allowed in path
(defun std-%findfile-try-exts (path extlst / fname)
  (while (and extlst (not fname))
    (setq fname (std-probe-file (strcat path (car extlst)))
          extlst (cdr extlst)))
  fname
)

;;; Cached version
;;; Cache re-init problem when the user changes the search-path setting!
;;;   (setq *STD:ACAD-LIBRARY-PATHS* nil)
(defun STD-ACAD-LIBRARY-PATHS ()
  (cond
    (*STD:ACAD-LIBRARY-PATHS*)
    ((setq *STD:ACAD-LIBRARY-PATHS*
      (std-remove-duplicates
        (mapcar (function (lambda (p)
          (std-fpslash (std-filename-upcase p))))
          (std-adjoin (getvar "DWGPREFIX")
            (std-string->strlist
	      (cond
	        ((std-ver-fcad-p)
		 (getenv "FCADSUP"))
		(T (getvar "ACADPREFIX")))
	      ";"))))))))


;;; Old stuff for textfiles only:

;;; Backup for text files
(defun STD-FT-BACKUP (fspec)
  (if (std-string-not-equal ".BAK" (std-filename-extension fspec))
    (std-ft-copy fspec (std-force-extension fspec ".BAK"))))

;;; Copies textfile file1 to file2
(defun STD-FT-COPY (file1 file2 / s *error*)
  (setq *error* *std-error*)
  (setq file1 (std-fopen file1 "r")
        file2 (std-fopen file2 "w"))
  (while (setq s (read-line file1))
    (write-line s file2))
  (std-fclose file1) (std-fclose file2)
  (princ))

;;; Append textfile file2 after file1. Extends file1
(defun STD-FT-APPEND (file1 file2 / s *error*)
  (setq *error* *std-error*)
  (setq file2 (std-fopen file2 "r")
        file1 (std-fopen file1 "a"))
  (while (setq s (read-line file2))
    (write-line s file1))
  (std-fclose file1) (std-fclose file2)
  (princ))

;;; Compares two textfiles. Is there any useful usage?
;;; Fixed by Serge Pashkov
(defun STD-FT-EQUAL (file1 file2 / s okay *error*)
  (setq *error* *std-error*)
  (setq file1 (std-fopen file1 "r")
        file2 (std-fopen file2 "r")
        okay t)
  (while (and okay (setq s (read-line file1)))
    (if (/= s (read-line file2))
      (setq okay nil)))
  ;; The 1st file is exhosted. Is the 2nd too? Set nil if not.
  (if (and (null s) (read-line file2))
    (setq okay nil))
  (std-fclose file1) (std-fclose file2)
  okay)

;;; returns the line-th line from the textfile
;;; line: one-based int
(defun STD-FT-NTH-LINE (fname line / s f i *error*)
  (setq *error* *std-error*)
  (setq f (std-fopen fname "r")	i 0)
  (while (and f (< i line) (setq s (read-line f)))
    (setq i (1+ i)))
  (std-fclose f)
  s)

;;; returns a list of lines from the textfile
;;; from, to: one-based int
(defun STD-FT-LINES (fname from to / s f i lst *error*)
  (setq *error* *std-error*)
  (setq f (std-fopen fname "r")	i 0)
  (while (and f (< i from) (setq s (read-line f)))
    (setq i (1+ i)))
  (if s (setq lst (list s)))
  (while (and f (< i to) (setq s (read-line f)))
    (setq lst (cons s lst)
	  i (1+ i)))
  (std-fclose f)
  (reverse lst))

;;; Insert asc-lst in text file fname after line num (starting with 1)
;;; Useful for manipulating menus if you know the line number.
;;; fixed by Serge Pashkov
(defun STD-FT-INSERT (fname strlst num / i fr fw tmp-fname s *error*)
  (setq *error* *std-error*)
  (setq fr (std-fopen fname "r")
        fw (std-fopen (setq tmp-fname (std-filename-mktemp nil)) "w")
	i 0)
  (while (< i num)
    (setq i (1+ i))
    (if (setq s (read-line fr))
      (write-line s fw)
      (setq i num)))			; EOF: break out
  (foreach s strlst			; insert lst
    (write-line s fw))
  (while (setq s (read-line fr))	; append rest
    (write-line s fw))
  (std-fclose fr)
  (std-fclose fw)
  (std-file-delete fname)		; fixed
  ;; rename tmpf to fname
  (std-file-rename tmp-fname fname))

;;; Handles dots in module names. omits extension.
;;; Needed for (std-load-external "FILEEXT.FAS")
;;; This is safer than the previous definition.


;;; assure that the value is between the given bounds
(defun std-%minmax (low high val)
  (max low (min high val)))

;;; Show contents of textfile in dialog with optimized size.
;;; Simple version, with action callback for "list".
;;; action_tile callback function for the list with the
;;;   four arguments:  $key $value $data $reason
;;; (lambda (key val data how)
;;;   (cond ((= how 4) (double-click-action key val data))
;;;         (T (single-click-action key val data))))
(defun STD-SHOWFILE-DIALOG  (fname title action /
			     width height lines dcl id l)
  (if (not title)(setq title ""))
  ;; optimize the dialog size
  (setq lines  (std-read-file-strings fname)
	;; 33 lines is enough to stay below 600 pixel
        height (std-%minmax 10 33 (setq l (length lines)))
	width (if (> l *MAX-ARGS-LIMIT*)
    	       (std-error
		 (std-msg "maximal number of arguments exceeded"))
	       (std-%minmax 35 85
		 (apply (function max)
		        (mapcar (function strlen) lines))))
        dcl    (std-%emit-showfile-dialog height width))
  ;; FCAD!
  (or (and (setq id (load_dialog dcl))
	   (if *dlg-pt*
              (new_dialog "showfile" id "" *dlg-pt*)
              (new_dialog "showfile" id)))
     (std-error (std-msg "std-%showfile-dialog aborted")))
  (set_tile "title" title)
  (start_list "list")
  (foreach line lines (add_list line))
  (end_list)
  ;; Store the position to be able to cascade the next dialogs,
  ;; possibly created by the action callback on the list entries.
  (action_tile "accept" "(setq *dlg-pt* (done_dialog 1))")
  (action_tile "cancel" "(setq *dlg-pt* (done_dialog 0))")
  (if (std-functionp action)
    (progn
      ;; support the $data argument for the action function callback
      ;; to avoid global data (but it is not needed generally)
      ;; with linked VL use $data, otherwise do not care.
      ;; thanksfully we have dynamic binding
      (client_data_tile "list" (std-strjoin lines "\n"))
      (action_tile "list"
		 (strcat "("
			 (std-prin1-to-string action)
			 " $key $value $data $reason)"))))
  (start_dialog)
  ;; Because this DCL is created dynamically we will delete
  ;; it from memory. We won't use caching, it is fast enough.
  ;; The temp file itself is deleted on the next call to
  ;; STD-VAR-RESTORE as every single temp file during a session.
  (unload_dialog id))

(defun std-%emit-showfile-dialog (height width)
  (if (not  width) (setq width  80))
  (if (not height) (setq height 25))
  (std-%file-write-lines
    ;; this temp filename gets deleted on (std-var-restore)
    (std-filename-mktemp "SHOW.DCL")
    (list
     "dcl_settings : default_dcl_settings { audit_level = 0; }"
     "list : list_box {"
     "    key = \"list\";"
     "    tabs = \"9 17 25 33 41 49 57 65 73 81\";"
     (strcat "    width  = " (itoa (+ 7 width)) ";" )
     (strcat "    height = " (itoa (+ 2 height)) ";" )
     "}"
     "showfile : dialog {"
     "    label = \"\"; key   = \"title\";"
     "    list;"
     "    ok_cancel_err;" "}")
    ))

(defun std-%file-write-lines (fname lines / f)
  (if (setq f (std-fopen fname "w"))
    (progn
      (foreach s lines
	(write-line s f))
      (std-fclose f)
      fname)))

(defun std-%showfile-console (fname / _std:f s *error*)
  (setq fname (cond ((std-probe-file fname))
    		    ((std-acad-connection-p) (findfile fname))))
  (if (setq _std:f (std-fopen fname "r"))
    (progn
      (defun *error* (s) (std-fclose _std:f)(princ s))
      (while (setq s (read-line _std:f))
	(terpri)
        (princ s))
      (std-fclose _std:f))))

;;; print in a dialog or on console
(defun STD-SHOWFILE (fname)
  (if (and (std-acad-connection-p)
           (not (zerop (getvar "CMDDIA"))))
    (std-showfile-dialog  fname
			   (strcat (std-msg "Contents of ") fname)
			   nil)
    (std-%showfile-console fname)))

;;; Returns content of file as list of strings,
;;; ?? ignoring lines starting with ";"
;;; leaving empty lines
(defun STD-READ-FILE-STRINGS (n / f s lst *error*)
  (setq *error* *std-error*)
  (if (and n (setq f (std-fopen n "r")))
    (progn
      (while (setq s (read-line f))
        (if (/= (std-firstchar s) ";")
          (setq lst (cons s lst))))
      (std-fclose f)))
  (reverse lst))

;;; Treats file as list of lisp-like lines,
;;; ignoring lines starting with ";"
;;; Returns a list of lists.
(defun STD-READ-FILE-LIST (n / f s lst *error*)
  (setq *error* *std-error*)
  (if (and n (setq f (std-fopen n "r")))
    (progn
      (while (setq s (read-line f))
        (if (and (/= s "") (/= (std-firstchar s) ";"))
          (setq lst (cons (read (strcat "(" s ")")) lst))))
      (std-fclose f)))
  (reverse lst))




;;; -------------------------------------------------------------------73
;;; External definitions:

;;; Directory access funcs:


;;; Functions which may be improved by external extensions:
(defun STD-FILE-EXIST-P	(fname)	; => T or nil
  (if (std-probe-file fname) T))

;;; Checks for file creation rights.
;;; You may have the right to create this file but not to delete it.
;;; Destructive version! Use with care! Deletes the old file if it exists.
;;; Fixed by Serge Pashkov
(defun STD-FILE-WRITABLE-P (fname / f)	; => fname or nil
  (and (stringp fname)
       (std-file-exist-p fname)
       (setq f (std-fopen fname "w"))
       (progn
         (std-fclose f)
	 ;; The OS may allow creation but not deletion!
	 ;; This is not detected, so there may be 0 byte files around.
         (std-file-delete fname)
	 fname)))

;;; Consistent temp. filenames under all systems and different from the
;;; VL extension filenames to be able to delete them automatically.
;;; fname defaults to : "%TEMP/$STD~001.AC$" instead of "%TEMP/$VL~~001"
;;;
;;; STD-FILENAME-MKTEMP is most often called by std-princ-to-string on
;;; not VL systems, in "AutoLISP compatibility mode".
;;;
;;; We use a global list *STD:MKTEMP* of those to delete them on END
;;; or when the number of created files exceed a certain filesystem
;;; dependent treshold. We use 500. With ~1000 files on a FAT %TEMP%
;;; the system slows down inacceptable.
(defun STD-FILENAME-MKTEMP (fname)
  (std-default '*STD:MKTEMP-LENGTH* 0)
  (std-default '*STD:MKTEMP-TRESHOLD* 500)
  (setq fname
   (if (not fname)
    (std-%filename-mktemp "$STD~.AC$")
    (std-%filename-mktemp
      (std-make-pathname
        (list
	  (std-filename-directory fname)
	  (cond ((not fname) "$STD~")	; vlisp alike
	        (T (std-filename-upcase (std-filename-base fname))))
	  (cond ((std-filename-extension-or-nil fname))
		(T ".AC$")))))))
  ;; After about 500 files it becomes inacceptably slow.
  ;; So we try delete all temp files as with a (gc)
  ;; Recursive std-filename-mktemp invocations must not hit this limit.
  (if (> *STD:MKTEMP-LENGTH* *STD:MKTEMP-TRESHOLD*)
    (std-%delete-mktemp))
  (setq *STD:MKTEMP* (cons fname *STD:MKTEMP*)
        *STD:MKTEMP-LENGTH* (1+ *STD:MKTEMP-LENGTH*))
  fname)


;;; use STD-SYS-LONGFNAME-P
(defun std-%filename-mktemp (fname / path ext base i fn maxlen)
  (setq maxlen (if (std-sys-longfname-p) 23 6))  ;fixed
  (setq path (cond ((std-filename-directory-or-nil fname))
		   ((not (std-acad-connection-p)) "")
	           ((getenv "TEMP"))
	           ((getenv "TMP"))
		   (T ""))
	ext  (cond ((std-filename-extension-or-nil fname))
		   (T ".AC$"))
	base (cond ((not fname) "$STD~")	; vlisp alike
		   (T (std-filename-upcase (std-filename-base fname))))
        base (if (= (std-firstchar base) "$")
	         (substr base 1 maxlen)
	         (substr (strcat "$" base) 1 maxlen))
	base (strcat (std-fpslash path) base)
	i 0)
  (while (std-file-exist-p
	   (setq fn (strcat base
			    (std-string-left-pad-char (itoa i) 3 "0")
			    ext)))
    (setq i (1+ i)))
  fn)


;;; We try to delete all temp. filenames created with
;;; std-filename-mktemp.
;;; 1) We cannot blindly delete all files, no open files.
;;;    That's why *STD:MKTEMP* may not be empty afterwards.
;;; 2) If we would know the fileobject file-delete would be much faster!
;;;    But then we would need something like std-fopen-temp and
;;;    (file-delete file)
(defun std-%delete-mktemp (/ f lst)
  (std-%file-bulk-delete *STD:MKTEMP*)
  (setq *STD:MKTEMP*
	 (std-remove-if 'null
	   (mapcar (function std-probe-file) *STD:MKTEMP*)))
  (setq *STD:MKTEMP-LENGTH* (length *STD:MKTEMP*))
)

;;;Returns DOS 8.3 file name, by Serge Pashkov
;;; use ads std-%dos-filename instead
;;;Works only on dosbased systems, (std-sys-dosbased-p)=>T
;;;How: we use shell command dir with reassigned output to temporary file
;;;     and subsequent parsing the output file.
;;;     Because Autocad prohibits OS calls like "dir > out"
;;;     we make temporary command file (BAT file) containing
;;;     this command  and call it via Autocad "Shell" command.
;;;     Temporary files should be deleted after parsing.
;;;     Assumed that we can sufficient rights for creating/writing.
;;;     Uses STD-SLEEP for delay
;;; ???Error processing???
;;; Maybe to be improved, quite dirty
(defun STD-DOS-FILENAME (fname / f batname outname fn s eof
			 found fnlst dosfname *error*)
  (if (std-module-defined-p "STDLIB-ADS")
    (std-%dos-filename fname)
  (progn
  (setq *error* *std-error*)
  (if (std-longfnamep fname)
    (progn
      (setq outname (std-filename-mktemp "$1.tmp")
            batname (std-filename-mktemp "$1.bat")
            dosfname nil)
      (if (setq f (std-fopen batname "w"))
        (progn
          (write-line
	    (strcat "DIR "
		    (std-%stringquote-filename fname)
		    " >" outname) f)
          (std-fclose f)
          (std-%command-shell-wait batname outname)
	  ;; to be improved to parse the result value...
          (if (setq f (std-fopen outname "r"))
           (progn
             ;; get fname w/o path
             (setq fn (std-filename fname))
             ;; Parse output
             (setq found nil eof nil)
             (while (not (or found eof))
               (if (setq s (read-line f))
               ;; This long name?  45 - start of long file name
                 (if (std-stricmp fn (substr s 45))
                 ;; Found - parse 12 first chars
                   (progn
                     (setq fnlst (std-strtok (substr s 1 12) " "))
                     (if (= 1 (length fnlst))
                       (setq dosfname (car fnlst))
                       (setq dosfname (strcat (car fnlst) "."
					      (cadr fnlst))))
                     (setq found T)
                   )
		 )
                 (setq eof T)
               )
	     )
             (std-fclose f)
            )
            (std-error (std-msg "DIR reassign failed"))
          )
        )
        (std-error (strcat (std-msg "Cannot open ") batname))
      )
      dosfname
    )
    (std-filename-upcase fname)
  )
)
))


(defun std-%open-warn-close (fname)
  (if (std-open-filename-p fname)
    (progn
      (std-fclose (std-open-filename->file fname))	; 19-Apr-00
      (std-warn (list 
		 (std-msg "Closed open file ")
		  "\"" fname "\""))
      T)))

;;; Create a backup copy with the .BAK extension and
;;; the original file attributes (timestamp).
;;; A simple (std-file-copy fname bak) is wrong.
(defun STD-FILE-BACKUP (fname / bak)
  (cond
    ((= ".BAK" (std-strcase (std-filename-extension fname)))
      ;; added 19-Apr-00
      (std-warn (list "(STD-FILE-BACKUP \"" fname "\")"
		      (std-msg " failed") ". "
		      (std-msg "Cannot backup a .BAK file"))))
    (T
     (if (std-file-readable-p fname)
       (progn
         (std-%open-warn-close fname)
         (std-file-delete
	   (setq bak (std-force-extension fname ".BAK")))))
     (std-file-rename fname bak)
     (if (std-probe-file fname)
       (progn
         (std-%open-warn-close fname)
	 (std-file-rename fname bak)
         (if (std-probe-file fname)
           (std-warn (list
		       "(STD-FILE-BACKUP \"" fname "\")"
		       (std-msg " failed"))))))
     (std-file-copy bak fname)
    )
  )
)

;;; surround the possibly illegal name with spaces for the command shell
(defun std-%stringquote-filename (fname / f)
  ;; Convert slashes
  (setq f (std-make-pathname (list (std-filename-directory fname)
				   (std-filename fname))))
  ;; Quote fname for the shell if there's a space
  (if (std-longfnamep f)
    (strcat "\"" f "\"") f))

;;; For now we have terribly performance problems with deleting a lot of
;;; files, such as cleanup after a lot of std-princ-to-string.
;;; That's why we provide this bulk file deleter which does some
;;; littel optimization on not VL systems (still not using doslib)
;;; Accepts wildcards and uses less shell calls. Used for
;;; std-%delete-mktemp
;;;  ex. (std-%file-bulk-delete '("c:/tmp/$std~*.ac$"))
(defun std-%file-bulk-delete (fnames)
  (cond
    ((std-vlisp-p)
     (mapcar (function vl-file-delete) fnames))
    ((std-vill-p)
     (mapcar (function vlx-file-delete) fnames))
    ((member (type std-file-delete) '(USUBR SUBR EXSUBR EXRXSUBR))
     (mapcar (function std-file-delete) fnames))
    ((std-sys-dosbased-p)
     ;; stupid dos cmdline has a limit of 256 chars?
     ;; if too much r13 crashes!
     ;; 12*20 = 240
     (foreach x (std-split-list 12 fnames)
       (std-%command-shell-wait
         (strcat "for %s in ("
	           (std-strjoin
                   (mapcar (function std-%stringquote-filename) x) " ")
		 ") do del %s" ) nil))
    )
    ((std-vlisp-beta1-p)
     (mapcar (function file-delete) fnames))
    ((std-sys-unix-p)
     (std-%command-shell-wait
       (strcat "rm "
	 (std-strjoin
  	   (mapcar (function std-%stringquote-filename) fnames) " "))
       nil))
    (T (mapcar (function std-file-delete) fnames)) ; no wildcards!
  )
)


;;; User version:
;;;   "Is any of the dependents newer than the target?"
;;; T if any dependent is newer than the target or there's no target.
;;; Used to update a target dependent on any source file like in MAKE.
;;; The argument deps may be filename or a list of filenames.
;;; So far it makes use of a fast internal file-modtime function which
;;; returns the date as seconds since 1970.
;;;   file-modtime: The number of non-leap seconds since 00:00:00 UTC,
;;;                 January 1, 1970
;;;   E.g.: 852559628 <==> 6.1.97 15:13 <==> 1/6/97 15:13
;;;                   <==> 19970106.1513
(defun STD-FILE-NEWER-P (_target deps / x)
  (cond ((not (std-file-modtime _target)))
	((consp deps)
          (if (> (length deps) *MAX-ARGS-LIMIT*)
            (std-error
	      (std-msg "maximal number of arguments exceeded")))
          (< (std-file-modtime _target)
            (apply (function max)
	      (mapcar
		(function (lambda (x)
		  (cond
		    ((std-file-modtime x))
		    (T (+ 1 (std-file-modtime _target))))))
                deps))))
	((setq x (std-file-modtime deps))
	 (< (std-file-modtime _target) x))
	))

;;; Strict version: both files must exist!
(defun std-%file-newer-p (new old)
  (> (std-file-modtime new)(std-file-modtime old)))

;;; Seconds since 1.1.1970 UTC as fixnum
;;; Note: This may be inexact and different to the FILEEXT.FAS version!
(defun STD-FILE-MODTIME (fname / date)
  (cond
    ;; incorrect results!
    ;;(std-%file-modtime (std-%file-modtime fname))
    ((and vl-file-systime
	  (setq date (vl-file-systime fname)))
      (fix (std-%datlst->internal-time
             (std-select date
	                '(0 1 3 4 5 6 7)))))
    ((setq date (std-file-systime fname))
      (fix (std-%datlst->internal-time
             (std-cdate->datlst date))))
    ))

;;; either call doslib, fileext or just return all possible
(defun STD-GET-DRIVE-LIST ()
  (cond
    ((std-doslib-p)
     (dos_drives))
    ;; Mac and unix have no drives
    ((not (std-sys-dosbased-p)) nil)	; added 9/23/00
    (std-%get-drives-string
     (mapcar
      (function (lambda (a)
        (strcat (chr a) ":")))
      (std-string->list (std-%get-drives-string))))
    ;; for the slow method below we cache it. added 9/23/00
    (*STD-GET-DRIVE-LIST*)
    (T 
     (setq *STD-GET-DRIVE-LIST*
       ;; TODO: check all root dirs
       (std-remove nil
	 (mapcar
	   (function (lambda (a / s)
	     (setq s (strcat (chr (+ a 65)) ":"))
	     ;; to be tested on DOS
	     (if (std-probe-file (strcat s "\\NUL"))
		 s
		 nil)))
           (std-int-list 26)))))))

;;; Load the required external FAS library (>=R13)
(defun std-%load-fileext-fas ()
  (or
    (progn
      (if (not (std-acad-connection-p))
	(std-%simple-require "STANDALONE"))
      (if (or (not (std-acad-connection-p))
              (< 12 (std-ver-num) 14.8))
	(progn
  	  (std-%unprotect-assign 
           '(STD-RMDIR STD-MKDIR STD-FILE-DIRECTORY-P))
          (std-require-version "FILEEXT.FAS" 0.1)))
      (if (and (numberp *FILEEXT-VERSION*)
	       (>= *FILEEXT-VERSION* 0.2))
        ;; recalc it to enable std-%acad-connection-p
	(progn
          (setq *ACAD-CONNECTED* nil)
          (defun std-rmdir (s)
	    (std-%rmdir s))
          (defun std-mkdir (s)
	    (std-%mkdir s))
	  (defun STD-FILE-DIRECTORY-P (path)
	    (STD-%FILE-DIRECTORY-P path))
;;;          (if (not STD-FILE-SYSTIME)
;;;	   (defun STD-FILE-SYSTIME (path)
;;;	     (STD-%FILE-SYSTIME path)))	; broken
  	  (std-%protect-assign 
           '(STD-RMDIR STD-MKDIR STD-FILE-DIRECTORY-P))
	  1
	)
      )
      *FILEEXT-VERSION*
    )
    (std-warn (std-msg "Module FILEEXT.FAS not found"))))

;;; Returns list of output lines.
;;;  (strcat "REN " (std-%stringquote-filename from)
;;;           " "   (std-%stringquote-filename to))))
(defun std-%command-shell-wait (cmd outfile / n dir endt)
  ;; to be fixed for FCAD
  (if (null outfile)
    (command "_SH" cmd)
    (progn
      (command "_SH" (strcat cmd " > " outfile))
      ;;by Serge Pashkov:
      ;; we should wait until the shell closes the temp file. (30 ms)
      (setq dir nil
	    endt (+ (getvar "DATE") (/ *STD:SHELL-TIMEOUT* 8640000.0)))
      (while (and (null dir) (< (getvar "DATE") endt))
        (if (std-file-readable-p outfile)
          (setq dir (std-read-file-strings outfile))))
      dir)))



(defun std-%shell-file-copy  (from to)
  (cond
    ((and (std-doslib-p) (std-functionp 'dos_copy))
     (dos_copy from to) T)
    ((not (std-probe-file from)) nil)
    ((std-sys-dosbased-p)
     (std-%command-shell-wait
       (strcat "COPY " (std-%stringquote-filename from)
               " "     (std-%stringquote-filename to))
       nil) T)
    ((std-sys-unix-p)                   ; quote fnames from shell expansion
     (std-%command-shell-wait
       (strcat "cp '" (std-%stringquote-filename from)
               "' '"  (std-%stringquote-filename to) "'") nil) T)
    (T (command "_FILES" 5 from to 0) T)))


;;; File function mappings
;;; Maybe TODO:
;;;   Check for the (loaded?) bonus/express tools which also have some
;;;   file funcs. The express tools are probably not needed, because VL
;;;   has the needed basics anyway, and doslib14 shipped with the R14
;;;   bonus tools as well.


(cond
  ((std-vill-p)		; Vital Lisp extensions


    (defun std-file-rename (from to)
      (vlx-file-rename from to))
    (defun std-file-copy (from to)
      (cond
	((vlx-file-copy from to))
	((std-%shell-file-copy from to))))
    ;; note reverse order
    (defun std-file-append (to from)
      (vlx-file-copy from to T))
    (defun std-file-delete (fn)
      (vlx-file-delete fn))
    (defun std-%filename-mktemp (fn)
      (std-%slash-unify (vlx-filename-mktemp fn)))
    (defun std-%directory-files (dir fn flag)
      (vlx-directory-files dir fn flag))
    (defun std-file-size (fname)
      (vlx-file-size fname))
;;; (defun std-file-systime (fname)
;;;      (std-datlst->cdate (std-select (vlx-file-systime fname)
;;;				      '(0 1 3 4 5 6 7))))



  )
  ((std-vlisp-p)	; Visual Lisp extensions


    (defun std-file-rename (from to)
      (vl-file-rename from to))

   ;; fixed
    (defun std-file-copy (from to)
      (cond
	;; VL- fails with space or dots in filename!
	;; "L:\\SRC\\STDLIB\\test\\VLisp RTS v.5.0a0-19b, 14.0.RET"
	;; but delete, copy-append, rename works okay
	((vl-file-copy from to))
	((std-%shell-file-copy from to))))
	      
    ;; note reverse order
    (defun std-file-append (to from)
      (vl-file-copy from to T))
    (defun std-file-delete (fn)
      (vl-file-delete fn))
    (defun std-%filename-mktemp (fn)
      (std-%slash-unify (vl-filename-mktemp fn)))
    ;; 2-Mar-00
    ;;(if (not (std-module-defined-p "STDLIB-ADS"))
    (defun std-%directory-files (dir fn flag)
      (vl-directory-files dir fn flag))
    ;;)
    (defun std-file-size (fname)
      (vl-file-size fname))
    (defun std-file-systime (fname / date)
      (if (setq date (vl-file-systime fname))
        (std-datlst->cdate
	  (std-select date
	             '(0 1 3 4 5 6 7)))))


  )

  ((std-doslib-p)
    ;;; std-get-drive-list switches dynamically
    (defun std-file-rename (from to)
      (dos_rename from to))
    (defun std-file-copy (from to)
      (dos_copy from to))
    ;; std-file-append missing!
    (defun std-file-delete (fn)
      (dos_delete fn))
    (defun std-%directory-files (dir fn flag)
      (if (= -1 flag)
        (dos_subdir dir)
	(dos_dir (std-make-pathname (list dir fn)))))
    (defun std-file-size (fname)
      (cdr (assoc fname (dos_filesize fname))))
    (defun std-truename (fname)
      (dos_fullpath fname))
    (defun std-file-attrib (fname)
      (cdr (assoc fname (dos_attrib fname))))
    (defun std-mkdir (fname)
      (dos_mkdir fname))
    (defun std-rmdir (fname)
      (dos_rmdir fname))
    (defun std-current-drive ()
      (dos_drive))
    (defun std-current-path ()
      (dos_pwdir))
    (defun std-file-systime (fname / l d m)
      (if (setq l (dos_file fname))
	(progn
	  (setq d (third l) m (fourth l))
	  ;; (fname "35530" "03-28-1997" "09:04:58p" "" "" "" "A")
          (std-datlst->cdate (list
	    (atoi (substr d 7 4))
	    (atoi (substr d 4 2))
	    (atoi (substr d 1 2))
	    (if (= "p" (substr m 9 1))
	      (+ 12 (atoi (substr m 1 2))))
	    (atoi (substr m 4 2))
	    (atoi (substr m 7 2))
	  )))))
  )
  (T 	; acomp or plain AutoLISP: use workarounds instead



    ;; either shell calls or FILEEXT extensions
    ;; for the following funcs:


;;; Only very dirty workarounds so far. FILES is discontinued on R14.
;;; Works only on DOSBASED systems. Mac and Unix use different commands.
;;; even with quoting long fnames don't work with r13
;;; doslib not loaded yet.
(defun STD-FILE-RENAME (from to)
  (if (and (std-probe-file from)(not (std-probe-file to)))
    (progn
      (cond
	((std-sys-dosbased-p)
	   (std-%command-shell-wait
	     (strcat "REN " (std-%stringquote-filename from)
		     " " (std-%stringquote-filename to)) nil))
	((std-sys-unix-p)	; quote fnames from shell expansion
	   (std-%command-shell-wait
	     (strcat "mv '" (std-%stringquote-filename from)
		     "' '"  (std-%stringquote-filename to) "'") nil))
        (T (command "_FILES" 4 from to 0)))
      (if (and (std-probe-file to)(not (std-probe-file from))) T))))

(defun STD-FILE-COPY (from to)
  (std-%shell-file-copy from to))

;;; note reverse order
(defun STD-FILE-APPEND (dest source)
  (if (and (std-probe-file dest)(std-probe-file source))
    (progn
      (cond
	((std-sys-dosbased-p)
	   (std-%command-shell-wait
	     (strcat "COPY " (std-%stringquote-filename dest)
			  "+" (std-%stringquote-filename source)
			  " " (std-%stringquote-filename dest)) nil))
	((std-sys-unix-p)	; quote fnames from shell expansion
	   (std-%command-shell-wait
	     (strcat "cat '" (std-%stringquote-filename dest)
			  "' '"
			  (std-%stringquote-filename source)
			  "' > '"
			  (std-%stringquote-filename dest) "'")
	     nil))
	;; AppleMac
        (T (std-error
	     (strcat "STD-FILE-APPEND "
		     (std-msg "not defined for this system"))))
      )
      (if (std-probe-file dest) T))))

(if (not STD-FILE-DELETE)
;;; Problem on Win: shell window is slow
(defun STD-FILE-DELETE (fname)
  (if (std-probe-file fname)
    (progn
      (cond
	((std-sys-dosbased-p)
	  (std-%command-shell-wait
	    (strcat "DEL " (std-%stringquote-filename fname))
	    nil))
	((std-sys-unix-p)	; quote fnames from shell expansion
	  (std-%command-shell-wait
	    (strcat "rm '" (std-%stringquote-filename fname) "'")
	    nil))
        (T (command "_FILES" 3 fname "" 0))
      )
      (if (std-probe-file fname) nil T)))))

(defun STD-MKDIR (fname)
  (if (not (std-probe-file fname))
    (progn
      (cond
	((std-sys-dosbased-p)
          (std-%command-shell-wait
            (strcat "MKDIR \"" (std-str-1 (std-fpslash fname)) "\"")
	    nil))
	((std-sys-unix-p)	; quote fnames from shell expansion
          (std-%command-shell-wait
	    (strcat "mkdir '" (std-str-1 (std-fpslash fname)) "'")
	    nil))
	;; AppleMac
        (T (std-error
	     (strcat "STD-MKDIR "
		     (std-msg "not defined for this system"))))
      )
      (if (std-probe-file fname) T))))

(defun STD-RMDIR (fname)
  (if (std-file-directory-p fname)
    (progn
      (cond
	((std-sys-dosbased-p)
          (std-%command-shell-wait
	    (strcat "RMDIR \"" (std-str-1 (std-fpslash fname)) "\"")
	    nil))
	((std-sys-unix-p)	; quote fnames from shell expansion
          (std-%command-shell-wait
           (strcat "rmdir '" (std-str-1 (std-fpslash fname)) "'")
	   nil))
	;; AppleMac
        (T (std-error
	     (strcat "STD-RMDIR "
		     (std-msg "not defined for this system"))))
      )
      (if (std-file-directory-p fname) nil T))))

(defun STD-FILE-SIZE (fname)
  (std-warn (strcat "STD-FILE-SIZE "
		    (std-msg "not yet implemented for plain ALISP")))
  0
)

;;; => flag
;;;   1: read-only
;;;   2: hidden
;;;   4: system
;;;   8: archive
;;;2048: compressed
(defun STD-FILE-ATTRIB (fname)
  (std-warn (strcat "STD-FILE-ATTRIB "
		    (std-msg "not yet implemented for plain ALISP")))
  0
)

;;; ADSrx version v0.4019 is fixed and matches this behaviour.
;;; Flag:
;;;  -1 - directories only
;;;   0 - files and directories (default)
;;;   1 - files only
;;; Improved for case 1, but it still is a hack.
;;; TODO: check RBarlow's reported bug [12/14/99]
(defun std-%directory-files (dir fn flag / cmd temp lst
			     		   *SLASH-CHAR* n opts)
  (if (not fn) (setq fn "*"))
  (cond ((std-sys-dosbased-p)
	   (setq *SLASH-CHAR* "\\")
	   (setq cmd
	     ;; we should check which dos version supports these flags
	     (cond ((= -1 flag) "DIR /B/A:D")
		   ((= 1 flag) "DIR /B/A:-D")
	           (T "DIR /B"))
		 opts ""))
	((std-sys-unix-p)
	   ;;Note that under unix the returned filenames include the
	   ;; path prefix! "test" => ("test/file1" "test/file2" ...)
	   ;; Under DOS or WIN32 not
	   (setq *SLASH-CHAR* "/")
           (cond
	     ((= -1 flag) (setq cmd "find" opts "-maxdepth 1 -type d"))
	     ((= 1 flag)  (setq cmd "find" opts "-maxdepth 1 -type f"))
	     (T (setq cmd "ls" opts ""))))
	(T (std-error
	     (strcat "(STD-%DIRECTORY-FILES dir fn flag) "
		     (std-msg "unsupported operating system")))))
  (setq temp (std-filename-mktemp "dir.lst"))
  (if (= -1 flag)
    (std-%command-shell-wait
      (strcat cmd " " (std-%stringquote-filename dir) opts)
      temp)
    (std-%command-shell-wait
      (strcat cmd " "
	      (std-%stringquote-filename
		(std-make-pathname (list dir fn))) opts)
      temp))
)




   ;; Load FILEEXT: probably from a FILEEXT.LSP (not yet provided)
   ;; file extensions searched by std-load-external (lsp, arx, ...)
   ;; not yet!
;;;    (or (std-require-version "FILEEXT" 0.1)
;;;        (std-warn "Module FILEEXT not found!"))
  )
)


;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign  *STDFILE-SYMBOLS*))
(setq *STDFILE-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDFILE") ; first provide it. But before you may
				; call it, be sure to have all supporting
				; functions.

(std-%simple-require "STDSTR")   ; various
(std-%simple-require "STDLISP")  ; various
;(std-%simple-require "STDERROR")        ; std-file-delete: std-var-push

;;; deferred to STDINIT2 after fix-module-path to be able to find the arx
;;; later we will need a strategy for R15 to go without it.
;;;(or (std-%require-external-ads "FILEEXT")
;;;    (and (not (std-ver-r2000-p))
;;;         (std-%accept-fas2-extensions-p)
;;;         (std-%load-fileext-fas)))


;;; STDINIT2 is no seperate module, just an extension for STDFILE.
;;; don't load it again, because it has top-level calls
(if (not *STD:%PROJECT*)
  (if (not (std-functionp std-%fix-module-path))
    (std-%simple-load "STDINIT2"))
)

; 'STDFILE
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDINIT2.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:32:31 rurban>
;;; Copyright (c) 1998,99,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Load required external libraries and do some toplevel calls
;;;
;;; This is no module. It is loaded by STDFILE or by the project.
;;; It is called after having all required functions
;;; to safely load the required external modules (FILEEXT) and
;;; define workarounds if not found.
;;; Here toplevel calls (evaluated at load-time) are allowed.
;;; With projects call (STDLIB-PROJECT-INIT) to do this.

;;; STATUS:
;;; This is dynamical code so there may be different errors if loaded as
;;; one project (stdall.lsp, stdlib.fas, stdlib.bi4) or by std-require.
;;; Tested with R13/R14 plain, with ViLL 2,3 and Vlisp and Vill 2
;;; standalone. Not yet with earlier versions.
;;; And it is very likely subject to change.
;;;
;;; $Log: STDINIT2.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; 2000-09-07 16:05:18 rurban
;;;   finds *STDLIB-PATH* via getcfg setting
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; 2000-04-27 15:10:19 rurban
;;;   added *STD:USER-COLOR-NAMES* init
;;; 2000-04-19 10:13:58 rurban
;;;   STDINIT2, fixed init: std-%top-level-init called only once
;;;
;;; ===================================================================73

;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDINIT2-SYMBOLS*
    '(STDLIB-FORCE-DELAYED STDLIB-PROJECT-INIT))))


;;; Check *MODULE-PATH* for correct path-names and fix it.
;;; Set global *STDLIB-PATH* to avoid problems with adding paths later.
;;; Requires STDLIB.LSP to be present in the correct path even if it's
;;; not loaded! If you want to aviod to carry a dummy STDLIB.LSP with,
;;; you, set *STDLIB-PATH* in your initialisation.
;;; Called in STDINIT2
;;; For using the stdlib inside other projects set STD:%PROJECT to T,
;;; then all STD-REQUIRE calls are delayed and *MODULE-PATH* and
;;; *STDLIB-PATH* are not checked.
;;;
;;; Stress this for tests:
;;; (setq *MODULE-PATH* '(1 2 3 "l:/src/stdlib" "l:/src" "l:/src/"))
(defun std-%fix-module-path (/ s)
  (if (and (not *MODULE-PATH*)
	   (std-ver-r13-p)	; at least r13
           (setq s (getcfg "AppData/STDLIB/MODULE-PATH")))
    (setq *MODULE-PATH* (read s)))
  (cond
    ((consp *MODULE-PATH*))
    ;; on a seperate project the paths must be provided seperately,
    ;; e.g. after having fully initialized the project
    (*STD:%PROJECT* nil)
    ((setq s
	    (cond ((findfile "STDLIB.VLX"))
	      	  ((findfile "STDLIB.FAS"))
		  ((findfile "STDLIB.BI4"))
		  ((findfile "STDLIB.LSP"))))
     (setq *MODULE-PATH* (list (std-filename-directory s))))
    (T
      (princ (strcat (std-msg "* Warning: *MODULE-PATH* not set!")
	     (std-msg "\n** STDLIB will not find its modules"))))
  )
  ;; temp fcad fix for std-glob-files
  (if (and (std-ver-fcad-p)(not *STDLIB-PATH*))
    (setq *STDLIB-PATH* (std-fpslash (std-filename-upcase (car *MODULE-PATH*)))))

  (setq *MODULE-PATH* (std-remove nil
    (mapcar (function (lambda (s)
      (if (and (stringp s)
	       (std-file-directory-p s))
        (progn
	  (setq s (std-fpslash (std-filename-upcase s)))
          (if (and (not *STDLIB-PATH*)
	           (std-glob-files (std-force-path s "STDLIB.*")))
	    (setq *STDLIB-PATH* s))
	  s
	))))
       *MODULE-PATH*
      )))
  ;;; on projects this path needs not to be checked
  (if (or *STD:%PROJECT*
	  *STDLIB-PATH*
	  (setq *STDLIB-PATH* (getcfg "AppData/STDLIB/STDLIB-PATH"))) ; 9/7/00
    nil
    (std-cerror "no valid STDLIB path found!"))
   ;; re-order it so that *STDLIB-PATH* is the first, fixed
  (if *STDLIB-PATH*
     (setq *MODULE-PATH* (cons (std-fpslash *STDLIB-PATH*)
			       *MODULE-PATH*)))
   ;; no duplicates for faster loading
   (setq *MODULE-PATH* (std-remove-duplicates *MODULE-PATH*))

  ;; store the cfg for other apps and lazy developers/users
  (if (std-ver-r13-p)	; at least r13
    (progn
      (if (/= (setq s (std-prin1-to-string *MODULE-PATH*))
	      (getcfg "AppData/STDLIB/MODULE-PATH"))
        (setcfg "AppData/STDLIB/MODULE-PATH" s))
      (if (and *STDLIB-PATH*
	       (/= (getcfg "AppData/STDLIB/STDLIB-PATH")
		   *STDLIB-PATH*))
        (setcfg "AppData/STDLIB/STDLIB-PATH"  *STDLIB-PATH*))))
)

;;; ===================================================================73
;;; Top-level stuff

;;; Use lambda to shadow temp. variables. (as let in CL)
;;; Put everything in here that is dynamically needed by
;;; STDINIT, STDSTR, STDLIST, STDLISP and STDFILE
;;; All other modules are allowed to call their own functions, because
;;; after this call everything should be up and running.
;;; (Well, the error handling not)
(defun std-%top-level-1 (/ lang lang-err)
  ;; redefine slash-char
  (if (std-acad-connection-p)
    (setq *SLASH-CHAR*
      (cond ((std-sys-dosbased-p) "\\")
	    ((std-sys-mac-p) ":")
	    (T "/"))	 ; fixed
      ;; FCAD doesn't have this
      *CODEPAGE* (getvar "SYSCODEPAGE")
    )

    ;; No acad: Define some useful defaults, just for testing
    (setq *SYS-MSWIN* 1
	  *SYS-DOSBASED* 1
	  *SYS-DOS* 0
	  *SYS-LANGUAGE* ':GERMAN
	  *SYS-LOCKED* 0
	  *SYS-LONGFNAMES* 1
	  *SYS-MAC* 0
	  *SYS-OS2* 0
	  *SYS-R12* 0
	  *SYS-R13* 0
	  *SYS-R14* 1
	  *SYS-FCAD* 0
	  *SYS-ICAD* 0
	  *SYS-TCAD* 0
	  *ACADVER* "14.0"
	  *ACADVER-NUM* 14.0
	  *ACADPLATFORM* "Microsoft Windows"
	  ;strcase std-strcase
          *CODEPAGE* "iso8859-1"
	  *STD:ACAD-LIBRARY-PATHS* *MODULE-PATH*
	  *LOCAL-LANGUAGE* ':GERMAN
    )
  )
  ;;; provide some expert knowledge about the maximal recursion stack
  ;;; Missing:
  ;;;   intellicad, r12/win (240 assumed),
  ;;;   <10.10 (138 assumed, but special LISPSTACK env variable)
  ;;;   acomp r10 (1240 assumed, but special compstack env variable)
  ;;; => error: stack-overflow (simulated in VLIDE), but *error* is
  ;;;    NOT called in VL!
  (std-defconstant '*MAX-RECURSION-STACK*
    (cond
      ((and (std-vl-p) (std-vlide-p)) 984); VL IDE: simulated,977?
      ((std-vl-p) *MAX-SHORTINT*)	; VL RTS: unlimited
      ((std-acomp-p) 1240)		; acomp (see above)
      ((std-ver-fcad-p) 1984) 		; then FCAD.EXE dies!
      ((= (_ver) "i7.00r") 251)		; r13
      ((= (_ver) "i8.00r") 239)		; r14
      ((and (std-ver-win-p)
  	  (< (std-ver-num) 15)) 240) 	; r12/13/14 win
      ((< (std-ver-num) 13) 138)	; r12/dos
      (T 200)				; ?? (unreachable code)
      ))

  ;;; Maximal number of arguments to functions like AND
  ;;; AutoLISP: => (*error* "too many arguments")
  ;;; VL: no *error* is called, hard error
  ;;; With FCAD the whole app dies.
  (std-defconstant '*MAX-ARGS-LIMIT*
    (cond
      ((std-ver-r2000-p) 1623)		; have to recheck this in FCS
      ((std-vl-p) 32767)
      ((std-acomp-p) 32)		; acomp
      ((std-ver-fcad-p) 2000) 		; even more, but with 3000 it needed
					; a few minutes and 100 MB Heap.
      ((std-ver-win-p) 1000)		; r12/13/14 win
      (T 65534)
      ))

  ;; protect a limited number of not-prefixed functions (VL only)
  ;; don't add anything here!

  (std-%protect-assign
   '(first second third fourth fifth sixth seventh eighth
     ninth tenth rest
     consp stringp x-of y-of z-of xy-of))


  ;; The VL-RTS should be loaded by the developer, not by the library.
  ;; So I removed the call.
  ;(if (and (not (std-vl-p)) (std-acad-connection-p))
  ; (std-%load-vl-rts)
  ;)

  (if (= (atoi (std-acadver)) 14)
    (std-defconstant '*R14-LWPOLY-BUG* T))

  ;; Localization stuff
  (std-defkeyword ':ENGLISH)

  (if (not (std-sys-mac-p))
    (setq *STD:SLASH-CHARS* (std-remove ":" *STD:SLASH-CHARS*)))
  ;; Fix the paths supplied so far.
  (std-%fix-module-path)
  ;; supply a special var for the stdlib path

  ;; Load an external supporting app if it can be found
  ;; This migth cause problems on R15 when trying to
  ;; (load "STDLIB15") recursively
  (or (std-%require-external-ads "FILEEXT")
      (and (not (std-ver-r2000-p))
         (std-%accept-fas2-extensions-p)
         (std-%load-fileext-fas)))


  (if (eq ':ENGLISH (std-ver-language))
    (progn
      (std-%simple-require "STDMISC")
      ;; With significant upcased chars.
      ;; This might get redefined in STDLOCAL,
      ;; used in color prompts and keywords.
      (std-defconstant '*STD:USER-COLOR-NAMES*
        (mapcar (function (lambda (s) (std-%extract-significant-key s nil)))
	        (mapcar (function car) *STD:COLOR-TABLE*)))
    )
    ;; loaded as single module, so we require the rest
    (progn
      (std-%simple-require "STDLOCAL")
      (std-%simple-require "STDERROR")
      ; (std-%set-language-error)
    )
  )

  (if (and (std-module-defined-p "FILEEXT")
           (<= 0.51 *FILEEXT-VERSION* 0.74))
    (progn
      (std-%unprotect-assign  '(std-randomize))	; fixed by RG
      ;; std-%randomize takes an optional random-state argument
      (setq std-randomize std-%sys-randomize)
      (std-%protect-assign '(std-randomize))	; fixed by RG
    ))

)

(defun STDLIB-FORCE-DELAYED (/ *STD:%PROJECT*)
  (foreach x *STD:MODULES-DELAYED*
    (std-require x)))

(defun STDLIB-PROJECT-INIT ()
  (if *STD:%PROJECT* ; otherwise it was already initialized below
    (std-%top-level-1))
  (if (std-functionp std-%locale-init)
    (std-%locale-init))
  (stdlib-force-delayed)
  
  ;; wait until stdlocal is loaded and after all delayed modules
  (std-%load-verbose-print
    (if std-cdate->date-string
      (list "STDLIB v" (stdlib-version)
	    (if (stdlib-beta-p) "beta" "")
	    ", " (std-cdate->date-string
		   (std-datlst->cdate *STDLIB-DATE*)) "\n")
      (list "STDLIB v" (stdlib-version) "\n")))
  
  (setq *STD:%PROJECT* nil)
  ;; (setq *STD:MODULES-DELAYED* nil)
  (setq *VERBOSE* 	*std:%old-verbose* 
        *LOAD-VERBOSE*  *std:%old-load-verbose*
	*std:%old-load-verbose* nil
	*std:%old-verbose*      nil
	)
)

(if (not *STD:%PROJECT*) (std-%top-level-1))

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDINIT2-SYMBOLS*))
(setq *STDINIT2-SYMBOLS* nil)


(std-%simple-provide "STDINIT2")
;;; Modulenames of further external extensions:
;;; (not required to continue)
;;; BINIO       : (std-%simple-require "BINIO")
;;; REGISTRY    : (std-%simple-require "REGISTRY")
;;; INIFILE     : (std-%simple-require "INIFILE")

;'STDINIT2  	; this fails in FCAD
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDMATH.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:30:17 rurban>
;;; Copyright (c) 1998,99 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Mathematical functions for the STDLIB
;;; - Logic
;;; - Base Conversion
;;; - Math (Type preserving arithmetic, random, statistics)
;;; - Trigonometry
;;;
;;; Note:
;;;   There is no linear algebra code included yet! On purpose.
;;;   Matrix code should be avoided at all to minimize problems with
;;;   numeric overflow on repeated multiplications and even worse
;;;   floating underflow on add operations.
;;;   Instead native AutoCAD methods like (trans) with command UCS
;;;   should be used.



;;; STATUS: Stable
;;; Renamed STD-FACULTY to STD-FACTORIAL in v0.4002.
;;; Most if it is tested.
;;; Most arithmetic and trigonometric funcs are excluded from SMALL.
;;; Not tested yet: hyperbolic and area functions.
;;; This module previously had a lot of implementation changes,
;;;   (type preservation, integer overflows, performance) but should
;;; be fairly stable now.
;;; No overflow checks are yet implemented for STD-LCM.
;;; Prime functions are now in PRIMES.LSP
;;; Dynamic random functions are now in RANDOM.LSP
;;; Combinatorial functions are in COMBINATIONS.LSP
;;; Missing:
;;;   Optimized algorithms for STD-ISQRT
;;;
;;; $Log: STDMATH.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:50:35  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-11 12:47:18 rurban
;;;   added std-%(un)protect-assign wrapper for dynamic random funcs
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; 2000-04-14 13:48:00 rurban
;;;   STD-TAN improved accuracy, correct return of *INFINITY* and 0.0
;;;
;;; Revision 0.4009  2000/04/07 17:29:20  rurban
;;; v0.4009, see Changes
;;;
;;; 2000-03-21 18:20:40 rurban
;;;   fixed STD-LOGCOUNT for A2000
;;; 2000-04-07 16:27:03 rurban
;;;   fixed STD-LOGCOUNT for A2000 without ActiveX RTS
;;;

;;; internal functions start with std-%
;;; ===================================================================73

(std-%simple-require "STDLISP")   ; for std-error, std-defconstant

;;; We either define std-random as not-linked, which is slower.
;;; But better we define it statically, ignore std-make-random-state
;;; and provide a dynamic version with the RANDOM module.

(pragma '((not-link std-random))); redefined dynamically?

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDMATH-SYMBOLS*
    '(*PI2*	     *PI/2*	    *PI/4*	   std-dtr
      std-rtd	     STD-FLOAT-PRECISION
      STD-LOGXOR     STD-LOGNOT	    STD-SETBIT	   STD-BITDEL
      STD-BITLIST    STD-LOGCOUNT   STD-BITTOGGLE  STD-NUM->HEX
      STD-HEX->NUM   STD-NUM->OCT   STD-OCT->NUM   STD-NUM->BIN
      STD-BIN->NUM   STD-NUM-SCALE  STD-EVENP	   STD-ROUND
      STD-FRACTION   STD-FLOOR	    STD-CEILING	   STD-SIGNUM
      STD-SIGN	     STD-SQR	    STD-SQRT	   STD-EXPT
      STD-LOG2	     STD-LOG10	    STD-ISQRT	   STD-GCD
      STD-LCM	     STD-RANDOM	    STD-MEAN	   STD-MEDIAN
      STD-STANDARD-DEVIATION	    STD-TAN	   STD-SEC
      STD-CSC	     STD-ASIN	    STD-ACOS	   STD-ASEC
      STD-ACSC	     STD-ACOT	    STD-SINH	   STD-COSH
      STD-TANH	     STD-COTH	    STD-SECH	   STD-CSCH
      STD-ARSINH     STD-ARCOSH	    STD-ARTANH	   STD-ARCOTH
      ))))

;;; avoid reload warnings in VL
;;;(eval (list 'pragma
;;;  (list 'quote
;;;    (list (cons 'unprotect-assign
;;;      '(*PI2* *PI/2* *PI/4*
;;;	std-dtr std-rtd
;;;	*NUM-TOL*
;;;	)
;;;      )))))


;;; Globals:

;;; Numeric tolerance, used in comparisons with std-equalp.
;;; Acceptable rounding errors
(setq *NUM-TOL* 1e-6)

;;; Constants:
(std-defconstant '*MAX-LONGINT*  2147483647)    ; signed longint, 32-bit
(std-defconstant '*MAX-SHORTINT* 32767)         ; signed shortint,16-bit
;;; max double, 64-bit, ~15 precise digits
;;; something like 1.7976931348623157e+308L normally
(std-defconstant '*MAX-REAL*     1.7e308)

;;; the minimum double is something like 2.2250738585072014e-308
(std-defconstant '*MIN-REAL*  2.2250738585072014e-308); min double



;;; Some numerical constants for CL compatibility only.
;;; Those floats are always normalized. We don't wanna wake the demon.
(std-defconstant 'MOST-POSITIVE-FIXNUM  2147483647)
(std-defconstant 'MOST-NEGATIVE-FIXNUM -2147483648)
;;; another AutoLISP - VL incompatibility:
;;; VL accepts 1.797693134862316e+308 and -1.797693134862315e+308 as
;;; largest numbers.
(std-defconstant 'MOST-POSITIVE-DOUBLE-FLOAT  1.797693134862315e+308)
(std-defconstant 'MOST-NEGATIVE-DOUBLE-FLOAT -1.797693134862315e+308)
;;; lowest precision, kind of zero
(std-defconstant 'LEAST-POSITIVE-DOUBLE-FLOAT  2.2250738585072014e-308)
(std-defconstant 'LEAST-NEGATIVE-DOUBLE-FLOAT -2.2250738585072014e-308)

;;; Common Lisp FLOAT-PRECISION function.
;;; The value returned here is IMPLEMENTATION DEPENDENT.
;;; Since only 64-bit, double precision, is supported
;;; we return 53 here, unless the number is 0.0, then 0 is returned.
;;; radix 2, 10^15 < 2^53 < 10^16 (15 decimal digits exactness)
(defun STD-FLOAT-PRECISION (r) (if (zerop r) 0 53))


;;; Inifinity is not ISO conformant, not NaN for simplicity.
(std-defconstant '*INFINITY* *MAX-REAL*)	; positive infinity

;;; Epsilon, lowest precision with doubles. Usually between
;;; 1e-15 and 1e-16. We use 1e-12 for more numerical stability.
;;; see *MACHINE-EPSILON*
(std-defconstant '*EPSILON* 1e-12)

;;; "machine dependent epsilon", lowest possible number
;;; something like 2.2204460492503131e-16 with long doubles (64-bit IEEE)
;;; but calculated dynamically, by Serge Pashkov, see *EPSILON*
(std-defconstant '*MACHINE-EPSILON*
  (apply (function (lambda (/ eps)
    (setq eps 1.0)
    (while (/= (+ 1.0 eps) 1.0)
      (setq eps (/ eps 2.0)))
    (* 2.0 eps)
   ))
  nil
  )
)

;;; "machine dependent maximal integer"
;;; panic mode, I know no autolisp version using only 16-bit integers
;;; but i'm not absolutely sure.
;;; so far only a simple version: detects only 16-bit or 32-bit
;;; signed integers. we'll need this for detecting integer overflows
(std-defconstant '*MAX-INT*
  (if (minusp (1+ *MAX-SHORTINT*))
    *MAX-SHORTINT*
    *MAX-LONGINT*))

;;; -------------------------------------------------------------------73
;;; Logic

(defun STD-LOGXOR (i j)
  (boole 6 i j))

(defun STD-LOGNOT (i)
  (~ i))

;;; moved to STDINIT
;;;;;; T if all bitvalues in flag are set or nil
;;;(defun STD-BITSETP (val flag)
;;;  (= (logand val flag) val))	; fixed

;;; returns the new flag with the bitvalue val enabled.
(defun STD-SETBIT (val flag)	; (std-setbit 128 1) => 129
  (logior val flag))

;;; returns the new flag with the bitvalue val disabled.
;;; (std-bitdel 64 (std-getflag ele)) ; without 64
(defun STD-BITDEL (val flag)
  (logand flag (std-lognot val)))

;;; returns a list of all set bitvalues in flag.
(defun STD-BITLIST (n / i lst)	; list of bitvalues
  (setq i 1)
  (while (>= n i)
    (if (= i (logand i n)) (setq lst (cons i lst)))
    (setq i (lsh i 1))
  )
  lst
)

;;; Counting bits in 32-bit fixnums:
;;; (an interesting topic)

;;; number of bits==1,
;;; slow log(n) version, there should exist a constant function
;;; 1:1, 2:1, 3:2, 4:1, 5:2, 6:2, 7:3, 8:1, 9:2, 10:2, 11:3
;;; (defun STD-LOGCOUNT (n / i r)
;;;   (setq i 1 r 0)
;;;   (while (>= n i)
;;;     (if (= i (logand i n)) (setq r (1+ r)))
;;;     (setq i (lsh i 1))
;;;   )
;;;   r
;;; )


;;; Counting bits in 32-bit fixnums: from the scheme slib
;;; Serge Pashkov fixed it for negtaive numbers:
;;; (std-logcount -1) => 0
(defun STD-LOGCOUNT (n)
  (cond ((zerop n) 0)
        ((minusp n) (- 32 (std-logcount (~ n)))) ;fixed
         (t (+ (std-logcount (lsh n -4))
            (nth (rem n 16) '(0 1 1 2 1 2 2 3 1 2 2 3 2 3 3 4)
                  )))))




;;; toggles all bits of val in flag
(defun STD-BITTOGGLE (val flag / i new)
  (setq new val)
  (foreach i (std-bitlist flag)
    (if (zerop (logand i val)) ;bit not set
      (setq new (+ new i))     ;add
      (setq new (- new i))     ;remove
    )
  )
  new
)

;;; -------------------------------------------------------------------73
;;; Base Conversion

(defun STD-NUM->HEX (i / s a)
  (setq s "")
  (while (> i 0)
    (setq a (rem i 16)
          i (lsh i -4))
    (setq s (strcat
      (if (< a 10)
        (chr (+ 48 a))			; 48: (ascii "0")
        (chr (+ 55 a))) s))))		; 55: (ascii "A") - 10

;;; "1C" -> 28
;;; strcase needs ADS
(defun STD-HEX->NUM (s / i n a)
  (std-%simple-require "STDSTR")
  (setq i 0 n 0 s (std-strcase s))	;to be ADS independant
  (if (= (substr s 1 2) "0X") (setq s (substr s 3)))
  (while (< i (strlen s))
    (setq a (substr s (setq i (1+ i)) 1))
    (if (not (<= "0" a "F")) (chr a))	; force bad argument type error
    (setq n (+ (lsh n 4)
	       (- (ascii a) (if (<= a "9") 48 55))))))

(defun STD-NUM->OCT (i / s a)
  (setq s "")
  (while (> i 0)
    (setq a (rem i 8)
          i (lsh i -3))
    (setq s (strcat (chr (+ 48 a)) s))))	; 48 = (ascii "0")

(defun STD-OCT->NUM (s / i n a)
  (setq i 0 n 0)
  (while (< i (strlen s))
    (setq a (substr s (setq i (1+ i)) 1))
    (setq n (+ (lsh n 3)
	       (- (ascii a) 48)))))

(defun STD-NUM->BIN (i / lst)
  (while (> i 0)
    (setq lst (cons (rem i 2) lst)
	  i   (lsh i -1)))
  lst
)

(defun STD-BIN->NUM (numlst / i n)
  (setq n 0)
  (foreach i numlst
    (setq n
      (cond
        ((= i 0) (lsh n 1))
        ((= i 1) (1+ (lsh n 1)))
        (T (chr (chr (+ 48 i))))))))    ;throw an meaningful
                                        ; bad argument type error

;;; scale the number from the source interval to the target interval
;;; to fit in the range to
;;; interval: nil:  (0 1.0) or
;;;	      num:  (0 num) or
;;;	      list: (start end)
;;;   (STD-NUM-SCALE 1.0 1.0 32767) => 32767
(defun STD-NUM-SCALE (num from to / from1 from2 to1 to2)
  (setq from (std-%force-interval from)
	to   (std-%force-interval to))
  (+ (car to) (* num (/ (- (cadr to) (car to))
			(- (cadr from) (car from))))))

(defun std-%force-interval (i)
  (cond
    ((numberp i) (list 0.0 i))
    ((null i)    (list 0.0 1.0))
    ((and (listp i)
          (= 2 (length i))
       (apply (function and) (mapcar (function numberp) i)))
      i
    )
    (T (std-error (list (std-msg "bad argument type")
				 " - " i)))))


;;; -------------------------------------------------------------------73
;;; Math

(defun STD-EVENP (i)
  (zerop (rem i 2)))

;;; round to the nearest integer: 0.5=>1, 0.49=>0
(defun STD-ROUND (num)
  (if (>= (abs (std-fraction num)) 0.5)
    (if (minusp num) (1- (fix num)) (1+ (fix num)))
    (fix num)))

;;; return the fractional part of a real only
;;; (std-fraction  1.1) => 0.1
;;; (std-fraction -1.1) => -0.1
(defun STD-FRACTION (num)
  (- num (fix num)))

;;; return the lower integer
;;; (std-floor  1.1)  =>  1
;;; (std-floor -1.1)  => -2
(defun STD-FLOOR (num)
  (if (minusp num)
    (1- (fix num))
    (fix num)))

;;; return the upper integer
;;; (std-ceiling  1.1)  =>  2
;;; (std-ceiling -1.1)  => -1
(defun STD-CEILING (num)
  (if (minusp num)
    (fix num)
    (1+ (fix num))))

;;; returns -1, 0 or 1 if the argument is negative zero or positive
(defun STD-SIGNUM (num)
  (cond
    ((minusp num) -1)
    ((zerop num) 0)
    (T 1)
    ))




;;; FORTRAN-like sign: "Assign sign of num2 to num1"
;;; Returns the absolute value of num1 with sign of num2.
;;; Type of result coincides with type of num1.
;;;   (sign 1 -2) => -1,   (sign 1 2) => 1,
;;;   (sign 1.0 0) => 0.0, (sign 3 -2.5) => -3
(defun STD-SIGN (num1 num2)
  (cond
    ((minusp num2) (- (abs num1)))
    ((zerop num2) (if (= (type num1) 'INT) 0 0.0))
    (T (abs num1))
    ))

;;; Square of number, n*n, n^2
;;; Obeye integer overflows.
(defun STD-SQR (n)
  (if (> n 46340)	; = (sqrt *MAX-LONGINT*)
    (* (float n) n)
    (* n n)))

;;; Type preserving square root
(defun STD-SQRT (n / r i)
  (if (and (std-integerp n)
	   (= (std-sqr (setq i (fix (setq r (sqrt n))))) n))
    i				; int (or real if too large)
    (cond (r) ((sqrt n))) 	; always real, avoid double calculation
    ))

;;; b^e, b raised by e, AutoLISP EXPT bugfix, (also often named POWER)
;;; We preserve types: (std-expt 2 4) => 16, not 16.0
;;; and fix integer overflow problems.
;;; REAL is only returned when x=REAL or n=REAL, n<0, x<0
;;; or the integer is too large. by Serge Pashkov and Reini Urban
(defun STD-EXPT (b e / r)
  (cond
    ((minusp e) (/ 1.0 (std-expt b (- e))))
    ((= e 1) b)
    ((std-realp e) (expt b e))
    ((std-integerp b)
       (setq r (expt (float b) e))
       (if (= r (fix r))
         (fix r) r))
    (T (expt b e))
))




;;; log base 2
;;; lg x = ln x/ln 2
(defun STD-LOG2 (i / x ln2)
  (setq ln2 1.4426950408889634)
  (if (and (std-integerp i)
           (= i (lsh 1 (setq x (std-round (* (log i) ln2))))))
    x
    ;(/ (log i) (log 2)) = (* (log i) (/ 1.0 (log 2)))
    (* (log i) ln2)))

;;; log base 10
;;; lg x = ln x/ln 10
(defun STD-LOG10 (i / x ln10)
  (setq ln10 0.4342944819032518)
  (if (and (std-integerp i)
	   (= i (std-expt 10 	; fixed
                  (setq x (std-round (* (log i) ln10))))))
    x
    ;(/ (log i) (log 10))
    (* (log i) ln10)))

;;; "integer square root"
;;; To-be-improved implementation, if Newton-Raphson is faster
(defun STD-ISQRT (n)
  (fix (sqrt n)))


;;; "Greatest common divisor", already defined in AutoLISP for two args
;;; using Euklid's algorithm, the most popular of all numeric algorithms
;;;  (std-gcd '(10 20 30)) => 10
;;; Not really needed because AutoLISP has a native (gcd num1 num2)
;;; To-be-improved implementation.
(defun STD-GCD (lst)
  (setq lst (std-fast-sort lst '<))   ;ascending integer list
  (while (cadr lst)
    (setq lst (cons (gcd (car lst) (cadr lst)) (cddr lst))))
  (car lst)
)

;;; recursive gcd: i1 < i2 !!
;;; same as the native gcd



;;; least common multiple
;;; no checks for overflows yet!
;;;   (2 4) => 4
(defun STD-LCM (lst)
  (if (> (length lst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (/ (abs (apply (function *) lst)) (std-gcd lst))))

;;; -------------------------------------------------------------------73
;;; STD-FACULTY eh STD-FACTORIAL
;;;   Moved to module COMBINATIONS
;;;   and renamed to FACTORIAL

;;; -------------------------------------------------------------------73
;;; Primes
;;;  Moved to module PRIMES

;;; -------------------------------------------------------------------73
;;; Random numbers

;;; Status:
;;; Internal system provided std-%sys-random funcs defined in
;;;   FILEEXT.FAS
;;; Here is only defined the basic STD-RANDOM, the other functions with
;;; dynamic redefinition of random methods, and support for states is in
;;; the seperate module RANDOM.

;;; (std-random) returns a pseudo random number between 0 (inclusive)
;;; and num (exclusive). E.g. (std-random 1) always returns 0, never 1.
;;; default arg: nil = 1.0
;;; Type preserving. Return types are dependent of the argument type:
;;; NIL:  0.0 <= (std-random nil) < 1.0
;;; REAL: 0.0 <= (std-random num) < num
;;; INT:  0   <= (std-random num) < num

;;; If we want a fast linked call at startup we should
;;; do something different...
(if (std-functionp 'std-%sys-rand)
  (progn

    (if std-%unprotect-assign (std-%unprotect-assign '(STD-RANDOM)))

    (defun STD-RANDOM (num)
      (std-%sys-rand (if (not num) 1.0 num)))

    (if std-%protect-assign (std-%protect-assign '(STD-RANDOM)))

  )  
  (progn


    (if std-%unprotect-assign
      (std-%unprotect-assign
	'(std-%mult-rand-1 std-%mult-rand std-random)))

    ;;; by Serge Pashkov:
    ;;; This is a multiplicative generator named
    ;;; URAND (Universial RANDom generator) with period length 2^31 and
    ;;; small value of the explicit correlations (though every
    ;;; multiplicative generator suffers from them in more and less
    ;;; extent). max seed: *max-int*, These numbers are fine for 32-bit
    ;;; ints.
    (defun std-%mult-rand-1 (/ seed)
      (setq seed *RANDOM-SEED*)
      (if (minusp (setq seed (+ 453816693 (* seed 843314861))))
	;; hint: divide 2^31 as 2^30+2^30
	(setq seed (+ seed 1073741824 1073741824)))
      (* 4.656612873077392E-10 (setq *RANDOM-SEED* seed))
      )

    ;;; multiplicative rand
    (defun std-%mult-rand (n)
      (cond
	((std-integerp n) (abs (fix (* n (std-%mult-rand-1)))))
	((std-realp n)    (* n (std-%mult-rand-1)))
	((null n)         (std-%mult-rand-1))
	(T (std-error (std-msg "bad argument type")))))

    ;;; Some generators require a special seed range.
    ;;; std-%get-internal-run-time will return a number between 0 and
    ;;; something like 86400000. (* 24 60 60 1000)
    ;;; But we avoid integer overflows before calculating random,
    ;;; because we have no exceptions before R15.
    (setq *RANDOM-SEED*
      (fix (rem
        (if (std-acad-connection-p)
	  ;; Truncate the days and use only the fractional
          ;; part of the seconds since today 0:00,
          ;; Convert it to milliseconds and integer.
	  ;; also avoid STDTIME requirement, so it's inlined.
	  (fix (* 86400000 (- (getvar "DATE") (fix (getvar "DATE")))))
          (if (std-require-version "FILEEXT" 0.3)
	    (std-%get-internal-run-time)	; FILEEXT
	    0
          )
	) *MAX-INT*)))

    (defun STD-RANDOM (num)
      (std-%mult-rand (if (not num) 1.0 num)))

    (if std-%protect-assign
      (std-%protect-assign
	'(std-%mult-rand-1 std-%mult-rand std-random)))

  )
)

;;; -------------------------------------------------------------------73
;;; Statistics

;;; Average of a list of numbers
(defun STD-MEAN (numlst)
  (if (> (length numlst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (/ (apply (function +) numlst)
       (float (length numlst)))))


;;; Median of the sorted list of numbers. 50% is above and 50% below
;;; "center of a distribution"
;;; Ex: (std-median (std-make-list 100 std-%random)) => 0.5 +- epsilon
;;;     (std-median (std-make-list 100 (lambda () (std-random 10))))
;;;       => 4.0-5.0 [0..9]
;;;     (std-median (std-make-list 99  (lambda () (std-random 10))))
;;;       => 4-5
;;;     (std-median '(0 0 2 4 12))	=> 2
;;;     (std-median '(0 0 4 12))	=> 2.0
(defun STD-MEDIAN (numlst / l)
  (setq numlst (std-sort numlst '<))            ; don't remove duplicates
  (if (= 0 (rem (setq l (length numlst)) 2))	; if even length
    (* 0.5 (+ (nth (/ l 2) numlst)              ; force float!
              (nth (1- (/ l 2)) numlst)))       ; fixed by Serge Pashkov
    (nth (/ l 2) numlst)))

(defun STD-STANDARD-DEVIATION (numlst / n _dev_m r)
  (setq n (length numlst)
	_dev_m (std-mean numlst)
	r (mapcar (function (lambda (x) (std-sqr (- x _dev_m)))) numlst))
  (sqrt (* (std-mean r) (/ n (float (- n 1))))))


;;; -------------------------------------------------------------------73
;;; Trigonometry

(std-defconstant '*PI2*  (* 2.0 PI))
(std-defconstant '*PI/2* (/ PI 2.0))
(std-defconstant '*PI/4* (/ PI 4.0))

;;; Degree to radian
(defun STD-DTR (deg)
  ;;(* PI (/ deg 180.0))
  (* deg 0.0174532925199433))

;;; Radian to degrees
(defun STD-RTD (rad)
  ;;(/ (* rad 180.0) PI)
  (* rad 57.29577951308233))

;;; Note that none of these functions check for valid
;;; arguments. Passing valid arguments is the responsibility
;;; of the calling program.

;;; Tangent accepts any angle in radians, and returns the
;;; tangent in the range -9.7E307+epsilon to 9.7E307 inclusive
;;; Fixed v0.5 to improve numeric accuracy
(defun STD-TAN (z / cosz)
  (cond ((zerop (rem z PI)) 0.0)	; added: 0, 180, 360, ...
	((zerop (rem z *PI/2*)) *INFINITY*) ; added: 90, 270, ...
	((zerop (setq cosz (cos z)))    ; cos *PI/2* is inaccurate
    	 *INFINITY*)
        (T (/ (sin z) cosz))))

;;; Some more trigonometric functions by Jon Fleming, May 20 1997.


;;; secant accepts any angle in radians, and returns the
;;; secant in the ranges -9.7E307+epsilon to -1.0 inclusive
;;; and 1.0 to 9.7E307 inclusive
(defun STD-SEC (z / cosz)
  (if (zerop (setq cosz (cos z)))
    *INFINITY*
    (/ 1.0 cosz)))

;;; cosecant accepts any angle in radians, and returns the
;;; cosecant in the ranges -9.7E307+epsilon to -1.0 inclusive
;;; and 1.0 to 9.7E307 inclusive
(defun STD-CSC (z / sinz)
  (if (zerop (setq sinz (sin z)))
    *INFINITY*
    (/ 1.0 sinz)))

;;; arcsine (inverse sine) accepts an argument in the range
;;; -1.0 to 1.0 inclusive, and returns an angle in radians in
;;; the range -pi/2 to pi/2 inclusive.
(defun STD-ASIN (z)
  (cond ((zerop z) 0.0)
        ((= z 1.0)  *PI/2*)
	((= z -1.0) (- *PI/2*))
        (T (atan z (sqrt (- 1.0 (* z z)))))))

;;; arccosine (inverse cosine) accepts an argument in the
;;; range -1.0 to 1.0 inclusive, and returns an angle in
;;; radians in the range pi to 0 inclusive
(defun STD-ACOS (z)
  (atan (sqrt (- 1.0 (* z z))) z))

;;; arcsecant (inverse secant) accepts an argument in
;;; one of two ranges: minus infinity to -1 inclusive or
;;; 1 to infinity inclusive, and returns an angle in
;;; radians in the range pi to 0 inclusive (except
;;; EXACTLY pi/2 will never be returned on a computer
;;; with finite numerical precision)
(defun STD-ASEC (z)
  (std-acos (/ 1.0 z)))

;;; arccosecant (inverse cosecant) accepts an argument
;;; in one of two ranges: minus infinity to -1 inclusive or
;;; 1 to infinity inclusive, and returns an angle in
;;; radians in the range -pi/2 to pi/2 inclusive (except
;;; EXACTLY 0.0 will never be returned on a computer
;;; with finite numerical precision)
(defun STD-ACSC (z)
  (std-asin (/ 1.0 z)))

;;; arccotangent (inverse cotangent) accepts an argument
;;; in the range minus infinity to plus infinity
;;; inclusive and returns an angle in radians in the
;;; range pi to 0 inclusive.
(defun STD-ACOT (z)
  (- *PI/2* (atan z)))

;;; -------------------------------------------------------------------73
;;; Hyperbolic and Area functions

;;; sinus hyperbolicus
(defun STD-SINH (z)
  (/ (- (exp z) (exp (- z))) 2))

;;; cosinus hyperbolicus
(defun STD-COSH (z)
  (/ (+ (exp z) (exp (- z))) 2))

;;; tangens hyperbolicus
(defun STD-TANH (z / x)
  (/ (- (setq x (exp (* 2.0 z))) 1)
     (+ x 1)))

;;; cotangens hyperbolicus
(defun STD-COTH (z / x)
  (/ (+ (setq x (exp (* 2.0 z))) 1)
     (- x 1)))

;;; secans hyperbolicus
(defun STD-SECH (z)
  (/ 2.0 (+ (exp z) (exp (- z)))))

;;; cosecans hyperbolicus
(defun STD-CSCH (z)
  (/ 2.0 (- (exp z) (exp (- z)))))

;;; area sinus hyperbolicus
(defun STD-ARSINH (z)
  (log (+ z (sqrt (+ 1.0 (* z z))))))

;;; area cosinus hyperbolicus, z: [1..inf), arcosh > 0
(defun STD-ARCOSH (z)
  (if (>= z 1.0)
    (log (+ z (sqrt (- (* z z) 1.0))))
    *INFINITY*))

;;; area tangent hyperbolicus
(defun STD-ARTANH (z)
  (if (<= (abs z) 1.0)
    (/ (log (/ (+ 1.0 z) (- 1.0 z))) 2.0)
    *INFINITY*))

;;; area cotangent hyperbolicus
(defun STD-ARCOTH (z)
  (if (>= (abs z) 1.0)
    (/ (log (/ (+ z 1.0) (- z 1.0))) 2.0)
    *INFINITY*))


;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDMATH-SYMBOLS*))
(setq *STDMATH-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDMATH")

;;; Requires defined above:
;;;     STDLISP (Top level: std-defconstant)
;;;  	STDLIST (std-gcd),
;;; 	STDINIT (Top level, std-random)

;;; No other requirements
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDPOINT.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $-*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:30:43 rurban>
;;; Copyright (c) 1998,99,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Abstract geometric AutoCAD independent code, for point and segment
;;; structures (points, segments, arcs, bulges)

;;; STATUS:
;;; moved STD-PLINE-SEGS back to STDENT so there's no entity accessing 
;;;  function remaining here.
;;; v0.5002: added 4th arg to STD-ARC->BUL,
;;;   added STD-%ARC->BUL-CCW
;;;   fixed STD-ARC->ANG, STD-ARC->SEG for UCS (the v0.5 fix was bad)
;;;   renamed STD-FIXANG360 to STD-FIXANG
;;;   STD-ARCLEN is now absolute
;;;   added STD-RTD, ... definitions from STDMATH
;;;   moved STD-GETPTS, STD-GET-GETPTS-VERTEXMASK, STD-SET-GETPTS-VERTEXMASK 
;;;     from STDPOINT to STDENT
;;; v0.5001: removed STD-FIXANG180
;;; v0.5: STD-BUL->ANG: the above fix was wrong! clarified definition,
;;;   fixed STD-ARC->SEG.
;;;   finally all segment functions are UCS independent.
;;; v0.4008: STD-BUL->ANG: fixed sign
;;; v0.4001: Entity independent code (points, segs) moved from STDENT.LSP
;;;   and some point code from STDLIST.LSP
;;;
;;; A2000 allows Safearrays: segments, segmentlists, points or
;;;   pointlists may be stored as destructive modifyable SAFEARRAY's.
;;;   This is left to an advanced module which may override the segment 
;;;   representation (maker and accessors).


;;; $Log: STDPOINT.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:50:35  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; 2000-06-19 11:00:52 rurban
;;;   moved STD-PLINE-SEGS back to STDENT so there's no entity accessing 
;;;   function remaining here.
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; 2000-06-18 22:55 rurban
;;;   moved STD-GETPTS, STD-GET-GETPTS-VERTEXMASK,
;;;   STD-SET-GETPTS-VERTEXMASK back to STDENT
;;; 2000-06-14 09:07:35 rurban
;;;   fixed STD-ARC->ANG, STD-ARC->SEG for UCS
;;;   STD-ARCLEN is now absolute (a distance without sign)
;;;   added STD-RTD, ... definitions from STDMATH
;;; 2000-06-09 17:53 rurban
;;;   added 4th arg for STD-ARC->BUL,
;;;   added STD-%ARC->BUL-CCW, 
;;;   renamed STD-FIXANG360 to STD-FIXANG
;;; 2000-05-24 rurban
;;;   removed STD-FIXANG180
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; 2000-04-14 12:57:00 rurban
;;;   fixed STD-%ARC->BUL, STD-ARC->BUL
;;;   clarified (wrong) definition for STD-BUL->ANG (wrong sign)
;;;   fixed STD-ARC->SEG: WCS values, not UCS dependent anymore
;;;   finally all segment functions are UCS independent.
;;; 2000-04-05 09:18:55 rurban
;;;   fixed STD-INSIDE-POLY-P for different UCS
;;; 2000-04-05 07:10:52 rurban
;;;   STD-BUL->ANG: fixed sign
;;;   added STD-FIXANG360, STD-FIXANG180, STD-CCW, STD-PTONSEG-P
;;; 2000-03-14 22:11:45 rurban
;;;   moved STD-SUBENTS to STDENT, added STD-FORCE3D
;;;   added STD-INSIDE-POLY-P, STD-POLY-INNER-PT (not complete yet)


;;; internal functions start with std-%
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDPOINT-SYMBOLS*
    '(x-of		xy-of		  y-of
      z-of		*PI2*		  *PI/2*
      *PI/4*		std-dtr		  std-rtd
      STD-X-MIN		STD-Y-MIN	  STD-Z-MIN
      STD-X-MAX		STD-Y-MAX	  STD-Z-MAX
      STD-XY-MIN	STD-XY-MAX	  STD-XYZ-MIN
      STD-XYZ-MAX	STD-MIDPT	  STD-HALFPT
      STD-FORCE3D	STD-PT+		  STD-PT-
      STD-NEW-Z		STD-ADD-Z	  STD-FIXANG
      STD-POINTP	STD-3DPOINTP	  STD-2DPOINTP
      STD-DISTANCE-2D	STD-TRANS01	  STD-TRANS10
      STD-MAPTRANS01	STD-MAPTRANS10	  STD-MAKE-SEG
      STD-SEG-P		STD-SEG-P1	  STD-SEG-P2
      STD-SEG-BULGE	STD-SEG-BULGE-NUM STD-SEG-ANGLE
      STD-SEG-PTS-DIST	STD-SEG-STRAIGHT-P
      STD-SEG-P1-UC	STD-SEG-P2-UC	  STD-SEG-ANGLE-UC
      STD-SEG-LENGTH	STD-SEGS-CLOSED-P STD-PTS->SEGS
      STD-POLYEDGE	STD-SEGS->PTS	  STD-CENTROID-2D
      STD-VECTOR-MEAN-3D		  STD-VECTOR-MEAN
      STD-CCW		STD-PTONSEG-P	  STD-INSIDE-POLY-P
      STD-SEG->CIR	STD-ARC->SEG	  STD-ARC->BUL
      STD-BUL->ANG	STD-ARC->ANG	  STD-ARCLEN))))



;;; avoid reload warnings in VL
;;;(eval (list 'pragma
;;;  (list 'quote
;;;    (list (cons 'unprotect-assign
;;;      '(x-of xy-of y-of z-of
;;;	*PI2* *PI/2* *PI/4*
;;;	std-dtr std-rtd
;;;	)
;;;      )))))


(if std-defconstant
  (progn
    (std-defconstant '*PI2*  (* 2.0 PI))
    (std-defconstant '*PI/2* (/ PI 2.0))
    (std-defconstant '*PI/4* (/ PI 4.0))))

;;; Degree to radian
(defun STD-DTR (deg)
  ;;(* PI (/ deg 180.0))
  (* deg 0.0174532925199433))

;;; Radian to degrees
(defun STD-RTD (rad)
  ;;(/ (* rad 180.0) PI)
  (* rad 57.29577951308233))

;;; Points (from <=0.4 STDLIST)

;;; point coordinate accessors
(defun XY-OF (p)
  (list (car p)(cadr p)))
(defun X-OF (p)
  (car p))
(defun Y-OF (p)
  (cadr p))
(defun Z-OF (p)
  (caddr p))

;;; boundaries
(defun STD-X-MIN (lst)
  (if (> (length lst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (apply (function min) (mapcar (function car) lst))))
(defun STD-Y-MIN (lst)
  (if (> (length lst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (apply (function min) (mapcar (function cadr) lst))))
(defun STD-Z-MIN (lst)
  (if (> (length lst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (apply (function min) (mapcar (function caddr) lst))))

(defun STD-X-MAX (lst)
  (if (> (length lst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (apply (function max) (mapcar (function car) lst))))
(defun STD-Y-MAX (lst)
  (if (> (length lst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (apply (function max) (mapcar (function cadr) lst))))
(defun STD-Z-MAX (lst)
  (if (> (length lst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
   (apply (function max) (mapcar (function caddr) lst))))

;;; 2d boundarybox
(defun STD-XY-MIN (lst)
  (list (std-x-min lst)(std-y-min lst)))
(defun STD-XY-MAX (lst)
  (list (std-x-max lst)(std-y-max lst)))

;;; 3d boundarybox
(defun STD-XYZ-MIN (lst)
  (list (std-x-min lst)(std-y-min lst)(std-z-min lst)))
(defun STD-XYZ-MAX (lst)
  (list (std-x-max lst)(std-y-max lst)(std-z-max lst)))

;;; midpoint of 2 points, (2d or 3d)
;;; p1 + ((p2-p1)/2)
;;;   (2 4)(1 1) -> (1.5 2.5)
(defun STD-MIDPT (p1 p2)
  (std-halfpt (std-pt+ p2 p1)))



;;; half vector, pt / 2, 2d or 3d, rankless
;;;   (2 4) -> (1 2)
(defun STD-HALFPT (pt)
  (mapcar (function /)
    pt
    (std-make-list (length pt) 2.0)))

;;; assume 2d or 3d points, for safety and convenience
(defun STD-FORCE3D (pt)
  (if (caddr pt)
    pt
    (std-new-z pt 0.0)))

;;; add two vectors
(defun STD-PT+ (p1 p2)
  ;; safety
  (mapcar (function +) (std-force3d p1) (std-force3d p2)))

;;; subtract two vectors
(defun STD-PT- (p1 p2)
  ;; safety
  (mapcar (function -) (std-force3d p1) (std-force3d p2)))

;;; change z of point
(defun STD-NEW-Z (pt h)
  (list (car pt)(cadr pt) h))

;;; add z value to point
(defun STD-ADD-Z (pt h)
  (list (car pt)
	(cadr pt)
	;; safety
	(cond ((caddr pt) (+ (caddr pt) h))
	      (T h))))

;;; Force the angle (radians) 0 <= ang < 2pi
;;; Reduces to [0, 360°)
;;; Takes an angle in radians and reduces it to less than 2pi if ang>=2pi
(defun STD-FIXANG (ang)
  (while (minusp ang) (setq ang (+ ang *PI2*)))
  (if (< ang *PI2*)
     ang
     (rem ang *PI2*)))

;;; Force the angle (radians) 0 <= ang < pi
;;; Reduces to [0, 180°)
;;; v0.5001: depricated (no replacement)
;;;(defun STD-FIXANG180 (ang)
;;;  (while (minusp ang) (setq ang (+ ang *PI2*)))
;;;  (if (< ang PI)
;;;     ang
;;;     (rem ang PI)))

;;; -------------------------------------------------------------------73
;;; (from <=0.4 STDENT)

;;; Predicates:
;;; (STD-POINTP       pt)             ; 2d or 3d
;;;   former std-ispointp
;;; (STD-2DPOINTP	pt)		;
;;; (STD-3DPOINTP	pt)		;

(defun STD-POINTP (pt)
  (and (listp pt)
       (<= 2 (length pt) 3)
       (apply (function and) (mapcar (function numberp) pt))))

(defun STD-3DPOINTP (pt)
  (and (listp pt)
       (= 3 (length pt))
       (apply (function and) (mapcar (function numberp) pt))))

(defun STD-2DPOINTP (pt)
  (and (listp pt)
       (= 2 (length pt))
       (apply (function and) (mapcar (function numberp) pt))))

;;; -------------------------------------------------------------------73
;;; Geometry:

;;; (STD-DISTANCE-2D p1 p2)	;
;;; (STD-TRANS01	pt)	; transform point to UCS
;;; (STD-TRANS10	pt)	; transform point to WCS
;;; (STD-MAPTRANS01	pts)	; transform pointlist to UCS
;;; (STD-MAPTRANS10	pts)	; transform pointlist to WCS

;;; 2d distance (omitting z values)
(defun STD-DISTANCE-2D (p1 p2)
  (distance (xy-of p1) (xy-of p2)))

;;; The following four TRANS functions are Acad Dependent, but not 
;;; entity accessing.
;;;
;;; Transform point to UCS
;;; easier for (mapcar) with just one argument
(defun STD-TRANS01 (pt)


  (trans pt 0 1)

)

;;; transform point to WCS
;;; easier for (mapcar) with just one argument
(defun STD-TRANS10 (pt)


  (trans pt 1 0)

)

;;; map some pts from WCS to UCS
(defun STD-MAPTRANS01 (pts)
  (mapcar (function std-trans01) pts))

;;; map some pts from UCS to WCS
(defun STD-MAPTRANS10 (pts / pt)
  (mapcar (function std-trans10) pts))

;;; -------------------------------------------------------------------73
;;; Segments, Bulges:

;;; v0.3013: complete rewrite
;;; Segment and circle structures:
;;;   segment: (p1 p2 [bulge])
;;;   circle:  (midpt rad)
;;; Note the changes in v0.3013 in the segment structure from the simple
;;; (bulge p1 p2) from the FAQ to the better (p1 p2 [bulge]) version.

;;; v0.5: cleared from UCS dependencies
;;;   now all segment and bulge functions are UCS independent.
;;;   all points and angles are WCS values or independent,
;;;   only the -UC function return UCS values.
;;;
;;; We may design this "simple" as in the FAQ or we may define it "good"
;;; This is a general "Worse-is-better" problem, see
;;;   http://www.ai.mit.edu/docs/articles/good-news/subsection3.2.1.html
;;; making it good but not so simple. I decided to change it to
;;; "the right thing" since I prefer Lisp over AutoLISP.
;;; But it may be the wrong decision.
;;; It may depend of how much code is already written in the FAQ version
;;; which needs to be converted from absolute accessors, like (car seg)
;;; to relative like (std-seg-bulge), and if care is taken on properly
;;; deciding between boolean or numeric use of the bulge.
;;;
;;; In general you should use abstract segment and circle accessors
;;; like (std-seg-p1 seg), (set-make-seg p1 p2 bulge), ...
;;; to be able to optimize the internal structure as needed.
;;; I changed the function names also from the FAQ to fit into the
;;; general naming scheme.

;;;Functions:
;;;!(STD-PLINE-SEGS	pline)		; is in STDENT
;;; (STD-SEG->CIR	seg)		; => cir: (midpt radius)
;;; (STD-ARC->SEG	cir ang1 ang2)	; => seg: (p1 p2 bulge)
;;; (STD-ARC->BUL	p1 p2 cir)	; => bulge as number
;;; (STD-ARC->ANG	chord rad)	; => arc angle
;;; (STD-ARCLEN	seg)			; => signed arc length
;;;Segments:
;;; (STD-MAKE-SEG p1 p2 bulge)		; create a segment, bulge may be nil
;;; (STD-SEG-P obj)			; is it a segment? (A2000!)
;;; (STD-SEG-P1 seg)			; first point of segment
;;; (STD-SEG-P2 seg)			; second point of segment
;;; (STD-SEG-BULGE seg)			; bulge of segment or nil
;;;Simple segment functions:
;;; (STD-SEG-BULGE-NUM seg)		; bulge of segment as number
;;; (STD-SEG-ANGLE seg)			; 2D WCS angle p1-p2
;;; (STD-SEG-PTS-DIST seg)		; straight length p1-p2
;;; (STD-SEG-LENGTH seg)		; also curved length
;;; (STD-SEG-STRAIGHT-P seg)		; bulge = nil?
;;; (STD-SEG-P1-UC seg)			; p1 in current UCS
;;; (STD-SEG-P2-UC seg)			; p2 in current UCS
;;; (STD-SEG-ANGLE-UC seg)		; 2D angle p1-p2 in current UCS
;;;Helpers:
;;; (STD-PTS->SEGS      pts)            ; (p1 p2 p3) => ((p1 p2)((p2 p3))
;;; (STD-SEGS->PTS      segs)           ; ((p1 p2)((p2 p3)) => (p1 p2 p3)
;;; (STD-POLYEDGE       i poly len)     ; 1 (p0 p1 p2 p3) => (p1 p2)
;;; (STD-SEGS-CLOSED-P segs)		; closed polygon?

;;; Segment Structure

;;; (STD-MAKE-SEG p1 p2 bulge) => (p1 p2 [bulge])
;;; Any line or arc may be presented as (p1 p2 [bulge])
;;; as in the internal polyline segments. (straight or curved edge)
;;; we decide to omit zero bulges to use shorter structures.
;;; possible A2000 optimization: use SAFEARRAY
(defun STD-MAKE-SEG (p1 p2 bulge)
  (if (and bulge (not (zerop bulge)))
    (list p1 p2 bulge)
    (list p1 p2)))			; shorter data structure

;;; Is it a segment?
;;; Beware of a possible A2000 optimization.
;;; That's why we need a special predicate.
(defun STD-SEG-P (obj)
  (and (consp obj)
       (std-pointp (car obj))
       (std-pointp (cadr obj))))

(defun STD-SEG-P1 (seg) (car seg))
(defun STD-SEG-P2 (seg) (cadr seg))

;;; STD-SEG-BULGE returns always nil on straight edges (bulge=0.0),
;;; so it's usable as predicate also.
(defun STD-SEG-BULGE (seg)
  (cond ((not (caddr seg)) nil)
	((zerop (caddr seg)) nil)
	((caddr seg))))

;;; STD-SEG-BULGE-NUM returns the numeric bulge value.
;;; For calculations with bulges, converts nil to 0.0
;;; This used less often than the boolean STD-SEG-BULGE
(defun STD-SEG-BULGE-NUM (seg)
  (cond ((std-seg-bulge seg)) (T 0.0)))

;;; Simple Segment Functions:
(defun STD-SEG-ANGLE (seg)    (angle (std-seg-p1 seg) (std-seg-p2 seg)))
(defun STD-SEG-PTS-DIST (seg) (distance (std-seg-p1 seg)(std-seg-p2 seg)))
(defun STD-SEG-STRAIGHT-P (seg) (not (std-seg-bulge seg)))
(defun STD-SEG-P1-UC (seg) (std-trans01 (car seg)))
(defun STD-SEG-P2-UC (seg) (std-trans01 (cadr seg)))
(defun STD-SEG-ANGLE-UC (seg)	; UCS angle
  (angle (std-seg-p1-uc seg)
         (std-seg-p2-uc seg)))
(defun STD-SEG-LENGTH (seg)
  (if (std-seg-straight-p seg)	; fixed
    (std-seg-pts-dist seg)
    (abs (std-arclen seg))))

;;; More Segment Functions:

;;; (STD-SEGS-CLOSED-P segs) => T/nil
;;; T if p1 of the first segment is the same as p2 of the last.
;;; Note: you might also need to check if the last segment is straight.
;;; Note: don't use it with pointlists! This is completely different.
(defun STD-SEGS-CLOSED-P (segs)
  (equal (std-seg-p1 (car segs))
         (std-seg-p2 (last segs))))

;;; (STD-PTS->SEGS pts) => segs
;;; Converts a pointlist of an open polygon to a list of straight
;;; segments. On closed polylines the last closing segment must be forced by
;;; adding the first point to the end of the list.
;;; (p1 p2 p3) => ((p1 p2)(p2 p3))		; open
;;; (p1 p2)    => ((p1 p2))			; straight single line
;;; (p1 p2 p3 p1) => ((p1 p2)(p2 p3)(p3 p1))	; closed
(defun STD-PTS->SEGS (_pts / _len)
  (setq _len (length _pts))
  (mapcar (function (lambda (_i) (std-polyedge _i _pts _len)))
          (std-int-list (1- _len))))

;;; (STD-POLYEDGE i poly len) => (p[i] p[i+1])
;;; i-th edge of polygon (zero-based) => segment
;;; (std-polyedge 1 (p1 p2 p3 p4) 4) => (p2 p3)
(defun STD-POLYEDGE (i poly len)
  (list
    (nth (std-nxtcyc i len 'abs) poly)  ;p1 - fixed
    (nth (std-nxtcyc i len '1+) poly))) ;p0 - fixed
    ;; dummy 'abs function to avoid errors with i=-1 or len+1
    ;; only if you're absolutely sure that 0 <= i < len then use (nth i poly)

;;; (STD-SEGS->PTS segs) => pts
;;; converts segments to pointlist
;;; ((p1 p2)(p2 p3)) => (p1 p2 p3)
;;; fixed by Serge Pashkov
(defun STD-SEGS->PTS (segs)
  (append (mapcar (function std-seg-p1) segs)
	  (list (std-seg-p2 (last segs)))))	;fixed

;;; -------------------------------------------------------------------73
;;; Some advanced geometry for centroids and planar polygons

;;; Twice the area of a simple 2d triangle
;;; the vector cross product in 2D, z ignored
(defun STD-%AREAx2 (a b c)
  (- (* (- (car  b)(car  a))
        (- (cadr c)(cadr a)))
     (* (- (car  c)(car  a))
        (- (cadr b)(cadr a)))))

;;; Three times the centroid of a simple 2d triangle,
;;; z ignored, 2d point returned
(defun STD-%CENTROID2Dx3 (pts)
  (if (> (length pts) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (list (apply (function +) (mapcar (function car) pts))
          (apply (function +) (mapcar (function cadr) pts)))))

;;; Computes the centroid (center of gravity) of an arbitrary
;;; simple polygon via the weighted sum of the signed triangle areas,
;;; weighted by the centroid of each triangle.
;;; Thanks to Kamal Boutora <synapse@ist.cerist.dz>
;;; Returns a 2D or 3D point, dependant on the first given point.
;;; The double area and three times centroid functions are used to
;;; avoid division. See comp.graphics.algorithms FAQ or
;;; the O'Rourke code centroid.c
;;; Curved areas are not honored, use REGION and (std-massprop) then.
(defun STD-CENTROID-2D (pts / areasum2 a2 p0 c3 cg)
  (setq areasum2 0.0
        cg       '(0 0)
        p0       (car pts)
        pts      (cdr pts))
  (if (equal p0 (last pts)) (setq pts (std-butlast pts)))
  (while (cadr pts)
    (setq c3 (std-%centroid2dx3 (list p0 (car pts) (cadr pts)))
          a2 (std-%areax2 p0 (car pts) (cadr pts))
          cg (list (+ (car  cg) (* a2 (car  c3)))
		   (+ (cadr cg) (* a2 (cadr c3))))
          areasum2 (+ areasum2 a2)
          pts (cdr pts))
  )
  (setq areasum2 (* areasum2 3.0))
  (cond
    ((zerop areasum2) nil) ; or (std-vector-mean-3d pts)
    ((caddr p0)
      (list (/ (car  cg) areasum2)
            (/ (cadr cg) areasum2)
            (caddr p0)))
    (T (list (/ (car  cg) areasum2)
             (/ (cadr cg) areasum2)))))


;;; STD-VECTOR-MEAN-3D
;;;   ((2 4)(1 1))      -> (1.5 2.5 0.0)
;;;   ((2 2)(2 4)(1 1)) -> (2.5 3.5 0.0)
(defun STD-VECTOR-MEAN-3D (pts / n)
  (setq n (float (length pts)))
  (if (> n *MAX-ARGS-LIMIT*)
   (std-error (std-msg "maximal number of arguments exceeded"))
   (list
    (/ (apply (function +)
         (mapcar (function car) pts)) n)
    (/ (apply (function +)
         (mapcar (function cadr) pts)) n)
    (if (caaddr pts)
      (/ (apply (function +)
           (mapcar (function caddr) pts)) n)
      0.0)
  ))
)

;;; General n-dimensional version. Quite hairy.
;;; Inspired by Doug Wilson's (list-transpose)
(defun STD-VECTOR-MEAN (pts)
  (if (> (length pts) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded")))
  ;; mean of each vector
  (mapcar (function /)
    ;; sum it up (all x,y..)
    (mapcar (function (lambda (v) (apply (function +) v)))
	    ;; transposition: flip rows with cols
            (apply (function mapcar) (cons 'list pts)))
    (std-make-list (length (car pts)) (float (length pts)))))

;;;  Test counter-clockwise of 3 points (the rotation)
;;;  => 1: ccw,
;;;    -1: cw or p0 between p1 and p2
;;;     0: p2 between p0 and p1
;;;  determinant of matrix >0  (2x signed area of triangle p0,p1,p2)
;;;     |p0x p0y 1|
;;;     |p1x p1y 1|
;;;     |p2x p2y 1|
;;;  see Sedgewick or others
(defun STD-CCW (p0 p1 p2 / dx1 dx2 dy1 dy2)
  (setq dx1 (- (car p1) (car p0))
        dy1 (- (cadr p1) (cadr p0))
        dx2 (- (car p2) (car p0))
        dy2 (- (cadr p2) (cadr p0)))
  (cond
    ((> (* dx1 dy2) (* dy1 dx2)) 1)
    ((< (* dx1 dy2) (* dy1 dx2)) -1)
    (T (cond
        ((or (minusp (* dx1 dx2))
             (minusp (* dy1 dy2))) -1)
        ;; explicit distance
        ((>= (+ (* dx1 dx1) (* dy1 dy1)) (+ (* dx2 dx2) (* dy2 dy2))) 0)
        (T 1)))))

;;; (STD-PTONSEG-P p1 p2 testpt)
;;; This function is used to test a point being exactly on a straight 
;;; line segment or not. (not on the extension, in between)
(defun STD-PTONSEG-P (p1 p2 pt)
  (if (equal p1 p2 1E-6)
    (equal pt p1 1E-6)
    (zerop (std-ccw p1 p2 pt))))

;;; (STD-INSIDE-POLY-P pt pts)
;;; T if the point is strictly inside the poly,
;;; nil if outside or on any boundary segment.
;;; STATUS: This is quite hairy and not 100% reliable, because
;;;   it relies on certain SSGET acad features. A fast independent 
;;;   solution is out of the scope of lisp, but there are C libs 
;;;   and a lisp file.
;;; Note:
;;;   There must be no duplicate points! 
;;;   The polygon must not be self-intersecting!
;;;   All points must be in the current UCS.
(defun STD-INSIDE-POLY-P (pt pts / found lay ss)
  (std-%simple-require "STDTBL")
  ;; ensure that the current layer is on
  (if (not (std-layer-visible-p (setq lay (getvar "CLAYER"))))
    ;; find the first visible layer or
    ;; if none is visible, make a new one
    (if (not (setq lay (car (std-tbl-layer "*"
			   STD:LAY-THAWED 0 STD:LAY-ON))))
      (std-layer-add (setq lay (std-tbl-tmpname "LAYER" "$STD-POLY")))
    )
  )
  ;; check zoom range: all pts must be visible!
  (std-var-push '((:ZOOM)))
  (std-zoom-if (std-xy-min pts)
    	       (std-xy-max pts))
  (if (not (entmake (list '(0 . "POINT")
			   (cons 10 (std-trans10 pt))
			   (cons 8 lay))))
    (std-error "entmake POINT failed"))
  (setq found (and (setq ss (ssget "_WP" pts))
                   (ssmemb (entlast) ss)))
  (std-var-pop)
  (entdel (entlast))
  found
)

;;; -------------------------------------------------------------------73
;;; Some simple curves: arcs and circles
;;; All functions are UCS independent, we may assume WCS or UCS 
;;; points and angles.

;;; (STD-SEG->CIR seg) => (midpt radius)
;;; Converts a bulged segment (p1 p2 [bulge]) of a polyline
;;; to a circle (ctr rad). the start- and endpoints are known,
;;; therefore the angles too: (angle ctr pt1)(angle ctr pt2)
;;; Returns nil on a straight segment! fixed by Serge Pashkov
(defun STD-SEG->CIR (seg / bulge p1 p2 cot x y rad dummy)
  (if (setq bulge (std-seg-bulge seg))  ;straight line => invalid circle
    (setq p1 (std-seg-p1 seg)
	  p2 (std-seg-p2 seg)
          cot (* 0.5 (- (/ 1.0 bulge) bulge))
          x (/ (- (+ (car p1)(car p2))		; x and y of midpoint
		  (* (- (cadr p2) (cadr p1)) cot)) 2.0)
          y (/ (+ (+ (cadr p1)(cadr p2))
		  (* (- (car  p2) (car  p1)) cot)) 2.0)
          rad   (distance (list (car p1) (cadr p1)) (list x y))
          dummy (list (list x y) rad)   ; return this, I hate progn's
    )
  )
)

;;; (STD-ARC->SEG  cir ang1 ang2) => (p1 p2 bulge)
;;; - inverse conversion:
;;; Calculates segment (p1 p2 bulge) of arc
;;;   with given circle (ctr rad), start-angle, end-angle
;;;   (std-arc->seg cir (angle (car cir) p1) (angle (car cir) p2)) => seg
;;; angles in WCS (measured to the world x axis)
;;; all points (cir p1 p2) UCS independent (WCS or UCS)
;;; Note: POLAR does not work as described in the docs. 
;;;       the angle is UCS independent, so our function as well.

(defun STD-ARC->SEG (cir ang1 ang2 / p1 p2)
  (setq p1 (polar (car cir) ang1 (cadr cir))
        p2 (polar (car cir) ang2 (cadr cir)))
  (list p1 p2				; use this sign
    (std-tan (* 0.25 (- ang2 ang1)))))


;;; (STD-%ARC->BUL ang1 ang2) 	=> bulge
;;; - calculates positive bulge of arc defined by its two angles
;;; ang1: angle from ctr to p1, ang2: angle from ctr to p2
;;; the correct rotation (sign) must be applied later, 
;;; see STD-%ARC->BUL-CCW
;;; fixed 14-Apr-00, 9-Jun-00
(defun STD-%ARC->BUL (ang1 ang2)
  (std-tan (* 0.25 (std-fixang (- ang2 ang1)))))

;;; with explicit rotation given
(defun STD-%ARC->BUL-CCW (ang1 ang2 ccw)
  (if (minusp ccw)
    (- (std-%arc->bul ang2 ang1))
    (std-%arc->bul ang1 ang2)))

;;; (STD-ARC->BUL p1 p2 cir ccw) => bulge
;;; Calculates positive bulge (CCW) of arc given both arc endpoints
;;; and the circle (ctr rad). (The radius is not needed)
;;; UCS independent (angle is UCS dependent, but the difference not)
(defun STD-ARC->BUL (p1 p2 cir ccw)
  (std-%arc->bul-ccw (angle (car cir) p1)
    		     (angle (car cir) p2)
    		     ccw))
;;; Note:
;;; Before you had to flip the sign on CW rotation like this:
;;; (* (std-ccw p1 xx p2) (std-arc->bul p1 p2 cir)) ; or
;;; (* (std-signum (std-seg-bulge segxx)) (std-arc->bul p1 p2 cir))

;;; Old code:
;;; This is of course a stupid recalculation of an already computed value
;;;(defun STD-BUL->ANG (seg / ctr)
;;;  (- (angle (setq ctr (car (std-seg->cir seg))) (std-seg-p2 seg))
;;;     (angle ctr (std-seg-p1 seg))))

;;; (STD-BUL->ANG seg) => angle
;;; Returns inner angle of an arc given by its bulge,
;;; angle from center to endpoint minus angle to startpoint.
;;; Note:
;;;   Fixed again for v0.5 14-Apr-00.
;;;   We assumed a wrong definition here for v0.4008-0.4009,
;;;   same sign as in the FAQ again
(defun STD-BUL->ANG (seg)
  (* 4 (atan (std-seg-bulge-num seg))))

;;; (STD-ARC->ANG chord rad) => angle
;;; Calculates inner angle of arc given the chord distance and radius
;;; This is the absolute inner angle (as with STD-BUL->ANG)
;;; 14-Jun-00: fixed divide by zero on square
;;; on chord > 2r => error: function undefined for argument
(defun STD-ARC->ANG (chord rad / root)
  (cond
    ((= (* 2 rad) chord) 0) ; PI would return wrong bulge
    (T (* 2.0 (atan (/ chord 2.0
		       (sqrt (- (* rad rad)
				(* 0.25 chord chord)))))))))


;;; "Bartsch": chord = 2r sin (ang/2)  (chord: "Sehne", S.236,224)
;;;   => ang = 2 * asin (chord / 2r)   , 2r >! chord
;;; asin: (atan z (sqrt (- 1.0 (* z z))))
;;;(defun STD-%ARC->ANG1 (chord rad)  ; simplier version? to time
;;;  (* 2 (std-asin (/ chord rad 2))))
;;;(defun STD-%ARC->BUL1 (chord rad)
;;;  (std-tan (* 0.5 (std-asin (/ chord rad 2)))))

;;; (STD-ARCLEN seg)
;;; => length of arc   = radius * angle
;;; Does NOT check for curved segment, bulge must be <> 0
;;; otherwise you'll get 0.0 and not the straight distance.
(defun STD-ARCLEN (seg)
  (abs (* (cadr (std-seg->cir seg))              ; radius
          4.0 (atan (std-seg-bulge-num seg)))))  ; angle = 4*atan(bulge)

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDPOINT-SYMBOLS*))
(setq *STDPOINT-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDPOINT"); first provide it, but before you may
				; call it, be sure to have all supporting
				; functions:

;;; If all required STDLIB modules are contained in one project,
;;; we don't have to load it explicitly.
;;; STDLIB.FAS already contains it!
;;;(if (not *STD:%PROJECT*) (progn

(std-%simple-require "STDINIT")
(std-%simple-require "STDLIST")
(std-%simple-require "STDLISP")
(std-%simple-require "STDSTR")
(std-%simple-require "STDFILE")
(std-%simple-require "STDMATH")
(std-%simple-require "STDTIME")

;;; (std-%simple-require "STDACI")   ; almost all

;;;))

;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDTIME.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:31:03 rurban>
;;; Copyright (c) 1994,1998,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Time functions for the STDLIB

;;; STATUS:
;;;   STD-CDATE->DATLST has accuracy errors. See the test files.
;;;   STD-TIME moved to STDDEBUG
;;;   Output and Date Arithmetic not in SMALL
;;;   Dates around year and month zero are erronous.
;;;     => wrong time and date calculations.
;;;     STD-CANONIZE-DATLST yet impossible
;;;     STD-CANONIZE-CDATE is useless art, DATLST is the only lossless
;;;     format



;;; $Log: STDTIME.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; 2000-07-27 17:02:18 rurban
;;;   added std-%timer-ms-str
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; 2000-06-20 16:12:35 rurban
;;;   added STD-CANONIZE-DATLST, try to fix dates around zero
;;;   fixed STD-%FORMAT-DATE-STRING for relative dates (low numbers)
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; 2000-04-19 10:06:24 rurban
;;; fixed (std-%format-date-string cdate "sss")
;;;
;;; Revision 0.4009  2000/04/07 17:29:20  rurban
;;; v0.4009, see Changes
;;;
;;; ==================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDTIME-SYMBOLS*
    '(STD-DATE->DATLST	     STD-DATLST->DATE
      STD-CDATE->DATLST	     STD-DATLST->CDATE
      STD-DATLST-YEAR	     STD-DATLST-MONTH
      STD-DATLST-DAY	     STD-DATLST-HOUR
      STD-DATLST-MIN	     STD-DATLST-SECS
      STD-DATLST-DOW	     STD-DATE->CDATE
      STD-CDATE->DATE	     STD-CANONIZE-CDATE
      STD-CANONIZE-DATLST    STD-DATE-DIFF
      STD-DATE-SUM	     STD-TODAY
      STD-TODAY-LONG	     STD-CDATE->DATE-STRING
      STD-CDATE->TIME-STRING STD-TIMER-START
      STD-TIMER-STOP	     STD-SLEEP))))


;;; DATE (julian date) may be used for subtraction and addition.
;;;      It is the number of days (plus fractional seconds, UTC) in the
;;;      julian calendar.
;;; CDATE  is a readable number of the calendar date, may be used for
;;;      sorting also.
;;; DATLST is a a list of numbers in calendar format as in CDATE.
;;;      (year  	like 1998
;;;       month 	[1-12]
;;; 	  day		[1-31]
;;;       hour 		[0-23]
;;;       minute 	[0-59]
;;;       seconds 	[0-59.9] real
;;;       [dow]		[0-6] Monday-Sunday
;;;      )
;;; INTERNAL-TIME is the number of non-leap seconds since
;;;	 00:00:00 UTC, January 1, 1970

;;; Round-off problems in CDATE format
;;;-----------------------------------
;;; The CDATE floating point number
;;; is defined as a floating-point number interpreted as:
;;;
;;;     yyyymmdd.hhiissttt
;;;
;;; This gives 17 digits but maximal 16 digits (up to milliseconds)
;;; can be represented by double IEE floating point numbers and this
;;; degrade to 15 precise digits after some calculations. (10 ms)
;;; So don't use CDATE numbers in calculations, do everything in DATLST
;;; (preferred because single numbers don't loose accuracy) or
;;; julian DATE if you need milliseconds accuracy.

;;; Decompose Julian DATE into calendar date datlst.
;;; The Algorithm is from Collected Algorithms from
;;; Communications of the ACM. The only "magic" function here.
;;; Note: This function is wrong for early dates,
;;;   for relative date calculations e.g.
(defun STD-DATE->DATLST (date / time julian yr day mo hh ss mm)
  (setq time (* 86400.0 (- date (setq julian (fix date)))))
  (setq julian (- julian 1721119))
  (setq yr (/ (1- (* 4 julian)) 146097))
  (setq julian (- (* julian 4) 1 (* 146097 yr)))
  (setq day (/ julian 4))
  (setq julian (/ (+ (* 4 day) 3) 1461))
  (setq day (- (+ (* 4 day) 3) (* 1461 julian)))
  (setq day (/ (+ day 4) 4))
  (setq mo (/ (- (* 5 day) 3) 153))
  (setq day (- (* 5 day) 3 (* 153 mo)))
  (setq day (/ (+ day 5) 5))
  (setq yr (+ (* 100 yr) julian))
  (if (< mo 10)
    (setq mo (+ mo 3))
    (setq mo (- mo 9)
          yr (1+ yr))
  )
  (setq hh (fix (/ time 3600.0)))
  (setq time (- time (* hh 3600)))
  (setq mm (fix (/ time 60.0)))
  (setq ss (- time (* mm 60)))
  (list yr mo day hh mm ss)
)

;;; STD-DATLST->DATE converts calender datlst into julian DATE Format
;;; DATLST:  (yr mo day hh mm ss)
;;; DATE:    julian date
;;; Problem: '(0 0 0 ...) => wrong
;;; Fails with negative years and dates around zero (for time differences)
(defun STD-DATLST->DATE (datlst / yr y mo day hh mm ss a b)
  (setq yr  (car datlst)
	y   yr
	mo  (cadr datlst)
	day (caddr datlst)
	hh  (fourth datlst)
	mm  (fifth datlst)
	ss  (sixth datlst))
  (if (and (zerop y)(zerop mo))
    ;; added 20-Jun-00: day 0 when? -4712 probably (=> 1721058 days)
    (+ 1721024 day (/ (+ (* (+ (* hh 60) mm) 60) ss) (* 24.0 60 60)))
    (progn
     (if (<= mo 2)
      (setq y  (1- y)
            mo (+ mo 12)))
     (if (or (< yr 1582)
            (and (= yr 1582) (or (< mo 10) (and (= mo 10) (< day 5)))))
      (setq b 0)                    ; simple old Julian calendar
      (setq a (fix (/ y 100))       ; Gregorian calendar fix
            b (+ (- 2 a) (fix (/ a 4)))))
     (+ (fix (+ (* 365.25 (+ y 4716)) (fix (* 30.6001 (+ mo 1)))))
           day b -1524.0 (/ (+ (* (+ (* hh 60) mm) 60) ss) (* 24.0 60 60)))
  ))
)

;;; yr: 4 digit fixnum! (such as 1984)
;;; fails with negative numbers
(defun std-%isleap-year (yr)
  (setq yr (fix yr))
  (or (zerop (rem yr 400))
      (and (zerop (rem yr 4))
           (not (zerop (rem yr 100))))))

;;; yr: 4 digit fixnum! (1984)
;;; fails with negative numbers
(defun std-%years->days (yr)
  (setq yr (fix yr))
  (+ (* yr 365) (/ yr 4) (- (/ yr 100)) (/ yr 400))
)

;;; mo: must be fixnum!
(defun std-%months->days (mo)
  (setq mo (fix mo))
  (/ (- (* mo 3057) 3007) 100)
)

;;; Calculate the day of the week for a given date.
;;; => 0-6: Monday-Sunday
;;; Autodesk's JULIAN.LSP as well as the clib dow functions use sunday
;;; as base, common lisp functions use the monday as 0. Hmm...
;;; Thanks to Serge Pashkov
(defun STD-%DAY-OF-WEEK (date)
  (rem (fix date) 7))

;;; STD-DATLST->INTERNAL-TIME converts calender datlst into
;;; the number of non-leap seconds since 00:00:00 UTC, January 1, 1970
;;; Commonly known as localtime or unix time.
;;; DATLST:  (yr mo day hh mm ss)
(defun STD-%DATLST->INTERNAL-TIME (datlst / yr mo day hh mm ss)
  (setq yr  (car datlst)
	mo  (cadr datlst)
	day (caddr datlst)
	hh  (fourth datlst)
	mm  (fifth datlst)
	ss  (sixth datlst))
  (+ (*		; days -> seconds
       (+ (std-%years->days (fix (- yr 1970)))
	  (std-%months->days (fix mo))
	  day)
       86400.0)
     (* hh 3600.0)
     (* mm 60.0)
     (fix ss)
  )
)

;;; CDATE (calendar date) is the string representation of DATLST:

;;; similar to the string decomposition
;;; max. to milliseconds accuracy if you are lucky.
;;; 19700101.0 => (1970 1 1 0 0 0.0)
(defun STD-CDATE->DATLST (cdate / yr mo day hh mm sec rst)
  (setq yr (fix (/ cdate 10000.0)))
  (setq cdate (- cdate (* 10000.0 yr)))
  (setq mo (fix (/ cdate 100.0)))
  (setq cdate (- cdate (* 100.0 mo)))
  (setq day (fix cdate))
  (setq rst (- cdate day))	; 0.121245 => 12 12 45.0

  (setq hh (fix (* rst 100)))
  (setq rst (- rst (/ hh 100.0)))
  (setq mm (fix (* rst 10000)))
  (setq sec (* 1000000.0 (- rst (/ mm 10000.0))))	; fixed
  (list yr mo day hh mm sec)
)

;;; similar to the string composition
;;; max. to milliseconds accuracy if you are lucky [improved by Serge Pahkov]
;;; (yr mo day hh mm ss) => CDATE
;;; TODO: normalization
(defun STD-DATLST->CDATE (datlst)
  (+
    (/ (nth 5 datlst) 1000000.0)
    (/ (nth 4 datlst) 10000.0)
    (/ (nth 3 datlst) 100.0)
    (nth 2 datlst)
    (* 100.0   (nth 1 datlst))
    (* 10000.0 (nth 0 datlst))
  )
)

;;; DATLST struct-like accessors
(defun STD-DATLST-YEAR (datlst) (first datlst))
(defun STD-DATLST-MONTH(datlst) (second datlst))
(defun STD-DATLST-DAY  (datlst) (third datlst))
(defun STD-DATLST-HOUR (datlst) (fourth datlst))
(defun STD-DATLST-MIN  (datlst) (fifth datlst))
(defun STD-DATLST-SECS (datlst) (sixth datlst))
(defun STD-DATLST-DOW  (datlst)
  (cond ((seventh datlst))
	((std-%day-of-week (std-datlst->date datlst)))))

;(defun std-%datlst-ampm (datlst) (eighth datlst))	;boolean?

;;; conversion via DATLST, best accuracy
(defun STD-DATE->CDATE (date)
  (std-datlst->cdate (std-date->datlst date)))

;;; conversion via DATLST, best accuracy
(defun STD-CDATE->DATE (cdate)
  (std-datlst->date (std-cdate->datlst cdate)))

;;; we must go via DATA for the correct decomposition
;;; e.g: day > 31 => month + 1
;;; but we cannot fix wrong seconds, so better use
;;; STD-CANONIZE-DATLST when it will work for low dates
;;; (time differences)
(defun STD-CANONIZE-CDATE (cdate)
  (std-date->cdate (std-cdate->date cdate)))

;;; shift overflows to the left and underflows to the right
;;; Doesn't work for low dates: (time differences)
(defun STD-CANONIZE-DATLST (datlst)
  (std-date->datlst (std-datlst->date datlst)))

;;; ------------------------------------------------------------------73
;;; Date Arithmetic:

	; no such funcs in SMALL

;;; Some date arithmetic, accept any reasonable date format.
;;; CDATE with only maximal seconds accuracy, no milliseconds!

;;; dat1 - dat2
;;;   (STD-DATE-DIFF '(1998 12 01 0 0 0) 4) => 19981127
;;;   (STD-DATE-DIFF 19981208 19981204)     => 19981212
(defun STD-DATE-DIFF (dat1 dat2)
  (setq dat1 (std-%guess-date-format dat1)
        dat2 (std-%guess-date-format dat2))
  (std-date->cdate (- dat1 dat2)))

;;; dat1 + dat2
;;; If one argument is not a resonable CDATE, convert it to a
;;; reasonable DATE format.
;;;   (STD-DATE-SUM 19981208 4) => 19981212
;;;   (STD-DATE-SUM 19981130 2) => 19981202
;;;   (STD-DATE-SUM 19981231.12 1) => 19990101.12
(defun STD-DATE-SUM (dat1 dat2)
  (setq dat1 (std-%guess-date-format dat1)
        dat2 (std-%guess-date-format dat2))
  (std-date->cdate (+ dat1 dat2)))

;;; accept either DATE, DATLST or CDATE,
;;; CDATE must be a number like 19xxxxxx
;;; return DATE format
(defun std-%guess-date-format (dat)
  (cond ((listp dat) (std-datlst->date dat))
	((< 19000000 dat 20320000) (std-cdate->date dat))
	(T dat)))



;;; ------------------------------------------------------------------73
;;; Output



	; no output funcs in SMALL, maybe consider std-today

;;; Current date as string, localized conventions
;;; such as "9.7.98" or "7/9/98"
(defun STD-TODAY ()
  (if (std-acad-connection-p)
    (std-cdate->date-string (getvar "CDATE"))
    (progn
      (std-require-version "FILEEXT" 0.56)
      (std-cdate->date-string
	(std-datlst->cdate (std-%standalone-actual-datlst))))))

;;; as "7.July 1998"
(defun STD-TODAY-LONG ()
  (std-%format-date-string
    (if (std-acad-connection-p)
      (getvar "CDATE")
      (std-datlst->cdate (std-%standalone-actual-datlst)))
    "d.mmmm yyyy"))

;;; User setable date format options:
;;; mm/dd/yy, m/d/yy, m/d/yy
;;; locale settings are done in STDLOCAL
(if (not *DATE-FORMAT-STRING*)
  (setq *DATE-FORMAT-STRING* "m/d/yy"))
(if (not *TIME-FORMAT-STRING*)
  (setq *TIME-FORMAT-STRING* "hh:nn:ss AMPM"))

;;; Format string: "d.m.yy" if not :ENGLISH
;;;                "m/d/yy" if :ENGLISH
(defun STD-CDATE->DATE-STRING (cdate)
  (std-%format-date-string cdate *DATE-FORMAT-STRING*))

;;; Format string: "hh:nn:ss", hour, minutes, seconds as "14:00:23"
(defun STD-CDATE->TIME-STRING (cdate)
  (std-%format-date-string cdate *TIME-FORMAT-STRING*))

;;; Format string: "%d/%d%/%2d", month, day, year if :ENGLISH
;;; 			=> "2/21/98"
;;; Format string: "%d.%d%.%2d", day, month, day if not :ENGLISH
;;; strips leading "0"



;;; keep leading "0", no milliseconds, no am/pm support yet


;;; localized versions see STDLOCAL
(std-defconstant '*MONTH-NAMES*
  '("January" "February" "March" "April" "May" "June"
   "July" "August" "September" "October" "November" "December"))
(std-defconstant '*WEEK-DAYS*
  '("Monday" "Tuesday" "Wednesday" "Thursday" "Friday" "Saturday"
    "Sunday" "???"))


;;; Returns portions of CDATE according the given format string.
;;; Not all format string options are supported yet.
;;;   yyyy - long year, 4 digits
;;;   yy   - short year, 2 digits
;;;   mmmm - long month string
;;;   mmm  - 3char month string abbrevation
;;;   mm   - month 2 digits
;;;   m    - month 1 digit if zero
;;;   dddd - day of week long string (Sunday)
;;;   ddd  - 2char day of week string abbrevation (Su-Sa)
;;;   dd   - day 2 digits
;;;   d    - day 1 digit if zero
;;;   hh   - hour 2 digits
;;;   h    - hour 1 digit if zero
;;;   nn   - minutes 2 digits
;;;   n    - minutes 1 digit if zero
;;;   sss  - seconds with milliseconds
;;;   ss   - seconds 2 digits
;;;   s    - seconds 1 digit if zero
;;;   ampm - am or pm literal suffix
;;;   AMPM - AM or PM
;;; Problem: ALL magic characters are expanded so far, so we cannot use
;;; verbose format strings, like "Today is ddd, the d.mmmm yyyy", because
;;; the "d" in "Today" and the "s" in "is" and the "h" in "the" will be
;;; expanded too. This could be fixed by having a look at word boundaries.
;;; FIXME: Wrong for "s" "ss" "sss", "sss" fixed 19-Apr-00
;;; relative cdates (empty days, ...) fixed 20-Jun-00
(defun std-%format-date-string (cdate fmt / w s datlst ret
					    x i l magic ampm upcase)
  
;;;  (if (std-acad-connection-p)
;;;    (setq s (std-string-right-pad-char (rtos cdate 2 9) 16 "0"))
;;;    ;; rounding to the 8-th digit. improved somewhat for standalone
;;;    (setq s (strcat (itoa (fix cdate)) "."
;;;		    (substr (std-rtos (std-fraction cdate)) 3)))
;;;  )
  ;; FIXED v0.5002 for 0.0xx seconds
  (if (std-acad-connection-p)
    (setq s (rtos cdate 2 9))
    (setq s (std-rtos cdate)))
  ;; add zeros at the front
  (if (setq i (std-strpos "." s))	; 11-Jul-00
    (repeat (- 9 i) (setq s (strcat "0" s))))
  ;; add trailing zeros
  (setq s (std-string-right-pad-char s 16 "0"))
  ;;(setq s (substr s 1 16))
  (setq datlst (std-cdate->datlst cdate))
  ;; hmm, not true anymore:
  ;; we need to calculate datlst for the month and the hour as number
  ;; else it's nil
  (if (or (std-strpos "ddd" fmt)
	  (std-strpos "mmm" fmt)
	  (setq ampm (std-stripos "AMPM" fmt)))
    (setq datlst (append datlst
      (list (if (std-strpos "ddd" fmt)
	      (std-%day-of-week (std-datlst->date datlst));=> 0-6 (Monday-Sunday)
	      nil)
            ampm))))
  ;; special handling of AMPM (this is really a pain)
  (if ampm
    (if (std-strpos "AMPM" fmt)
      (setq fmt (std-strchg fmt "AMPM" "") upcase T)
      (setq fmt (std-strchg fmt "ampm" ""))))
  ;; parse out the relevant format string subportions
  (setq magic (std-string->list "mdyhns")
	i 1
	l (strlen fmt)
	ret "")
  (while (< i l)
    (setq x "")
    (while (member (ascii (substr fmt i 1)) magic)
      (setq x (strcat x (substr fmt i 1))
	    i (1+ i)))
    (setq ret (strcat ret
      (if (std-string-not-empty-p x)
        (std-%format-date-substring s x datlst ampm)
	(substr fmt (1- (setq i (1+ i))) 1))))
  )
  (if ampm
    (strcat
      ret
      (if (= (std-lastchar fmt) " ") " " "")	; temp workaround
      (std-%format-date-substring s (if upcase "AMPM" "ampm") datlst ampm))
    ret))

;;; wrong? hmm..
;;; 12:00 am is midnight, 12:00 pm is noon
(defun std-%time->ampm (hh prefix)
  (cond
    ((= hh 0)
      "12")
    ((>= hh 22)
      (itoa (- hh 12)))
    ((> hh 12)
      (strcat prefix (itoa (- hh 12))))
    ((>= hh 10)
      (itoa hh))
    (T
      (strcat prefix (itoa hh)))
    ))

;;; s      is the string representation of the CDATE
;;; fmt    the magic substring
;;; datlst the date in datlst format to avoid double calcluations
;;; ampm   is boolean
(defun std-%format-date-substring (s fmt datlst ampm / mo)
  (setq mo (std-datlst-month datlst))
  (cond
    ((= fmt "yyyy") (substr s 1 4))
    ((= fmt "yy")   (substr s 3 2))
    ((= fmt "mmmm") (nth (1- mo) *month-names*))		; "January"
    ((= fmt "mmm")  (substr (nth (1- mo) *month-names*) 1 3))	; "Jan"
    ((= fmt "mm")   (substr s 5 2))				; "01"
    ((= fmt "m")
     (if (= "0" (substr s 5 1))
       (substr s 6 1)
       (substr s 5 2)))
    ((= fmt "dddd") (nth (std-datlst-dow datlst) *week-days*))  ; "Sunday"
    ((= fmt "ddd")
      (substr (nth (std-datlst-dow datlst) *week-days*) 1 3)); "Sun"
    ((= fmt "dd")  (substr s 7 2))			     ; "07"
    ((= fmt "d")
     (if (= "0" (substr s 7 1))
       (substr s 8 1)
       (substr s 7 2)))
    ((= fmt "hh")
     (if ampm
       (std-%time->ampm (std-datlst-hour datlst) "")
       (substr s 10 2)))
    ((= fmt "h")
     (if ampm
       (std-%time->ampm (std-datlst-hour datlst) "0")
       (substr s 10 2)))
    ((= fmt "nn")  (substr s 12 2))
    ((= fmt "n")
     (if (= "0" (substr s 12 1))
       (substr s 13 1)
       (substr s 12 2)))
    ((= fmt "sss")	; fixed 19-Apr-00
       (strcat (substr s 14 2)
	       (if (and (std-module-defined-p "STDLOCAL")
			(std-functionp stdlocal-ads-version))
    		 ;; Should we use the locale comma delimiter?
		 (std-get-locale-info "decimal_point")
	         ".")
	       (substr s 16))) 
    ((= fmt "ss")  (substr s 14 2))
    ((= fmt "s")
     (if (= "0" (substr s 14 1))
       (substr s 15 1)
       (substr s 14 2)))
    ((= fmt "AMPM")
     (if (> (std-datlst-hour datlst) 11) "PM" "AM"))
    ((= fmt "ampm")
     (if (> (std-datlst-hour datlst) 11) "pm" "am"))
    (T "")
  )
)


;;; ------------------------------------------------------------------73
;;; Simple Benchmarking:
;;; see STDDEBUG or (std-require 'STDDEBUG) for STD-TIME which
;;; provides much more features.

;;; Number of milliseconds since midnight 0:00
;;; We want a integer for better arithmetics.
;;; The milliseconds accuracy should be enough
(defun std-%date->milliseconds (date)
  ;; Truncate the days and use only the fractional
  ;; part of the seconds since today 0:00,
  ;; Convert it to milliseconds and integer.
  (std-round (* 86400000 (std-fraction date))))

;;; Try to improve it with an internal extension,
;;; also needed for stand-alone mode.

(if (std-functionp std-%get-internal-run-time)	; fileext v0.4
  (defun std-%portable-run-time ()		; => milliseconds
    (std-%get-internal-run-time))


  
  ;;; Any number interpretable as milliseconds
  (defun std-%portable-run-time ()
    (std-%date->milliseconds (getvar "DATE")))
  

)


;;; Starts stopclock, stores a number in milliseconds.
;;; Around 0:00 you will experience problems, so don't run it in
;;; plain AutoLISP over midnight!
(defun STD-TIMER-START ()
  (setq *STD:TIMER-START* (std-%portable-run-time)))

;;; Stops stopclock, stores and returns the difference to
;;; std-timer-start in milliseconds
;;; Around 0:00 you will experience problems, so don't run it in
;;; plain AutoLISP over midnight!
(defun STD-TIMER-STOP (timer)
  ; this is the difference, no stop-clock
  (- (std-%portable-run-time)
     (cond (timer) (*STD:TIMER-START*))))

;;; elapsed time in seconds (real)
(defun std-%timer-secs (timer)
  (/ (std-timer-stop timer) 1000.0))

;;; elapsed milliseconds formatted as string.
;;; we loose precision, rounding the last number
;;;   (1000-th -> 100-th)
(defun std-%timer-ms-str (msec / cdate)
  (setq	cdate (std-datlst->cdate
	        (std-canonize-datlst
	          (list 0 0 0 0 0 (/ msec 1000.0)))))
  (std-%format-date-string
    cdate
    (cond ((> msec 3600000) "hh:nn:sss")
	  ((> msec   60000) "nn:sss")
	  (T "sss"))))

;;; Sleeps for seconds, does nothing but waiting.
;;; Takes real and int. Uses AutoCAD's DATE or the internal millisecond
;;; timer. This is only approximate, not exact.

(if (std-functionp std-%get-internal-run-time)	; fileext v0.4
  (defun STD-SLEEP (secs / endt)
    (setq endt (+ (* 1000 secs) (std-%get-internal-run-time)))
    (while (< (std-%get-internal-run-time) endt) T))



  ;; I provide a simplier autolisp version if someone is
  ;; concerned about consing.
  (defun STD-SLEEP (secs / endt)
    (setq endt (+ (getvar "DATE") (/ secs 86400.0)));fixed
    (while (< (getvar "DATE") endt) T))


)


;;;(defun STD-%SLEEP-MS (ms / endt)
;;;  (setq endt (+ (getvar "DATE") (/ ms 8640000.0)))
;;;  (while (< (getvar "DATE") endt) T))

;;; ------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDTIME-SYMBOLS*))
(setq *STDTIME-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDTIME") ; first provide it. But before you may
				; call it, be sure to have all supporting
				; functions.

(std-%simple-require "STDSTR")   ; padding, trimming
(std-%simple-require "STDLIST")
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDINPUT.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $-*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:32:43 rurban>
;;; Copyright (c) 1993,98 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; User Input functions for the STDLIB
;;; Because user input functions are the frontend of a program to the
;;; user, this module is quite generic but powerful.
;;; Of course not all fancy input styles are supported and is
;;; questionable if and how hooks should be provided.
;;; But it is at least a common and better framework than the system
;;; provided one. 
;;;
;;; Status: Experimental
;;;   STD-YES-OR-NO-P missing.
;;;   STD-INP-SSGET and STD-SSGET are good candidates to be removed.
;;;   Maybe we need hooks for additional :before :after :around methods
;;;   How to name std-getscale? (or std-inp-getscale)
;;;   Translated messages from STDLOCAL supported.

;;; Problem:
;;;   User-interactive funcs cannot be tested automatically


;;; ==================================================================73
;;; $Log: STDINPUT.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:50:35  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; 2000-04-26 18:19:49 rurban
;;;   renumbered MSG ID's
;;; 2000-04-09 18:36:09 rurban
;;;   removed STD-YES-OR-NO-P
;;;
;;; ==================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDINPUT-SYMBOLS*
    '(STD-%INP-RETURN	  STD-GETINT	      STD-GETREAL
      STD-GETPOINT	  STD-GETCORNER	      STD-GETANGLE
      STD-GETANGLE-DEG	  STD-GETDIST	      STD-GETSCALE
      STD-INP-GETKWORD	  STD-GETSTRING	      STD-INP-EDIT-STRING
      STD-INP-GETNAME	  STD-INP-GETVECTOR   STD-INP-ENTSEL
      STD-INP-NENTSEL	  STD-INP-YESNO	      STD-YESNO
      STD-DCL-YESNO	  STD-GETKWORD	      STD-GET1POINT
      STD-GET2POINT	  STD-GET1DIST	      STD-GET2DIST
      STD-ENTSEL	  STD-NENTSEL	      STD-SSGET
      STD-INP-SSGET))))


(if (/= (type *STD:MAX-KEYWORDLEN*) 'INT)
  (setq *STD:MAX-KEYWORDLEN* 65))


;;; Formats message with options and defaults.
;;; On keyword list append the default,
;;; remove def from it or bracket it in the real position
;;; think of supporting different formatting styles:
;;;    "msg kwd1/kwd2/... <def>: "
;;; or "msg [kwd1/kwd2/...] <def>: "
(defun STD-%INP-FORMAT-MSG (msg def kwd / x l)
  ;;(setq zin (getvar "DIMZIN"))
  ;;(setvar "DIMZIN" 0)
  (setq msg (std-trim msg))
  ;;kwd -> list, remove def, kwd -> "/kwd1/kwd2"
  (setq kwd (cond
    ((std-string-not-empty-p kwd)
      (setq x (std-string->strlist kwd " "))
      (setq l (> (length x) 1))
      (std-strlist->string (std-remove def x) "/"))
    ((not kwd) "")
    ((listp kwd)
      (setq l (> (length kwd) 1))
      (std-strlist->string (std-remove def kwd) "/"))
  ))
  ;; use abbrevated option list?
  (if (and (boundp '*EXPERT*) (or (eq *EXPERT* T) (> *EXPERT* 1)))
    (while (and (stringp kwd)(> (strlen kwd) *STD:MAX-KEYWORDLEN*))
      (setq x 1
            kwd (std-strlist->string (std-butlast
                  (std-string->strlist kwd "/")) "/"))))
  (if (= x 1) (setq kwd (strcat kwd "/...")))	; set abbrevation
  (if (= (std-firstchar msg) "\n")(setq msg (substr msg 2)))
  (if (= (std-lastchar msg) ":")(setq msg (std-str-1 msg)))
  ;;(setvar "DIMZIN" zin)
  ;; format the prompt
  (setq msg (if *NEW-INPUT-STYLE*	; use []
   (if def
    (if (std-string-not-empty-p kwd)
      (strcat "\n" msg "[" kwd "/<" (std-tostr def) ">]: ")
      (strcat "\n" msg " <" (std-tostr def) ">: ")
    )
    (if (std-string-not-empty-p kwd)
      (strcat "\n" msg "[" kwd "]: ")
      (strcat "\n" msg ": ")
    )
   )
   ;; traditional style
   (if def
    (if (std-string-not-empty-p kwd)
      (strcat "\n" msg (if l " " "/") kwd "/<" (std-tostr def) ">: ")
      (strcat "\n" msg " <" (std-tostr def) ">: ")
    )
    (if (std-string-not-empty-p kwd)
      (strcat "\n" msg (if l " " "/") kwd ": ")
      (strcat "\n" msg ": ")
    )
   )
  ))
  msg
)

;;; Main user-input driver, Returns input or default value.
;;; To catch lisp functions we accept strings as well (initget 128)
;;; and check the string for valid lisp types returned.
(defun STD-%INP-RETURN (inp def type-pred can-be-string)
  (setq *STD:INP-LOOP* nil)
  (if (stringp inp)
    (if (member (std-firstchar inp) '("(" "'" "!"))
      ;; if lisp function, set a loop-again flag.
      (if (setq inp (if (= (std-firstchar inp) "!")
                        (eval (read (substr inp 2)))
                        (eval (read inp))))
        ;; check here for the correct return value
        (if (apply type-pred (list inp))
          inp
          (progn (setq *STD:INP-LOOP* 1) def)
        )
        ;; no return value, loop again
        (progn (setq *STD:INP-LOOP* 1) def)
      )
      ;; no lispfunction, so return the string or loop again
      (if can-be-string
        inp
        (progn (setq *STD:INP-LOOP* 1) inp)
      )
    )
    (if inp
      inp def
    )
  )
)

(defun STD-GETINT (flag kwd msg def / inp can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter number")))
  (setq *STD:INP-LOOP* 1)
  (setq msg (std-%inp-format-msg msg def kwd))
  (if def (setq flag (logand flag (~ 1))))
  (setq can-be-string
    (or (logand flag 128) (std-string-not-empty-p kwd)))
  (setq flag (logior flag 128))	;Allow lisp input for temp. functions
  ;(if (and def (/= (type def) 'INT)) (setq def nil))
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (while *STD:INP-LOOP*
    (initget flag kwd)
    (setq inp (getint msg))       ;use the GETINT function
    (std-%inp-return inp def 'std-integerp can-be-string)
  )
)

;;; std-getreal   User interface real number, but better use std-getdist
(defun STD-GETREAL (flag kwd msg def / inp zin can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter number")))
  (setq *STD:INP-LOOP* 1)
  (setq msg (std-%inp-format-msg msg def kwd))
  ;(if (and def (/= (type def) 'REAL)) (setq def nil))
  (setq can-be-string
    (or (logand flag 128) (std-string-not-empty-p kwd)))
  (if def (setq flag (logand flag (~ 1))))
  (setq flag (logior flag 128))		;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (while *STD:INP-LOOP*
    (initget flag kwd)
    (setq inp (getreal msg))           ;the GETREAL function
    (std-%inp-return inp def 'numberp can-be-string)
  )
)

;;; std-getpoint  User interface point function
(defun STD-GETPOINT (flag kwd msg def bpt / inp can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Select point")))
  (setq *STD:INP-LOOP* 1)
  (if (not (std-pointp def)) (setq def nil))
  (setq msg (std-%inp-format-msg msg def kwd))
  ;(if (and def (/= (type def) 'LIST)) (setq def nil))
  (setq can-be-string
    (or (logand flag 128) (std-string-not-empty-p kwd)))
  (if def (setq flag (logand flag (~ 1))))
  (setq flag (logior flag 128))		;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (graphscr)
  (while *STD:INP-LOOP*
    (initget flag kwd)
    (setq inp (if bpt (getpoint bpt msg) (getpoint msg)))
    (std-%inp-return inp def 'std-pointp can-be-string)
  )
)

;;; (std-getcorner flag kwd msg def bpt)
(defun STD-GETCORNER (flag kwd msg def bpt / inp can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Select second corner")))
  (setq *STD:INP-LOOP* 1)
  ;(if (not (std-pointp def)) (setq def nil))
  (setq msg (std-%inp-format-msg msg def kwd))
  (setq can-be-string
    (or (logand flag 128) (std-string-not-empty-p kwd)))
  (if def (setq flag (logand flag (~ 1))))
  (setq flag (logior flag 128))		;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (graphscr)
  (while *STD:INP-LOOP*
    (initget flag kwd)
    (setq inp (if bpt (getcorner bpt msg) (getcorner msg)))
    (std-%inp-return inp def 'std-pointp can-be-string)
  )
)

;;; std-getangle    User interface angle function
;;; accepts and returns radians, but shows degrees
(defun STD-GETANGLE (flag kwd msg def bpt / def1 inp can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter angle")))
  (setq *STD:INP-LOOP* 1)
  (std-var-push '(("DIMZIN" . 8)))
  (if def
    (if (numberp def)
      (setq def1 (angtos (std-rtd def)); added v0.4002
            flag (logand flag (~ 1)))
      (setq def1 nil)
    )
  )
  (setq msg (std-%inp-format-msg msg def1 kwd))
  (setq can-be-string
    (or (logand flag 128) (std-string-not-empty-p kwd)))
  (setq flag (logior flag 128))		;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))
  (std-var-pop)

  (graphscr)
  (while *STD:INP-LOOP*
    (initget flag kwd)
    (setq inp (if bpt (getangle bpt msg) (getangle msg)))
    (std-%inp-return inp def 'numberp can-be-string)
  )
)

;;; accepts and returns degrees
(defun STD-GETANGLE-DEG (flag kwd msg def bpt / inp can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter angle")))
  (setq *STD:INP-LOOP* 1)
  (if def
    (if (/= (type def) 'REAL)
      (setq def nil)
      (setq flag (logand flag (~ 1)))	;remove flag 1
    )
  )
  (setq msg (std-%inp-format-msg msg def kwd))
  (setq can-be-string
    (or (logand flag 128) (std-string-not-empty-p kwd)))
  (setq flag (logior flag 128))		;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (graphscr)
  (while *STD:INP-LOOP*
    (initget flag kwd)
    (setq inp (if bpt (getangle bpt msg) (getangle msg)))
    (if (= (type inp) 'REAL)   ;getangle returns radians
      (setq inp (std-rtd inp)))
    (std-%inp-return inp def 'numberp can-be-string)
  )
)

;;; std-getdist   User interface distance function
;;; BPT is base point (nil for none).
(defun STD-GETDIST (flag kwd msg def bpt / inp can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter distance")))
  (setq *STD:INP-LOOP* 1)
  (setq msg (std-%inp-format-msg msg def kwd))
  (if (and def (/= (type def) 'REAL)) (setq def nil))
  (setq can-be-string
    (or (logand flag 128) (std-string-not-empty-p kwd)))
  (if def (setq flag (logand flag (~ 1))))
  (setq flag (logior flag 128))		;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (graphscr)
  (while *STD:INP-LOOP*
    (initget flag kwd)
    (setq inp (if bpt (getdist bpt msg) (getdist msg)))
    (std-%inp-return inp def 'numberp can-be-string)
  )
)

;;; Scalefactor with Option Reference
(defun STD-GETSCALE (flag kwd msg def bpt / inp ref)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter scalefactor")))
  (setq *STD:INP-LOOP* 1)
  (setq ref (std-msg "Reference"))
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))
  (if (not (wcmatch kwd (strcat "* " ref " *")))
    (setq kwd (strcat kwd " " ref)))
  (graphscr)
  (if (= (setq inp (std-getdist flag kwd msg def bpt)) ref)
    (progn ; remove REF from kwd
      (* (/ 1.0
           (std-getdist 7 "" (std-msg "old length") nil nil))
        (std-getdist 7 "" (std-msg "new length") nil nil)
      )
    )
    inp
  )
)



;;; std-inp-getkword
;;; DEF, if any, must match one of the KWD strings
;;; flag (1 for no null, 0 for none) and KWD key word ("" for none)
;;; are same as for INITGET.
;;; MSG is the prompt string, to which a default string is added as
;;; <DEF> (nil or "" for none), and a : is added.
(defun STD-INP-GETKWORD (flag kwd msg def / inp can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter Option ")))
  (setq *STD:INP-LOOP* 1)
  (setq msg (std-%inp-format-msg msg def kwd))
  (if (and def (/= (type def) 'STR)) (setq def nil))
  (setq can-be-string
    (or (logand flag 128) (std-string-not-empty-p kwd)))
  (if def (setq flag (logand flag (~ 1)))) ;remove flag 1
  (setq flag (logior flag 128))		 ;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (while *STD:INP-LOOP*
    (initget flag kwd)
    (setq inp (getkword msg))           ;the GETKWORD function
    (std-%inp-return inp def 'stringp can-be-string)
  )
)

;;; (std-getstring flag msg def spflag)
;;; If flag && 1: no null "" input allowed, 0 for none,
;;; flag is ignored if DEF present.
;;; MSG is the prompt string, to which a default string is added as
;;; <DEF> (nil or "" for none), and a : is added.
;;; If SPFLAG T, spaces are allowed in string.
;;; see also (std-edit-text s)
(defun STD-GETSTRING (flag msg def spflag / inp)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter string")))
  (setq msg (std-%inp-format-msg msg def nil))
  (if def (setq flag (logand flag (~ 1))))       ;remove flag 1
  (if (std-string-not-empty-p def)
    (setq inp (getstring msg spflag)             ;ignore no null flag
          inp (if (or (not inp) (= inp ""))
		def inp))			 ;return default
    ;; empty def:
    (progn
      (if (= (logand 1 flag) 1)                  ; bit 1 set?
        (while (= "" (setq inp (getstring msg spflag))) ;no ""
          (prompt (std-msg "\ninvalid string")))
        (setq inp (getstring msg spflag))    ;else get input, "" ok
      )
    )
  )
  inp
)

;;; GUI one line textedit: edits textstring in DDEDIT dialog
(defun STD-INP-EDIT-STRING (msg def / l)
  (if (not def) (setq def ""))
  (if (std-string-not-empty-p msg)
    (prompt (std-%inp-format-msg msg def nil)))
  (if (entmake
        (list '(0 . "TEXT")
               (cons 1 def)
              '(40 . 1.0)
               (cons 10 (cadr (std-zoom-pts))) ; out of sight
      ))
    (progn
      (setq l (entlast))
      (command "_DDEDIT" (list l (getvar "LASTPOINT")) "")
      (setq def (cdr (assoc 1 (entget l))))	; fixed
      (entdel l)
    )
    (std-error (std-msg "(entmake) temp text"))
  )
  def
)


;;; std-inp-getname  User interface string for valid table names
;;; Enter a "." for an empty string.
(defun STD-INP-GETNAME (flag msg def / inp errstr)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Enter name")))
  (setq msg (std-%inp-format-msg msg def nil))
  (setq errstr (std-msg " invalid name"))
  (if def (setq flag (logand flag (~ 1)))) 	;remove flag 1

  (if (and def (/= def "") (not (std-%valid-name def)))
    (progn
      (princ " invalid chars in default")
      (setq def "")))
  (if (and def (/= def ""))
    (progn
      (if (= "." (setq inp (getstring msg)))(setq inp ""))
      (while (not (std-%valid-name inp))
        (prompt errstr)
        (setq inp (getstring msg)))
      (setq inp (if (= inp "") def inp))
    )
    (progn
      (if (= flag 1)
        (while (or (= "" (setq inp (getstring msg)))
                   (not (std-%valid-name inp)))
          (prompt errstr))
        (while (not (std-%valid-name (setq inp (getstring msg))))
          (prompt errstr))
    ) )
  )
  (if inp (strcase inp))
)

;;; (std-inp-getvector)        return a vector
;;; with bpt enter the 2.point (here the bpt is the default vector)
;;; without bpt enter 2 points or the 1.point is the vector
(defun STD-INP-GETVECTOR (flag kwd msg def bpt / inp)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Basepoint or displacement")))
  ;; def only with no bpt
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))
  (if bpt
    (setq inp (std-getpoint flag kwd msg (setq def nil) bpt))
    (if (and
        (setq bpt (std-getpoint (if def flag (logior flag 1))
			        kwd msg def nil))
        (listp bpt)
        (/= bpt def))
      (setq inp (std-getpoint flag kwd
	(std-msg "Second point of displacement") nil bpt))
    )
  )
  (if (and (stringp inp) (= (std-firstchar inp) "(")) ;)
    (setq inp (eval inp)))
  (cond
    (inp (mapcar (function -) inp bpt))
    (bpt bpt)
    (t def)
  )
)

;;; Adds no null and key words to "ENTSEL"
(defun STD-INP-ENTSEL (flag kwd msg def / inp ss can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Pick object")))
  (setq *STD:INP-LOOP* 1)
  (setq msg (std-%inp-format-msg msg def kwd))
  (if def (setq flag (logand flag (~ 1))))
  (setq can-be-string (logand flag 128))
  (setq flag (logior flag 128))		;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (graphscr)
  (while *STD:INP-LOOP*
    (setq inp
      (cond
        ((std-picksetp *STD:SS-PICKED*)
          (princ msg)
          (setq ss *STD:SS-PICKED* *STD:SS-PICKED* nil)
          (std-princ (list " " (sslength ss) " "
			   (std-msg "found")
			   ". "))
          (if (= 1 (sslength ss))
            (list (ssname ss 0) '(0 0 0))
          )
        )
        ((and (logand 1 (getvar "PICKFIRST"))
              (setq ss (ssget "_I"))
              (= 1 (sslength ss)))
          (princ (strcat msg " 1 " (std-msg "found") ". "))
          (list (ssname ss 0) '(0 0 0))
        )
        (T
          (initget flag kwd)
          (entsel msg)
        )
      )
    )
    (std-%inp-return inp def 'std-%pick-list-p can-be-string)
  )
)

(defun std-%pick-list-p (x)
  (and (listp x)
       (std-enamep (car x))))

;;; std-inp-nentsel - attrib's and dimtext's
(defun STD-INP-NENTSEL (flag kwd msg def / inp ss can-be-string)
  (if (std-string-not-empty-p msg) nil
    (setq msg (std-msg "Pick object")))
  (setq *STD:INP-LOOP* 1)
  (setq msg (std-%inp-format-msg msg def kwd))
  (if def (setq flag (logand flag (~ 1))))
  (setq can-be-string (logand flag 128))
  (setq flag (logior flag 128))		;Allow lisp input
  (if (listp kwd) (setq kwd (std-strlist->string kwd " ")))

  (graphscr)
  (while *STD:INP-LOOP*
    (setq inp
      (cond
        (*STD:SS-PICKED*
          (princ msg)
          (setq ss *STD:SS-PICKED* *STD:SS-PICKED* nil)
          (std-princ (list " " (sslength ss) " "
			   (std-msg "found")
			   ". "))
          (if (= 1 (sslength ss))
            (list (ssname ss 0) '(0 0 0))
          )
        )
        ((and (logand 1 (getvar "PICKFIRST"))
              (setq ss (ssget "_I"))
              (= 1 (sslength ss)))
          (princ (strcat msg " 1 " (std-msg "found") ". "))
          (list (ssname ss 0) '(0 0 0))
        )
        (T
          (initget flag kwd)
          (nentsel msg)
        )
      )
    )
    (std-%inp-return inp def 'std-%pick-list-p can-be-string)
  )
)

;;; Hmm, this is not quite correct code yet.
;;; Returns T or nil
(defun STD-INP-YESNO (msg def / inp kwds yes no)
  (setq yes (std-msg "Yes")
	no  (std-msg "No")
        kwds (list yes no))
  (setq def
        (cond
          ((member def (list nil
			     no (std-firstchar no)
			     "_No" "_N"))
            no)
          (def yes)
          (T   no)))
  (while (not (member
                (setq inp (std-inp-getkword 0 kwds msg def)) kwds)))
  (cond ((= inp yes) T)
       (T nil))
)

(defun STD-YESNO (msg def)
  (setq def (if (member def
	(list nil "_N" "_No"
	      (std-msg "No")
	      (std-firstchar (std-msg "No"))))
      nil
      t))
  (cond ((not (std-acad-connection-p)) def)
	;; to be fixed!
	((std-dclactive-p) (std-dcl-yesno msg def))
	((std-scriptactive-p) def)
        (T (std-inp-yesno msg def))
        ))

(defun STD-DCL-YESNO (msg def / dcldef dcl id result msgs i)
  (setq msgs (std-firstn 10 (std-%line-split msg 40)))
  (setq	dcldef (list
		 "yesno_row : column {"	"    : row {"
		 "        fixed_width = true;"
		 "        alignment = centered;"
		 (strcat "        : ok_button     { label = \"  "
			 (std-msg "Yes")
			 " \";}")
		 "         : spacer { width = 2; }"
		 (strcat "         : cancel_button { label = \"  "
			 (std-msg "No")
			 "  \";}}}")
		 "YESNO : dialog {"
		 (if (not def) "initial_focus = \"cancel\";" "")
		 "label=\"Please answer\";"
		 "width = 50;"))
  (setq i 1)
  (foreach s msgs
    (setq dcldef (append dcldef (list
		 (strcat ": text {key = \"yesno_msg" (itoa i) "\";"
			 "  label = " (std-prin1-to-string s) ";}"))))
	  i (1+ i))
  (setq dcldef (append dcldef '("spacer_1;" "yesno_row;}")))
  ;; This temp filename gets deleted on (std-var-restore)
  (setq dcl (std-%file-write-lines
              (std-filename-mktemp "YESNO.DCL") dcldef))
  ;; FCAD!
  (or (and (setq id (load_dialog dcl))
           (new_dialog "YESNO" id))
     (std-error "std-dcl-yesno aborted"))
  (setq i 1)
  (foreach s msgs
    (set_tile (strcat "yesno_msg" (itoa i))
	      (std-strcenter s 40)))
  (setq result (= 1 (start_dialog)))
  (unload_dialog id)
  result
)

;;; kdwlst either string list or (initget) string "Opt1 Opt2 ..."
(defun STD-GETKWORD (flag kwd def)
  (std-inp-getkword flag kwd "" def))

;;; Replaces (getpoint)
;;; (std-get1point msg)      without base point (1 argument)
;;; (std-get2point msg bpt)  with    base point (2 arguments)
(defun STD-GET1POINT (msg)
  (std-getpoint 0 "" msg nil nil))
(defun STD-GET2POINT (bpt msg)
  (std-getpoint 0 "" msg nil bpt))

;;; Replaces (getdist)
;;; (std-get1dist msg)      without base point (1 argument)
;;; (std-get2dist msg bpt)  with    base point (2 arguments)
(defun STD-GET1DIST (msg)
  (std-getdist 0 "" msg nil nil))
(defun STD-GET2DIST (bpt msg)
  (std-getdist 0 "" msg nil bpt))

;;; Replaces (entsel)
(defun STD-ENTSEL (msg)
  (std-inp-entsel 0 "" msg nil))
;;; Replaces (nentsel)
(defun STD-NENTSEL (msg)
  (std-inp-nentsel 0 "" msg nil))

(defun STD-%VALID-NAME (name)
  (not (wcmatch name "*[] `#`@`.`?`*`~`[`,`'!%^&()+={}|`\\:;\"<>/]*")))
(defun STD-%VALID-CHAR (ch)
  (not (wcmatch ch "[] `#`@`.`?`*`~`[`,`'!%^&()+={}|`\\:;\"<>/]")))

;;; see STDENT:
;;;(setq *ENTITY-TYPE-BREAKABLE* '("LINE" "POLYLINE" "ARC" "CIRCLE" "TRACE" "LWPOLYLINE"))
;;;(setq *ENTITY-TYPE-POLYLINE*  '("LWPOLYLINE" "POLYLINE"))
;;;(setq *ENTITY-TYPE-LINEAR*    '("LINE" "ARC" "POLYLINE" "LWPOLYLINE"))
;;;(setq *ENTITY-TYPE-TEXT*      '("TEXT" "MTEXT"))
;;;(setq *ENTITY-TYPE-TEXTALIKE* '("TEXT" "MTEXT" "ATTRIB" "ATTDEF" "DIMENSION"))

(setq *SSFILTER-INSERT*  '((0 . "INSERT")))
(setq *SSFILTER-POLY*    '((0 . "POLYLINE,LWPOLYLINE")))
(setq *SSFILTER-TEXT*    '((0 . "TEXT,MTEXT,ATTDEF")))
;;;(setq *SSFILTER-LINEAR*    (list (cons 0 (std-strlist->csstring *ENTITY-TYPE-LINEAR*))))
(setq *SSFILTER-TEXTALIKE* (list (cons 0 (std-strlist->csstring *ENTITY-TYPE-TEXTALIKE*))))

(defun STD-SSGET (msg lst)
  (std-inp-ssget msg lst nil))

;;; SSGET with defaults, like '(ssx), msg-prompt and filterlist
;;; DEF is usually a secondary filter function, like '(ssx) or '(filter)
;;; or simplier '(ssget "X") for all, ...
;;; Ex:
; (std-inp-ssget "sel" *SSFILTER-INSERT* '(ssget "X" '((0 . "INSERT"))))
(defun STD-INP-SSGET (msg lst def / ss)
  (if (stringp msg)(prompt msg))
  (cond
    (*STD:SS-PICKED*
      (setq ss *STD:SS-PICKED* *STD:SS-PICKED* nil)
      (std-princ (list " " (sslength ss) " "
		       (std-msg "found")
		       ". "))
      ;; check for locked entities!
      (cond (ai_ssget (ai_ssget ss))
	    (T ss))
    )
    ((and (logand 1 (getvar "PICKFIRST"))
          (setq ss (ssget "_I")))
      (std-princ (list " " (sslength ss) " "
		       (std-msg "found")
		       ". "))
      (cond (ai_ssget (ai_ssget ss))
	    (T ss))
    )
    (T
      (if (equal def '(ssx))
	(std-princ (list "\n"
          (std-msg "Select Objects"
	   )
          "/"
          (std-msg "SSX with ENTER"
	   )
          ": ")))
      (if (setq ss (if lst (ssget lst) (ssget)))
        (setq ss (cond (ai_ssget (ai_ssget ss))
                       (T ss)))
	(if def
          (setq ss (cond (ai_ssget (ai_ssget (eval def)))
	    	         (T (eval def))))
	)
      )
      (if (and ss *STD:MAX-SSLEN* (> (sslength ss) *STD:MAX-SSLEN*))
	(setvar "HIGHLIGHT" 0))
      ss
    )
  )
)

(if (not ai_ssget) (load "AI_UTILS"))


;;; ------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDINPUT-SYMBOLS*))
(setq *STDINPUT-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDINPUT");first provide it. But before you may
				;call it, be sure to have all supporting
				;functions.

(std-%simple-require "STDSTR")
(std-%simple-require "STDMATH")
(std-%simple-require "STDLISP")
;(std-%simple-require "STDENT")
;(std-%simple-require "STDACI")
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDENT.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:32:08 rurban>
;;; Copyright (c) 1995,98,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Entity functions for the STDLIB
;;; Some AutoCAD entity dependent code:
;;;   query/change entities, useful abstractions for properties
;;;   selection sets, subentities (attrib, vertex)

;;; STATUS:
;;; Entity independent code (points, segs) moved to STDPOINT.LSP
;;; STD-ENTMAKE-xxx moved to seperate ENTMAKE.LSP
;;; #+ SMALL uses no entget cache, no std-entmake-xxx, no std-dimstr
;;; A2000 allows safearrays: segments or pointlists may be coded as
;;;   destructive modifyable SAFEARRAY's (much faster). This will affect
;;;   the abstract point accessors in STDLIST then as well.
;;;   We should better provide a seperate STDPOINT module, for points,
;;;   segments and pointlist data structures and the geometric stuff.
;;;   (geometry and segments from STDENT; point stuff of STDLIST)



;;; $Log: STDENT.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; 2000-10-05 18:11:49 rurban
;;;   fixed STD-SSVAL, this never worked so far (!)
;;;   Thanks David Kozina
;;; Revision 0.5004  2000/09/20 12:50:35  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; 2000-06-22 10:36:34 rurban
;;;   fixed STD-BYLAYER, STD-BYLAYER for versions without GETCNAME (<r13?)
;;;   fixed STD-GETPTS for LWPOLYLINE, z != 0
;;; 2000-06-19 11:06:16 rurban
;;;   added STD-PLINE-SEGS (from STDPOINT)
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; 2000-06-18 22:55 rurban
;;;   fixed STD-GETPTS for PFACE
;;;   added STD-GETPTS, STD-GET-GETPTS-VERTEXMASK,
;;;     STD-SET-GETPTS-VERTEXMASK from STDPOINT
;;;
;;; internal functions start with std-%
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDENT-SYMBOLS*
    '(STD-GETVAL	   STD-ENTITY		STD-INIT-ENTGET
      STD-ENTGET	   STD-ENTDEL		STD-ENTMOD
      STD-ENT-LASTSUB-P	   STD-ENT-SUB-P	STD-MAIN-ENTITY
      STD-SUBENTS	   STD-ENTNEXT-MAIN	STD-NENTSEL-PARENT
      STD-GETTYPE	   STD-GETDXF1		STD-GETNAME
      STD-GETFLAG	   STD-GETLAY		STD-GETHANDLE
      STD-GETPT		   STD-GETENDPT		STD-GETCOLOR
      STD-GETCOLOR-STRING  STD-GETLTYPE		STD-ARC-PT
      STD-ARC-ENDPT	   STD-BYLAYER		STD-BYBLOCK
      STD-GETEXTTYPE	   STD-GETTEXTVAL	STD-DIMSTR
      STD-GETPTS	   STD-GET-GETPTS-VERTEXMASK
      STD-SET-GETPTS-VERTEXMASK			STD-PLINE-SEGS
      STD-ENTCHG	   STD-CHGLAY		STD-PUTTEXTVAL
      STD-FIX-LWPOLY	   STD-FLAGSETP		STD-ENTITY-TYPE-P
      STD-ENTITY-EXTTYPE-P STD-HATCH-P		STD-ENTITY-SUBTYPE-P
      STD-NENTSEL-BLOCK-P  STD-SSLIST		STD-SSMAP
      STD-SSVAL		   STD-ATTELE		STD-ATTCHG))))


;;; Some entity type constants:
;;; They are not constant, because you shoudl be able to add custom
;;; objects dynamically.
(setq *ENTITY-TYPE-BREAKABLE* '("LINE" "POLYLINE" "ARC" "CIRCLE" "TRACE"
                                "LWPOLYLINE" "SPLINE" "ELLIPSE"))
(setq *ENTITY-TYPE-POLYLINE*  '("LWPOLYLINE" "POLYLINE"))
;;; Forgot what this was for: applyable to area, perimeter or divide/measure?
;;; (setq *ENTITY-TYPE-LINEAR*  '("LINE" "ARC" "POLYLINE" "LWPOLYLINE"
;;;				"SPLINE" "ELLIPSE"))
(setq *ENTITY-TYPE-TEXT*      '("TEXT" "MTEXT"))
(setq *ENTITY-TYPE-TEXTALIKE* '("TEXT" "MTEXT" "ATTRIB" "ATTDEF"
                                "DIMENSION"))



;;; Value of group in assoc list of element
;;; which is either a plain dotted pair list, entsel-,
;;; entget-list or ENAME
;;; Cache the entget lookup for faster comparison.
(defun STD-GETVAL (grp ele)

  (cond
    ((and (listp ele) (/= (type (car ele)) 'ENAME))  ; entsel

      (cdr (assoc grp ele))		; already a entget-list

    )
    ((eq (std-entity ele) (car *ENTGET-CACHE*)) ; cache lookup
      (cdr (assoc grp (cdr *ENTGET-CACHE*))))
    (T  (cdr (assoc grp (std-entget ele)))))

)

;;; std-entity returns the entity name of an entget, entsel
;;; or ename argument. This is quite fast, at least faster than
;;; std-entget.
(defun STD-ENTITY (ele)
  (cond
    ((= (type ele) 'ENAME) ele)
    ((not ele) nil)
    ((not (listp ele)) nil)
    ((= (type (car ele)) 'ENAME) (car ele)); entsel
    (t (cdr (assoc -1 ele)))               ; dotted pair list
  )
)

(setq *ENTGET-CACHE* '(nil))    ; initialize the cache:
                                ; (ename . entget-list)
;;; Clear entget-cache
;;; This is needed in special usercode, when you change any entity property
;;; without any stdlib function, e.g. by not using std-entmod.
;;; You ere not advised to do so, but if so you have to call (STD-INIT-ENTGET)
;;; otherwise you will get old results, not reflecting your changes.
;;; (proposed and insisted by Serge Pashkov)
(defun STD-INIT-ENTGET ()
  (setq *ENTGET-CACHE* nil))



	; with standalone there's no acad connection,
			; no entities to work with

;;; Fast (entget (std-entity ele))
;;; Use this, if you're not sure, if ele is not already an entget list
;;; It is cached, because entget is slow.
;;; Thanks to Vladimir Nesterovsky, who mentioned that entget is slow.
(defun STD-ENTGET (ele)
  (cond
    ((eq ele (car *ENTGET-CACHE*))(cdr *ENTGET-CACHE*))
    ((= (type ele) 'ENAME)
     (setq *ENTGET-CACHE* (cons ele (entget ele)))
     (cdr *ENTGET-CACHE*))
    ((not (listp ele)) nil)
    ((= (type (car ele)) 'ENAME)   ; entsel
     (setq *ENTGET-CACHE* (cons ele (entget (car ele))))
     (cdr *ENTGET-CACHE*))
    (T ele)))                        ; already entget list

;;; Delete entity with cleaning the cache
;;; by Serge Pashkov
(defun STD-ENTDEL (ele)
  (if (setq ele (std-entity ele))
    (progn
      (if (eq ele (car *ENTGET-CACHE*))
        (setq *ENTGET-CACHE* nil))
      (entdel ele))))

;;; Modify entity with updating the cache, fixed for invalid entmod's
;;; by Serge Pashkov
(defun STD-ENTMOD (elist / ele)
  (if (setq ele (std-entity elist))
    (progn
      (if (eq ele (car *ENTGET-CACHE*))
        (setq *ENTGET-CACHE* nil))
      (entmod elist))))
;;;old:
;;; (defun STD-ENTMOD (elist / rslt)
;;;   (if (setq rslt (entmod elist))
;;;     (progn
;;;       (setq *ENTGET-CACHE* (cons (cdr (assoc -1 elist)) rslt))
;;;       rslt)))


;;; the rest doesn't bother us too much:
;;; only tbl*, get*, ent* and ss* funcs are omitted

;;; last element of a complex entity?
(defun STD-ENT-LASTSUB-P (ele)
  (std-entity-type-p ele '("SEQEND" "ENDBLK")))

;;; sub-element of a complex entity?
(defun STD-ENT-SUB-P (ele)
  (std-entity-type-p ele '("VERTEX" "ATTRIB" "ATTDEF")))

;;; main entity of sub or identity.
;;; returns ENAME of the main entity of any entity or subentity
;;; ele may be a (entget) list, a (entsel) list or of type 'ename
(defun STD-MAIN-ENTITY (ele)
  (setq ele (std-entity ele))
  (while (std-ent-sub-p ele)	    ; fixed
    (setq ele (entnext ele)))
  (if (std-ent-lastsub-p ele)
    (std-getval -2 ele)
    ele
  )
)

;;; return list of all sub entity ENAME's of a complex main-entity.
;;; (i.e. polyline vertices or block attributes)
;;; ele may be a (entget) list, a (entsel) list or of type 'ENAME
;;; Ex: (mapcar 'std-getname (std-subents (entsel "Blockattr's: ")))
;;; other definition in STDPOINT
(defun STD-SUBENTS (ele / lst)
  (setq ele (std-entity ele)
	lst nil)
  (if (> (std-getval 66 ele) 0)
    (progn
      (while (std-ent-sub-p (setq ele (entnext ele)))
	(setq lst (cons ele lst)))
      (reverse lst))))

;;; next main entity, fixed by Serge Pashkov
(defun STD-ENTNEXT-MAIN (ele)
  (if (std-ent-lastsub-p (setq ele (std-entity ele)))
    (entnext ele)
    (progn
      (setq ele (entnext ele))
      (while (std-ent-sub-p ele)
        (setq ele (entnext ele)))
      (if (std-ent-lastsub-p ele)
        (entnext ele)
        ele
      ))))


;;; returns ENAME of the parent block of an with (nentsel) picked element
(defun STD-NENTSEL-PARENT (ele)
  (if (std-nentsel-block-p ele)
    (car (nth 3 ele))
    (std-entity ele)))


;;; -------------------------------------------------------------------73
;;; Getting Entity Properties:

;;; (STD-GETTYPE ele) 		- group 0
;;; (STD-GETEXTTYPE ele)       - extended type info
;;; (STD-GETDXF1 ele) 		- group 1
;;; (STD-GETNAME ele) 		- group 2
;;; (STD-GETLAY	ele) 		- group 8
;;; (STD-GETHANDLE ele)		- group 5
;;; (STD-GETCOLOR ele)		- group 62 as number
;;; (STD-GETLTYPE ele)		- group 6 as string
;;; (STD-GETFLAG ele) 		- group 70
;;; (STD-GETPT	ele) 		- group 10 or poly/arc startpoint
;;; (STD-GETENDPT ele)		- group 11 or poly/arc endpoint
;;; (STD-ARC-PT	ele) 		- arc startpoint
;;; (STD-ARC-ENDPT ele)		- arc endpoint
;;; (STD-GETTEXTVAL ele)        - visible textvalue of TEXT,MTEXT,
;;;                                 DIMENSION,ATTDEF,ATTRIB
;;; (STD-DIMSTR ele)		- calculated dimension textvalue
;;; (STD-GETPTS	ele)		- pointlist from entity

(defun STD-GETTYPE (ele)   (std-getval  0 ele))
(defun STD-GETDXF1 (ele)   (std-getval  1 ele))
(defun STD-GETNAME (ele)   (std-getval  2 ele))
(defun STD-GETFLAG (ele)   (std-getval 70 ele))
(defun STD-GETLAY (ele)    (std-getval  8 ele))
(defun STD-GETHANDLE (ele) (std-getval  5 ele))

;;; on polylines and arcs the startpoint
(defun STD-GETPT (ele)
  (cond
    ((std-entity-type-p ele *ENTITY-TYPE-POLYLINE*)
      (car (std-getpts ele)))
    ((std-entity-type-p ele "ARC")
      (std-arc-pt ele))
    (T (std-getval 10 ele))))

;;; on polylines and arcs the endpoint
(defun STD-GETENDPT (ele)
  (cond
    ;; ((std-entity-type-p ele "VERTEX")    ;of a segment
    ;;   (std-require 'POLY)
    ;;   (std-nextpolypt ele))
    ((std-entity-type-p ele *ENTITY-TYPE-POLYLINE*)
      (last (std-getpts ele)))
    ((std-entity-type-p ele "ARC")
      (std-arc-endpt ele))
    (T (std-getval 11 ele))))

(defun STD-GETCOLOR (ele)
  (std-getval 62 ele))

(defun STD-GETCOLOR-STRING (ele)
  (std-%simple-require "STDTBL")
  (std-%color-num->string (cond ((std-getcolor ele)) (T 256))))

(defun STD-GETLTYPE (ele / s)
  (if (stringp (setq s (std-getval 6 ele))) s (std-bylayer)))

(defun STD-ARC-PT (ele)
  (trans
    (polar (std-getval 10 ele)(std-getval 50 ele)(std-getval 40 ele))
    (std-entity ele) 0))

(defun STD-ARC-ENDPT (ele)
  (trans
    (polar (std-getval 10 ele) (std-getval 51 ele) (std-getval 40 ele))
    (std-entity ele) 0))

;;; Returns localized string for "BYLAYER".
;;; (When was GETCNAME introduced? R13?) fixed for earlier versions
(defun STD-BYLAYER ()
  (if (and (boundp 'getcname)(getcname "_BYLAYER"))
    (getcname "_BYLAYER")
    (cond ((std-module-defined-p "STDLOCAL")
           (std-%translate-string "BYLAYER"))
          (T "BYLAYER"))))

;;; Returns localized string for "BYBLOCK", fixed.
(defun STD-BYBLOCK ()
  (if (and (boundp 'getcname)(getcname "_BYBLOCK"))
    (getcname "_BYBLOCK")
    (cond ((std-module-defined-p "STDLOCAL")
           (std-%translate-string "BYBLOCK"))
          (T "BYBLOCK"))))

;;; Extended type string for polylines, special blocks
;;; adds new types:
;;;   PFACE, MESH, 3DPOLY, SPLINE
;;;   SOLMESH, SOLWIRE, MINSERT, XREF, HATCH
(defun STD-GETEXTTYPE (ele / typ tbl)
  (setq ele (std-entity ele))
  (cond
    ((= (setq typ (std-gettype ele)) "POLYLINE")
      (cond
        ((std-flagsetp 64 ele) "PFACE")
        ((std-flagsetp 32 ele) "MESH")
        ((std-flagsetp 16 ele) "MESH")
        ((std-flagsetp 8 ele)  "3DPOLY")
        ((std-flagsetp 4 ele)  "SPLINE")
        (T typ)
      )
    )
    ((/= typ "INSERT") typ)
    ;((std-solmesh-p ele) "SOLMESH")	; old AME Mesh
    ;((std-solwire-p ele) "SOLWIRE")	; old AME Wireframe
    (T

      (setq tbl (tblsearch "BLOCK" (std-getval 2 ele)))

      (cond
        ((std-hatch-p ele) "HATCH")
        ((or (> (std-getval 70 ele) 1)
             (> (std-getval 71 ele) 1)) "MINSERT")
        ((std-flagsetp 4 tbl) "XREF")
        (T typ)
      )
    )
  )
)

;;; Returns the visible textvalue from any TEXT element
;;; from a DIMENSION the calculated text if default text
;;; from a MTEXT all textlines with formatting info
(defun STD-GETTEXTVAL (ele / val x)
  (cond
    ;; dimension only
    ((and (= (type ele) 'ENAME)
          (std-entity-type-p ele "DIMENSION"))
      (if (member (setq val (std-getval 1 ele)) '("" "<>"))

        (std-dimstr ele)	; not yet implemented!

        val
      )
    )
    ((std-entity-type-p ele "MTEXT")
      (setq val "")
      (foreach x (std-entget ele)
	(if (member (car x) '(3 1))
	  (setq val (strcat val (cdr x)))))
      val
    )
    ((and (consp ele)
          (std-entity-type-p (car (cadddr ele)) "DIMENSION")
     )
     (setq val (if (std-entity-type-p ele '("TEXT" "MTEXT"))
       (std-getval 1 ele)

       (std-dimstr (car (nth 3 ele)))    ;calcs the text

     ))
     ;; strip MTEXT formatting pre- and suffices
     (if (and (std-entity-type-p ele "MTEXT") (= "\\" (substr val 1 1)))
       (setq val (substr val (1+ (std-strpos ";" val))))
     )
     val
    )
    ;; blocknames? naa!

    ((std-entity-type-p ele "ATTDEF")
     (std-getval 2 ele)
    )
    (T
     (std-getval 1 ele)
    )
  )
)


;;; Recalcs geometrically the dimension text, returns the string.
;;; Status: Tolerance missing, not tested
(defun STD-DIMSTR (ele / s d p lfac p13 p14 p15 rnd)
  (if (std-entity-type-p ele "DIMENSION")
    (progn
      (setq s (std-getval 1 ele)
;;; on standalone ignore lfac and rnd

            lfac
              (cond
                ((not (std-getval 3 ele))
		  (getvar "DIMLFAC"))
                ((= (std-getval 3 ele) "*UNNAMED")
                  (getvar "DIMLFAC"))
		(T
                 (std-getval 144
			     (tblsearch "DIMSTYLE" (std-getval 3 ele)))
		)
		;; error: check also overriding values in attached xdata!
              )
	    rnd (getvar "DIMRND")

            p13 (std-getval 13 ele)
            p14 (std-getval 14 ele)
            p15 (std-getval 15 ele)
      )
      (cond
        ;; ignore _NEWTEXT dimension texts. we want the geometric,not the
        ;; textual info
    ;;   ((and (/= s "") (/= s "<>"))
    ;;     s
    ;;   )
        ;; normal generated dimtxt
        ((= (logior (std-getval 70 ele) 224) 224)      ; rotated,hor,ver
          ;; rotation angle!
          (setq p (mapcar (function -) p14 p13)
                d (* (distance p13 p14)
                    (cos (- (angle p13 p14) (std-getval 50 ele)))
                     lfac
                  )
          )
          (rtos (std-%dimround (abs d) rnd))
        )
        ((= (logior (std-getval 70 ele) 224) (+ 224 1))     ; aligned
          (setq d (* (distance p13 p14) lfac))
          (rtos (std-%dimround (abs d) rnd))
        )
        ((= (logior (std-getval 70 ele) 224) (+ 224 3))     ; diameter
          (setq d (* (distance (std-getpt ele) p15) lfac))
          (strcat "%%c" (rtos (std-%dimround (abs d) rnd)))
        )
        ((= (logior (std-getval 70 ele) 224) (+ 224 2))     ; angle
          (setq d (- (angle p13 p14) (angle p13 p15)))
          (angtos (std-%dimround (abs d) rnd))
        )
        ((= (logior (std-getval 70 ele) 224) (+ 224 5))     ; 3P Angle
          (setq d (- (angle p15 p14) (angle p15 p13)))
          (angtos (std-%dimround (abs d) rnd))
        )
        ((= (logior (std-getval 70 ele) 224) (+ 224 6))     ; Ordinate
          (setq p (trans p13 0 1)
                d
            (if (std-flagsetp ele 64)      ; Type X
              (car p)
              (cadr p)                                    ; Type Y
            )
          )
          (rtos (std-%dimround (abs (* lfac d)) rnd))
        )
        (T s)
      )
    )
  )
)

;;; rounds real number as in DIM with DIMRND
;;; Ex: (std-%dimround 1.43 0.5) => 1.0
(defun std-%dimround (r rnd)
  (std-require 'STDMATH)
  (if (zerop rnd)
    r
    (* (std-round (/ r rnd)) rnd)
  )
)



;;; Return only some assoc values in the list (for LWPOLYLINE)
;;; (std-group-only 10 '((0 . x)(8 . y)(10 . p1)(10 . p2)(40 . 0)))
;;;   => (p1 p2)

(defun std-%group-only (_grp lst)
  (mapcar (function cdr)
    (std-remove-if-not
     ;; acomp will fail here!
  (function	
 ;)
        (lambda (_pair)
	  (if (listp _grp)
	    (member (car _pair) _grp)
	    (= (car _pair) _grp))))
      lst)))


;;; For VL provide optimized versions:




;;; (STD-GETPTS ele) => pts
;;; Return the vertex list of a polyline or of any other element.
;;; Always returns a list of WCS points. For ARC's the two endpoints.
;;; For polylines the current vertex filtermask is used.
(defun STD-GETPTS (_ele / pts _elist mask _z)
  (setq _elist (std-entget _ele)
	_ele   (std-entity _ele))		;force type ENAME
  (cond

    ;; new: PFACE without facerecords, v0.5002
    ((and (std-entity-type-p _elist "POLYLINE")(std-flagsetp 64 _elist))
      (setq pts nil)
      (while (std-entity-type-p (setq _ele (entnext _ele)) "VERTEX")
        ;;accept only 192 vertices
        (if (= 192 (std-getflag _ele))
          (setq pts (cons (trans (std-getval 10 _ele) _ele 0) pts))))
      (reverse pts))
    ((std-entity-type-p _elist "POLYLINE")
      ;(setq z (cond ((std-getval 38 _elist))((z-of (std-getval 10 _elist)))))
      (setq pts nil mask (std-get-getpts-vertexmask)); reject bits
      (while (std-entity-type-p (setq _ele (entnext _ele)) "VERTEX")
        ;;accept only those subentities which have all mask bits clear
        (if (zerop (logand mask (std-getflag _ele)))	; changed
          (setq pts (cons (trans (std-getval 10 _ele) _ele 0) pts))))
      (reverse pts))

    ;; Special case: you have to map it, assoc finds only the first.
    ;; Fix a LWPOLYLINE bug in R14: internally stored as 2d point,
    ;;   (entget) returns fantasy z-values.
    ((std-entity-type-p _elist "LWPOLYLINE")
      (setq _z (cond ((std-getval 38 _elist))((z-of (std-getval 10 _elist)))))
      (mapcar
	; the R14 z bug is fixed here explicitly
        (function (lambda (_pt) (trans (list (car _pt)(cadr _pt) _z) _ele 0)))
        (std-%group-only 10 _elist)))
    ;; insert here possible other types, such as:
    ((std-entity-type-p _ele '("TEXT" "CIRCLE"))
      (list (std-getpt _elist)))
    ((std-entity-type-p _elist "ARC")	; added v3.017
      (list (std-arc-pt _elist)(std-arc-endpt _elist)))
    ;; TODO: HATCH boundary?
    (T
      (apply (function append)
	(mapcar
	  (function (lambda (_n / _p)
            (if (setq _p (std-getval _n _elist)) (list _p))))
         '(10 11 12 13)))
    )
  )
)

;;; Returns group codes of the all subentities (vertices, attributes)
;;; of the complex element as list.
;;; Rejection of subentities is ruled out by flag *STD:GETPTS-VERTEXMASK*
(defun std-%cplx-list (grp ele / lst mask)
  (if (= 1 (std-getval 66 ele))		;logand?
    (progn
      (setq mask (std-get-getpts-vertexmask))
      (setq ele (entnext (std-entity ele))
            lst nil)
      (while (and ele (not (std-ent-lastsub-p ele)))
        ;;accept only those subentities which have all mask bits clear
        (if (zerop (logand mask (std-getflag ele)))
          (setq lst (cons (std-getval grp ele) lst)))
        (setq ele (entnext ele)))
      (reverse lst))))

;;; List of all subelement enames, by Serge Pashkov
;;; moved to STDENT
;(defun STD-SUBENTS (ele / *STD:GETPTS-VERTEXMASK*)
  ;;we are interesting in all entities so temp. clear mask
;  (setq *STD:GETPTS-VERTEXMASK* 0)
;  (std-%cplx-list -1 (std-main-entity ele)))

;;; Get current vertex bitmask, by Serge Pashkov
;;; Functional interface to *STD:GETPTS-VERTEXMASK*
(defun STD-GET-GETPTS-VERTEXMASK ()
  *STD:GETPTS-VERTEXMASK*)

;;; Set current vertex bitmask filter for (STD-GETPTS) and (STD-SUBENTS)
;;; bitmask 16 would reject all spline frame points,
;;; bitmask 9 reject every computer-added vertices,
;;; bitmask 0 rejects none.
;;; Ex:
;;;  (std-set-getpts-vertexmask '(1 8)) => 9
;;;  (std-set-getpts-vertexmask 16) => 16
;;;  (std-set-getpts-vertexmask '(1 aa)) => 0
;;;  (std-set-getpts-vertexmask nil) => 0
;;; by Serge Pashkov
(defun STD-SET-GETPTS-VERTEXMASK (mask)
  (setq *STD:GETPTS-VERTEXMASK*
    (cond
      ((numberp mask) mask)
      ((null mask) 0)
      ((std-number-list-p mask)
        (apply (function logior) mask))
      (T 0))))

;;; Initialize vertex bitmask used for rejecting vertices.
;;; Ex: 0 - all vertices passed
;;;     9 = 1+8 - reject spline-fit and curve-fit vertices
;;;    16 - reject spline frame vertices
;;; by Serge Pashkov
(std-set-getpts-vertexmask 0)

;;; (STD-PLINE-SEGS pline) => segs
;;; creates a segment list for the polyline
;;; as a list of (p1 p2 [bulge]). WCS return values.
;;; A straight line has no bulge.
(defun STD-PLINE-SEGS (pline / pts segs)
  (setq pline (std-entget pline))
  (setq segs
    (mapcar (function std-make-seg)
      (setq pts (std-getpts pline))
      (std-rotate-left pts)
      (if (std-entity-type-p pline "LWPOLYLINE")
        (std-%group-only 42 pline)
        (std-%cplx-list 42 pline))
      ))
  (if (std-flagsetp 1 pline)	; closed
    segs			; fixed
    (std-butlast segs)))  ; open: without the last segment

;;; -------------------------------------------------------------------73
;;; Entity Modifiers:

;;; (STD-ENTCHG ele types grp val)
;;; (STD-CHGLAY	ele lay)
;;; (STD-PUTTEXTVAL	ele val)	- to dim, text, attrib
;;; (STD-FIX-LWPOLY	ele)		- fixes z bug


;;; Change dxf group of entity to value if it is of this type.
;;; types is a list of strings or if nil at takes all types.
;;; Returns 1 or 0 to be able to use it in increment functions.
;;; nr-value maybe atoms or list, which must match then.
;;; Ex:
;;;   (foreach ele (std-sslist (ssget))
;;;     (setq i (+ i (std-entchg ele '("LINE") 62 7))))
;;;   (std-princ (list i " lines changed to color white"))
;;; or: (std-entchg ele nil (62 8) (7 "0")))))
(defun STD-ENTCHG (ele types nr value / _elist new old)
  (if (and
        (setq _elist (entget (std-entity ele)))   ;not deleted entity
        value
        ;; don't use std- functions for performance reasons
        (or (not types) (member (cdr (assoc 0 _elist)) types))
        ;; fix arguments to lists
        (progn
          (if (atom nr) (setq nr (list nr) value (list value)))
          (setq new (mapcar (function cons) nr value)
                old (mapcar (function (lambda (n)
                      (cons n (cdr (assoc n _elist))))) nr))
          (not (equal old new)))
        (progn
          ;; This should be set at initialization time, T as long as
          ;; this is not fixed, also affects hatches.
          (if *R14-LWPOLY-BUG* (setq _elist (std-fix-lwpoly _elist)))
          (mapcar (function (lambda (n o)
            ;;make subst else append (cons doesn't work on LWPOLYLINE!)
            (setq _elist (if (cdr o)
			   (subst n o _elist)
			   (append _elist (list n)))) ;fixed
          )) new old)
          (std-entmod _elist)
        )
      )
    1
    0
  )
)


;;; previous definition:

;;;(defun STD-ENTCHG (ele types nr value / _elist new old)
;;;
;;;  (if (and
;;;	(setq _elist (entget (std-entity ele)))   ;not deleted entity
;;;        value
;;;	;; don't use std- functions for performance reasons
;;;        (or (not types) (member (cdr (assoc 0 _elist)) types))
;;;	;; fix arguments to lists
;;;	(progn
;;;	  (if (atom nr) (setq nr (list nr) value (list value)))
;;;	  (setq new (mapcar 'cons nr value)
;;;		old (mapcar (function (lambda (n)
;;;		      (cons n (cdr (assoc n _elist))))) nr))
;;;	  (not (equal old new)))
;;;        (progn
;;;	  ;; This should be set at initialization time, T as long as
;;;	  ;; this is not fixed, also affects hatches.
;;;          (if *R14-LWPOLY-BUG* (setq _elist (std-fix-lwpoly _elist)))
;;;          (mapcar (function (lambda (n o)
;;;	    (setq _elist (subst n o _elist))))
;;;	    new old)
;;;	  (std-entmod _elist)
;;;	)
;;;      )
;;;    1
;;;	; let it return 0
;;;    0
;;;
;;;  )
;;;
;;;)

;;; changes the layer for the element
(defun STD-CHGLAY (ele lay)
  (std-entchg ele nil '(8) (list lay))
  lay
)

;;; sets new text value for dim, text, attrib,...
(defun STD-PUTTEXTVAL (val ele)
  (cond
    ((std-entity-subtype-p ele "DIMENSION")
     (std-entchg (std-main-entity ele) nil 1 val)
    )
    ((std-entity-type-p ele "ATTDEF")
     (std-entchg ele nil 2 val)
    )
    (T
     (std-entchg ele nil 1 val)
    )
  )
)

;;; fixing a R14 bug, random z values for points in entget lists
;;; => fixed elist of entity for LWPOLYLINE and HATCH
(defun STD-FIX-LWPOLY (ele / _z _e)
  (if (std-entity-type-p ele '("HATCH" "LWPOLYLINE"))
    (progn
      (setq _z (cond ((std-getval 38 ele))(T 0.0)))
      (mapcar (function (lambda (_e)
	(if (= 10 (car _e))
	  (cons 10 (std-new-z (cdr _e) _z))	; fixed
	  _e)))
	(std-entget ele)
      )
    )
    (std-entget ele)	; more or less unchanged
  )
)


;;; -------------------------------------------------------------------73
;;; Predicates:

;;; (STD-ENAMEP	ele)			; entity?
;;; (STD-PICKSETP	ss)		; pickset?
;;; (STD-FLAGSETP	val ele)	; val in flag 70?
;;; (STD-ENTITY-TYPE-P        ele type)       ;
;;;   former std-istypep
;;; (STD-ENTITY-EXTTYPE-P ele type)          ;
;;;   former std-istypep-ext
;;; (STD-ENTITY-SUBTYPE-P  ele typlst)         ;for (nentsel) objects
;;;   former std-issubtype
;;; (STD-HATCH-P        ele)            ;
;;;   former std-ishatch
;;; (STD-NENTSEL-BLOCK-P pick)		;

;;; see STDLISP
;;; (defun STD-ENAMEP (ele)
;;;   (eq (type ele) 'ENAME))
;;; (defun STD-PICKSETP (ss)
;;;   (eq (type ss) 'PICKSET))

;;; STD-FLAGSETP  - bitvalue val in flag of element set?
;;; Ex: (std-flagsetp 1 pline)   => T if closed
;;; Ex: (std-flagsetp 16 vertex) => T if spline control point
(defun STD-FLAGSETP (val ele)
  (std-bitsetp val (std-getflag ele)))


;;; type argument must be uppercase!
(defun STD-ENTITY-TYPE-P (ele typlst)
  (cond
    ((listp typlst)   (member (std-gettype ele) typlst))
    ((stringp typlst) (= (std-gettype ele) typlst))
    (T nil)))

;;; type argument must be uppercase!
(defun STD-ENTITY-EXTTYPE-P (ele typlst)
  (cond
    ((listp typlst)   (member (std-getexttype ele) typlst))
    ((stringp typlst) (= (std-getexttype ele) typlst))
    (T nil)))

;;; r14 hatch object or previous hatch block (associative or not)
(defun STD-HATCH-P (ele / eed)
  (or
    (std-entity-type-p ele "HATCH")             ; r14: native object or

    (and ele					; pre-r14 hatch insert
       (= "INSERT" (std-gettype ele))		; with EED info
       (= "*" (substr (std-getval 2 ele) 1 1))
       (setq eed (cdadr (assoc -3 (entget (std-entity ele) '("ACAD")))))
       (= "HATCH" (cdr (assoc 1000 eed)))
    )

  )
)

;;; for (nentsel) objects
(defun STD-ENTITY-SUBTYPE-P (ele typlst)
  (std-entity-type-p (std-nentsel-parent ele) typlst))

(defun STD-NENTSEL-BLOCK-P (ele)
      (and
        (listp ele)(= 4 (length ele))
        (= (type (car (nth 3 ele))) 'ENAME)))

;;; -------------------------------------------------------------------73
;;; Selection sets:

;;; (STD-SSLIST	ss)		; selset -> list of ents
;;; (STD-SSMAP	func ss)	; apply a function to each element
;;; (STD-SSVAL	ss grp)		; common value in selset

;;; Selection Set => ordered list of entities
(defun STD-SSLIST (ss / n lst)


  (if (eq 'PICKSET (type ss))
    (repeat (setq n (fix (sslength ss)))	; fixed
      (setq lst (cons (ssname ss (setq n (1- n))) lst))))

)

;;; Apply a function to each ent in ss, in reversed order
;;; Faster than with std-sslist, but not so easy to understand.
;;; in the FAQ named: SSAPPLY
;;; Ex: (std-ssmap 'entupd (ssget))   ; regenerate only some entities
(defun STD-SSMAP (func ss / n)


  (if (eq 'PICKSET (type ss))
    (if (> (sslength ss) *MAX-ARGS-LIMIT*)
      (std-error (std-msg "maximal number of arguments exceeded"))
      (repeat (setq n (fix (sslength ss)))	; fixed
        (apply func (list (ssname ss (setq n (1- n))))))))

)

;;; std-ssval returns the common value of the group as string
;;; or the special strings "varies" or "too much".
;;; Usually used in user prompts.
(defun STD-SSVAL (ss _grp / values)
  (std-default-type '*STD:MAX-SSLEN* 600)
  (cond

    ((> (sslength ss) *STD:MAX-SSLEN*)
      (std-msg "too much"))	; only english msg?
    ((apply (function =)
	    (setq values (mapcar
  
			  (function	
 ;)
			   (lambda (ele) (std-getval _grp ele)))
			  (std-sslist ss))))
     (if (<= 50 _grp 59)
         (angtos (car values))	        ; treat angles special
         (std-princ-to-string (car values))))
	; use any string in standalone for testing
    (T (std-msg "varies"))	; only english msg?
  )
)

;;; -------------------------------------------------------------------73
;;; Attributes:
;;;
;;; (STD-ATTELE	block name)	; attribute list
;;; (STD-ATTCHG	block name val)	; change attribute value

;;; std-attele returns entget-list of attribute attname (STRING) in
;;; ele or nil if not found
;;;    (std-attele (entsel) "DATE")
(defun STD-ATTELE (block attname / res ele elist)

  (cond
    ;; block insertion
    ((= 1 (std-getval 66 block))
      (setq ele (entnext (std-entity block)))
      (while (and ele
                  (std-entity-type-p (setq elist (entget ele)) "ATTRIB"))
        (if (std-string-equal (std-getval 2 elist) attname)
          (setq res elist ele nil)
          (setq ele (entnext ele))
        )
      )
    )
    ;; table definition
    ((and (setq ele (std-getval -2 block))
	  (std-flagsetp 2 ele))
      (while (and ele
                  (std-entity-type-p (setq elist (entget ele)) "ATTDEF"))
        (if (std-string-equal (std-getval 2 elist) attname)
          (setq res elist ele nil)
          (setq ele (entnext ele))
        )
      )
    )
  )
	; return nil
  res
)

;;; Change the attribute value (group 1) of INSERT entity to new value.
;;; Takes also ATTRIB entities.
;;; Fixed by Raymond Barlow. 10/27/99.
;;;   (std-attchg (entsel) "DATE" (std-today))
(defun STD-ATTCHG (block name val / att)
  (if (std-entity-type-p block "ATTRIB")
    (setq att block)
    (setq att (std-attele block name))
  )
  (if att
    (std-entmod (subst (cons 1 val) (cons 1 (std-getval 1 att)) att))))

;;; -------------------------------------------------------------------73
;;; Geometry: 		entity independent funcs moved to STDPOINT
;;; Segments, Bulges:   completely moved to STDPOINT (entity independent)

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDENT-SYMBOLS*))
(setq *STDENT-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDENT")   ; first provide it, but before you may
				 ; call it, be sure to have all supporting
				 ; functions:

;;; If all required STDLIB modules are contained in one project,
;;; we don't have to load it explicitly.
;;; STDLIB.FAS already contains it!
;;;(if (not *STD:%PROJECT*) (progn

(std-%simple-require "STDINIT")
(std-%simple-require "STDLIST")
(std-%simple-require "STDLISP")
(std-%simple-require "STDSTR")
(std-%simple-require "STDFILE")
(std-%simple-require "STDMATH")
(std-%simple-require "STDPOINT")
(std-%simple-require "STDTIME") ;?

;;; (std-%simple-require "STDACI")   ; almost all

;;;))

;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDTBL.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:30:56 rurban>
;;; Copyright (c) 1994,1998,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Table functions for the STDLIB

;;; STATUS:
;;; Not tested by RUNTEST at all, but in heavy production use since
;;;   1992-1999
;;; Several FCAD fixes, but not complete yet.
;;;

;;; $Log: STDTBL.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; 2000-10-18 14:47:12 rurban
;;;   renamed std-%zoom-if-apply to STD-ZOOM-IF-APPLY
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; 2000-08-30 10:32:18 rurban
;;;   fixed STD-ZOOM-IF, STD-ZOOM-EXTENTS for DCLACTIVE
;;;   added STD-%ZOOM-IF-APPLY
;;; 2000-07-22 17:15:18 rurban
;;;   added layer arg as list to layer functions
;;;   some return the last, some the whole list
;;;     (thanks David for reminding me)
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; 2000-04-15 11:06:00 rurban
;;;   STD-RESTORE-UCS added arbitrary strings,
;;;   like "World" "Prev" (pro: flexibility), but may cause errors in
;;;   std-error (contra: stability). Fixed transparent bug
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;; 2000-04-07 15:00:07 rurban
;;;   removed ENTMAKE dependency
;;; 2000-04-05 09:04:48 rurban
;;;   fixed STD-ZOOM-IF for UCS
;;; 2000-03-14 22:09:57 rurban
;;;   fixed STD-RESTORE-UCS, fixed STD-TBL-LAYER for "*"
;;;   reversed output from STD-TBL-PROPS, STD-TBL-NAMES, STD-TBL,
;;;     STD-TBL-LAYER to reflect the normal DB order.
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDTBL-SYMBOLS*
    '(STD-TBL-P		    STD-TBL-GROUP	  STD-TBL-NAMES
      STD-TBL-PROPS	    STD-TBL		  STD-TBL-LAYER
      STD-TBL-TMPNAME	    STD-PIX->UNITS	  STD-UNITS->PIX
      STD-VIEW-PLAN-P	    STD-VIEW-PERSP-P	  STD-PAPERSPACE-P
      STD-VPORT-NEXT	    STD-ZOOM-PTS	  STD-ZOOM-IF
      STD-ZOOM-EXTENTS	    STD-TBL-RENAME	  STD-NOT-WORLDUCS-P
      STD-LAYER-VISIBLE-P   STD-LAYER-ON-P	  STD-LAYER-FROZEN-P
      STD-SAVE-LAYER	    STD-RESTORE-LAYER	  STD-SAVE-UCS
      STD-RESTORE-UCS	    STD-SAVE-ZOOM	  STD-RESTORE-ZOOM
      STD-SAVE-VIEW	    STD-RESTORE-VIEW	  STD-SAVE-VPORTS
      STD-RESTORE-VPORTS    STD-SAVE-UNDO	  STD-RESTORE-UNDO
      STD-DISABLE-UNDO	    STD-ENABLE-UNDO	  STD-LAYER-LOCK
      STD-LAYER-UNLOCK	    STD-LAYER-FREEZE-VP	  STD-LAYER-THAW-VP
      STD-LAYER-FREEZE	    STD-LAYER-THAW	  STD-LAYER-OFF
      STD-LAYER-ON	    STD-LAYER-PUT-COLOR	  STD-LAYER-PUT-LTYPE
      STD-LAYER-ADD	    STD-LAYER-SET	  STD-LAYER-MAKE
      STD-ZOOM-IF-APPLY
      ))))


(std-%simple-require "STDLISP")

(std-defconstant '*TBL-GROUPS*	;tables and dxf groups
  '(("BLOCK" . 2)("LAYER" . 8)("STYLE" . 7)("COLOR" . 62)
    ("LTYPE" . 6)("DIMSTYLE" . 3)
    ("VPORT" . nil)("VIEW" . nil)("UCS" . nil)("APPID" . nil)
   ))

(std-defconstant '*TABLES*	;names only
  (mapcar (function car) *TBL-GROUPS*))

;;; tbl must be uppercase!
(defun STD-TBL-P (tbl)
  (member tbl *TABLES*))

;;; nil - cannot pick
(defun STD-TBL-GROUP (tbl)
  (cdr (assoc (strcase tbl) *TBL-GROUPS*)))

;;; Returns all table names
;;; see also (std-tbl tbl "*" 0 0)
(defun STD-TBL-NAMES (tbl)
  (std-tbl-props tbl 2))

;;; Returns list of table properties of all table elements
;;; Treat COLOR as virtual table to be able to use the same tbl-get
;;; funcs
(defun STD-TBL-PROPS (tbl grp / lst ele)
  (if (= tbl "COLOR")
    ;; TODO: use *STD:COLOR-TABLE*
    (if (= grp 2) (mapcar (function car) *STD:COLOR-TABLE*))
      ;;(list "ByLayer" "ByBlock" "red" "yellow" "green"
      ;;      "cyan" "blue" "magenta" "white")
    (progn

      (setq ele (tblnext tbl T))
      (while ele
        (setq lst (cons (std-getval grp ele) lst)
              ele (tblnext tbl))
      )

      (reverse lst)
    )
  )
)

;;; flags for std-tbl, common table groups
(std-defconstant 'STD:TBL-ALL        0)
(std-defconstant 'STD:TBL-EXTERN    16)	; external dependent (XREF)
(std-defconstant 'STD:TBL-BOUND     32) ; bound external xref
(std-defconstant 'STD:TBL-EXIST     64) ; referenced, not valid if >= r13

;;; returns all table names which flags in <must> must be set,
;;;  and which flags in <ignore> may not be set,
;;;  using wcmatch with match on the name
;;; e.g:
;;;   (std-tbl "LAYER" "*" STD:TBL-ALL STD:TBL-BOUND)
;;;      => all not externally bound layer names
;;;   (std-tbl "LAYER" "*" (+ STD:LAY-FROZEN STD:LAY-EXIST)
;;;			   STD:LAY-EXTERN)
;;;	=> all existing + frozen + off layer names
;;;        ignoring external
(defun STD-TBL (tbl match must ignore / name names ele)
  (setq	must (if (std-ver-r13-p) (std-bitdel STD:TBL-EXIST must) must))
  (if (= tbl "COLOR")	; all
    (std-tbl-names tbl)
    (if (std-tbl-p tbl)
      (progn

        (foreach name (std-tbl-names tbl)
          (setq ele (tblsearch tbl name))
          (if (and
	         (std-flagsetp must ele)
                 (or (= match "*") (wcmatch name match))
          	 (if (zerop ignore)
		   T
	           (not (std-flagsetp ignore ele))))
            (setq names (cons name names))
          )
        )

        (reverse names)
      )
    )
  )
)

;;; flags for std-tbl-layer
(std-defconstant 'STD:LAY-THAWED     0)  ;1.Arg - must
(std-defconstant 'STD:LAY-FROZEN     1)  ;1.Arg - must
(std-defconstant 'STD:LAY-LOCKED     4)  ;1.Arg - must
(std-defconstant 'STD:LAY-EXIST     64)  ;1.Arg - must
(std-defconstant 'STD:LAY-EXTERN    48)  ;2.Arg - ignore
(std-defconstant 'STD:LAY-ON         1)  ;3.Arg - on/off
(std-defconstant 'STD:LAY-OFF       -1)  ;3.Arg - on/off
(std-defconstant 'STD:LAY-ON-OR-OFF  0)  ;3.Arg - on/off

;;; returns all layer names which flags in <must> must be set,
;;;  and which flags in <ignore> may not be set,
;;;  using wcmatch with match on the name,
;;; ison=0: ignore on/off;
;;;      1: only turned on layers
;;;     -1: only turned off layers
;;; e.g:
;;;   (std-tbl-layer "*" STD:LAY-THAWED STD:TBL-ALL STD:LAY-ON-OR-OFF)
;;;      => all layer names
;;;   (std-tbl-layer "*" (+ STD:LAY-FROZEN STD:LAY-EXIST)
;;;			STD:LAY-EXTERN STD:LAY-OFF)
;;;	=> all existing + frozen + off layer names
;;;        ignoring external
;;;   (std-tbl-layer "*" STD:LAY-THAWED 0 STD:LAY-ON) all visible ones
(defun STD-TBL-LAYER (match must ignore ison / name names ele tbl)

  (setq tbl "LAYER"
        ele (tblnext tbl T)
	;; >=r13: ignore flag 64
	must (if (std-ver-r13-p) (std-bitdel STD:TBL-EXIST must) must))
  (while ele
    (setq name (std-getname ele))
    (if (and
	  (std-flagsetp must ele)
          (or (= match "*")(wcmatch name match))  ; fixed
          (if (zerop ignore)
	    T
	    (not (std-flagsetp ignore ele)))
	  (if (zerop ison) 	;any
	    T
	    (if (minusp ison)     ;only off
              (minusp (std-getcolor ele))
              (> (std-getcolor ele) 0)))
	) ; and
      (setq names (cons name names))
    )
    (setq ele (tblnext tbl))
  )

  (reverse names)
)

;;; Creates a new unique table name.
;;; e.g. for new named blocks, views or such (error handling)
(defun STD-TBL-TMPNAME (tbl prefix / i)
  (if (not prefix)(setq prefix "$STD-"))

  (if (tblsearch tbl prefix)
    (progn
      (setq i 0)
      (while (tblsearch tbl (strcat prefix (itoa i)))
        (setq i (1+ i)))
      (strcat prefix (itoa i))
    )
    prefix
  )

)


;;; -------------------------------------------------------------------73
;;; User Input: (getting existing names)
;;; These will be removed from the stdlib!

;;; Ask for table name[s]
;;; Status: not ready yet, missing is the extendability
;;;   and various checks
;;; Parameter flag:
;;;   data driven extensions via flag?
;;;   attributes: multiple, mustexist, allowspaces, noxref, onlyvisible, allowpick, ...
;;;(defun STD-TBL-GETNAME (tbl flag msg def / l)
;;;  (setq tbl (strcase tbl))
;;;  (cond
;;;    ((= tbl "COLOR")
;;;      (std-getstring 0 msg def nil))     ;numbers as well
;;;    ((std-tbl-p tbl)
;;;      (std-inp-getname 1 msg def)
;;;    )
;;;  )
;;;)
;;;
;;;;;; prompt for single blockname
;;;(defun STD-TBL-GETBLOCKNAME (msg def)
;;;  (std-tbl-getname "BLOCK" 0 msg def))
;;;
;;;;;; prompt for mult. blocknames
;;;(defun STD-TBL-GETBLOCKNAMES (msg def)
;;;  (std-tbl-getname "BLOCK" 1 msg def))
;;;
;;;;;; prompt for mult. linetypes
;;;(defun STD-TBL-GETLTYPENAME (msg def)
;;;  (std-tbl-getname "LTYPE" 1 msg def))
;;;
;;;;;; prompt for mult. color names or numbers
;;;(defun STD-TBL-GETCOLORNAME (msg def)
;;;  (std-tbl-getname "COLOR" 1 msg def))
;;;
;;;;;; prompt for single color number
;;;(defun STD-TBL-GETCOLORNUM (msg def)
;;;  (std-%color-string->num (std-tbl-getname "COLOR" 0 msg def)))

;;; english color names: (upcase-string . number)
;;; will not be localized
(std-defconstant '*STD:COLOR-TABLE*
  '(	("BYBLOCK" . 0)
	("BYLAYER" . 256)
	("RED" 	   . 1)
	("YELLOW"  . 2)
	("GREEN"   . 3)
	("CYAN"    . 4)
	("BLUE"    . 5)
	("MAGENTA" . 6)
	("WHITE"   . 7)
	("GREY"    . 8)))

;;; Converts full color string in number
;;; no localization support yet.
;;; color string must be in english so far,
;;; no abbrevations allowed.
(defun std-%color-string->num (s / n)
  (cond
    ((numberp s) s)
    ((not (std-string-not-empty-p s)) nil)
    ((cdr (assoc (strcase s) *STD:COLOR-TABLE*))) ;fixed
    (T (atoi s))))

;;; Converts color number into english uppercase string
;;; no localization support yet.
(defun std-%color-num->string (n)
  (cond
    ((stringp n) n)
    ((cdr (std-rassoc n *STD:COLOR-TABLE*)))
    (T (itoa n))))

;;; -------------------------------------------------------------------73
;;; View Calculations:

	; no workarounds possible
;;; conversion pixel to drawing units, FCAD 4.01 ok
(defun STD-PIX->UNITS (pix)
  (* pix (/ (getvar "VIEWSIZE") (cadr (getvar "SCREENSIZE")))))

;;; conversion drawing units to pixel, FCAD 4.01 ok
(defun STD-UNITS->PIX (ze)
  (* ze (/ (cadr (getvar "SCREENSIZE"))(getvar "VIEWSIZE"))))

;;; plan view mode, FCAD has no VIEWMODE
(defun STD-VIEW-PLAN-P ()
  (and (or (std-ver-fcad-p) (zerop (getvar "VIEWMODE")))
       (equal (getvar "VIEWDIR") '(0.0 0.0 1.0) 1e-3)))

;;; perspective view mode
(defun STD-VIEW-PERSP-P ()
  (cond ((std-ver-fcad-p) nil)
        (T (not (zerop (getvar "VIEWMODE"))))))

;;; in paperspace?, FCAD 4.01 ok
(defun STD-PAPERSPACE-P ()
  (= (caar (vports)) 1))

;;; switch to next vport: like Ctrl-V
(defun STD-VPORT-NEXT (/ vp)
  (if (setq vp (cdr (vports)))
    (setvar "CVPORT" (caar vp))))

;;; returns actual ZOOM points, in WCS
;;; vport extents, FCAD 4.01 ok
(defun STD-ZOOM-PTS (/ ctr h screen ratio size size_2)
  (setq ctr (xy-of (std-trans10 (getvar "VIEWCTR")))  ;2D
	h   (getvar "VIEWSIZE") 	;real
	screen (getvar "SCREENSIZE") 	;2D: Pixel x,y
	ratio (/ (float (car screen))
	 	 (cadr screen))
	size (list (* h ratio) h)
        size_2 (mapcar (function /) size '(2.0 2.0)))
  (list (mapcar (function -) ctr size_2)
        (mapcar (function +) ctr size_2))
)

;;; Do ZOOM Window if we don't already see everything
;;; if the actual view is larger, if we see all sides, we won't zoom
;;; minpt/maxpt in WCS. not DCL-transparent!
;;; use STD-ZOOM-EXTENTS instead
(defun STD-ZOOM-IF (minpt maxpt)
  (std-zoom-if-apply minpt maxpt
    (function (lambda (p1 p2)
      (if (std-dclactive-p)
	(std-error "DCL active. cannot perform COMMAND.")
        (command "'_ZOOM" "_W" p1 p2))
      T
      ;;(if (zerop (getvar "REGENMODE"))(command "_Y"))
      ))))

;;; Expects a function with two args, suitable to
;;;   (command "_zoom" "_w" arg1 arg2)
;;; Point arguments in WCS
(defun STD-ZOOM-IF-APPLY (minpt maxpt func / zm z1 z2)
  (setq zm (std-zoom-pts)
        z1 (std-trans01 (car zm))
	z2 (std-trans01 (cadr zm)))
  (if (or (> (x-of z1)
             (x-of minpt))
          (> (y-of z1)
	     (y-of minpt))
          (< (x-of z2)
             (x-of maxpt))
          (< (y-of z2)
	     (y-of maxpt)))
    (apply func
	   (list (std-xy-min (list z1 minpt))
		 (std-xy-max (list z2 maxpt))))))

;;; Do ZOOM Extents if we don't already see everything
;;; if the actual view is larger, if we see all sides, we won't zoom.
;;; Fixed for transparent DCL usage with VLA.
(defun STD-ZOOM-EXTENTS (/ zm ei ea)
  (std-var-push '(("EXPERT" . 1)))
  (if (or (> (x-of (car (setq zm (std-zoom-pts))))
             (x-of (setq ei (getvar "EXTMIN"))))
          (> (y-of (car zm)) (y-of ei))
          (< (x-of (cadr zm))
             (x-of (setq ea (getvar "EXTMAX"))))
          (< (y-of (cadr zm)) (y-of ea)))
    (progn
      (if (std-dclactive-p)
	(if (std-vla-p)
	  (if (std-ver-r2000-p) ;r2000 improvement
            (vla-zoomextents (vlax-get-acad-object))
            (vla-zoomextents    ;r14 quirks
	      (vla-get-ActiveViewport
		(vla-get-ActiveDocument
	          (vlax-get-acad-object)))))
	  (std-error "DCL active. cannot perform COMMAND.")
	)
        (command "'_ZOOM" "_E"))
      ;;(if (zerop (getvar "REGENMODE"))(command "_Y"))
      ))
  (std-var-pop))

;;; -------------------------------------------------------------------73
;;; Miscellaneous:

;;; Returns T on success or nil
;;; to be able to print user prompts as below on failure or success.
;;; (std-tbl-rename "LAYER" "WAND" "WAND-01")
(defun STD-TBL-RENAME (tbl old new)
  (if (tblsearch tbl old)
    (if (tblsearch tbl new)
      ;(std-verbose-print (list "\n" tbl " " new " already exists"))
      nil
      (progn
        (command "_RENAME" tbl old new) ; FCAD 4.01 ok
        ;(std-verbose-print (list "\n" tbl " " old " renamed to " new))
	T
      )
    )
    ;(std-verbose-print (list "\n" tbl " " old " doe not exist"))
  )
)

(defun STD-NOT-WORLDUCS-P ()
  (zerop (getvar "WORLDUCS")))

(defun STD-LAYER-VISIBLE-P (layname)
  (and (std-layer-on-p layname)
       (not (std-layer-frozen-p layname))))

;;; existing and on
(defun STD-LAYER-ON-P (layname / elist)
  (and (setq elist (tblsearch "LAYER" layname))
       (> (std-getcolor elist) 0)))

(defun STD-LAYER-FROZEN-P (layname)
  (std-flagsetp STD:LAY-FROZEN (tblsearch "LAYER" layname))) ; 1

;;; -------------------------------------------------------------------73
;;; Save/Restore pairs

;;; Save layer state
(defun STD-SAVE-LAYER (laynam)
  (tblsearch "LAYER" laynam))

;;; Restore layer state
(defun STD-RESTORE-LAYER (layer / layobj laynam changed)
  (if (std-ver-r14-p) (command)); command buffer bug
  (setq laynam (std-getname layer))
  ;; fixed v0.4002 8/19/99: std-save-layer may return nil
  (if (and layer (not (tblsearch "LAYER" laynam)))
    (entmake layer)
    ;;(std-entmake-layer layer)
    (progn
      (setq layobj (entget (tblobjname "LAYER" laynam)))
      (foreach grp '(7 62 70)
	(if (/= (cdr (assoc grp layobj))
		(cdr (assoc grp layer)))
	  (setq layobj (subst (assoc grp layer)(assoc grp layobj)
			      layobj)
		changed T)))
      (if changed (entupd (cdr (assoc -1 (entmod layobj))))))))

;;; returns UCS list to store in *STD:VAR-STACK*
;;; => actual ucs
(defun STD-SAVE-UCS ()
  (list (getvar "UCSORG")(getvar "UCSXDIR")(getvar "UCSYDIR")))

;;; ucs: list of (UCSORG UCSXDIR UCSYDIR) or a symbol table name
(defun STD-RESTORE-UCS (ucs / u x y)
  
  (if (not (std-ver-fcad-p))              ; added v0.4006
    (progn
  
      (if (std-ver-r14-p) (command)); command buffer bug
      (while (std-dclactive-p) (done_dialog))
      (while (std-cmdactive-p) (command))
  
    )
  )
  
  (cond
    ((stringp ucs)
      ;; TODO: add transparent methods instead of command UCS
      (if (tblsearch "UCS" ucs)		;v0.5001 added "World"
        (command "_UCS" "_Restore" ucs)
	;; allow (std-var-push '((:UCS . "World"))) or "Prev" or such
        (command "_UCS" ucs))
      ;; This is some magic here: "$" denotes a temp UCS to be deleted!
      (if (= (std-firstchar ucs) "$")
        (command "_UCS" "_Delete" ucs)))
    ((not (std-view-persp-p))
      (setq u (trans (car ucs) 0 1)
	    ;; fcad fix: T -> 1
	    x (mapcar (function +) u (trans (cadr ucs)  0 1 1))
	    y (mapcar (function +) u (trans (caddr ucs) 0 1 1)))
      (if (std-ver-fcad-p)
        (command "._UCS" "_3" u x y)
        (command "._UCS" "_3" "_NON" u "_NON" x "_NON" y)
      )
    )
  )
)

;;; TODO: transparent ucs methods
;;; (std-ucs-make ...)
;;; (std-ucs-delete ...)
;;; (std-%ucs-restore "name"|3points)

;;; transparent helper for strings
(defun std-%ucs-restore (ucs)
  (if (tblsearch "UCS" ucs)
    (command "_UCS" "_Restore" ucs)
    (command "_UCS" ucs)))

;;; => actual zoom in WCS
(defun STD-SAVE-ZOOM ()
  (std-zoom-pts))

;;; zoom: list of two points in WCS
(defun STD-RESTORE-ZOOM (zoom)
  (command "'_ZOOM" "_W"
	   (std-trans01 (car zoom))
	   (std-trans01 (cadr zoom))))

;;; => view name
(defun STD-SAVE-VIEW (/ name)
  (if (std-ver-r14-p) (command)); command buffer bug
  (while (std-dclactive-p) (done_dialog))
  (setq name (std-tbl-tmpname "VIEW" "$VIEW")); unique temp. name
  (if (std-ver-fcad-p)
    (command "_QVIEW" "_Save" name)
    (command "_VIEW" "_Save" name))	; A2000 bugfix
  name
)
;;; String of existing table name
;;; We have to distinguish temporary system generated names
;;; and user given names. The first have to be deleted, the latter not.
;;; After (std-var-push '((:VIEW . "myview"))) "myview" may not be
;;; deleted but the view generated with (std-var-push '((:VIEW))) should
(defun STD-RESTORE-VIEW (name)
  (if (std-ver-r14-p) (command)); command buffer bug
  (while (std-dclactive-p) (done_dialog))
  (if (std-ver-fcad-p)
    (command "_QVIEW" "_Restore" name)
    (command "_VIEW" "_Restore" name))	; A2000 bugfix
  (if (= (std-firstchar name) "$")
    (if (std-ver-fcad-p)
      (command "_QVIEW" "_Delete" name)
      (command "_VIEW" "_Delete" name)))	; A2000 bugfix
)

;;; This is not the same as view, because users could change the
;;; viewport entity in paperspace and fool the view info therefore.
;;; This is needed esp. for paperspace plotting.
;;; => vport-name
(defun STD-SAVE-VPORTS (/ name)
  (if (std-ver-r14-p) (command)); command buffer bug
  (while (std-dclactive-p) (done_dialog))	; end dialogs
  (setq name (std-tbl-tmpname "VPORT" "$VPORT")); unique temp. name
  (if (std-ver-fcad-p)
    (command "_QVIEWPORT" "_Save" name)
    (command "_VPORTS" "_Save" name))
  name
)

;;; string of existing table name
(defun STD-RESTORE-VPORTS (name)
  (if (std-ver-r14-p) (command)); command buffer bug
  (std-break-command)
  (if (std-ver-fcad-p)
    (command "_QVIEWPORT" "_Restore" name)
    (command "_VPORTS" "_Restore" name))
  (if (= (std-firstchar name) "$")
    (if (std-ver-fcad-p)
      (command "_QVIEWPORT" "_Delete" name)
      (command "_VPORTS" "_Delete" name)))
)

;;; This could be renamed to UNDOCTL because it handles UNDO
;;; according this var, hmm...
;;; => UNDOCTL bitcode
;;; 0	UNDO is turned off
;;; 1	UNDO is turned on
;;; 2	Only one command can be undone
;;; 4	Turns on the Auto option
;;; 8	A group is currently active
(defun STD-SAVE-UNDO ()
  (getvar "UNDOCTL"))

;;; restore old state. It accepts a integer value, which is the
;;; same as the UNDOCTL setting.
(defun STD-RESTORE-UNDO (old / crt)
  (if (std-ver-r14-p) (command)); command buffer bug
  (setq crt (getvar "UNDOCTL")) ; fixed
  (if (zerop old)     		; disable
    (if (zerop crt)             ; fixed
      nil       		; already off or
      (command "._UNDO" "_Control" "_None")
    )
    (progn
      ;; truthtable:
      ;; old crt new
      ;;  2   0   one
      ;; ~2   0   all
      (if (zerop crt)
        (if (std-bitsetp 2 old)
          (command "._UNDO" "_O")
          (command "._UNDO" "_All")))
      ;; old crt new
      ;;  8  ~8   begin
      ;; ~8   8   end
      ;;  8   8   -
      ;; ~8  ~8   -
      (if (std-bitsetp 8 old)	; if old had a group
	(if (std-bitsetp 8 crt)
	  nil
	  (if (std-ver-fcad-p)
            (command "._UNDO" "_Group-B")
            (command "._UNDO" "_Begin")))
	(if (std-bitsetp 8 crt)
	  (if (std-ver-fcad-p)
            (command "._UNDO" "_Group-E")
            (command "._UNDO" "_End"))))
      ;; old crt new
      ;;  2  ~2   one
      ;; ~2   2   all
      ;;  2   2   -
      ;; ~2  ~2   -
      (if (std-bitsetp 2 old)	; one
	(if (std-bitsetp 2 crt)
	  nil
          (command "._UNDO" "_Control" "_O")) ; One | Once: FCAD
	(if (std-bitsetp 2 crt)
          (command "._UNDO" "_Control" "_All"))
      )
      ;; old crt new
      ;;  4  ~4   on
      ;; ~4   4   off
      ;;  4   4   -
      ;; ~4  ~4   -
      (if (std-bitsetp 4 old)	; Auto on or off
	(if (std-bitsetp 4 crt)
	  nil
          (command "._UNDO" "_Auto" "_ON")
	)
	(if (std-bitsetp 4 crt)
          (command "._UNDO" "_Auto" "_OFF")
	)
      )
    )
  )
)

;;; set some useful predefined undo states:

;;; Not the same as in AI_UTILS.LSP: (ai_undo_off)
(defun STD-DISABLE-UNDO ()
  (if (zerop (getvar "UNDOCTL"))
    nil
    (command "._UNDO" "_Control" "_None")))

;;; Not the same as in AI_UTILS.LSP: (ai_undo_on)
;;; Turn Menu Autogrouping off,
;;; Don't start a new group, as this is done by (ai_undo_push)
(defun STD-ENABLE-UNDO (/ ctl)
  (setq ctl (getvar "UNDOCTL"))
  (if (std-bitsetp 8 ctl)     		   ; end active group
    (if (std-ver-fcad-p)
      (command "._UNDO" "_Group-E")
      (command "._UNDO" "_End")))
  (if (or (zerop ctl) (std-bitsetp 2 ctl)) ; Undo is disabled or One
    (command "._UNDO" "_Control" "_All"))
  (if (and (std-bitsetp 4 ctl)     	; Undo is one
           (not (std-ver-fcad-p)))
    (command "._UNDO" "_Auto" "_OFF"))
)

;; I know, this is confusing.


;;; -------------------------------------------------------------------73
;;; Layer methods, require STDENT
;;; Written by Ralph Gimenez. Minor changes by Reini Urban

;;;(STD-%LAYER-OBJ  "0") =>
;;;((-1 . <Entity name: 2de0798>)
;;;  (0 . "LAYER")
;;;  (5 . "3FB")
;;;  (100 . "AcDbSymbolTableRecord")
;;;  (100 . "AcDbLayerTableRecord")
;;;  (2 . "0")
;;;  (70 . 0)
;;;  (62 . 7)
;;;  (6 . "CONTINUOUS"))
(defun std-%layer-obj (name / layer)
  ;;(tblsearch "LAYER" (strcase name))
  (if (setq layer (tblobjname "LAYER" name))
    (entget layer)))

(defun std-%flag-add-bit (bit obj)
  (if (std-flagsetp bit obj)
    (progn
      (setq obj (subst (cons 70 (std-setbit bit (std-getflag obj)))
		       (assoc 70 obj)
		       obj))
      (entmod obj)
      (entupd (cdr (assoc -1 obj)))))
  (cdr (assoc -1 obj)))

(defun std-%flag-del-bit (bit obj)
  (if (std-flagsetp bit obj)
    (progn
      (setq obj (subst (cons 70 (std-bitdel bit (std-getflag obj)))
		       (assoc 70 obj)
		       obj))
      (entmod obj)
      (entupd (cdr (assoc -1 obj)))))
  (cdr (assoc -1 obj)))

;;; -------------------------------------------------------------------73
;;;1 = Layer is frozen, otherwise layer is thawed
;;;2 = Layer is frozen by default in new viewports
;;;4 = Layer is locked
(defun STD-LAYER-LOCK (name / layer)
  (if (listp name)
    (foreach x name (std-layer-lock x))
    (if (setq layer (std-%layer-obj name))
      (std-%flag-add-bit 4 layer))))
(defun STD-LAYER-UNLOCK (name / layer)
  (if (listp name)
    (foreach x name (std-layer-unlock x))
    (if (setq layer (std-%layer-obj name))
      (std-%flag-del-bit 4 layer))))
(defun STD-LAYER-FREEZE-VP (name / layer)
  (if (listp name)
    (foreach x name (std-layer-freeze-vp x))
    (if (setq layer (std-%layer-obj name))
      (std-%flag-add-bit 2 layer))))
(defun STD-LAYER-THAW-VP (name / layer)
  (if (listp name)
    (foreach x name (std-layer-thaw-vp x))
    (if (setq layer (std-%layer-obj name))
      (std-%flag-del-bit 2 layer))))
(defun STD-LAYER-FREEZE (name / layer)
  (if (listp name)
    (foreach x name (std-layer-freeze x))
    (if (setq layer (std-%layer-obj name))
      (std-%flag-add-bit 1 layer))))
(defun STD-LAYER-THAW (name / layer)
  (if (listp name)
    (foreach x name (std-layer-thaw x))
    (if (setq layer (std-%layer-obj name))
      (std-%flag-del-bit 1 layer))))
;;62	Color number if negative, layer is Off
(defun STD-LAYER-OFF  (name / layer)
  (if (listp name)
    (mapcar (function std-layer-off) name)
    (if	(setq layer (std-%layer-obj name))
      (progn
	(if (not (minusp (std-getcolor layer)))
	  (progn
	    (setq layer (subst (cons 62 (- (std-getcolor layer)))
			   (assoc 62 layer)
			   layer))
	    (entmod layer)
	    (entupd (cdr (assoc -1 layer)))))
	(cdr (assoc -1 layer))))))


(defun STD-LAYER-ON  (name / layer)
  (if (listp name)
    (mapcar (function std-layer-on) name)
    (if	(setq layer (std-%layer-obj name))
      (progn
	(if (minusp (std-getcolor layer))
	  (progn
	    (setq layer (subst (cons 62 (- (std-getcolor layer)))
			   (assoc 62 layer)
			   layer))
	    (entmod layer)
	    (entupd (cdr (assoc -1 layer)))))
	(cdr (assoc -1 layer))))))

;;; Changes the color of the layer.
;;; Current settings of the layer visibility is maintained.
;;; However, you can force the layer off is you pass a negative number,
;;; but you cannot force it on (to keep the off state)
;;; Returns the ename of the layer. Returns nil only when the color
;;; argument is 0 or greater than 255 or is not a number
(defun STD-LAYER-PUT-COLOR (name colorAsInteger /  layer)
  (if (listp name)
    (foreach x name (std-layer-put-color x colorAsInteger))
    (if	(or (not (numberp colorAsInteger))
	    (not (<= 1 (abs colorAsInteger) 255))) ; fix by David Kozina
      (progn
	(std-warn
	  (list
	    "STD-LAYER-PUT-COLOR - "
	    (std-msg 
	      "Color argument must be an integer from 1 to 255.\n")
	    (list 'std-layer-put-color name colorAsInteger)))
	nil)
      (if (setq layer (std-%layer-obj name))
	(progn
	  (setq	colorAsInteger (if (minusp (std-getcolor layer))
				 (- (abs colorAsInteger)) ; keep it off
				 colorAsInteger)
		layer	       (subst (cons 62 colorAsInteger)
				      (assoc 62 layer)
				      layer))
	  (entmod layer)
	  (entupd (cdr (assoc -1 layer)))
	  (cdr (assoc -1 layer)))))))

;;; tries to load it
(defun STD-LAYER-PUT-LTYPE  (name ltype / layer)
  (if (listp name)
    (foreach x name (std-layer-put-ltype x ltype))
    (if	(not (stringp ltype))
      (std-warn
	(list
	  "STD-LAYER-PUT-LTYPE - "
	  (std-msg 
	    "Ltype argument must be an string.\n")
	  (list 'std-layer-put-ltype name ltype)))
      (progn
	(if (not (tblsearch "LTYPE" ltype))
	  (command "_'LINETYPE" "_LOAD" ltype "" ""))
	(if (not (tblsearch "LTYPE" ltype))
	  (std-warn
	    (list "STD-LAYER-PUT-LTYPE - "
		  (std-msg  "Ltype not found.\n")
		  (list 'std-layer-put-ltype name ltype)))
	  (if (setq layer (std-%layer-obj name))
	    (progn (setq layer
			  (subst (cons 6 ltype) (assoc 6 layer) layer))
		   (entmod layer)
		   (entupd (cdr (assoc -1 layer)))
		   (cdr (assoc -1 layer)))))))))

;;; IF the layer name exists nothing is performed. If it does not exist it
;;; creates the layer.  Always returns the ename of the layer
(defun STD-LAYER-ADD  (name / layer)
  (if (listp name)
    (mapcar (function std-layer-add) name)
    (if	(setq layer (std-%layer-obj name))
      (cdr (assoc -1 layer))
      (progn
	(if (>= (std-ver-num) 14)
	  (entmake (list
		     '(0 . "LAYER")
		     '(100 . "AcDbSymbolTableRecord")
		     '(100 . "AcDbLayerTableRecord")
		     (cons 2 name)
		     '(70 . 0)
		     ))
	  (entmake (list
		     '(0 . "LAYER")
		     (cons 2 name)
		     '(70 . 0)
		     )))
	(cdr (assoc -1 (std-%layer-obj name)))))))

;; makes the layer name current
;; Always returns the ename of the layer
(defun STD-LAYER-SET (Name / layer)
  (if (setq layer (std-%layer-obj name))
    (progn
      ;; since when is this allowed?
      (setvar "CLAYER" Name)
      (cdr (assoc -1 layer)))))

;;; synonymous to command layer make. create or set
;;; Always returns the ename of the layer
;;; Groups: '("color" 6 "ltype" "hidden") or just nil
;;; Changed 13-Jul-00: keep old properties,
;;; only assure that it's current and on.
(defun STD-LAYER-MAKE (name groups)
  (if (not (tblobjname "LAYER" name))
    (progn
      (std-layer-add Name)
      (while groups
	(cond
	  ((= (strcase (car groups)) "COLOR")
	   (std-layer-put-color name (cadr groups)))
	  ((= (strcase (car groups)) "LTYPE")
	   (std-layer-put-ltype name (cadr groups))))
	(setq groups (cddr Groups)))
      ))
  (std-layer-set name)
  (std-layer-on name)
)

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDTBL-SYMBOLS*))
(setq *STDTBL-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDTBL")  ; At first provide it. But before you may
				; call it, be sure to have all supporting
				; functions.

;(std-%simple-require "STDACI")
(std-require "STDINIT")
(std-require "STDSTR")
(std-require "STDLIST")
(std-require "STDMATH")
(std-require "STDFILE")
(std-require "STDERROR")
(std-require "STDTIME")
(std-require "STDMISC")

;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDMISC.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:30:29 rurban>
;;; Copyright (c) 1998,99 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Miscellaneous Acad Specific Functions for the STDLIB

;;; STATUS:
;;;   not tested: STD-BLOCK-UPDATE,std-%install-acomp-ctrl-c-interceptor
;;;


;;; $Log: STDMISC.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:50:35  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; Revision 0.4008  2000/05/14 17:36:23  rurban
;;; 0.5 release
;;;
;;; 2000-04-26 18:37:49 rurban
;;;   added std-%all-significant-chars, std-%create-significant-chars
;;; 2000-04-09 10:46:36 rurban
;;;   added std-%install-acomp-ctrl-c-interceptor

;;;===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDMISC-SYMBOLS*
    '(STD-BLOCK-REQUIRE	     STD-BLOCK-UPDATE
      STD-BPOLY		     STD-SCRIPTACTIVE-P
      STD-DCLACTIVE-P	     STD-BREAK-COMMAND
      STD-PAUSE-COMMAND	     STD-OPEN-COMMAND
      STD-ENTITY-MARK	     STD-ENTITY-UNMARK
      STD-OSMSTR
      ))))


	; no workarounds possible

;;; Assure block is loaded. If not inserts the first found DWG.
;;; Does NO block update, use (std-block-update name) therefore.
(defun STD-BLOCK-REQUIRE (name / fn)
  (if (not (tblsearch "BLOCK" name))
    ;;; Truncate for DOS 8.3 or is it done by acad?
    (if (setq fn (findfile (std-force-extension
			     (std-%block-fname name) ".DWG")))
      (if (std-scriptactive-p)
	  (std-block-update name)
        (progn
          (command "_INSERT" (strcat name "=" fn))
          (command)
          fn
	  )
	)
      (std-error (list (std-msg "Block ") 
		       name (std-msg " not found!\n")))
    )
    name
  )
)


;; truncate blockname for DOS8.3
(defun std-%block-fname (s)
  (if (std-sys-longfname-p)
    s
    (substr s 1 8)))


;;; Re-load a block from the harddisc, update its internal definition
;;; If it was not defined before load a new one.
;;; Either return T/nil or call std-cerror
(defun STD-BLOCK-UPDATE (name / fname fname1 l ele)
  (std-var-push '(("ATTREQ" . 0)))
  (setq fname1 (std-force-extension (std-%block-fname name) ".DWG"))
  (if (setq fname (findfile fname1))
    (if (std-scriptactive-p)
      ;; scripting variant. slow, but (command) is not allowed.
      (progn
        (std-verbose-print (list (std-msg " Update block ") name
				 " (SCRIPT)...\r"))
	(setq l (entlast))
        (command "_INSERT" (strcat name "=" fname) "-1000,1000"
		 "" "" "")
        (if (and (= name (std-getname (setq ele (entlast))))
                 (= (std-gettype ele) "INSERT")
		 (/= l ele))
          (entdel ele)
          (std-cerror (std-msg "Couldn't delete temp. block"))
        )
      )
      ;; normal variant with (command)
      (progn
        (std-verbose-print (list (std-msg " Update block ") 
				name " ...\r"))
	;; old ACOMP workaround
	(if (= (type std-acomp-ctrl-c-interceptor) 'LIST)
	  (std-acomp-ctrl-c-interceptor))
        (command "_INSERT" (strcat name "=" fname))
        (command)
      )
    )
    (std-verbose-print
      (list (std-msg " File ") fname1 
	    (std-msg " not found!\n")))
  )
  (std-var-pop)
  fname
)

;;; Create a BPOLY, R14 compatible, 2 arguments, ss may be nil
(defun STD-BPOLY (pt ss / ele)
  (cond
    ((member (type C:BPOLY) '(SUBR EXRXSUBR EXSUBR))
      (if ss (C:BPOLY pt ss)	; old arx or ads function
             (C:BPOLY pt)))
    ((std-ver-r14-p)	;>=r14: native command
      (setvar "CMDDIA" 0)
      (setq ele (entlast))
      (std-break-command)
      (command "_BPOLY" "_A" "_I" "_N" "");advanced options
					  ;without island detection
      (if ss (command "_B" "_N" ss ""))	  ;define boundary set if ss
      (command "" pt "")
      (setvar "CMDDIA" 1)
      (if (/= (entlast) ele)
	(entlast)))			  ; return created BPOLY
    (T (std-cerror
	 (std-msg "command _BPOLY not available"))))) ; old version

;;; no ordinary [1] or transparent [2] command active.
;;; defined in STDERROR
;;;(std-%cond-defun
;;; '(defun STD-CMDACTIVE-P ()
;;;   (std-bitsetp 3 (getvar "CMDACTIVE")))
;;;)
(defun STD-SCRIPTACTIVE-P ()
  (std-bitsetp 4 (getvar "CMDACTIVE")))
(defun STD-DCLACTIVE-P ()
  (std-bitsetp 8 (getvar "CMDACTIVE")))

(defun STD-BREAK-COMMAND ()
  ;; cancel all possibly open dialogs
  (while (std-dclactive-p) (done_dialog))	; ~ term_dialog
  ;; cancel all command loops (DIM, -LAYER, ...)
  (while (std-cmdactive-p) (command)))

;;; repeat user prompts while in command loop
;;; set TEXTEVAL for DTEXT and such.
(defun STD-PAUSE-COMMAND (/ name)
  (std-var-push '(("TEXTEVAL" . 1)("CMDECHO" . 1))) ; v0.3013
  (setq name (getvar "CMDNAMES"))
  (or (zerop (getvar "CMDACTIVE"))
      (while (= (getvar "CMDNAMES") name)
        (command pause)))
  (std-var-pop))

;;; Open-ended command loop
;;; Repeat user prompts while in command loop
;;; (std-open-command (list "_COPY" ele "" "_M" pt))
;;;   Asks for all further points, e.g. _ENDP,...
(defun STD-OPEN-COMMAND (lst)
  (setvar "CMDECHO" 1)
  (if (> (length lst) *MAX-ARGS-LIMIT*)
    (std-error (std-msg "maximal number of arguments exceeded"))
    (apply (function command) lst))
  (std-pause-command)
)

;;; Highlights entity
;;; on errors it's unhighlighted
(defun STD-ENTITY-MARK (ele)
  ;; calls (redraw ele 3) implicitly
  (std-%var-add-pair (cons ':redraw ele)))

;;; Normal again
;;; restore error stack frame
(defun STD-ENTITY-UNMARK (ele)
  ;; calls (redraw ele 4) implicitly
  (std-%var-delete-pair (cons ':redraw ele)))


;;; -------------------------------------------------------------------73


;;; Upcase all significant chars, for getkword options.
;;; ("BYBLOCK" "BYLAYER" "RED" "YELLOW" "GREEN" "CYAN" "BLUE")
;;; => ("BYBlock" "BYLayer" "Red" "Yellow" "Green" "Cyan" "BLue")
(defun std-%all-significant-chars (_lst)
   (mapcar (function (lambda (s)
	     (std-%create-significant-chars s _lst))) _lst))

;;; "GREEN" '("BYBLOCK" "BYLAYER" "RED" "YELLOW" "GREEN" "CYAN" "GREY")
;;;  => "GREEn" (because of "GREY"
(defun std-%create-significant-chars (s lst / _i l)
  ;;; search all union prefixes, upcase this, downcase the rest
  (setq s   (strcase s)
	lst (mapcar 'strcase lst)
	lst (std-remove s lst)
	_i   1
	l   (strlen s))
  (while (and (<= _i l)
           (member (substr s 1 _i)
		   (mapcar (function (lambda (x) (substr x 1 _i))) lst)))
    (setq _i (1+ _i)))
  (if (= _i l)
    (strcase s)
    (strcat (strcase (substr s 1 _i)) (strcase (substr s (1+ _i)) t))
  )  
)

;;; Return all uppercase chars as string.
;;; Only the first maxlen chars or all chars if not maxlen.
;;; Ignore leading underscore or dot.
;;;   (std-%extract-significant-key "CeNTeR" 3)   => "CNT"
;;;   (std-%extract-significant-key "_.CeNTeR" nil) => "_.CNTR"
(defun std-%extract-significant-key (s maxlen / i l c ret lret)
  (setq ret (std-%strip-command-prefixes s) ;if we had multiple values..
	s   (cdr ret)			; okay we have it as sample
	ret (car ret))			; but I don't want it to include
  (setq i 1				; into the STDLIB.
	lret (strlen ret)
	l    (strlen s))
  (if (not maxlen) (setq maxlen l))
  (while (and (< lret maxlen) (<= i l))
    (if (std-isupper (setq c (substr s i 1)))
      (setq ret  (strcat ret c)
	    lret (1+ lret)))
    (setq i (1+ i)))
  ret
)

;;; returns a list of stripped prefixes ("_" "." "_." "._") and the rest
;;; => (prefix . s)
(defun std-%strip-command-prefixes (s)
  (std-%strip-prefix "."
    (std-%strip-prefix "_"
      (std-%strip-prefix "." (cons "" s)))))

;;; lst: <= (s . prefix) => (s . prefix)
(defun std-%strip-prefix (prefix lst)
  (if (std-strcmp prefix (std-firstchar (cdr lst)))
    (cons (strcat (car lst) prefix) (substr (cdr lst) 2))
    lst))


;;; other language settings override this in STDLOCAL
(std-require "STDLISP")
(std-defconstant '*STD:OSMODE-LONG-STRINGS*
 '("NONe" "ENDpoint" "MIDpoint" "CENter" "NODe" "QUAdrant"
   "INTersection"  "INSert"   "PERpendicular" "TANgent" "NEArest"
   "QUIck"))
(if (std-ver-r14-p)
  (std-defconstant '*STD:OSMODE-LONG-STRINGS*
    (append *STD:OSMODE-LONG-STRINGS*
	   '("APParent Intersection"))))
(if (std-ver-r2000-p)
  (std-defconstant '*STD:OSMODE-LONG-STRINGS*
    (append *STD:OSMODE-LONG-STRINGS*
	   '("EXTension" "PARallel"))))

(std-defconstant '*STD:OSMODE-SHORT-STRINGS*
      (mapcar (function (lambda (s)
                (std-%extract-significant-key s 3)))
              *STD:OSMODE-LONG-STRINGS*))

;;; Returns osmode string for OSMODE value such as
;;;   (STD-OSMSTR (+ 1 4 8)) => "END,CEN,NOD"
;;; The first string has to be treated special.
(defun STD-OSMSTR (i / osm n x)
  (foreach n (std-int-list (length *STD:OSMODE-SHORT-STRINGS*))
    (if (= (logand (setq x (expt 2 n)) i) x)
       (setq osm (cons (nth (1+ n) *STD:OSMODE-SHORT-STRINGS*) osm))))
  (if osm
    (std-strjoin (reverse osm) ",")
    (car *STD:OSMODE-SHORT-STRINGS*)
  )
)

;;; -------------------------------------------------------------------73
;;; Install ACOMP-CTRL-C-INTERCEPTOR, the acomp (ctrl-break) enabler.
;;; Without calling this somewhere acomp will never break on <Esc>
;;; or <Ctrl-C>. But this must be defined in an uncompiled lisp.
;;; Best in the MNL file or in ACAD.LSP before S::STARTUP
;;; The problem is to load it into the AutoLISP space instead of into
;;; the compiled lisp.
;;;   (defun ACOMP-CTRL-C-INTERCEPTOR () (princ))
;;;
;;; Usage:
;;; ;;; Must be at TOPLEVEL!
;;; (if (not (listp ACOMP-CTRL-C-INTERCEPTOR))
;;;   (std-%install-acomp-ctrl-c-interceptor))
(defun std-%install-acomp-ctrl-c-interceptor (/ fn f)
  (std-require "STDFILE")
  (setq fn (std-filename-mktemp "$acomp.scr")
	f  (std-fopen fn "w"))
  (write-line "(defun ACOMP-CTRL-C-INTERCEPTOR () (princ))" f)
  (std-fclose f)
  (command "_SCRIPT" fn)
)

;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDMATH-SYMBOLS*))
(setq *STDMATH-SYMBOLS* nil)


;;; Module dependencies:
(std-provide "STDMISC") 	; At first provide it. But before you may
		      		; call it, be sure to have all supporting
				; functions.

;(std-require 'STDACI)   ; the whole bunch (better list them explictly)
(std-require "STDINIT")
(std-require "STDSTR")
(std-require "STDLIST")
;;(std-require "STDMATH")	; std-bitsetp now in STDINIT
(std-require "STDFILE")
(std-require "STDERROR")
(std-require "STDTIME")
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDERROR.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $-*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:32:12 rurban>
;;; Copyright (c) 1998,99 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Error handling and state save/restore
;;; This is an important and tricky one.
;;;
;;; STATUS:
;;;   Seems to be okay though there are some critical functions here.
;;;
;;; $Log: STDERROR.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; 2000-05-19 rurban
;;;   fixed std-%cmdecho-0 print
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; 2000-03-14 22:07:54 rurban
;;;   fixed not changed sysvars handling, when they had been 
;;;	changed during execution
;;;   fixed empty :UCS handling (in STDTBL)
;;; 2000-04-07 14:52:32 rurban
;;;   removed STD-ASSERT from error handler
;;;

;;; internal functions start with std-%
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign (setq *STDERROR-SYMBOLS*
    '(*STD-ERROR*	    STD-VAR-PUSH	  STD-VAR-POP
      STD-ENAME-EXIST-P	    STD-UNCHANGED-DWG-P	  STD-CMDACTIVE-P
      STD-VAR-INIT	    STD-VAR-RESTORE
      STD-%VAR-SIMPLE-INIT STD-%VAR-SIMPLE-RESTORE
      ))))


;;; Keywords as error handler keys:
;;; Make them self evaluating and IDE expandable (blue).
;;; Needed as special error initializers.
;;; As keyword you may also forget the quotes.
;;;                            Usage with (std-var-init '(pairs*))

(std-%simple-require "STDLISP")

;;; System:
(std-defkeyword ':TRANSPARENT)	; (:TRANSPARENT)
(std-defkeyword ':ERROR)	; (:ERROR my-err)
(std-defkeyword ':FUNCTION)	; (:FUNCTION std-princ my-princ)
(std-defkeyword ':PROTECTED-FUNCTION)
; (:PROTECTED-FUNCTION princ my-princ)
(std-defkeyword ':FILE)		; (:FILE file)
;;; Entities:
(std-defkeyword ':ENTUPD)       ; (:ENTUPD ele*)
(std-defkeyword ':ENTDEL)       ; (:ENTDEL ele)
(std-defkeyword ':REDRAW)       ; (:REDRAW [ele])
(std-defkeyword ':GRVECS)	; (:GRVECS [vec-sym | clear-func])
;;; Tables:
(std-defkeyword ':UCS)		; (:UCS    [ucs]), see STDTBL
(std-defkeyword ':ZOOM)		; (:ZOOM   [p1 p2])
(std-defkeyword ':VIEW)		; (:VIEW   [view-name])
(std-defkeyword ':VPORTS)	; (:VPORTS [vports-name])
(std-defkeyword ':UNDO)		; (:UNDO   [undoctl])
(std-defkeyword ':LAYER)	; (:LAYER  [laynam])



;;; FCAD errors:
;;; "Warning: Program terminated by EXIT!"
;;; "User break"
(setq *STD:ERROR-STRINGS*
  (cond
    ((std-ver-fcad-p)
      '("Warning: Program terminated by EXIT!"	; (quit)
        "User break"      	  ;*cancel* on prompts
        ))         		  ;*cancel* during evaluation impossible
    ((eq (std-ver-language) :GERMAN)
     '("quit / exit abort"
       "Funktion abgebrochen"    ;*Abbruch* bei Prompts
       "Tastatur-Abbruch"))      ;*Abbruch* in Ausführung
    ;; Thanks to Serge Pashkov <psw@rt.kiam.ru> for the russian strings.
    ;; The cp ansi_1251 strings are the same as in cp dos866 (dos/windows)
    ((eq (std-ver-language) :RUSSIAN)
     '("\242\353\345\256\244 \341 \257\256\254\256\351\354\356 \344\343\255\252\346\250\250 \242\353\345\256\244\240"
       "\224\343\255\252\346\250\357 \257\340\245\340\242\240\255\240."
       "\257\340\245\340\353\242\240\255\250\245 \341 \252\256\255\341\256\253\250"))
    ;; Thanks to Nobuhiro Haketa <nobuhiro@trialsoftware.com>
    ;; Trial Software Laboratories, Japan
    ((eq (std-ver-language) :JAPANESE)
      '("quit / exit \202\311\202\346\202\351\222\206\216~"
	"\212\326\220\224\202\252\267\254\335\276\331\202\263\202\352\202\334\202\265\202\275"
 	"\272\335\277\260\331 \314\336\332\260\270"))
    (T
      '("quit / exit abort"
        "Function cancelled"      ;*cancel* on prompts
        "Console break"))         ;*cancel* during evaluation
))

;;; Standard error handler, honors *FOPEN* and (STD-VAR-RESTORE),
;;; but doesn't process environment cleanup: local vars become globals!
;;; Error handlers must be fast, so there are special versions for
;;; each supported language.
(defun *STD-ERROR* (msg)
  ;; verbosity:
  ;;   print nothing on (quit),(exit) and user breaks.
  (if (not (member msg *STD:ERROR-STRINGS*))
    (princ msg))
  (std-var-restore)
  (term_dialog)		; ??? FIXME

  (if (and *DEBUG* (std-ver-r2000-p)) (vl-bt))

)

(defun *std-%simple-error* (msg)
  (princ msg)
  (std-%var-simple-restore)
)


;;; -------------------------------------------------------------------73
;;; Error stack frames

;;; *STD:VAR-STACK* is a list of assoc-lists with saved states.
;;; Each list is a frame to be restored by (std-var-pop).
;;;
;;; Ex: ( ((:FILE . <file>)) ((:ERROR . *error*)) ) has two frames:
;;; The first (or top) frame holds the original *error* handler,
;;; The last (or actual) frame an open file, which will be closed
;;; in case of errors.
;;;
;;; (std-var-init) starts with a complete new stack,
;;; (std-var-restore) empties the whole stack, restores all frames.
;;; both are to be used as first and last function of a user C: command
;;; definition. (std-var-push) and (std-var-pop) are used in between
;;; library or user functions to be able to restore changes in case of
;;; errors, such as open files, special error handlers, highlighted or
;;; temp. deleted entities, changed zoom, view or ucs,and such.
;;;
;;; This behaviour is extendable by applying more std-save-... and
;;; std-restore-... functions for any key.


;;; Adds variables (stack-frame) to the global var-stack
;;; Used in inner functions to be able to clean up in case of errors.
;;; Must be followed by a matching (STD-VAR-POP)
;;; (std-var-push '(("DIMZIN" . 12)("LUPREC" . 16)))
;;; ==> std:var-stack ( (("DIMZIN" . 12)("LUPREC" . 16))
;;;			var-state1 var-state2 ...)
(defun STD-VAR-PUSH (alst / pair lst)
  (foreach pair alst
    (setq lst (cons (std-%var-handler (car pair) (cdr pair) T) lst)))
  (setq *STD:VAR-STACK* (cons lst *STD:VAR-STACK*))
)

;;; Removes last std-var-push'ed variables from global *STD:VAR-STACK*
;;; Used in inner functions in case of errors.
;;; Must be used in conjunction with a previous matching (STD-VAR-PUSH)
;;; An error in this routine will leave a pending *STD:VAR-STACK*
;;; We could use our own *error* hook just for this func or try to reset
;;; it at (std-var-init)
;;; Example: (std-var-pop)
;;;   => *STD:VAR-STACK* ( var-state1 var-state2 ...)
(defun STD-VAR-POP (/ pair)
  (foreach pair (car *STD:VAR-STACK*)
    (std-%var-handler (car pair) (cdr pair) nil))
  (setq *STD:VAR-STACK* (cdr *STD:VAR-STACK*))
)

;;; Adds a pair -but no seperate stack frame- to the
;;; actual (first) stack-frame of the global var-stack
(defun std-%var-add-pair (pair / frame)
  (setq frame (cons (std-%var-handler (car pair) (cdr pair) T)
	      	    (car *STD:VAR-STACK*)))
  (setq *STD:VAR-STACK* (cons frame (cdr *STD:VAR-STACK*)))
)

;;; deletes (pops) this pair from the actual (first) stack-frame
;;; of the global var-stack.
(defun std-%var-delete-pair (pair / frame)
  (setq frame (subst nil pair (car *STD:VAR-STACK*)))
  (setq *STD:VAR-STACK* (cons frame (cdr *STD:VAR-STACK*)))
  (std-%var-handler (car pair) (cdr pair) nil)
)

;;; regen a single entity if it exists and is not deleted
(defun std-%safe-entupd (ele)
  (if (std-ename-exist-p ele) (entupd ele)))

;;; not deleted AutoCAD entity
(defun STD-ENAME-EXIST-P (ele)
  (and (std-enamep ele)
;#- STANDALONE|;
       (entget ele)

))

;;; The complete engine for *STD:VAR-STACK* handling
;;;-------------------------------------------------
;;; Sets sysvars (strings) and treats special commands (symbols)
;;; If init then initialize *VAR-STACK*, else restore it.
;;; The init part must return a meaningful list to be able to restore
;;; the previous state at restore, restore doesn't have to provide a
;;; return value.
;;;   (:VPORTS nil T) only saves a name to be restored with
;;;   (:VPORTS name nil)
(defun std-%var-handler (key val init / x func)
  (cond
    ((not key) nil)

    ((stringp key)   ;set normal system var
     (if init	     ;we cannot check if key is a write-protected sysvar,
       (progn	     ;if val is nil we'll get the error on restauration
         (setq x (getvar key)) ; and an ERROR inside *error*!
	 (if val (setvar key val))
         (cons key x) ; store it always to restore intermediate changes
       )
       (if val (setvar key val))))


    ((eq key ':TRANSPARENT)
     (list key)
    )
    ;; (:ERROR new-err) - overload *error* function
    ;; The init value must be a symbol, no function,
    ;; but stored is the function, not the symbol.
    ;;  (defun my-err (s)(princ s)(std-%var-simple-restore))
    ;;  (std-var-push '((:ERROR . my-err)))
    ;;     => (((:ERROR . <subr *error*>)))
    ;; or even:
    ;; (std-var-push (list (cons :ERROR
    ;;    (defun my-err (s)(princ s)(std-%var-simple-restore)))))
    ((eq key ':ERROR)
     ;(if (and (not (std-vl-p))	; fixed autolisp 0.3009
     ;         init
     ;	      (std-symbolp val))
     ;  (setq val (eval val)))
     (cons key
      (if init
       (progn
	 ;;(print val)(print (type val))
	 ;; fixed v0.5003: in ALISP *std-error* is passed as SYM
	 (cond
	   ((eq val '*STD-ERROR*))
	   ((std-symbolp val)
	     (if (not (std-functionp (std-symbol-value val)))
	       (std-error (list "(std-functionp (std-symbol-value " val ")) failed"))))
	   ((not (std-functionp val))
	     (std-error (list "(std-functionp " val ") failed"))))
	 ;;(std-assert (list 'std-functionp val) nil)
         (setq x       *error*	; set new and store old error handler
	       *error* (if (std-symbolp val)
			 (std-symbol-value val)
			 val))
	 x
       )
       (setq *error* val))))     ; restore previous

    ;; (:FUNCTION ori-func new-func) - overload any function
    ;; (:function my-princ new-princ) => (:function princ <usubr>)
    ;;   use this if the new function should not be protected, regardless
    ;;   if the ori-func is protected
    ;; overload a protected function:
    ;; (:PROTECTED-FUNCTION ori-func new-func)
    ;; (:PROTECTED-FUNCTION princ std-princ)
    ;;   => (:protected-function princ <usubr>)
    ;;   Use this if the new function should be protected also, and the
    ;;   if the ori-func is protected, but this not important.
    ;;
    ;; Get the original value of the ori-func with:
    ;; (setq ori-func (std-%ori-function ori-func)) resp.
    ;; (setq ori-func (std-%ori-protected-function ori-func))
    ;;
    ;; Note: Function overloading will not work with compiler mode
    ;; :LINK or :MAKE-INTERNAL, unless you provide a NOT-LINK pragma for
    ;; these functions!
    ((member key '(:FUNCTION :PROTECTED-FUNCTION))
     (cons key
      (if init
       (progn
         ;; we do strict assertions here, to check errors at init,
         ;; not at restore
         (if (not (std-functionp (car val)))  ; removed assertion v0.3015
	   (std-error (list "std-functionp " (car val) 
			    (std-msg " failed"))))
         (if (not (std-functionp (cadr val)))
	   (std-error (list "std-functionp " (car val) 
			    (std-msg " failed"))))
	 ;; (setq ori-func (car val) new-func (cadr val))
	 (setq x (std-gensym))
         (set x (std-symbol-value (car val)))
	 (std-%unprotect-assign (list (car val)))
	 (set (car val) (std-symbol-value (cadr val)))
	 (if (eq key ':PROTECTED-FUNCTION)
  	   (std-%protect-assign (list (car val))))
	 (list (car val) (std-symbol-value x))
       )
       (progn
         (std-%unprotect-assign (list (car val)))
         (set (car val) (cadr val))	; restore ori-func
	 (if (eq key ':PROTECTED-FUNCTION)
  	   (std-%protect-assign (list (car val))))
         ))))

    ;; close file on restauration
    ((eq key ':FILE)
     (cons key
      (if init
       val			; store open file
       (std-fclose val))))      ; close it


    ;; do and undo highlighting
    ((eq key ':REDRAW)		; undo highlighting, single entity only
     (if (std-ename-exist-p val)
       (if init
	 (redraw val 3)	 	; called by std-entity-mark
	 (redraw val 4))	; called by std-entity-unmark
       (redraw))		; added v0.4001
     (cons key val)
    )
    ((eq key ':ENTUPD)	; regen element on restore,
     (if init           ; multiple entities allowed also
       (cons key val)
       (if (listp val)
         (mapcar (function std-%safe-entupd) val)
         (std-%safe-entupd val))))	;same file

    ;; undo entdel on restore only
    ((eq key ':ENTDEL)	; undo temp. deletion
     (std-require "STDENT")
     (if init
       (cons key val)
       (if (std-enamep val)
         (std-entdel val))))	; fixed by Serge Pashkov

    ((eq key ':GRVECS)	; undo temp. grvecs on restore only
     ;; accept symbol holding the temp vecs or a function to clear it
     (if init
       (cons key val)
       (cond ((null val))	; changed v0.4001
             ((std-functionp val)
              (eval val))
             ((and (std-symbolp val)
                   (consp (setq x (std-symbol-value val))))
              (grvecs x)))))

    ((eq key ':UCS)
     (std-require "STDTBL")
     ;; FIXME: check for perspective viewport, no errors!
     (if init
       (progn
         (setq x (std-save-ucs)); save ucs
	 (if val (std-restore-ucs val))
         (cons key x)
       )
       (std-restore-ucs val)))  ; restore ucs

    ((eq key ':LAYER)
     (std-require "STDTBL")
     (if init
       (if val
         (cons key (std-save-layer val))
	 (cons "CLAYER" (getvar "CLAYER"))	; change key!!
       )
       (if val (std-restore-layer val))))


    ;; Extendable definition for your own keywords, no error checking.
    ;; Usable for :VIEW, :ZOOM, :VPORTS, :UNDO, ...
    ;; Just supply a std-save- and a std-restore- function.
    ;; The symbol must not have a leading colon. If so it is removed
    ;; for calling the save/restore function.
    ((std-symbolp key)
     (setq x (std-symbol-name key)
	   x (if (= ":" (std-firstchar x))
	       (substr x 2) x))
     (setq func (read (strcat "std-"
			      (if init "save-" "restore-")
			      x)))
     (std-require "STDTBL")
     (if (std-functionp func)
       (if init
	 (progn
           (setq x (apply func nil)) ; return actual setting
	   ;; if a object (a name) was supplied, restore (=set) it,
	   ;; else do nothing, we just had stored the original value
	   ;; without setting a new.
	   (if val (apply (read (strcat "std-restore-" x)) (list val)))
	   (cons key x)
	 )
	 (apply func (list val))
       )
       (std-error (list (std-msg "missing function definition: ") func))
     )
    )
    (T nil)
    ))

;;; Accessor for the original protected function.
;;; Return the last definition of the original function
;;; definition in the stack frame.
;;;   *STD:VAR-STACK*: (((:PROTECTED-FUNCTION + #<SUBR @00188664 +>)))
;;;   (std-%ori-protected-function '+) => #<SUBR @00188664 +>
(defun std-%ori-protected-function (func)
  (std-%search-stack ':PROTECTED-FUNCTION 2 func))

;;; Accessor for the original function.
;;; Better than (caddr (assoc ':FUNCTION (car *STD:VAR-STACK*)))
;;; because there may be some more std-var-push in between
;;;   *STD:VAR-STACK*: (((:FUNCTION + #<SUBR @00188664 +>)))
;;;   (std-%ori-function '+) => #<SUBR @00188664 +>
(defun std-%ori-function (func)
  (std-%search-stack ':FUNCTION 2 func))

;;; General accessor to get the first saved value
;;; from the error stack frame.
;;; Ex: (std-%find-error-key "OSMODE") => ("OSMODE" . 0)
(defun std-%find-error-key (key / stack x)
  (setq stack *STD:VAR-STACK*)
  (while (and stack
	      (not (setq x (std-%search-list key nil (car stack)))))
      (setq stack (cdr stack)))
  x)

;;; search for the last key in the frame and return the original value
;;; which is stored in position pos
;;;   *STD:VAR-STACK* : (((:PROTECTED-FUNCTION + #<SUBR @00188664 +>)))
;;;   (std-%search-stack ':PROTECTED-FUNCTION 2 '+) => #<SUBR @00188664 +>
(defun std-%search-stack (key pos val / stack x)
  (setq stack *STD:VAR-STACK*)
  (while (and stack
	      (not (setq x (std-%search-list key val (car stack)))))
      (setq stack (cdr stack)))
  (if x (nth pos x))
)

;;; searches the list for a (key val ...) or (key . val) pair
;;; val may be the cadr of a plain list or a cdr of an alist.
;;;   (std-%search-list ':FUNCTION '+
;;;           '((:FUNCTION - #<SUBR @00188665 ->)
;;;             (:FUNCTION + #<SUBR @00188664 +>)))
;;;    => (:FUNCTION + #<SUBR @00188664 +>)
(defun std-%search-list (_key _val lst)
  (if (not _val)
    (car (member _key lst))
    (car (std-member-if
 (quote     ;)

           (lambda (x)
	    (and (eq (car x) _key)
                 (if (std-dotted-pair-p x)
	  	   (eq (cdr x) _val)
	           (eq (cadr x) _val)))))
	  lst))))

;;; Set CMDECHO to 0. But not on drawing startup, not to change DBMOD.
;;; Otherwise the dwg would be marked as changed.
;;; Returns old setting.
(defun std-%cmdecho-0 (/ msg var)
  (setq msg (std-msg "CMDECHO 0 suppressed during startup"))
  (setq var (getvar "CMDECHO"))
  (if (std-unchanged-dwg-p)
    (progn
      (if (not (zerop var))(std-verbose-print msg))
      var)
    (progn
      (if (zerop (getvar "DBMOD"))
        (progn
	  (if (not (zerop var))(std-verbose-print msg))
 	  var)
        (setvar "CMDECHO" 0))
      )))

;;; not to change the database in startup functions
(defun STD-UNCHANGED-DWG-P ()
  (or (not (std-acad-connection-p))
;#- STANDALONE|;
      (zerop (getvar "DBMOD"))

))


;;; no ordinary [1] or transparent [2] command active
(defun STD-CMDACTIVE-P ()
  (/= 0 (logand 3 (getvar "CMDACTIVE"))))


;;; this happens most likely because of VLIDE-Abort <Ctrl-R>,
;;; but it can be error inside your user- *error* as well.
(std-defconstant 'std:errmsg-pend-stack
  (std-msg "pending *STD:VAR-STACK* deleted. Previous function didn't end correctly."))

;;; this happens most likely because of nested init/restore calls!
(std-defconstant 'std:errmsg-wrong-error
  (std-msg "Tried to restore wrong error handler. Fixed."))

;;; Used as first functions in a user function. To initialize systemvars
;;; and such. Similar to std-var-push, it only does some more book
;;; keeping stuff: Undo handling, implicit picksets, ...
(defun STD-VAR-INIT (alst / oldcmd)
  (setq oldcmd (std-%cmdecho-0))        ; non-destructive CMDECHO 0
                                        ; sets verbosity off

  ;; Remember PICKFIRST selection because _UNDO _GROUP deletes it
  ;; in (BD4A) compiled functions (ssget "_I") does not work! (acomp)
  ;; needed for user-input funcs
  (if (and (std-bitsetp 1 (getvar "PICKFIRST"))
           (not (std-cmdactive-p))
	   (setq *STD:SS-PICKED* (ssget "_I"))
  	   )
    nil
    (setq *STD:SS-PICKED* nil)
  )
  ;; initialize break stepper
  (if *BREAK* (setq *CONT* nil))
  ;; Initialize STD-ENTGET cache, to avoid cache inconsistencies,
  ;; E.g. Entities changed by not Stdlib commands.
  ;; Many thanks to Ralph Gimenez
  (setq *ENTGET-CACHE* nil)	; v0.4001

  ;; UNDO handling
  ;; no UNDO with TRANSPARENT flag or active command
  (if (or (std-cmdactive-p) (assoc ':TRANSPARENT alst))
    nil
    (progn
      (if (std-bitsetp 8 (getvar "UNDOCTL")) ;Group already active,
        (progn				     ;  most likely an error
          (std-warn (std-msg "end old UNDO group"))
          (command "_.UNDO" "_END")
	)
      )
      ;; Panic mode 12/14/99
      (if (not undo_init) (setq undo_init (getvar "UNDOCTL")))
      ;; save actual command on command stack?
      ; (std-push (getvar "CMDNAMES") '*std:cmdstack*)
      ;; this is dangerous!
      (if (std-functionp 'ai_undo_push)
        (ai_undo_push)  ; start new UNDO group
        (command "_.UNDO" "_Begin") ; earlier called _Group (R12?)
      )
    )
  )

  ;; delete pending *STD:VAR-STACK*, only if :TRANSPARENT flag
  ;; most likely an error
  (if (and *STD:VAR-STACK* (not (assoc ':TRANSPARENT alst)))
    (progn
      (std-debug-print *STD:VAR-STACK*)
      (std-warn std:errmsg-pend-stack)
      (setq *STD:VAR-STACK* nil)))
  ;; save all keys
  (std-var-push alst)
  ;; add error handler and cmdecho to stack frame

  (if (and (not *DEBUG*) (not (assoc "CMDECHO" alst)))
    (std-%var-add-pair (cons "CMDECHO" oldcmd)))

  ;; see if the standard error handler is already supplied
  (if (and (not (eq *STD-ERROR* *error*)) ; fixed v0.4001
           (not (assoc ':ERROR alst)))
    (std-%var-add-pair '(:ERROR . *STD-ERROR*)))
  *STD:VAR-STACK*
)

;;; use this simple without acad connection,
;;; just for files and error handling
(defun STD-%VAR-SIMPLE-INIT (alst)
  (if *BREAK* (setq *CONT* nil))
  ;; delete pending *STD:VAR-STACK*, only if :TRANSPARENT flag
  ;; most likely an error
  (if (and *STD:VAR-STACK* (not (assoc ':TRANSPARENT alst)))
    (progn
      (std-debug-print (list "*error*: " *STD:VAR-STACK*))
      (std-warn std:errmsg-pend-stack)
      (if (eq *error* *std:ori-error*)
        nil
        (progn
          (std-debug-print *ERROR*)
          (std-warn std:errmsg-wrong-error)
          (setq *error* *std:ori-error*)
          ))
      (setq *STD:VAR-STACK* nil)))
  ;; save all keys without the acad symbol vars
  (std-var-push (std-remove-if 'stringp alst))
  (if (and (not (eq *std-%simple-error* *error*)) ; fixed v0.4001
           (not (assoc ':ERROR alst)))
    (std-%var-add-pair '(:ERROR . *std-%simple-error*)))
  *STD:VAR-STACK*
)

;;; use it without acad connection, just for files and error handling
;;; FIXME :TRANSPARENT usage will fail so far
(defun STD-%VAR-SIMPLE-RESTORE (/ topframe)
  ;; restore vars, ignore for now recursive stack frames
  (setq topframe (last *STD:VAR-STACK*))
  (while *STD:VAR-STACK* (std-var-pop))
  (if (and (listp topframe) ; safe assoc
	   (std-every 'std-dotted-pair-p topframe)
	   (not (assoc ':TRANSPARENT topframe))
           (std-module-defined-p "STDFILE"))
    (progn (std-fcloseall)(std-%delete-mktemp)))
  (if (eq *error* *std:ori-error*)
    nil
    (progn
      (std-debug-print *ERROR*)
      (std-warn std:errmsg-wrong-error)
      (setq *error* *std:ori-error*)
      ))
  (prin1))

;;; Restore all stack frames.
;;; FIXME :TRANSPARENT usage will fail so far
;;; Used as last user function
(defun STD-VAR-RESTORE (/ topframe oldcmd)
  ;; restore vars, ignore for now recursive stack frames
  (setq topframe (last *STD:VAR-STACK*))
  (setq oldcmd (std-%find-error-key "OSMODE")) ; get safed CMDECHO
  (while *STD:VAR-STACK*
    ;; The way to handle CMDECHO is arguable:
    ;; CMDECHO could be forced to ("CMDECHO" . 1) eg. for debugging or
    ;; interactive commands as in std-open-command.
    ;; Now we turn off CMDECHO for every restoration stack frame,
    ;; mainly to avoid UNDO END verbosity.
    (std-%cmdecho-0)	; set it off until the end
    (std-var-pop))
  (if (or (std-cmdactive-p)
	  ;; safe assoc
	  (and (listp topframe)
	       (std-every 'std-dotted-pair-p topframe)
	       (assoc ':TRANSPARENT topframe)))
    nil
    (progn
      (std-%cmdecho-0)
      (if (std-module-defined-p "STDFILE")
        (progn (std-fcloseall)(std-%delete-mktemp)))
      (if (std-functionp 'ai_undo_pop)
        (progn
          (command)		; R2000 fix
	  ;; Panic mode 12/14/99
	  (if (not undo_init) (setq undo_init (getvar "UNDOCTL")))
          (ai_undo_pop))
        (command "_.UNDO" "_End"))
      (term_dialog)))
  (if (and (numberp oldcmd)
	   (/= oldcmd (getvar "CMDECHO")))
    (setvar "CMDECHO" oldcmd))
  (if (eq *error* *std:ori-error*)
    nil
    (progn
      (std-warn std:errmsg-wrong-error)
      (setq *error* *std:ori-error*)))
  (prin1))	; prin1 does not expand escape sequences, so it may be
		; a bit faster.


;;; -------------------------------------------------------------------73

(if std-%protect-assign (std-%protect-assign *STDENT-SYMBOLS*))
(setq *STDENT-SYMBOLS* nil)


;;; Module dependencies:
(std-%simple-provide "STDERROR"); At first provide it. But before you may
				; call it, be sure to have all supporting
				; functions.


;;; If all required STDLIB modules are contained in one project,
;;; we don't have to load it explicitly. STDLIB.FAS already contains it
(if (not *STD:%PROJECT*)
  (progn
    (std-%simple-require "STDINIT")
    (std-%simple-require "STDFILE")      ; std-fcloseall
  )
  ;; else: If it's loaded as project (STDALL, STDLIB.FAS or STDLIB.BI4)
  ;; we have to do it in (STDLIB-PROJECT-INIT)
)


; 'STDERROR
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: ENTMAKE.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $-*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:31:21 rurban>
;;; Copyright (c) 1998,99,2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Entmake module for the STDLIB
;;; Checked but fast creation of AutoCAD objects.

;;; STATUS:
;;;   Fixed for FelixCad
;;;   Not full featured yet, more entities and objects to be added
;;;     eventually. Thanks to David Kozina who's doing this.
;;;   The new as temporary marked functions by David Kozina might fail.
;;;     They are very experimental.
;;;   With the new STD-ENTMAKE-PFACE update, changes must be fixed at
;;;     the user side: either add a "0" to the call or fix the face
;;;     indices.
;;; std-entmake-xxx moved from STDENT.LSP
;;; Not included in the seperate package SMALL
;;; Bug in template opts (2000i with lwpolyline temp. fixed)


;;; $Log: ENTMAKE.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; 2000-11-27 20:56:42 rurban
;;;   fixed STD-ENTNAKE-LWPOLYLINE for A2000i
;;;   TODO: fix bug in std-%entmake-template (reverts 100 opts)
;;; 2000-09-23 19:17:38 rurban
;;;   removed STD-ENTMAKE-MLINE, added STD-ENTMAKE-XRECORD,
;;;   fixed std-%entmake-valid-group-p for 280
;;; Revision 0.5004  2000/09/20 12:48:22  rurban
;;;   0.5004 release, see Changes
;;;
;;; 2000-07-12 00:54:12 rurban
;;;   fixed STD-ENTMAKE-PFACE for explicit layer (in faces)
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;;   fixed last segment for STD-ENTMAKE-POLYLINE,
;;;   STD-ENTMAKE-LWPOLYLINE. it broke June 21, 2000
;;; 2000-07-04 19:08:26 rurban
;;;   added layer arg to STD-ENTMAKE-SEQEND
;;;   fixed vertex layer in STD-ENTMAKE-3DPOLY, STD-ENTMAKE-POLYLINE,
;;;   STD-ENTMAKE-PFACE
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; 2000-06-18 19:05:50 rurban
;;;   fixed STD-ENTMAKE-ENDBLK, STD-ENTMAKE-SEQEND
;;; 2000-05-24 rurban
;;;   fixed std-%force-3dpoint and added it to various calls
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; 2000-05-05 rurban
;;;   fixed (STD-ENTMAKE-PLINE nil segs)
;;; 2000-04-26 18:19:49 rurban
;;;   renumbered MSG ID's
;;; 2000-04-25 12:32:27 rurban
;;;   renamed STD-ENTMAKE-PFACE to STD-ENTMAKE-PFACE0,
;;;   the face indices in STD-ENTMAKE-PFACE are now one-based

;;; internal functions start with std-%
;;; ===================================================================73
(if std-%unprotect-assign
  (std-%unprotect-assign (setq *ENTMAKE-SYMBOLS*
    '(STD-ENTMAKE-BLOCK
      STD-ENTMAKE-ATTDEF	 STD-ENTMAKE-ENDBLK
      STD-ENTMAKE-INSERT	 STD-ENTMAKE-ATTRIB
      STD-ENTMAKE-SEQEND	 STD-ENTMAKE-LAYER
      STD-ENTMAKE-STYLE		 STD-ENTMAKE-TEXT
      STD-ENTMAKE-MTEXT		 STD-ENTMAKE-CIRCLE
      STD-ENTMAKE-POINT		 STD-ENTMAKE-PLINE
      STD-ENTMAKE-POLYLINE	 STD-ENTMAKE-LWPOLYLINE
      STD-ENTMAKE-PFACE		 STD-ENTMAKE-PFACE0
      STD-ENTMAKE-3DPOLY	 STD-ENTMAKE-LINE
      STD-ENTMAKE-LEADER	 STD-ENTMAKE-ARC
      STD-ENTMAKE-ELLIPSE	 STD-ENTMAKE-GROUP
      STD-ENTMAKE-HATCH		 STD-ENTMAKE-SOLID
      STD-ENTMAKE-SPLINE	 STD-ENTMAKE-MLINE
      STD-ENTMAKE-MLINESTYLE))))

;;; corresponds to the stdlib version for now
(std-defconstant '*ENTMAKE-VERSION*
  (atof (substr "$Revision: 0.5006 $" 11)))

;;; To load STDENT ACAD should be connected :)
;;; For now assume this:
;;; (if (std-ver-r14-p) (setq *R14-LWPOLY-BUG* T))

;;; ENTMAKE Templates

	; for SMALL the whole module is too large

;;; Simplify entmake calls by assuming defaults.
;;; textpt defaults may be improved.
;;; The new 100 groups are not supported for now,
;;; but will added to the opts lists step by step.

;;; test if the group number is valid, release specific,
;;; used to filter out groups 100 and so on.
(defun std-%entmake-valid-group-p (n)
  (cond
    ((<  (std-ver-num) 1) T)	; alpha/beta versions
    ((>= (std-ver-num) 14) T)	; allow all
    ((<= 100 n 199) nil) 	; subclass data markers and other new
    ((= 280 n) nil) 		; duplicate flag
    ((<= 300 n 399) nil)	; object handles, pointer and owner id's
    (T T)
    ))

;;; Helper to check an entmake list template and to fill in defaults.
;;; Needs the entget-list, some defaults and some required groups.
;;; The idea is also useful for other purposes.
;;; 0.4006: added xflag as 4th parameter
;;; TODO: Check and fix points for 2D/3D. (Thanks to David Kozina)
;;;   Rationale: If 3d needed but only 2d provided sometimes a random
;;;   z-value is used.
;;;   Logic: All points are 3D, unless they are 2D elements: polylines
;;;     But for those the z-value is correctly stripped.
;;; FIXME: Broken for double opts groups: reverts order.
;;;     (e.g. 100, fails with lwpolyline)
(defun std-%entmake-template (_elist def req xflag / i new-elist newx x)
  (setq def (reverse def)
	i 0)
  (if (or (not req)
          (apply (function and)
		 (mapcar
		   (function (lambda(_x)(assoc _x _elist)))
		   req)))
    (progn
      ;; Rebuild the list of dxf groups in the correct order
      ;; and with defaults.
      ;; Note that invalid groups are only removed if they
      ;; appear in the default list!
      ;; FIXME: double 100 get reverted!
      (foreach x def
	(setq newx
	  (cond
	    ((not (std-%entmake-valid-group-p (car x))) nil)
	    ((assoc (car x) _elist))		; okay
            (T (nth i def))			; get default
	    ))
        (if newx (setq new-elist (cons newx new-elist)))
	(setq _elist (std-remove (assoc (car x) _elist) _elist)
              i (1+ i)))
      (setq new-elist (append new-elist _elist)))
    (if req
      (std-cerror (list
		   (std-msg "missing dxf group in ")
		   "(STD-ENTMAKE-" (std-gettype def) ")"
		   (std-msg "\nele-list: ") _elist
		   (std-msg "\ndefaults: ") def
		   (std-msg "\nrequired: ") req))))
  ;; R15 fix, 13-Mar-00
  (if (or (assoc 330 new-elist)(assoc 410 new-elist))
    (setq xflag t))
  (std-%entmake-call new-elist xflag))

;;; v0.4005
;;; Will this work with multiple 10 groups? (e.g. Leader)
;;; FIXME: replace subst by position and setnth
(defun std-%force-3dpoint (elist groups / x)
  (foreach g groups
    (if (and (setq x (assoc g elist))
	     (not (std-3dpointp (cdr x))))
      ;; UR fixed 24-May-00
      (setq elist (subst (cons g (std-new-z (cdr x) 0.0))
			 x elist))))
  elist)

;;; For debugging and converting std-entmake- calls to plain entmake
;;; 0.4006: added 2nd parameter xflag
(defun std-%entmake-call (elist xflag)
  (if *DEBUG*
    (progn
      (princ (strcat "\n(ENTMAKE" (if xflag "x" "") " '"))
      (prin1 elist)(princ ")")))
  elist

  (if xflag (entmakex elist) (entmake elist))

)

;;; added v0.4007
(defun std-%textpt-below (pt)
  ;; 1.25 * height: typical shx linespacing
  (std-pt- pt (list 0 (* 1.25 (getvar "TEXTSIZE")) 0)))

;;; Sample:
;;; (std-entmake-block '((2 . "testblock")(70 . 2)))
;;; entmake some geometry...
;;; (std-entmake-attdef (list '(2 . "ATT1")(cons 1 name)))
;;; (std-entmake-attdef '((2 . "ATT2")))
;;; (std-entmake-attdef '((2 . "ATT3")))
;;; (std-entmake-endblk)
(defun STD-ENTMAKE-BLOCK (elist / opts)
  ;; 100 groups missing for r14, but not required
  (setq opts (list
                '(0 . "BLOCK")
                '(2 . "")
                '(70 . 0)
		'(3 . "")
		;; FCAD fails with '(10 . (0 0 0))
                '(10 0 0 0)))
  ;; v0.4007
  (setq *last-insertion-point*
	(cond ((cdr (assoc 10 elist))) (T '(0 0 0))))
  (setq elist (std-%force-3dpoint elist '(10)))
  (std-%entmake-template elist opts '(2) nil))

(defun STD-ENTMAKE-ATTDEF (elist / opts x)
  (setq x (cond (*last-insertion-point*) (T '(0 0 0))))

  (setq *last-insertion-point*
	 (if (not (assoc 10 elist))
	   (std-%textpt-below x)
	   (std-%textpt-below (cdr (assoc 10 elist)))))
  (setq opts (list
           '(0 . "ATTDEF")
	   '(100 . "AcDbEntity")
           (cons 10 x)

            (cons 40 (getvar "TEXTSIZE"))

           '(8 . "0")
	   '(100 . "AcDbText")
           '(1 . "")	; prompt
	   '(3 . "")	; value

            (cons 7 (getvar "TEXTSTYLE"))

	   '(100 . "AcDbAttributeDefinition")
           '(2 . "")	; name
           '(70 . 0)))  ; flag (visibility, constant, ...)
  (setq elist (std-%force-3dpoint elist '(10)))
  (std-%entmake-template elist opts '(2) nil))     ;required 2

;;; not really required but nice to have
(defun STD-ENTMAKE-ENDBLK ()
  (if (std-%entmake-valid-group-p 100)
    (std-%entmake-call '((0 . "ENDBLK")(100 . "AcDbBlockEnd")) nil)
    (std-%entmake-call '((0 . "ENDBLK")) nil)))

;;; Sample:
;;; (std-entmake-insert (list '(2 . "testblock")'(66 . 1)(cons 10 pt)))
;;; (std-entmake-attrib (list '(2 . "ATT1")(cons 1 name)))
;;; (std-entmake-attrib '((2 . "ATT2")))
;;; (std-entmake-attrib '((2 . "ATT3")))
;;; (std-entmake-seqend)
(defun STD-ENTMAKE-INSERT (elist / opts x)
  (setq opts (list
                '(0 . "INSERT")
		'(100 . "AcDbEntity")
		'(100 . "AcDbBlockReference")
		'(2 . "")
                '(66 . 0)
                '(10 0 0 0)))
  (setq elist (std-%force-3dpoint elist '(10)))
  (setq x (std-%entmake-template elist opts '(2) nil))
  (setq *last-insertion-point* (cdr (assoc 10 x)))
  x)

(defun STD-ENTMAKE-ATTRIB (elist / opts x)
  (setq x (cond (*last-insertion-point*) (T '(0 0 0))))

  (setq *last-insertion-point*
	 (if (not (assoc 10 elist))
	   (std-%textpt-below x)
	   (std-%textpt-below (cdr (assoc 10 elist)))))

  (setq opts (list
           '(0 . "ATTRIB")
  	   '(100 . "AcDbEntity")
	   '(100 . "AcDbText")
	    (cons 10 x)        		; same as insert

            (cons 40 (getvar "TEXTSIZE"))

           '(1 . "")			; value
	   '(100 . "AcDbAttribute")
           '(2 . "")			; name

            (cons 7 (getvar "TEXTSTYLE"))

           '(70 . 0)))
  (setq elist (std-%force-3dpoint elist '(10)))
  (std-%entmake-template elist opts '(2) nil))     ;required: 2 - attname

;;; Not really required but nice to have.
;;; Fixes layer problem in R14 (see FAQ).
;;; v0.5003: Added layer argument which might be nil
(defun STD-ENTMAKE-SEQEND (layer / elist)
  (setq elist (if (stringp layer)
    (list '(0 . "SEQEND")(cons 8 layer))
    '((0 . "SEQEND"))))
  (if (std-%entmake-valid-group-p 100)
    (std-%entmake-call (append elist '((100 . "AcDbEntity"))) nil)
    (std-%entmake-call elist nil)))

(defun STD-ENTMAKE-LAYER (elist / opts name)
  (or (setq name (cdr (assoc 2 elist)))
      (std-error "STD-ENTMAKE-LAYER: require group 2"))
  (if (and (snvalid name)(not (tblsearch "LAYER" name)))
    (progn
      ;; 100 groups required for >=r14
      (setq opts (list
		  '(0 . "LAYER")
		  '(100 . "AcDbSymbolTableRecord")
		  '(100 . "AcDbLayerTableRecord")
		  '(2 . "")
		  '(70 . 0)
		  ))
      (std-%entmake-template elist opts '(2) nil))
    (std-error (list "STD-ENTMAKE-LAYER "
      (std-msg "Layer already exists or wrong name")))))

;;; Some properties are required, some optional
;;; more is a entget list which might contain groups
;;;  41  width, default 1.0
;;; (70 . 4) vertical flag
;;;  50  oblique angle, default 0.0
;;;  4   bigfont filename, default ""
;;; Based on code provided by Michael Puckett
(defun STD-ENTMAKE-STYLE (name font height more / opts)
  (if (and (snvalid name)(not (tblsearch "STYLE" name)))
    (progn
      (setq opts (list
         '(0 . "STYLE")
         '(100 . "AcDbSymbolTableRecord")
         '(100 . "AcDbTextStyleTableRecord")
         '(2 . "")     ; style name
         '(70 . 0)     ; 1 describes a shape, 4 vertical
         '(40 . 1.0)   ; height
         '(41 . 1.0)   ; width
         '(50 . 0.0)   ; oblique angle
         '(71 . 0)     ; 2 backwards, 4 upside down
         '(42 . 1.0)   ; last height used (?)
         '(3 . "txt")  ; default font file
         '(4 . "")     ; bigfont file name
          ))
      (std-%entmake-template
        (append (list (cons 2 name)
                      (cons 40 height)
                      (cons 3 font))
                more)
        opts '(2 40 3) nil))))

(defun STD-ENTMAKE-TEXT (elist / opts x)
  (setq x (cond (*last-insertion-point*) (T '(0 0 0))))

  (setq *last-insertion-point*
	 (if (not (assoc 10 elist))
	   (std-%textpt-below x)
	   (std-%textpt-below (cdr (assoc 10 elist)))))

  ;; 100 groups missing for r14, but not required
  (setq opts (list
           '(0 . "TEXT")
            (cons 10 x)

            (cons 40 (getvar "TEXTSIZE"))
            (cons 7 (getvar "TEXTSTYLE"))

           '(1 . "")))
  (setq elist (std-%force-3dpoint elist '(10)))
  (std-%entmake-template elist opts '(1) nil))  ;required: 1 - textvalue

(defun STD-ENTMAKE-MTEXT (elist / opts x)
  (setq x (cond (*last-insertion-point*) (T '(0 0 0))))

  (setq *last-insertion-point*
	 (if (not (assoc 10 elist))
	   (std-%textpt-below x)
	   (std-%textpt-below (cdr (assoc 10 elist)))))

  (setq opts (list
           '(0 . "MTEXT")
	   '(100 . "AcDbEntity") ; Subclass marker
      	   ;'(67 . 0)             ; Model space
           '(100 . "AcDbMText")  ; Subclass marker
            (cons 10 x)

            (cons 40 (getvar "TEXTSIZE"))

      	   '(41 .  100.0)        ; Rectangle width (100% ?)
      	   '(71 . 1)             ; Attachment point
           '(72 . 5)             ; Drawing Direction
           '(1 . "")

            (cons 7 (getvar "TEXTSTYLE"))

           '(11 1.0 0.0 0.0)     ; X Axis
            (cons 42 (* 0.8 (getvar "TEXTSIZE"))) ; Horizontal width of characters

            (cons 43 (getvar "TEXTSIZE"))         ; Vertical height of characters

           '(50 . 0.0)           ; Rotation angle in radians.
	   ))
  (setq elist (std-%force-3dpoint elist '(10)))
  (std-%entmake-template elist opts '(1) nil))  ;required: 1 - textvalue

(defun STD-ENTMAKE-CIRCLE (elist / opts)
  (setq opts (list
                '(0 . "CIRCLE")
		'(100 . "AcDbEntity")
		'(100 . "AcDbCircle")
                '(10 0 0 0)
                '(40 . 1)))
  (setq elist (std-%force-3dpoint elist '(10)))
  (std-%entmake-template elist opts '(10 40) nil))

(defun STD-ENTMAKE-POINT (elist / opts)
  (setq opts (list
                '(0 . "POINT")
		'(100 . "AcDbEntity")
		'(100 . "AcDbPoint")
                '(10 0 0 0)
                '(50 . 0.0)	;ECS rotation
                ))
  (setq elist (std-%force-3dpoint elist '(10)))
  (std-%entmake-template elist opts '(10) nil))

;;; (STD-ENTMAKE-PLINE  elst pts)
;;; Creates a simple 2D-polyline, either open or closed.
;;; Accepting either segment-list like ((seg1) (seg2)...),
;;; supporting bulges or a simple pointlist (=> straight pline)
;;; Creates Lwpolylines or 2D-polylines
;;; elist may be nil or contains layer info: ((8 . "0")) is enough
(defun STD-ENTMAKE-PLINE (elist pts / segs flag)
  ;; convert pointlist to segments
  (setq segs (if (std-pointp (car pts))
    	       (std-pts->segs pts)
               pts))
  (setq flag (std-getflag elist))
  ;; check for explicit straight closing segment:
  ;; we may safely omit it. check the closed flag accordingly
  (if (and (std-segs-closed-p segs)	     ; last segment = closing
	   (std-seg-straight-p (last segs))) ; and zero bulge
    ;; closed flag?
    (if (and flag (std-flagsetp 1 elist))    ; fixed 5-May-00
      ;; close: delete the segment, acad draws it for you.
      ;; FIXME with segs
      nil ; (setq segs (std-butlast segs))
      ;; open: change flag to closed and delete last segment
      (setq ;segs (if (std-pointp (car pts)) (std-butlast segs) segs)
	    elist (if flag
		    (subst (cons 70 (1+ flag)) (cons 70 flag) elist)
		    (cons '(70 . 1) elist)))))
  ;; lwpolyline?
  (if (and (std-ver-r14-p)

	   (not (zerop (getvar "PLINETYPE")))

           (/= "POLYLINE" (cdr (assoc 0 elist))))
    (std-entmake-lwpolyline elist segs)
    (std-entmake-polyline   elist segs)))

;;; General linear 2d-pline, needs segments to support bulges
;;; cannot be used for splined plines (no tangent info) or width
;;; segs: ((p1 p2 [bulge])(p2 p3 [bulge])...)
;;; elist may be nil or contain other properties, such ((8 . "0"))
(defun STD-ENTMAKE-POLYLINE (elist segs / opts flag seg)
  ;; check for pointlist instead of segmentlist
  (if (numberp (caar segs)) (setq segs (std-pts->segs segs)))
  ;; check again for last closing segment. if so set the flag
  (setq flag
    (cond
      ((cdr (assoc 70 elist)))
      ((and (std-segs-closed-p segs)
            (std-seg-straight-p (last segs)))
       1)
      (T 0)))
  (setq opts (list
              '(0 . "POLYLINE")
	      '(100 . "AcDbEntity")
	      '(100 . "AcDb2dPolyline")
	      '(66 . 1)
	       (cons 10 (list 0 0 (z-of (std-seg-p1 (car segs)))))
               (cons 70 flag)))
  (std-%entmake-template elist opts nil nil)
  ;; Add layer info, wrong layers might crash acad.
  (setq elist (std-%filter-groups '(8 60 67 62) elist))
  (setq opts '((0 . "VERTEX")
	       (100 . "AcDbEntity")
	       (100 . "AcDbVertex")
	       (100 . "AcDb2dVertex")
	       (70 . 0)))
  (foreach seg segs
    (std-%entmake-template (append
			     (list (cons 10 (std-seg-p1 seg))
	              	           (cons 42 (std-seg-bulge-num seg)))
			     elist)
    			  opts nil nil))
  ;; always add last vertex, 7-Jul-00
  (std-%entmake-template (list (cons 10 (std-seg-p2 (last segs))))
                         opts nil nil)
  (std-entmake-seqend (cdr (assoc 8 elist))))

;;; General linear LWPOLYLINE
;;; Needs segments to support bulges, segs: ((0 p1 p2)(0 p2 p3)...)
;;; elist may be nil or contain layer info: ((8 . "0"))
;;;   but points or bulges are ignored.
;;; nvert fix by Serge Pashkov
;;; Ignores segment specific widths
;;; Not release ready yet. Fails on r15 sometimes.
(defun STD-ENTMAKE-LWPOLYLINE (elist segs / opts flag
				     seg nvert zdir xflag)
  ;; check for pointlist instead of segmentlist
  (if (numberp (caar segs)) (setq segs (std-pts->segs segs)))
  ;; closed? if so set the flag
  (setq flag
    (cond
      ((cdr (assoc 70 elist)))
      ((and (std-segs-closed-p segs)
            (std-seg-straight-p (last segs)))
       1)
      (T 0)))
  ;; Provide number of vertices
  (setq nvert (1+ (length segs)))       ; 7-Jul-00
  (setq zdir (cond ((assoc 210 elist))	; 13-Mar-00
		   (T '(210 0 0 1))))
  ;; 2000-11-27: better defaults, because std-%entmake-template is broken
  (if (and (std-position '(0 . "LWPOLYLINE") elist)
	   (std-position '(100 . "AcDbEntity") elist)
	   (std-position '(100 . "AcDbPolyline") elist)
	   (< (std-position '(100 . "AcDbEntity") elist)
	      (std-position '(100 . "AcDbPolyline") elist)))
    (setq opts (append
		 (std-firstn (1+ (std-position '(100 . "AcDbPolyline")
					       elist))
		   elist)
                 (list (cons 90 nvert)
                       (cons 70 flag))))
    (setq opts (list '(0 . "LWPOLYLINE")
                   '(100 . "AcDbEntity")
                   '(100 . "AcDbPolyline")
                    (cons 90 nvert)     ; fixed
                    (cons 70 flag))))
  ;; Remove geometry from elist. Take only segs.
  (if (assoc 8 elist)
    (setq opts (append (std-firstn 3 opts)
		       (list (cons 8 (std-getlay elist)))
		       (std-nthcdr 3 opts)
		       )))
  (setq elist nil)
    ;(std-remove-if
    ; (function (lambda (_x)
    ;  (member (car _x) '(-1 0 100 90 70 5 8 330 410 10 40 41 42 210))))
					; elist))
  (foreach seg segs
    (setq elist (append elist (if (std-seg-bulge seg)
      (list (cons 10 (std-seg-p1 seg))(cons 42 (std-seg-bulge seg)))
      (list (cons 10 (std-seg-p1 seg))(cons 42 0.0)) ; 13-Mar-00
    ))))
  ;; always add last vertex, fixed 11/27/00
  (setq elist (append opts elist
		      (list (cons 10 (std-seg-p2 (last segs)))
			   '(42 . 0.0)
			    zdir)))
  ;; needed?
  ;(if (std-bitsetp 1 flag) (setq elist (append elist '((42 . 0.0)))))
  ;(setq elist (append elist (list zdir)))
  (std-%entmake-template elist nil nil nil))

;;; (STD-ENTMAKE-PFACE elist points faces)
;;; elist contains layer info: ((8 . "Layer0")) is enough, nil also.
;;; faces: ((-1 2 3)(3 4 -1)...)  - one based
;;; NOTE: The old zero-based backwards compatible function is called
;;;       STD-ENTMAKE-PFACE0
(defun STD-ENTMAKE-PFACE (elist pts faces / opts flag layer face pt)
  (setq flag (cond ((cdr (assoc 70 elist))) (0)))	;flag or 0
  (setq layer (cond ((cdr (assoc 8 elist))) ((getvar "CLAYER"))))
  (setq opts (list
              '(0 . "POLYLINE")
	      '(100 . "AcDbEntity")
	      '(100 . "AcDbPolyFaceMesh")
	      '(66 . 1)
	      '(10 0 0 0)
               (cons 70 (+ 64 (logand 128 flag)))))	; 64 or 192
  (std-%entmake-template elist opts nil nil)

  (setq opts '((0 . "VERTEX")
	       (100 . "AcDbEntity")
	       (100 . "AcDbVertex")
	       (100 . "AcDbPolyFaceMeshVertex")
	       (70 . 192)))	;points
  (foreach pt pts
    (if (std-2dpointp pt) (setq pt (std-new-z pt 0.0)))
    (std-%entmake-template (list (cons 10 pt)
				 (cons 8 layer))
      opts nil nil))

  (setq opts '((0 . "VERTEX")
	       (100 . "AcDbEntity")
	       (100 . "AcDbFaceRecord")
	       (70 . 128)))	;face indices
  ;; relevant attributes from elist for the vertices
  (setq elist (std-%filter-groups '(8 60 67 62) elist))
  (foreach face faces
    ;(if (not (cadddr face)) (setq face (append face (list (car face)))))
    (std-%entmake-template
      (append elist  ;; added 11-Jul-00
        (list '(10 0 0 0)
             (cons 71 (car face))
             (cons 72 (cadr face))
             (cons 73 (caddr face))
             (cons 74 (cond ((cadddr face))
	                    (T 0)))))
      opts nil nil))
  (std-entmake-seqend layer))

;;; Depricated, For backwards compatibility only.
;;; (STD-ENTMAKE-PFACE0 elst points faces)
;;; elist contains layer info: ((8 . "0")) is enough, () also
;;; faces: ((0 1 2)(-1 2 3)...)  - zero based
;;; NOTE: we have a problem if the first vertex must be invisible!
;;; We cannot define it this way if you use zero-based indices.
(defun STD-ENTMAKE-PFACE0 (elist pts faces)
  (std-verbose-print (list "\nSTD-ENTMAKE-PFACE0 "
			   (std-msg "is depricated")))
  (setq faces (mapcar (function (lambda (flst)
		(mapcar (function (lambda (i)
		  ;; 0 => 1, -1 => -2
		  (if (minusp i) (1- i)(1+ i))))
		  flst)))
		faces))
  (std-entmake-pface elist pts faces)
)

(defun std-%filter-groups (_groups elist)
  (std-remove-if-not (function (lambda (x) (member (car x) _groups)))
    elist))

(defun STD-ENTMAKE-3DPOLY (elist pts / opts flag seg)
  (setq flag (cond 		; closed?
      	       ((assoc 70 elist)
                (std-setbit 8 (cdr (assoc 70 elist))))
               ((equal (car pts) (last pts)) 9)
               (T 8)))
  (setq opts (list
              '(0 . "POLYLINE")
	      '(100 . "AcDbEntity")
	      '(100 . "AcDb3dPolyline")
	      '(66 . 1)
	      '(10 0 0 0)
               (cons 70 flag)))
  (std-%entmake-template elist opts nil nil)
  ;; relevant attributes from elist for the vertices
  (setq elist (std-%filter-groups '(8 60 67 62) elist))
  (setq opts '((0 . "VERTEX")
	       (100 . "AcDbEntity")
	       (100 . "AcDbVertex")
	       (100 . "AcDb3dPolylineVertex")
	       (70 . 32)))
  (foreach pt pts
    (if (std-2dpointp pt) (setq pt (std-new-z pt 0.0)))
    (std-%entmake-template (cons (cons 10 pt) elist) opts nil nil))
;;;  (if (and (std-bitsetp 1 flag)	; closed logic
;;;	   (not (equal (car pts) (last pts))))
;;;    (std-%entmake-template (list (cons 10 (car pts))) opts nil nil))
  (std-entmake-seqend (cdr (assoc 8 elist))))

;;; -------------------------------------------------------------------73
;;; The following functions were all written completely by David Kozina

(defun STD-ENTMAKE-LINE (elist / opts)
  (setq opts (list
                '(0 . "LINE")
                '(100 . "AcDbEntity")
                '(100 . "AcDbLine")
                '(10 0 0 0)
                '(11 1 0 0)))
  (setq elist (std-%force-3dpoint elist '(10 11)))
  (std-%entmake-template elist opts '(10 11) nil)
)

;;; no MTEXT support yet. Only the arrow and the lines
;;; partially by David Kozina
(defun STD-ENTMAKE-LEADER (elist / opts pts)
  (setq opts (list
                '(0 . "LEADER")
                '(100 . "AcDbEntity")
                '(100 . "AcDbLeader")
                (cons 3 (getvar "DIMSTYLE"))
                '(71 . 1)
                '(72 . 0)
                '(73 . 3)
                '(74 . 0)
                '(75 . 0)
                '(40 . 0.0)
                '(41 . 0.0)
                '(76 . 2)
;;;                '(10 0 0 0)	; fixed v0.4001
;;;                '(10 1 0 0)
                '(77 . 256)
                '(210 0 0 1)
                '(211 1 0 0)
                '(212 0 0 0)))
  (setq pts (std-%group-only 10 elist))
  (if (> (length pts) 1)
    (progn
      (setq elist (subst (cons 76 (length pts))
			 (assoc 76 elist) elist))
      ;;(setq elist (std-%force-3dpoint elist '(10 210 211 212)))
      (std-%entmake-template elist opts '(10) nil))))

(defun STD-ENTMAKE-ARC (elist / opts)
  (setq opts (list '(0 . "ARC")
                   '(100 . "AcDbEntity")
                   '(100 . "AcDbCircle")
                   '(10 0 0 0)           ; Center
                   '(40 . 1.0)           ; Radius
                   '(210 0 0 1)
                   '(100 . "AcDbArc")
                   '(50 . 0)             ; Start Angle (Radians)
                   (cons 51 *PI/2*)))    ; End Angle (Radians)
  (setq elist (std-%force-3dpoint elist '(10)))
  (std-%entmake-template elist opts '(10 40 50 51) nil))

(defun STD-ENTMAKE-ELLIPSE (elist / opts)
  (setq opts (list '(0 . "ELLIPSE")
                   '(100 . "AcDbEntity")
                   '(100 . "AcDbEllipse")
                   '(10 0 0 0)           ; Center
                   '(11 2 0 0)           ; Major Axis Endpoint
                   '(40 . 0.5)           ; Ratio: Minor Axis / Major Axis
                   '(41 . 0)             ; Start Parameter
					 ; 0.0 for Full Ellipse
                   (cons 42 *PI2*)       ; End Parameter
		   			 ; 2*PI for Full Ellipse
                   '(210 0 0 1)))
  (setq elist (std-%force-3dpoint elist '(10 11)))
  (std-%entmake-template elist opts '(10 11 40) nil))

;; (STD-ENTMAKE-GROUP elist) - Group Entmake Function
;;   In R14, this function can create a group, but only an empty group.
;;   Of very limited usefulness - if useful at all.
;;   Untested in A2000.
;;
;; Example:


(defun STD-ENTMAKE-GROUP (elist / opts)
  (std-require "DICT")
  (cond
   ;; Make sure the group name doesn't exist
   ((not (DICT-SEARCH "ACAD_GROUP" (setq name_str (std-getval 2 elist))))
    (setq elist (std-remove-if
		  (function (lambda (_x) (member (car _x) '(2)))) elist)
	  opts  (list '(0 . "GROUP")
		      '(102 . "{ACAD_REACTORS")
		      (cons 330 (DICT-ENTITY "ACAD_GROUP"))
		      '(102 . "}")
		      '(100 . "AcDbGroup")
		      '(300 . "")  ; Description
		      '(70 . 0)    ; "Unnamed" Flag Values:
					; 0 - Named, 1 - Unnamed
					; 2 - BlockGroup?
		      '(71 . 1))   ; Selectability Flag:
				   ; 0 - not selectable, 1 - selectable
	  grp_ent (std-%entmake-template elist opts '() T))
    ;; this crashes R14 if the elist contains any group 340 ents
    (dictadd (DICT-ENTITY "ACAD_GROUP") name_str grp_ent)
    ))
  )

;;; Testcode for the func above:
;;; Following lines necessary as well? (unable to test in R14):


;;(STD-ENTMAKE-HATCH elist bdy_ents pathflag_lst) - Hatch Entmake
;;
;; Use data from closed polylines or circles or seglists to entmake
;; non-associative (only) hatch.  (use VLR for associative hatches)
;; Must extract vertices, bulges, # sides from plines, seglists
;;
;; Pathflag_lst => (flag1 flag2 flag3...)
;; => '(92 . flag1) '(92 . flag2) '(92 . flag3)...
;;
;; Example: '(0 0 0) for a hatch w/ 3 boundary loops
;;   Boundary Path Type:
;;    0 - Default
;;    1 - External
;;    2 - Polyline
;;    4 - Derived
;;    8 - Textbox
;;   16 - Outermost
;;
;;; only non-associative hatches yet
;;; highly experimental!
(defun STD-ENTMAKE-HATCH (elist bdy_ents pathflag_lst /
				_elev _segs_lst opts data_alst
			  	330_list rct_list ent_list)
  (cond
    ((and bdy_ents			; Entity list argument?
	  (apply 'and
		 (mapcar		; bdy_ents OK?
		   (function (lambda (ele)
		      (or (STD-ENTITY-TYPE-P ; Pline or Circle?
			    ele
			    '("LWPOLYLINE" "POLYLINE" "CIRCLE"))
			  (listp ele))))	; Seglist?
		   bdy_ents))
	  (apply
	    'and
	    (mapcar			; Check members of bdy_ents
	      (function (lambda (ele / _segs)		; and get an elev
		 (cond ((= (STD-GETTYPE ele) "CIRCLE") ; CIRCLE
			(if (not _elev)
			  (setq _elev (THIRD (STD-GETVAL 10 ele))))
			(setq _segs_lst (cons (cons "CIRCLE" ele)
					      _segs_lst)))
		       ((= (STD-GETTYPE ele) "POLYLINE")
					; Closed POLYLINE?
			(if (not _elev)
			  (setq _elev (THIRD (STD-GETVAL 10 ele))))
			(if (STD-SEGS-CLOSED-P
			      (setq _segs (STD-PLINE-SEGS ele)))
			  (setq _segs_lst (cons _segs _segs_lst))))
		       ((= (STD-GETTYPE ele) "LWPOLYLINE")
					; Closed LWPOLYLINE?
			(if (not _elev) (setq _elev (STD-GETVAL 38 ele)))
			(if (STD-SEGS-CLOSED-P
			      (setq _segs (STD-PLINE-SEGS ele)))
			  (setq _segs_lst (cons _segs _segs_lst))))
		       (T		; Seglist?
			(if (not _elev) (setq _elev 0))
			(if (STD-SEGS-CLOSED-P ele)
			  (setq _segs_lst (cons ele _segs_lst)))))))
	      bdy_ents)))
      ;; cond and clause => T
      (setq _segs_lst  (reverse _segs_lst) ; Since _segs_lst is reverse
	    data_alst
	    (mapcar		 ; of bdy_ents, reverse again
	     (function (lambda (ele)
	       (cond
		((= (car ele) "CIRCLE")	; Create data_alst - circle
		 (list
		  '(93 . 1) ; Number of edges this loop
		  '(72 . 2) ; Return alst
		  (cons 10 (XY-OF (STD-GETVAL 10 (cdr ele))))
		  (cons 40 (STD-GETVAL 40 (cdr ele)))
		  '(50 . 0.0)
		  (cons 51 *PI2*)
		  '(73 . 1)
		  '(97 . 0)))
		;; Source bdy objects? (not yet)
		(T			; Create data_alst - seglist
		 (cons
		  (cons 93 (length ele)); Number of edges this loop
		  (append
		   (apply
		    'append
		    (mapcar
		     (function (lambda (seg / ccw_flag ctr)
		       (cond
			((STD-SEG-STRAIGHT-P seg) ; Line Edge?
			 (list
			  '(72 . 1) ; Return alst
			  (cons 10 (XY-OF (STD-SEG-P1 seg)))
			  (cons 11 (XY-OF (STD-SEG-P2 seg)))))
			(T ; Arc Edge?
			 (setq ccw_flag ; CCW?
			       (/(1+(std-signum(std-seg-bulge-num seg)))
				2))
			 (append
			   (list
			    '(72 . 2) ; Return alst
			     (cons 10
			      (setq ctr(first(xy-of(std-seg->cir seg)))))
			     (cons 40 (SECOND (STD-SEG->CIR seg)))
			   )
			   (cond
			    ((zerop ccw_flag) ; CW
			     (list
			      (cons 50 (- *PI2*
			        (angle ctr (XY-OF (STD-SEG-P1 seg)))))
			      (cons 51 (- *PI2*
				(angle ctr (XY-OF (STD-SEG-P2 seg)))))
			      ))
			     (T		; CCW
			      (list
				(cons 50
				  (angle ctr(xy-of(std-seg-p1 seg))))
				(cons 51
				  (angle ctr(xy-of(std-seg-p2 seg))))
				)))
			   (list (cons 73 ccw_flag)))))))
				      ele))
				  '((97 . 0))))))))
			;; Source bdy objects? (not yet)
			_segs_lst)
	    data_alst (apply 'append	; Append data_alst after
			     (mapcar
			       'cons	; Consing pathtype flag alst
			       (mapcar
				 'cons	; to each data_alst member
				 (STD-MAKE-LIST (length pathflag_lst) 92)
				 pathflag_lst)
			       data_alst))
	    opts (append
		   (list
		     '(0 . "HATCH")	; Entity Type
		     '(100 . "AcDbEntity")
		     (cons 67
		       (cond ((STD-GETVAL 67 bdy_ents)) ; Space
			     (T		; Current Space if seglist
			      (STD-BITTOGGLE (getvar "TILEMODE") 1))))
		     '(100 . "AcDbHatch")
		     (list 10 0 0 _elev) ; Elevation
		     (cons 210
			   (cond ((STD-GETVAL 210 bdy_ents))
					; Extrusion Vector
				 (T	; Current Ext Vect if seglist
				  (trans '(0 0 1) 1 0 T))))
		     '(2 . "SOLID")	; Hatch Pattern Name
		     '(70 . 1)		; Solid Fill Flag:
					;    0 - Pattern Fill
					;    1 - Solid Fill
		     '(71 . 0))		; Non-associative flag
		   ;; (cond ((= (STD-GETVAL 71 elist) 0)
		   ;; Associativity Flag:
		   ;;   '((71 . 0)))     ; 0 - Non-associative
		   ;;  (T                ; 1 - Associative
		   ;;   '((71 . 1)))     ; we must use OLE
		   ;;  )
		   (list (cons 91 (length _segs_lst)))
		   ;; Number of Bdy Loops
		   data_alst		; Bdy_ents Data
		   ;;(cond ((= (STD-GETVAL 71 elist) 0) ; Boundary Objs
		   ;;      '((97 . 0))            ; Non-associative only
		   ;;      )
		   ;;      (T       ; Yes (won't happen)
		   ;;       (list '(97 . 1)
		   ;;             (cons 330 (STD-ENTITY bdy_ents))))
		   ;;      )
		   (list '(75 . 0)	; Hatch Style:
					;    0 - Normal,
					;        "Odd Parity"
					;    1 - Hatch Outermost
					;        Area Only
					;    2 - Hatch Entire
					;        Area, "holes"
					;        included
			 '(76 . 1)	; Hatch Pattern Type:
					;    0 - User-Defined
					;    1 - Predefined
					;    2 - Custom
			 '(98 . 1)	; Number of Seed Points
			 '(10 0 0)))	; Seedpoint
	    elist     (STD-REMOVE-IF
			(function (lambda (_x)
			   (member (car _x) '(71 91 92 93 97 330))))
			elist))
      (cond (T  ;(= (STD-GETVAL 71 elist) 0) ; Non-associative Hatch
	     (std-%entmake-template elist opts '() nil))
	    (nil                          ; Associative Hatch (not easy)
	      (list elist opts)
	      ;; Return name of new hatch
	      (setq hatch_ent (std-%entmake-template elist opts '() nil)
	            ent_list  (std-entget bdy_ents)
	            rct_list  (append (std-%group-only 330 ent_list)
	                              (list hatch_ent)
	                      )
	            330_list  (append	; Add hatch_ent to existing
	                        '((102 . "{ACAD_REACTORS")) ;=> 330 ents
	                        (mapcar (function (lambda (_ele)
				          (cons 330 _ele)))
	                                (cons hatch_ent
					 (std-%group-only 330 ent_list)))
	                        '((102 . "}"))
	                      )
	            ent_list  (std-remove-if
				(function(lambda (_x)
			          (member (car _x) '(102 330))))
				ent_list)
	            ent_list  (append ent_list 330_list)
	            )
	      (entmod ent_list)
	      (entupd bdy_ents)
	      (foreach ele elist
	        (setq ent_list (std-entget ele)
	              330_list (append
	                       '((102 . "{ACAD_REACTORS"))
	                       (mapcar
				(function(lambda (_ele) (cons 330 _ele)))
				(cons grp_ent
				      (std-%group-only 330 ent_list)))
	                       '((102 . "}"))
	                     )
	              ent_list (std-remove-if
				 (function(lambda (_x)
				   (member (car _x) '(102 330))))
				 ent_list)
	              ent_list (append ent_list 330_list))
	      (entmod ent_list)) ;_ end of foreach
	      (STD-ENTCHG bdy_ents nil
	             (append '(102)
			     (std-make-list (length elist) 330) '(102))
	             (append '("{ACAD_REACTORS") elist  '("}")))
	      (entupd bdy_ent)
	      (entupd hatch_ent)
	    )
	    ))))

;;;  (STD-ENTMAKE-SOLID elist) - Solid Entmake  ---  TEMPORARY
;;; experimental!
(defun STD-ENTMAKE-SOLID (elist / opts)
  (setq opts (list '(0 . "SOLID")
                   '(100 . "AcDbEntity")
                   '(100 . "AcDbTrace")
                   ;;'(39 . 0.0)           ; Thickness
                   ;;'(210 0 0 1)          ; Extrusion Direction
                   ))
  (std-%entmake-template elist opts '(10 11 12 13) nil))

;;; experimental!
(defun STD-ENTMAKE-SPLINE (elist pts / opts flag nvert pttype)
  ;; closed?
  (setq flag
    (cond
      ((cdr (assoc 70 elist)))
      ((and (assoc 73 elist)
	    (std-segs-closed-p (std-pts->segs pts))) 9) ; Control Points?
      ((and (assoc 74 elist)
	    (std-segs-closed-p (std-pts->segs pts))) 9) ; Fit Points?
      (T 8)))
  ;; provide number of vertices
  (setq nvert (if (std-bitsetp 1 flag) ; fixed
                  (1+ (length pts))
    (length pts)))
  (setq opts
	(list '(0 . "SPLINE")
	      '(100 . "AcDbEntity")
	      '(100 . "AcDbSpline")
	      (cons 70 flag)    ; Spline Flag - bit coded:
	      			; 8 - Planar, 9 - Closed Planar
	      '(71 . 3)         ; Default degree of spline curve
	      (cond ((assoc 73 elist) (cons 73 nvert)) ; # Control pts or
		    ((assoc 74 elist) (cons 74 nvert)) ; # Fit Points
		    )))
  (setq pttype (cond ((assoc 73 opts) 10)
                     ((assoc 74 opts) 11)))
  ;; remove geometry from elist, take only pts.
  (setq elist (std-remove-if
    '(lambda (_x) (member (car _x) '(10 11 73 74))) elist))
  (foreach pt pts
    (setq elist (append elist (list (cons pttype pt))))) ; Point data
  (std-%entmake-template elist opts nil nil)
)

;;  STD-ENTMAKE-MLINESTYLE Usage Example:
;;  (setq elist
;;    '((2 . "MYSTYLE")
;;      (3 . "My MLinestyle")
;;      (62 . 0)
;;      (70 . 0)
;;      (71 . 3)
;;      (49 . -5.0)
;;      (62 . 256)
;;      (6 . "BYLAYER")
;;      (49 . 0.0)
;;      (62 . 152)	;; color
;;      (6 . "ABE05")	;; ??
;;      (49 . 5.0)
;;      (62 . 256)
;;      (6 . "BYLAYER")))
;;  (STD-ENTMAKE-MLINESTYLE elist)
(defun STD-ENTMAKE-MLINESTYLE (elist / opts)
  (std-require "DICT")
  (setq opts (list
	      '(0 . "MLINESTYLE")
	      '(102 . "{ACAD_REACTORS")
	      (cons 330 (dict-entity "ACAD_MLINESTYLE"))
	      '(102 . "}")
	      '(100 . "AcDbMlineStyle")
	      '(70 . 0)
	      ;; End Cap Flag Values: 0 - No caps; 16 - Cap first end
	      ;;   256 - Cap last end; 272 - Cap each end;
	      ;;   See also AutoCAD Customization Guide, Appendix C -
	      ;;   "ACAD_MLINESTYLE Group Codes" Table for other values
	      ;;'(62 . 0)    ; Fill Color, if flag 70 has 1 set
	      (cons 51 (/ PI 2))   ; Start Angle
	      (cons 52 (/ PI 2)))) ; End Angle
  (dictadd (dict-entity "ACAD_MLINESTYLE")
           (std-getval 2 elist)
           (std-%entmake-template elist opts '(2 3 71 49 62 6) T)))

(defun STD-ENTMAKE-XRECORD (elist / opts)
  (setq opts (list '(0 . "XRECORD")
		   '(100 . "AcDbXrecord")
		   '(280 . 1)))
  (std-%entmake-template elist opts nil T))



;;; -------------------------------------------------------------------73
(if std-%protect-assign (std-%protect-assign *ENTMAKE-SYMBOLS*))
(setq *ENTMAKE-SYMBOLS* nil)

;;; Module dependencies:
(std-provide "ENTMAKE") 	; At first provide it. But before you may
			 	; call it, be sure to have all supporting
				; functions.
(std-require "STDPOINT")  	; segment stuff
(std-require "STDENT")  	; attributes
				; also some LIST, LISP and INIT stuff

;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: DICT.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:31:16 rurban>
;;; Copyright (c) 1999 by David Kozina
;;; Copyright (c) 2000 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Dictionaries as seperate STDLIB module
;;; A 'dict' dictionary entity may be defined as ename or string, the
;;; valid (and in A2000 even case-sensitive) name.

;;; STATUS:
;;;  not fully tested.
;;;  seperated, new namespace with DICT- prefix.
;;;  so far only querying functions, 
;;;  destructive operations (dict-put) and converter functions
;;;    later. See http://members.tripod.com/~vnestr/SaveData.lsp for a 
;;;    commercial version.


;;; ===================================================================73
;;; $Log: DICT.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; 2000-09-23 19:44:54 rurban
;;;   added DICT-GET, DICT-PUT, DICT-LIST->XDATA, DICT->XDATA->LIST
;;;   (DICT-LIST->XDATA only non-recursive yet)
;;; Revision 0.5004  2000/09/20 12:50:35  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; 2000-07-07 12:02:18 rurban
;;;   added std-%(un)protect-assign wrapper
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;
;;; internal functions start with std-%
;;; ===================================================================73

(if std-%unprotect-assign
  (std-%unprotect-assign
    '(DICT-VERSION DICT-P DICT-ENTITY DICT-SEARCH DICT-VAL DICT-LIST
      DICT-NAMES 
      DICT-GET DICT-PUT DICT-LIST->XDATA DICT->XDATA->LIST)))


(setq *DICT-VERSION* 0.4)
(defun DICT-VERSION () *DICT-VERSION*)

;;; -------------------------------------------------------------------73
;;;  (DICT-P dict)
;;;  Is 'dict' one of the dictionaries in (namedobjdict)?
;;;  'dict' may be a dictionary entity or valid name
;;;  Usage:  (DICT-P "ACAD_GROUP")           => T
;;;          (DICT-P "ACAD_MLINESTYLE")      => T
;;;          (DICT-P (namedobjdict))         => T
;;;          (DICT-P "group")                => nil
;;;		(Note that the name must be valid)
;;;          (DICT-P "mlinestyle")           => nil
;;;          (DICT-P "goober")               => nil
;;; -------------------------------------------------------------------73
(defun DICT-P (dict)
  (cond ((and (std-enamep dict)
	      (member dict (mapcar (function cdr)
				   (std-entget (namedobjdict))))))
        ((and (stringp dict)
	      (dictsearch (namedobjdict) dict)))))

;;; -------------------------------------------------------------------73
;;;  (DICT-ENTITY dict)
;;;  Return the dictionary entity name of 'dict' if valid, else return nil
;;;  'dict' may be a dictionary entity or valid name
;;;  Usage:  (DICT-ENTITY "ACAD_GROUP")      => <Entity name: xxxx>
;;;          (DICT-ENTITY "ACAD_MLINESTYLE") => <Entity name: yyyy>
;;;          (DICT-ENTITY (namedobjdict))    => <Entity name: zzzz>
;;;          (DICT-ENTITY "group")           => nil  invalid name
;;;          (DICT-ENTITY "mlinestyle")      => nil
;;;          (DICT-ENTITY "goober")          => nil
;;; -------------------------------------------------------------------73
(defun DICT-ENTITY (dict)
  (cond ((and (std-enamep dict)
	      ;; this should better check group 350 only
	      (member dict (mapcar (function cdr)
				   (std-entget (namedobjdict)))))
	 dict)
        ((stringp dict) (std-entity (dictsearch (namedobjdict) dict)))))

;;; -------------------------------------------------------------------73
;;;  (DICT-SEARCH dict name)
;;;  Return a dictionary entry alst upon valid input, else return nil
;;;  'dict' may be a dictionary entity or valid name
;;; -------------------------------------------------------------------73
(defun DICT-SEARCH (dict name)
  (if (and (setq dict (dict-entity dict)) (stringp name))
    (dictsearch dict name)))

;;; -------------------------------------------------------------------73
;;; (DICT-VAL grp dict name)
;;; Return dxf group value of a dictionary entry alst upon valid input,
;;; else return nil
;;; 'dict' may be a dictionary entity or valid name
;;; -------------------------------------------------------------------73
(defun DICT-VAL (grp dict name)
   (std-getval grp (dict-search dict name)))

;;; -------------------------------------------------------------------73
;;; (DICT-LIST dict)
;;; Return a dictionary alist or nil
;;; 'dict' may be a dictionary entity or valid name
;;; -------------------------------------------------------------------73
(defun DICT-LIST (dict / alst lst)
  (cond
    ;; complicated way to get alst: force the first two cond's
    ((and (std-enamep dict) ; is dict one of the dictionary entities
          (member dict (mapcar (function cdr)
			       (std-entget (namedobjdict))))
          (setq alst (entget dict))
          nil))	; always nil
    ((and (stringp dict)                   ; is dict a dictionary name?
          (setq alst (dictsearch (namedobjdict) dict))
          nil))	; always nil
    ;; get name and entities from alst
    ((setq lst (mapcar (function cdr) (member (assoc 3 alst) alst)))
      (mapcar (function (lambda (a b) (cons a b))) ; build alist
              (std-remove-if-not (function stringp) lst)
              (std-remove-if (function stringp) lst)))))

;;; -------------------------------------------------------------------73
;;;  (DICT-NAMES dict)
;;;  Return a dictionary name strlst or nil
;;;  'dict' may be a dictionary entity or valid name
;;; -------------------------------------------------------------------73
(defun DICT-NAMES (_dict)
  (mapcar (function car) (dict-list _dict)))

;;; -------------------------------------------------------------------73
;;; By Reini Urban
;;; Status: not tested yet.

;;; (DICT-GET dict key)
;;; Similar to vladimir's version. 
;;;   See http://members.tripod.com/~vnestr/SaveData.lsp
;;; Decode xdata-alike, dwg-friendly alist data from XRecords in 
;;;   a dictionary.
;;; 'dict' may be a dictionary entity or valid name
(defun DICT-GET (dict key / x)
  (setq x (cdr (member '(100 . "AcDbXrecord")
    	                (dict-search dict key))))
  ;; remove group 280 in R15
  (dict-xdata->list (if (= (caar x) 280) (cdr x) x)))

;;; (DICT-PUT dict key value)
;;; Similar to vladimir's version. Encode arbitrary data into 
;;;   xdata-alike, dwg-friendly XRecords.
;;; The dictionary (a string then) is created automatically if not 
;;;   existing. dict may be a dictionary entity or valid name
(defun DICT-PUT (dict key value)
  (if (dict-search dict key)
    (dictremove (dict-entity dict) key))
  (if (and (stringp dict) 
	   (not (dict-entity dict)))
    (setq dict ; create new dict
           (dictadd (namedobjdict)
             dict
	     (entmakex '((0 . "DICTIONARY")(100 . "AcDbDictionary"))))))
  (dictadd (dict-entity dict)
             key
	     (entmakex  ; maybe use STD-ENTMAKE-XRECORD
	       (append (list '(0 . "XRECORD")
			     '(100 . "AcDbXrecord"))
		       (if (std-ver-r2000-p) '((280 . 1))) ; keep existing
		       (dict-list->xdata value t)))))      ; encode < 1000

;;; EED-GET/EED-PUT are similar methods for entity specific data.

;;; non-nil <1000 encodes to dxf keys < 1000 for xrecords in dictionaries,
;;; nil to XDATA values > 1000 for extended entity data.
;;; no nested lists so far, no recursion into () besides numbers
;;;  (dict-list->xdata '(20 "data") t) 
;;;     => ((2 . "{")(40 . 20) (0 . "data")(2 . "}"))
;;;  (dict-list->xdata '(20 "data") nil)
;;;     => ((1002 . "{")(1070 . 20) (1000 . "data")(1002 . "}"))
(defun DICT-LIST->XDATA (xdata <1000)
  (apply 'append (list
    (if <1000 '((2 . "{")) '((1002 . "{")))
    ;; no recursion here
    (if <1000 (mapcar 'dict-%xdata-encode<1000 xdata)
	      (mapcar 'dict-%xdata-encode xdata))
    (if <1000 '((2 . "}")) '((1002 . "}"))))))

;;; (dict->xdata->list '((2 . "{")(40 . 20) (0 . "data")(2 . "}"))) 
;;;   => (20 "data")
;;; only flat, no nested lists! no recursion into "{" yet.
(defun DICT-XDATA->LIST (xdata)
  (cond 
   ((and (equal (car xdata)  '(2 . "{"))
	 (equal (last xdata) '(2 . "}")))
    (if (member '(2 . "{") (cdr xdata))
	(std-warn "nested xdata lists not yet supported"))
    (mapcar (function dict-%xdata-decode<1000)
            (cdr (std-butlast xdata))))
   ((and (equal (car xdata)  '(1002 . "{"))
	 (equal (last xdata) '(1002 . "}")))
    (if (member '(1002 . "{") (cdr xdata))
	(std-warn "nested xdata lists not yet supported"))
    (mapcar (function dict-%xdata-decode)
            (cdr (std-butlast xdata))))
  ))

;;; TODO: maybe improve fix for longint: 1041<=>1071
(defun dict-%xdata-encode<1000 (x)
  (if (= (car (setq x (dict-%xdata-encode x))) 1005)
    (cons 2 (cdr x)) ; handle as string, but how to mark as entity?
    (cons (- (car x) 1000) (cdr x))))

(defun dict-%xdata-encode (x)
  (cond
   ((null x) 		'(1000 . "*NIL*")) ; for easier debugging
   ((stringp x) 	(cons 1000 x))
   ((std-pointp x) 	(cons 1010 x))
   ((eq (type x) 'REAL) (cons 1040 x))
   ((and (numberp x)(< -32768 x 32767))
    			(cons 1070 x))
   ((numberp x)		(cons 1041 (float x))) ; instead of (1071 . (fix x))
   ;; handles, not decodable! (for nice to have in Xdata)
   ((eq (type x) 'ENAME) (cons 1005 (std-gethandle x))) 
   ((listp x)
    (dict-list->xdata x <1000))
   (T (std-error (list "unsupported xdata type: " x)))
   ))

(defun dict-%xdata-decode (x)
  (cond
   ((null x) nil)
   ((equal x '(1000 . "*NIL*")) nil)
   ((= (car x) 1041) (fix (cdr x)))
   ;; how to detect handles?
   (T (cdr x))))

(defun dict-%xdata-decode<1000 (x)
  (cond
   ((null x) nil)
   ((equal x '(0 . "*NIL*")) nil)
   ((= (car x) 41) (fix (cdr x)))
   ;; how to detect handles?
   (T (cdr x))))

;;; recursive version
(defun dict-xdata->list-rec (xdata / tok)
  (cond 
   ((and (equal (car xdata)  '(2 . "{"))
	 (equal (last xdata) '(2 . "}")))
    (car (dict-%xdata-decode-rec)))
   ((and (equal (car xdata)  '(1002 . "{"))
	 (equal (last xdata) '(1002 . "}")))
    (car (dict-%xdata-decode-rec)))))

(defun dict-%xdata-decode-rec (/ tok)
  (if (atom xdata)
    xdata
    (if (not (equal (setq tok (std-pop 'xdata)) '(1002 . "}")))
      (cons
        (if (equal tok '(1002 . "{"))
          (dict-%xdata-decode-rec)
          (dict-%xdata-decode tok))
        (dict-%xdata-decode-rec)))))

(defun dict-%xdata-decode-rec<1000 (/ tok)
  (if (atom xdata)
    xdata
    (if (not (equal (setq tok (std-pop 'xdata)) '(2 . "}")))
      (cons
        (if (equal tok '(2 . "{"))
          (dict-%xdata-decode-rec)
          (dict-%xdata-decode<1000 tok))
        (dict-%xdata-decode-rec)))))


;;; -------------------------------------------------------------------73

(if std-%protect-assign
  (std-%protect-assign
    '(DICT-VERSION DICT-P DICT-ENTITY DICT-SEARCH DICT-VAL DICT-LIST
      DICT-NAMES
      DICT-GET DICT-PUT DICT-LIST->XDATA DICT->XDATA->LIST)))


;;; Module dependencies:
(if std-provide
  (progn

(std-provide "DICT") 		; At first provide it. But before you may
			 	; call it, be sure to have all supporting
				; functions.
(std-require "STDLIST")
(std-require "STDENT")

))
;;; >>cut here  ---------------------------------------------- <<cut here
;$Id: STDLIB.LSP 0.5006 2000/12/31 10:59:00 rurban Rel $ -*-AutoLISP-*-
;;; Time-stamp: <2000-12-31 11:32:50 rurban>
;;; Copyright (c) 1998,99 by Reini Urban
;;; Available for free at http://xarch.tu-graz.ac.at/autocad/stdlib/
;;; See Disclaimer for permissions
;;; --------------------------------------------------------------------
;;; Handy loader for the main STDLIB functions

;;; STATUS:
;;; Not complete yet!
;;; See STDLIB.HLP for the complete documentation

;;; Logical layout for the STDLIB


;;; ===================================================================73
;;; $Log: STDLIB.LSP $
;;; Revision 0.5006  2000/12/31 10:59:00  rurban
;;;   Release, see Changes
;;;
;;; Revision 0.5004  2000/09/20 12:50:35  rurban
;;;   0.5004 release, see Changes
;;;
;;; Revision 0.5003  2000/07/11 15:59:52  rurban
;;;   0.5003 release, see Changes
;;;
;;; Revision 0.5002  2000/06/19 08:31:18  rurban
;;;   0.5002 release, see Changes
;;;
;;; Revision 0.5000  2000/05/14 17:41:36  rurban
;;; 0.5 release
;;;

;;; -------------------------------------------------------------------73
;;; Dealing with projects
;;; This code is needed for closing a project definition.

;;; Last file of a project or first file or dynamic module loading.

;;; This is for the project part:
;;; Now we may safely call our top-level stuff.
;;; Remove std-require delaying for further stdlib calls.
;;; We may do this here because with projects this will be the last file.
;;; Without packaged projects this will be the first file.
(if *STD:%PROJECT*
  (progn
    (stdlib-project-init)		; STDINIT2

    ;;; Print the list if delayed modules (for debugging only)
    (if (and *LOAD-VERBOSE* *STD:MODULES-DELAYED*)
      (std-princ (list
	(std-msg "\nLoaded Modules: ")
	(std-modules)
        "\n")))

    (setq *STD:%PROJECT* nil)
    ))

;;; Better put this to your ACAD.LSP
;;; Must be customized, may be set by the calling application.
;;; Best is first defining the STDLIB location.
;;; Here we need only the STDLIB path anyway, with \\ at the end,
;;; The full definition can be given after having loaded all modules
;;;(if (not (boundp '*MODULE-PATH*))
;;;  (setq *MODULE-PATH* '("l:\\src\\STDLIB\\")))

;;; -------------------------------------------------------------------73

;;; load the first required module
(if (not std-%simple-require)
  (load (strcat (car *MODULE-PATH*) "STDINIT") 'ERROR))

;;; Loading scheme:
;;; We first load all with STD-%SIMPLE-REQUIRE
;;; After loaded STDLISP and STDFILE we can savely use STD-REQUIRE.
;;; A faster method is obviously to define *MODULE-PATH* and just load
;;; the FAS or VLX
(std-%simple-require "STDSTR")
(std-%simple-require "STDLIST")
(std-%simple-require "STDLISP")
(std-%simple-require "STDMATH") ; load it before STDFILE for same flag
                                ; checking on errors
(std-%simple-require "STDFILE")
(std-%simple-require "STDPOINT")
(std-%simple-require "STDINIT2")

(setq	*PRINT-LENGTH* 25	; don't print long lists to the console
	*PRINT-LEVEL*  4)

(if (std-acad-connection-p)
  (progn
    (std-require "STDINPUT")
    (std-require "STDTBL")
    (std-require "STDENT")
    (std-require "STDMISC")
    (std-require "STDERROR")
    (std-require "ENTMAKE")
  )
)
(std-require "STDTIME")	; so far acad independent
(std-provide "STDLIB")

;; Include this also? Only for VL provided so far
(if (and (std-vl-p)
         (>= (std-ver-num) 12))
 (progn
;  (std-require "BINIO")
;  (std-require "INIFILE")
  (std-require "REGISTRY")
))

;;; here we need load to force redefinitions
(if (not (std-acad-connection-p))
  (std-load "STANDALONE"))

;;;(if (STD-VLIDE-P)
;;;  (progn
;;;    (std-require "STDINIT-IDE")
;;;    (std-%protect-allsyms)
;;;    ))



;;; Add customized paths, these are searched with std-load
;;; before the acad library path

;;; >>cut here  ---------------------------------------------- <<cut here
