;; ---------------------------------------------------------------------------
;; Function: ASO
;; Purpose : Returns the data from an association list (cdr (assoc))
;; Author  : S J Johnson
;; Date    : 23 May 1991
;; Call    : (aso a <atom> l <list>)
;; Example : (aso 2 '((1 . "FRED") (2 . "BLOB"))) returns "BLOB"
;;           (aso 0 eg) returns "LINE", same as (cdr (assoc 0 eg))
;; ---------------------------------------------------------------------------

(defun aso (a l)
  (cdr (assoc a l))
)


;; ---------------------------------------------------------------------------
;; Function: BEGFUN
;; Purpose : Stores contents of system variables, sets CMDECHO to 0 and
;;           starts an UNDO Group.
;; Author  : S J Johnson
;; Date    : 12 March 1990
;; Call    : (begfun)
;; Global  : ~AB, ~AD, ~AP, ~AR, ~AT, ~AUN, ~AUP, ~BL, ~DI, ~DR, ~FD,
;;           ~HI, ~LA, ~LU, ~OS, ~PI, ~RE, ~TE
;; Assumed : UNDO command is fully enabled.
;; Notes   : To be used at the beginning of each C: function.  See ENDFUN and
;;           ERR
;; ---------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 21 November 1991
;; Note    : ~UN should be set to T if an UNDO Group should not be performed.
;;           ~AU changed to ~AUP, ~AUN added
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 27 May 1992
;; Note    : ~DZ added
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 29 October 1992
;; Note    : ~DR removed to work around R12 bug where DRAGMODE gets randomly
;;           set to silly values.
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 20 November 1992
;; Note    : ~CM added
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 23 April 1993
;; Note    : Now returns a selection set containing  those objects which have
;;           been pre-selected.  This allows calling functions to get hold of
;;           these objects and still use the "UNDO" facility.
;; ----------------------------------------------------------------------------

(defun begfun (/ ss)
  (setvar "CMDECHO" 0)
  (setq ss (ssget "I"))
  (if (not ~UN) (command "UNDO" "G"))
  (setq
    ~ERROR *ERROR*
    *ERROR* ERR
    ~AB (getvar "ANGBASE")
    ~AD (getvar "ANGDIR")
    ~AP (getvar "APERTURE")
    ~AR (getvar "ATTREQ")
    ~AT (getvar "ATTDIA")
    ~AUN (getvar "AUNITS")
    ~AUP (getvar "AUPREC")
    ~BL (getvar "BLIPMODE")
    ~CM (getvar "CMDDIA")
    ~DI (getvar "DIMASO")
    ~DZ (getvar "DIMZIN")
    ~FD (getvar "FILEDIA")
    ~HI (getvar "HIGHLIGHT")
    ~LA (getvar "CLAYER")
    ~LU (getvar "LUPREC")
    ~OS (getvar "OSMODE")
    ~PI (getvar "PICKBOX")
    ~RE (getvar "REGENMODE")
    ~TE (getvar "TEXTEVAL")
  )
  ss
)


;; ----------------------------------------------------------------------------
;; Function: ENDFUN
;; Purpose : Restores system variables, performs an UNDO Group and (princ).
;; Author  : S J Johnson
;; Date    : 14 March 1990
;; Call    : (endfun)
;; Global  : ~AB, ~AD, ~AP, ~AR, ~AT, ~AUN, ~AUP, ~BL, ~DI, ~DR,
;;           ~FD, ~HI, ~LA, ~LU, ~OS, ~PI, ~RE, ~TE
;; Assumed : UNDO command is fully enabled.
;; Notes   : To be used at the end of each C: function.  See BEGFUN and ERR.
;; ----------------------------------------------------------------------------
;; Modified: S J Johnson
;; Date    : 31 May 1991
;; Note    : (setvar "CMDECHO" 0) added to ensure LAYER command does not echo.
;;           (layer) made conditional on current layer being different to ~LA.
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 21 November 1991
;; Note    : ~UN should be set to T if an UNDO End should not be performed.
;;           ~AU changed to ~AUP, ~AUN added
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 27 May 1992
;; Note    : ~DZ added
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 29 October 1992
;; Note    : ~DR removed to work around R12 bug where DRAGMODE gets randomly
;;           set to silly values.
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 20 November 1992
;; Note    : ~CM added
;; ----------------------------------------------------------------------------

(defun endfun ()
  (setvar "CMDECHO" 0)
  (setvar "ANGBASE" ~AB)
  (setvar "ANGDIR" ~AD)
  (setvar "APERTURE" ~AP)
  (setvar "ATTREQ" ~AR)
  (setvar "ATTDIA" ~AT)
  (setvar "AUPREC" ~AUP)
  (setvar "AUNITS" ~AUN)
  (setvar "BLIPMODE" ~BL)
  (setvar "CMDDIA" ~CM)
  (setvar "DIMASO" ~DI)
  (setvar "DIMZIN" ~DZ)
  (setvar "FILEDIA" ~FD)
  (setvar "HIGHLIGHT" ~HI)
  (setvar "LUPREC" ~LU)
  (setvar "OSMODE" ~OS)
  (setvar "PICKBOX" ~PI)
  (setvar "REGENMODE" ~RE)
  (setvar "TEXTEVAL" ~TE)
  (if (/= (getvar "CLAYER") ~LA) (layer ~LA))
  (if (not ~UN) (progn (command) (command "UNDO" "E")))
  (if (= 'FILE (type FR)) (setq FR (close FR)))
  (if (= 'FILE (type FA)) (setq FA (close FA)))
  (if (= 'FILE (type FW)) (setq FW (close FW)))
  (setq
    ~UN nil
    *error* ~ERROR
  )
  (princ)
)


;; ----------------------------------------------------------------------------
;; Function: ERR
;; Purpose : Error handler for use with BEGFUN and ENDFUN
;; Author  : S J Johnson
;; Date    : 12 March 1990
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 22 November 1991
;; Note    : Only displays error message if other than "Function cancelled"
;;           error occurs.
;; ----------------------------------------------------------------------------
;; Modified: Steve Johnson
;; Date    : 29 October 1992
;; Note    : PR now checked for 2 possible ^C errors R12 can produce
;; ----------------------------------------------------------------------------

(defun ERR (PR)
  (command)
  (if (and (/= PR "Function cancelled") (/= PR "console break"))
    (princ (strcat "\nError: " PR " "))
  )
  (endfun)
)


;; ----------------------------------------------------------------------------
;; Function: DELFST
;; Purpose : Returns a list with the first occurence of a given item deleted
;; Author  : S J Johnson
;; Date    : 24 April 1992
;; Params  : ITEM:   item to delete
;;           LST:    list to delete from
;; Local   : it1:    first item in list
;;           notfnd: boolean: T while item not found
;;           tlst:   temporary list
;; Examples: (delfst 5 '(1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6))
;;           returns => (1 2 3 4 6 7 8 9 0 1 2 3 4 5 6)
;;           (delfst 5 '(1 2 3 4))  => (1 2 3 4)
;;           (delfst 5 nil)         => nil
;; ----------------------------------------------------------------------------

(defun delfst (ITEM LST / it1 notfnd tlst)
  (setq notfnd T)
  (while (and LST notfnd)
    (setq
      it1 (car LST)
      LST (cdr LST)
    )
    (if (/= ITEM it1)
      (setq tlst (cons it1 tlst))
      (setq
        notfnd nil
        tlst (append (reverse LST) tlst)
      )
    )
  )
  (reverse tlst)
)


;; ----------------------------------------------------------------------------
;; Function: LASTENT
;; Purpose : Gets the last entity or sub-entity.  More useful than (entlast)
;;           which just gets the last entity.
;; Author  : S J Johnson
;; Date    : 29 April 1991
;; ----------------------------------------------------------------------------
;; Modified: S J Johnson
;; Date    : 11 Setpember 1992 
;; Note    : Now actually works!
;; ----------------------------------------------------------------------------

(defun lastent (/ en entemp)
  (setq
    entemp (entlast)
    en (entnext entemp)
  )
  (while en
    (setq
      entemp en
      en (entnext entemp)
    )
  )
  entemp
)


;; ----------------------------------------------------------------------------
;; Function: SSFROM
;; Purpose : Selects all objects drawn after a given entity.
;; Author  : S J Johnson
;; Date    : 12 March 1990
;; Call    : (ssfrom EN <Entity name>)
;; Example : (setq L (entlast))
;;           (command "LINE" .... etc)
;;           (setq SS (ssfrom L)) - selects all entities drawn after l
;; Params  : EN: name of entity after which to start selecting
;; Local   : ss: selection set
;; Returns : selection set
;; Notes   : If EN is nil, (e.g. (entlast) returns nil because nothing has been
;;           drawn yet), returns selection set containing the whole drawing.
;;           This eliminates the need to ensure something is drawn before using
;;           (entlast).
;; ----------------------------------------------------------------------------

(defun ssfrom (EN / ss)
  (if EN
    (progn
      (setq ss (ssadd))
      (while (setq EN (entnext EN))            
        (ssadd EN ss)
      )
    )
    (ssget "X")
  )
)


'LOADED