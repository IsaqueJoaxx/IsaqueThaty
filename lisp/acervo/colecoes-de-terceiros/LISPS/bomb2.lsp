; This is a modification of BOMB.LSP which is also in the ADESK
; library 2.  Like BOMB, BOMB2 explodes all "normal" blocks and all
; mirrored blocks (of course it will not explode blocks with scale
; factors of unequal magnitude).  Differences from the original BOMB:
; BOMB2 prompts you to select objects and then can filter those objects
; by a layer name (the original BOMB went strictly by layer).  BOMB2
; also checks the scale factors' magnitude before performing any
; mirroring.  Error handling and a block counter have been added and
; other additions and modifications have been made.  Please send any
; comments to:  _____________________ 
;               Eric Brown 71570,2031
;               ~~~~~~~~~~~~~~~~~~~~~
;correction to x,-y subroutine (XY) by Donald Pirl 6/2/93
(defun C:B2 ( / ename *serror* uselay explay inslay xval yval
                rtn rtn_str zval inspt dxf set1 size count unexp)
;---------------------------------------------------------------- 
  (defun *serror* (msg)
	  (prompt (strcat "\nError: " msg "\n"))
    (setvar "cmdecho" oldech)
    (setvar "blipmode" oldblip)
		(setq *error* olderr)
    (foreach var '(oldech olderr elist ss buildss mirblk xy blkexp)
      (set var nil)                                ;these vars are global
    )
    (princ)
  )
;----------------------------------------------------------------
  (setq olderr *error*
        *error* *serror*    
        oldech (getvar "cmdecho")
        oldblip (getvar "blipmode")
        blkexp 0
        unexp 0
  )
  (setvar "cmdecho" 0)     
  (setvar "blipmode" 0)
;---------------------------------------------------------------

  (defun DXF (code)
    (cdr (assoc code elist))
  )

;----------------EXPLODE AND PUT PIECES IN SELECTION SET-----------------

  (defun BUILDSS ( ename / lastname )
    (prompt "..")
    (setq lastname (entlast))                 ;last entity in dwg. dbase
    (command ".explode" ename)
    (setq blkexp (1+ blkexp)                  ;count expoded blocks
      SS nil
      ss (ssadd)                              ;creates empty selection set
    )
    (while (setq lastname (entnext lastname))
      (setq SS (ssadd lastname SS))
    ) 
    (if (/= uselay "")
      (command ".change" ss "" "p" "la" uselay "") )
    ss
  ) 
;------------------------MIRROR -X,Y BLOCKS-------------------------------

  (defun MIRBLK ( ename elist xval yval inspt/ )
    (if (and (< xval 0) (> yval 0))
      (progn
        (prompt "..")
        (command ".mirror" ename "" inspt rtn_str "y") ;makes xval positive
        (setq ss (buildss ename))                     ;explodes and puts
                                                      ;pieces in ss
        (command
          ".mirror" ss "" inspt rtn_str "y")    ;un-mirrors pieces
      )
      (xy ename elist xval yval inspt)                ;else go to XY
    ) 
    ss
  )
;--------------------MIRROR -X,-Y and X,-Y BLOCKS-----------------------

  (defun XY ( ename elist xval yval inspt / )
    (if (and (< xval 0) (< yval 0))               ;block inserted -x and -y
      (progn
        (prompt "..")
        (setq elist (subst (cons 41 (abs xval))      ;makes xval positive
                           (assoc 41 elist) elist))        
        (setq elist (subst (cons 42 (abs yval))
                           (assoc 42 elist) elist))  ;makes yval positive
        (entmod elist)
        (setq ss (buildss ename))           ;explodes and puts pieces in ss.
        (command
          ".rotate" ss "" inspt "180") ;makes pieces look like orig. blk.
      )
      (progn                                         ;block inserted +x and -y
        (prompt "..")                           
        (setq elist (subst (cons 42 (abs yval))    
                           (assoc 42 elist) elist)   ;makes yval positive
        )                                          
        (entmod elist)
        (setq ss (buildss ename))           ;explodes and puts pieces in ss.
        (command
          ".mirror" ss "" inspt rtn_str "y") ;makes pieces look like
                                                  ;original block.
      ) 
    )
    SS
  )
;----------------------------------------------------------------------

  (setq set1 (ssget)
        count 0
        size (sslength set1)
        explay 
         (getstring
           (strcat 
             "\n" (itoa size) " objects: Layer to explode <all layers>: "
           )
         )        
  )
  (while (not (or (tblsearch "LAYER" explay) (= explay "")))
    (princ (strcat "\nCannot find layer " explay))
    (setq explay (getstring "\nTry again: "))
  )
  (setq uselay (getstring
             "\nLayer to place exploded entities on <no layer change>: ")
  )
  (while (not (or (tblsearch "LAYER" uselay) (= uselay "")))
    (princ (strcat "\nCannot find layer " uselay))
    (setq uselay (getstring "\nTry again: "))
  )
  (prompt "\nWorking")
  (while (< count size)
    (prompt "..")
    (setq ename (ssname set1 count)
          elist (entget ename)
          count (1+ count)
    )
    (if (= "INSERT" (dxf 0))                        ;is entity a block
      (progn
        (setq inslay (dxf 8)                        ;layer of entity
              xval (dxf 41)                         ;x scale
              yval (dxf 42 )                        ;y scale
              zval (dxf 43)                         ;z scale
              inspt (dxf 10)                        ;block insertion point
              rtn (dxf 50)                          ;block rotation angle in radians
              rtn_str (strcat "@1<" (angtos rtn 0 4))  ;block rotation angle in degrees
        )
        (if (or (= explay "") (= explay inslay))
                                      ;if all layers are to be exploded or
                                      ;block is on the layer to be exploded...
          (progn
            (if (= xval yval zval)                ;if explodable w/o mirror,
              (buildss ename)                     ;explode.
              (progn                              ;else...
                (if (= (abs xval) (abs yval) (abs zval));if explodable w/ mirr,
                  (mirblk ename elist xval yval inspt)  ;go to MIRBLK, else
                  (setq unexp (1+ unexp))         ;count un-explodable blocks.
                )
              ) ;progn
            ) ;if
          ) ;progn
        ) ;if
      ) ;progn
    ) ;if
  ) ;while
  (prompt (strcat "\n" (itoa blkexp) " blocks exploded."))
  (prompt (strcat "\n" (itoa unexp) " blocks un-explodable."))
  (setvar "cmdecho" oldech)
  (setvar "blipmode" oldblip)
  (setq *error* olderr)
  (foreach var '(oldech olderr elist ss buildss mirblk xy blkexp)
    (set var nil)                                ;these vars are global
  )
  (redraw)
  (princ)
)
