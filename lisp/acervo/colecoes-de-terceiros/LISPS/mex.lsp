;;; ---------------------------------------------------------------------------
;;; File    : MEX.LSP
;;; Purpose : Allows explosion of blocks, xrefs, dimensions and polylines.
;;;           Provides additional explosion features over standard AutoCAD.
;;;           Two commands are provided: MEX and MEXALL, plus a function
;;;           (mex_main SS ETYP SHOW).  MEX allows user selection of entity
;;;           type and objects, and explodes only one level.  MEXALL explodes
;;;           anything and everything it can in the current space, down to
;;;           the deepest level.  (mex_main) allows programmers to create 
;;;           other functions which perform MEX-type explosions.
;;; Author  : S J Johnson
;;; Date    : 29 May 1991
;;; Revision: 1.1 - 7 June 1994
;;; Notes   : Polylines (and derivatives such as meshes) are exploded normally.
;;;           Dimensions have their DEFPOINTS removed after explosion. New
;;;           dimension entities are placed on the layer of the original
;;;           entity, as are any new layer 0 entities created by exploding
;;;           blocks.  Similarly, BYBLOCK entities have their colour and/or
;;;           linetype set to match the original entity.  Mirrored blocks are
;;;           exploded by mirroring the block, exploding and re-mirroring the
;;;           new entities.  Minserted blocks are exploded into an array of
;;;           single blocks.  Blocks with attributes are exploded, then each
;;;           attribute definition created by the explosion is replaced by a
;;;           piece of text with the appearance of the original attribute.
;;;           Blocks with unequal X, Y and Z scales cannot be exploded.
;;;           If such blocks are selected, they are placed in selection set
;;;           UXB and any new entities created by explosion are placed in GR.
;;;           XREFs are bound rather than exploded (ie. they become normal
;;;           blocks, which may be subsequently exploded if required).  If one
;;;           XREF is selected, all XREFs in the drawing of the same name are
;;;           bound.
;;; Warning : Just because MEX makes it easy to explode things does not make it
;;;           a good idea.  Exploding is essentially the process of removing
;;;           intelligence and associativity from a drawing, and will almost
;;;           certainly increase the drawing size.  So think carefully before
;;;           lighting the touchpaper.
;;; ---------------------------------------------------------------------------
;;; Function definition structure:
;;;  C:MEX
;;;  C:MEXALL
;;;  ÀÄÄadd_xref
;;;  mex_main
;;;  ÃÄÄexblk
;;;  ³  ÃÄÄatfix
;;;  ³  ÀÄÄatlist
;;;  ÃÄÄexdim
;;;  ÃÄÄexpol
;;;  ÀÄÄfixnew
;;; ---------------------------------------------------------------------------
;;; Modified: S J Johnson
;;; Date    : 12 May 1993
;;; Notes   : Exploded blocks with attributes now create text entities, not
;;;           attribute definitions with modified tags.
;;; ---------------------------------------------------------------------------
;;; Modified: S J Johnson
;;; Date    : 26 May 1994 - 1.0 (GEN 5ix - not generally distributed)
;;; Notes   : General tidy-up.  Many functions made local.  Specific (lasent)
;;;           function removed and replaced by generic (lastent) function.
;;;           Layer 0, colour and linetype BYBLOCK now treated seperately,
;;;           which means dimensions with different colours now explode as
;;;           expected.  MINSERTs now explode correctly in different UCSs.
;;; ---------------------------------------------------------------------------
;;; Modified: S J Johnson
;;; Date    : 7 June 1994  - 1.1 (GEN 5j)
;;; Notes   : Fixed bug where routine crashed if it encountered XREFs, and
;;;           added the facility which binds them instead.  Divided code
;;;           into (C:MEX) and (mex_main) to provide an API, and used that API
;;;           to create (C:MEXALL) command.  Changed entity type to mixed case
;;;           in messages, and made the messages optional within the API.
;;;           Removed <All> as the default for MEX entity selection.
;;; ---------------------------------------------------------------------------


