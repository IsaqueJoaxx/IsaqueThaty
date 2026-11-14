; 3DFILLET.LSP     4/25/94
;
; Redefines the FILLET command to allow filleting
; of non-UCS coplanar lines, and allows radius
; definition during FILLET
;
;
; ***************************************
; ****  Author:  Owen Wengerd        ****
; ****                               ****
; ****  Manu-Soft Computer Services  ****
; ****  P.O. Box 84                  ****
; ****  Fredericksburg, OH  44627    ****
; ****  (216) 695-5903               ****
; ****  Compu-Serve ID:  71324,3252  ****
; ***************************************

(if (not c:fillet) (command "._UNDEFINE" "_FILLET"))

(defun C:FILLET (/ 

;*** Local Variables ***

  ent1
  ent2
  frad
  inp
  olderr
  oldvar 
  restore
  tmp1
  tmp2


;*** Local Functions ***

  dxf
  errexit
  filletx

)

;*** Local Function Definitions ***

(defun errexit (s)
  (if (= 8 (logand 8 (getvar "UNDOCTL"))) 
    (command "UNDO" "E" "UNDO" 1)
  )
  (if (not (member s '("console break" "Function cancelled")))
    (princ (strcat "\nError:  " s))
  )
  (restore)
)

(defun filletx (/ t1)
  (if (/= 1 (setq t1 (logand 3 (nth 1 oldvar))))
    (progn
      (command "_UNDO")
      (if (/= 0 (logand 3 (getvar "UNDOCTL"))) (command "_C"))
      (command (if (= 0 t1) "_N" "_O"))
    )
  )
  (setvar "UCSFOLLOW" (nth 4 oldvar))
  (setvar "UCSICON" (nth 3 oldvar))
  (setvar "BLIPMODE" (nth 2 oldvar))
  (setvar "CMDECHO" (car oldvar))
  (if frad (setvar "FILLETRAD" frad))
  (setq *error* olderr)
  (princ)
)

(defun dxf (a e) (cdr (assoc a e)))



; ***********************************************
; ***************  Main Program  ****************
; ***********************************************

  (setq T (not nil))
  (setq oldvar
    (list
      (getvar "CMDECHO")
      (getvar "UNDOCTL")
      (getvar "BLIPMODE")
      (getvar "UCSICON")
      (getvar "UCSFOLLOW")
    )
  )
  (setvar "CMDECHO" 0)
  (setvar "BLIPMODE" 0)
  (setq restore filletx
        olderr  *error*
        *error* errexit
  )
  (setq frad (getvar "FILLETRAD"))
  (initget "Radius")
  (while (not inp)
    (setq inp
      (entsel
        (strcat
          "\nRadius ("
          (rtos frad)
          ")/<Select first object>: "
        )
      )
    )
    (cond 
      ( (= inp "Radius")
        (if
          (setq inp
            (getreal
              (strcat
                "Enter fillet radius <"
                (rtos frad)
                ">: "
              )
            )
          )
          (setq frad inp)
        )
        (setq inp nil)
      )
      ( (and
          inp
          (setq ent1 (dxf 0 (entget (car inp))))
          (or
            (member ent1 '("LINE" "ARC" "CIRCLE"))
            (and
              (= ent1 "POLYLINE")
              (setq ent1 "COMPLEX POLYLINE")
              (zerop 
                (logand 16 (setq tmp1 (dxf 70 (entget (car inp)))))
              )
              (zerop (logand 64 tmp1))
              (setq ent1 "POLYLINE")
            )
          )
        )
        (if (= ent1 "POLYLINE")
          (progn
            (if
              (zerop (logand 8 tmp1))
              (progn
                (setvar "UCSICON" 0)
                (command 
                  "._UCS"
                  "_E"
                  (car inp)
                  "._FILLET"
                  "_R"
                  frad
                  "._FILLET"
                  "_P"
                  (car inp)
                  "._UCS"
                  "_P"
                )
                (setvar "UCSICON" (nth 3 oldvar))                
              )
              (prompt "\nSorry, can't do 3D Polylines yet!")
            )
          )
          (progn
            (redraw (car inp) 3)
            (initget "Radius Undo")
            (setq ent1 (cons ent1 (list (car inp) (trans (cadr inp) 1 0)))
                  inp  nil
            )
            (while (not inp)
              (setq inp
                (entsel
                  (strcat
                    "\nRadius ("
                    (rtos frad)
                    ")/Undo/<Select second object>: "
                  )
                )
              )
              (cond 
                ( (= inp "Undo")
                  (redraw (cadr ent1) 4)
                  (setq ent1 nil)
                )
                ( (= inp "Radius")
                  (if
                    (setq inp
                      (getreal
                        (strcat
                          "Enter fillet radius <"
                          (rtos frad)
                          ">: "
                        )
                      )
                    )
                    (setq frad inp)
                  )
                  (setq inp nil)
                )
                ( (and
                    (not (redraw (cadr ent1) 4))
                    inp
                    (setq ent2 (dxf 0 (entget (car inp))))
                    (member ent2 '("LINE" "ARC" "CIRCLE"))
                  )
                  (setvar "UCSICON" 0)
                  (command
                    "._FILLET"
                    "_R"
                    frad
                  )
                  (cond
                    ( (and
                        (= (car ent1) ent2 "LINE")
                        (inters
                          (dxf 10 (entget (cadr ent1)))
                          (dxf 11 (entget (cadr ent1)))
                          (dxf 10 (entget (car inp)))
                          (dxf 11 (entget (car inp)))
                          T
                        )
                      )
                      (setq ent2
                        (cons ent2 (list (car inp) (trans (cadr inp) 1 0)))
                      )
                      (command
                        "._UCS"
                        "_3"
                        (trans (dxf 10 (setq tmp1 (entget (cadr ent1)))) 0 1)
                        (trans (dxf 11 tmp1) 0 1)
                        (trans
                          (if
                            (or
                              (equal
                                (dxf 10 (setq tmp2 (entget (cadr ent2))))
                                (dxf 10 tmp1)
                                0.0000001
                              )
                              (equal
                                (dxf 10 tmp2)
                                (dxf 11 tmp1)
                                0.0000001
                              )
                            )
                            (dxf 11 tmp2)
                            (dxf 10 tmp2)
                          )
                          0
                          1
                        )
                        "._FILLET"
                        (trans (caddr ent1) 0 1)
                        (trans (caddr ent2) 0 1)
                        "._UCS"
                        "_P"
                      )
                      (setvar "UCSICON" (nth 3 oldvar))
                    )
                    ( (or (setq tmp1 (= (car ent1) "LINE")) (= ent2 "LINE"))
                      (setq ent2
                        (cons ent2 (list (car inp) (trans (cadr inp) 1 0)))
                      )
                      (command
                        "._UCS"
                        "_E"
                        (if tmp1 (cadr ent2) (cadr ent1))
                      )
                      (if
                        (and
                          (zerop
                            (last
                              (trans
                                (dxf 10
                                  (entget (cadr (if tmp1 ent1 ent2)))
                                )
                                0
                                1
                              )
                            )
                          )
                          (zerop
                            (last
                              (trans
                                (dxf 11
                                  (entget (cadr (if tmp1 ent1 ent2))) 
                                )
                                0
                                1
                              )
                            )
                          )
                        )
                        (command
                          "._FILLET"
                          (trans (caddr ent1) 0 1)
                          (trans (caddr ent2) 0 1)
                        )
                        (prompt "\nEntities are not co-planar!")
                      )
                      (command
                        "._UCS"
                        "_P"
                      )
                      (setvar "UCSICON" (nth 3 oldvar))
                    )
                    ( (equal
                        (dxf 210 (entget (cadr ent1)))
                        (dxf 210 (entget (car inp)))
                      )
                      (command
                        "._UCS"
                        "_E"
                        (cadr ent1)
                        "._FILLET"
                        (trans (caddr ent1) 0 1)
                        (trans (cadr inp) 0 1)
                        "._UCS"
                        "_P"
                      )
                      (setvar "UCSICON" (nth 3 oldvar))
                    )
                    ( T (prompt "\nEntities are not co-planar!"))
                  )
                )
                ( inp
                  (prompt (strcat "\nCannot fillet to " ent2 " entity!"))
                  (setq inp nil)
                )
                ( T (setq inp T))
              )
            )
            (if (not ent1) (setq inp nil))
          )
        )
      )
      ( inp
        (prompt (strcat "\nCannot fillet " ent1 " entity!"))
        (setq inp nil)
      )
      ( T (setq inp  T))
    )
  )
  (restore)
)