;; ---------------------------------------------------------------------------
;; Function: C:MEX
;; Purpose : AutoCAD command to explode specified entities by one level.
;; Global  : GR: selection set for new entities
;;           UXB: selection set for unexploded blocks
;; Local   : ss: selection set
;;           etyp: entity type string from user
;;           etlist: list used in (ssget) to restrict entity types selected
;;           el: last entity (used as marker to identify new entities)
;;           xlist: list of XREFs bound in one iteration
;; Uses    : (mex_main) (begfun) (endfun) (lastent) (ssfrom): global
;; ---------------------------------------------------------------------------

(defun C:MEX (/ ss etyp etlist el xlist)
  (setq ss (begfun))
  (initget "Block Dimension Polyline Xref All")
  (setq
    etyp
    (getkword
      "\nEntity type to explode (Block/Dimension/Polyline/Xref) <All>: "
    )
    etlist
    (cond
      ((or (= etyp "Block") (= etyp "Xref")) '((0 . "INSERT")))
      ((= etyp "Dimension") '((0 . "DIMENSION")))
      ((= etyp "Polyline") '((0 . "POLYLINE")))
      (T ; All (default)
        '((-4 . "<OR")
           (0 . "INSERT") (0 . "DIMENSION") (0 . "POLYLINE")
          (-4 . "OR>"))
      )
    )
  )
  (if (not ss)
    (progn
      (prompt "\nSelect objects to explode...")
      (setq ss (ssget etlist))
    )
  )
  (if ss
    (progn
      (setq
        el (lastent)
        UXB (ssadd)
      )
      (setq xlist (mex_main ss etyp T))
      (if (setq GR (ssfrom el))
        (prompt "\nNew entities are in selection set GR.")
        (prompt "\nNothing exploded.")
      )
      (if (> (sslength UXB) 0)
        (prompt "\nUnexploded blocks are in selection set UXB.")
      )
      (if xlist
        (progn
          (prompt "\nThe following XREFs have been bound:\n")
          (foreach xref xlist
            (prompt (strcat " " xref))
          )
        )
      )
    )
    (prompt "\nNothing selected.")
  )
  (endfun)
) ; End C:MEX


;; ---------------------------------------------------------------------------
;; Function: C:MEXALL
;; Purpose : AutoCAD command to explode all entities to the deepest possible
;;           level.
;; Global  : GR: selection set for new entities
;;           UXB: selection set for unexploded blocks
;; Local   : ss: selection set
;;           #: loop counter
;;           el1, el2: last entity (used as markers to identify new entities)
;;           xlist: list of XREFs bound in one iteration
;;           xlist2: list of XREFs bound in this command
;; Uses    : (add_xref): local
;;           (mex_main) (begfun) (endfun) (lastent) (ssfrom): global
;; ---------------------------------------------------------------------------

(defun C:MEXALL (/ add_xref
                   ss # el1 el2 xlist xlist2)


;; ---------------------------------------------------------------------------
;; Function: ADD_XREF
;; Purpose : Adds all insertions of a given block name to a given selection
;;           set.  Used to add newly bound XREFs to the entities to re-process.
;; Param   : NAME: xref/block name string
;;           SS: selection set to which to add insertions
;; Local   : ss2: selection set of insertions
;;           #: loop counter
;; Uses    : (atfix) (atlist): local
;;           (fixnew)
;;           (aso) (lastent) (ssfrom): global
;; ---------------------------------------------------------------------------

  (defun add_xref (NAME SS / ss2 #)
    (setq
      ss2 (ssget "X" (list (cons 2 NAME)))
      # (sslength ss2)
    )
    (while (> # 0)
      (ssadd (ssname ss2 (setq # (1- #))) SS)
    )
  ) ; End add_xref


; Start C:MEXALL -----------------------

  (begfun)
  (setq
    ss
    (ssget "X"
      '((-4 . "<OR")
         (0 . "INSERT") (0 . "DIMENSION") (0 . "POLYLINE")
        (-4 . "OR>"))
    )
    el1 (lastent)
    UXB (ssadd)
    # 1
  )
  (prompt "\nMEXALL explodes everything it possibly can.  Pass:\n")
  (while ss
    (prompt (strcat "\r " (itoa #)))
    (setq
      el2 (lastent)
      xlist (mex_main ss "All" nil)
      xlist2 (append xlist xlist2)
      ss (ssfrom el2)
      # (1+ #)
    )
    (foreach xref xlist ; Add any bound XREFs to selection set to be processed
      (add_xref xref ss)
    )
  )
  (if (setq GR (ssfrom el1))
    (prompt "\nNew entities are in selection set GR.")
    (prompt "\nNothing exploded.")
  )
  (if (> (sslength UXB) 0)
    (prompt "\nUnexploded blocks are in selection set UXB.")
  )
  (if xlist2
    (progn
      (prompt "\nThe following XREFs have been bound:\n")
      (foreach xref xlist2
        (prompt (strcat " " xref))
      )
    )
  )
  (endfun)
) ; End C:MEXALL


;; ---------------------------------------------------------------------------
;; Function: MEX_MAIN
;; Purpose : Main section of MEX: provides API for AutoCAD commands.
;; Params  : SS: selection set (may contain unexplodable entities)
;;           ETYP: entity type string ("Block" "Dimension" "Polyline" "Xref")
;;           SHOW: if T, messages are displayed.  If nil, messages supressed.
;; Global  : UXB: selection set for unexploded blocks
;; Local   : #: loop counter
;;           en: entity name
;;           eg: (entget) list for entity
;;           et: entity type from entity
;;           xlist: list of XREFs bound in this command
;; Returns : xlist (nil if no XREFs bound)
;; Uses    : (exblk) (exdim) (expol) (prmex): local
;;           (aso): global
;; ---------------------------------------------------------------------------

(defun mex_main (SS ETYP SHOW / expol exdim exblk fixnew prmex
                                # en eg et xlist)


;; ---------------------------------------------------------------------------
;; Function: EXBLK
;; Purpose : Explode a block
;; Param   : EGBLK: (entget) list of block
;; Local   : el: last entity in database before explosion
;;           en: entity name of block insertion
;;           bn: block name string
;;           blklst: list of information about block
;;           p1: block insertion point
;;           p2: second point on mirror line
;;           sa: snap angle before adjustment for array
;;           al: attribute list containing tags and values
;; Uses    : (atfix) (atlist): local
;;           (fixnew)
;;           (aso) (lastent) (ssfrom): global
;; ---------------------------------------------------------------------------
  
  (defun exblk (EGBLK / atfix atlist
                        el en bn blklst p1 p2 sa al)


;; ---------------------------------------------------------------------------
;; Function: ATFIX
;; Purpose : Fix up attribute definitions from a given entity onwards using a
;;           list of attribute tags and values.  Converts ATTDEFs to TEXT
;;           entities.
;; Params  : EL: entity after which to start processing
;;           AL: attribute list containing tags and values
;; Local   : en: entity name
;;           eg: (entget) list for en
;;           tl: tag list
;; Uses    : (aso) (delfst): global
;; ---------------------------------------------------------------------------
  
    (defun atfix (EL AL / en eg tl)
      (setq en EL)
      (while (setq en (entnext en))
        (if
          (and
            (= "ATTDEF" (aso 0 (setq eg (entget en)))) ; Found ATTDEF
            (setq tl (assoc (aso 2 eg) al))            ; with TAG in al
          )
          (progn
            (entdel en)
            (if (/= "" (cadr tl))
              (entmake
                (setq
                  eg (subst (cons 0 "TEXT") (assoc 0 eg) eg)
                  eg (subst (cons 1 (cadr tl)) (assoc 1 eg) eg)
                  eg (delfst (assoc 2 eg) eg)
                  eg (delfst (assoc 3 eg) eg)
                  eg (delfst (assoc 70 eg) eg)
                  eg (delfst (assoc 74 eg) eg)
                )
              )
            )
          )
        )
      )
    ) ; End atfix
    

;; ---------------------------------------------------------------------------
;; Function: ATLIST
;; Purpose : Returns a list of attribute tags and values contained in a block.
;; Param   : EGBLK: (entget) list for block
;; Local   : en: entity name of block subentity
;;           eg: (entget) list for en
;;           retlst: list to return in format (("TAG" "VAL") ("TAG" "VAL"))  
;; Uses    : (aso): global
;; ---------------------------------------------------------------------------
    
    (defun atlist (EGBLK / en eg retlst)
      (setq en (entnext (aso -1 EGBLK)))
      (while (/= "SEQEND" (aso 0 (setq eg (entget en))))
        (setq
          retlst (cons (list (aso 2 eg) (aso 1 eg)) retlst)
          en (entnext en)
        )
      )
      retlst
    ) ; End atlist
    

;; Start exblk -------------------

    (prmex (strcat "\nBlock " (aso 2 EGBLK) " on layer " (aso 8 EGBLK)))
    (setq
      el (lastent)
      en (aso -1 EGBLK)
      bn (aso 2 EGBLK)
      blklst
      (list
        (aso 44 EGBLK)         ; 0 Column distance
        (aso 45 EGBLK)         ; 1 Row distance
        (aso 50 EGBLK)         ; 2 Rotation angle
        (max 1 (aso 70 EGBLK)) ; 3 Number of columns
        (max 1 (aso 71 EGBLK)) ; 4 Number of rows
        (aso 66 EGBLK)         ; 5 Attributes follow flag
      )
    )
    (cond
      ((= 4 (logand 4 (aso 70 (tblsearch "BLOCK" bn)))) ; XREF block
        (if (= "Block" ETYP)
          (prmex " is an XREF, ignored")
          (progn
            (prmex " is an XREF.  NOTE: binding all XREFs of this name")
            (command "._XREF" "_B" bn)
            (setq xlist (cons bn xlist))
          )
        )
      )
      ((member bn xlist)                                ; Bound XREF
        (prmex " is an XREF - already bound")
      )
      ((= "Xref" ETYP) ; This stops the rest of the (cond) being processed
        (prmex " is not an XREF, ignored")
      )
      ((> (max (nth 3 blklst) (nth 4 blklst)) 1)        ; MINSERT block
        (prmex ", MINSERTed")
        (setq
          sa (getvar "SNAPANG")
          EGBLK (subst (cons 44 0.0) (assoc 44 EGBLK) EGBLK)
          EGBLK (subst (cons 45 0.0) (assoc 45 EGBLK) EGBLK)
          EGBLK (subst (cons 70 0) (assoc 70 EGBLK) EGBLK)
          EGBLK (subst (cons 71 0) (assoc 71 EGBLK) EGBLK)
        )
        (entmod EGBLK)
        (setvar "SNAPANG" 0)
        (command
          "._UCS" "_E" en
          "._ARRAY" en "" "_R" (nth 4 blklst) (nth 3 blklst)
        )
        (if (> (nth 4 blklst) 1)
          (command (nth 1 blklst))
        )
        (if (> (nth 3 blklst) 1)
          (command (nth 0 blklst))
        )
        (setvar "SNAPANG" sa)
        (command "._UCS" "P")
      )
      ((= (aso 41 EGBLK) (aso 42 EGBLK) (aso 43 EGBLK)) ; Normal block
        (if (= (nth 5 blklst) 1)
          (progn                                        ; With attributes
            (prmex " with attributes")
            (setq al (atlist EGBLK))                    ; so get tags, values
          )
        )
        (command "._EXPLODE" en)
        (if (= (nth 5 blklst) 1) (atfix el al))         ; Fix up attributes
      )
      ((= (- (aso 41 EGBLK)) (aso 42 EGBLK))            ; Mirrored block
        (prmex ", MIRRORed")
        (if (= (nth 5 blklst) 1)
          (progn                                        ; With attributes
            (prmex " with attributes")
            (setq al (atlist EGBLK))                    ; so get tags, values
          )
        )
        (command
          "._MIRROR" en "" (setq p1 (aso 10 EGBLK))
          (setq p2 (polar p1 (aso 50 EGBLK) 1)) "_Y"
          "._EXPLODE" en
          "MIRROR" (ssfrom el) "" p1 p2 "Y"
        )
        (if (= (nth 5 blklst) 1) (atfix el al))         ; Fix up attributes
      )
      (T                                                ; Unexplodable block
        (prmex " is unexplodable")
        (ssadd en UXB)                                  ; so add it to UXB
      )
    )
    (fixnew el EGBLK)                                   ; fix new entities
  ) ; End exblk
  

;; ---------------------------------------------------------------------------
;; Function: EXDIM
;; Purpose : Explode a dimension
;; Param   : EG: (entget) list of dimension
;; Local   : el: last entity in database before explosion
;; Uses    : (aso) (lastent): global
;;           (fixnew)
;; ---------------------------------------------------------------------------
  
  (defun exdim (EG / el)
    (setq el (lastent))
    (prmex (strcat "\nDimension on layer " (aso 8 EG)))
    (command "._EXPLODE" (aso -1 EG))
    (fixnew el EG)          ; fix new layer 0 entities
  )


;; ---------------------------------------------------------------------------
;; Function: EXPOL
;; Purpose : Explode a polyline
;; Param   : EG: (entget) list of polyline
;; Uses    : (aso): global
;; ---------------------------------------------------------------------------

  (defun expol (EG)
    (prmex (strcat "\nPolyline on layer " (aso 8 EG)))
    (command "._EXPLODE" (aso -1 EG))
  ) ; End expol


;; ---------------------------------------------------------------------------
;; Function: FIXNEW
;; Purpose : Changes layer, colour, linetype of new layer 0 BYBLOCK entities.
;;           Changes the layer, colour and linetype to that of the parent
;;           block.  Deletes any POINT entities it finds on layer DEFPOINTS.
;; Params  : EL: entity after which to start processing
;;           EGBLK: (entget) list of parent block
;; Local   : ss: selection set of new entities (after EL)
;;           #: loop counter
;;           en: entity name of a new entity
;;           oldeg, eg: original (entget) list from en, modified version
;;           la, col, lt: layer, colour and linetype of parent block
;; Uses    : (ssfrom) (aso): global
;; ---------------------------------------------------------------------------

  (defun fixnew (EL EGBLK / ss # en oldeg eg la col lt)
    (if (setq ss (ssfrom EL))
      (progn
        (setq ; Get properties of original entity
          la (aso 8 EGBLK)
          col (aso 62 EGBLK)
          lt (aso 6 EGBLK)
          # 0.0
        )
        (if (not col) (setq col 256))
        (if (not lt) (setq lt "BYLAYER"))
        (while (setq en (ssname ss #))
          (setq
            oldeg (entget en)
            eg oldeg
          )
          (if (and (= "DEFPOINTS" (aso 8 eg)) (= "POINT" (aso 0 eg)))
            (entdel en)     ; Dimension point?  Yes, delete it
            (progn          ; Check for layer 0, BYBLOCK properties
              (if (= "0" (aso 8 eg))        ; Layer
                (setq eg (subst (cons 8 la) (assoc 8 eg) eg))
              )
              (if (= 0 (aso 62 eg))         ; Colour
                (setq eg (subst (cons 62 col) (assoc 62 eg) eg))
              )
              (if (= "BYBLOCK" (aso 6 eg))  ; Linetype
                (setq eg (subst (cons 6 lt) (assoc 6 eg) eg))
              )
              (if (/= eg oldeg)
                (entmod eg)
              )
            )
          )
          (setq # (1+ #))
        )
      )
    )
  ) ; End fixnew


;; Start mex_main -----------------------

  (setq # 0.0) ; use real as index to allow large selection sets
  (if (or (not UXB) (/= 'PICKSET (type UXB)))
    (setq UXB (ssadd)) ; Initialise UXB if not already done
  )
  (if SHOW ; Set up (prmex) for showing messages (or not if SHOW = nil)
    (setq prmex princ)
    (defun prmex (DUMMY) nil)
  )
  (setvar "BLIPMODE" 0)
  (setvar "HIGHLIGHT" 0)
  (setvar "OSMODE" 0)
  (while (setq en (ssname SS #)) ; Get each entity
    (setq
      eg (entget en)
      et (aso 0 eg)
    )
    (cond
      ((and
         (= et "INSERT")
         (/= ETYP "Dimension") (/= ETYP "Polyline")
       )
        (exblk eg)
      )
      ((and
         (= et "DIMENSION")
         (/= ETYP "Block") (/= ETYP "Xref") (/= ETYP "Polyline")
       )
        (exdim eg)
      )
      ((and
         (= et "POLYLINE")
         (/= ETYP "Block") (/= ETYP "Xref") (/= ETYP "Dimension")
       )
        (expol eg)
      )
    )
    (setq # (1+ #))
  )
  xlist
) ; End mex_main

'LOADED