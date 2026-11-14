; TIP1001.LSP: DDLIST.LSP   Improved List Command   (c)1994, M. Jenkins

(defun C:DDLIST (/ etype _go! ent dxf again _pts)
   (defun _pts (PT)
      (strcat
         (rtos (car PT))
         ","
         (rtos (cadr PT))
         (if (/= (caddr PT) nil)
            (strcat
               ","
               (rtos (caddr PT))
            )
            ""
         )   
      )
   )
   (defun DXF(code lst)(cdr(assoc code lst)))
   (defun _go! (ENT)
      (setq etype (dxf 0 ENT))
      (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) "Entity Type:")
      (set_tile (strcat "value" (itoa NUMBER))
         (cond
            ((= etype "INSERT")
               "BLOCK"
            )
            ((= etype "ATTDEF")
               "ATTRIBUTE DEFINITION"
            )
            ((= etype "ATTRIB")
               "ATTRIBUTE"
            )
            (T (dxf 0 ent))
         )
      )
      (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) "Layer:")
      (set_tile (strcat "value" (itoa NUMBER)) (dxf 8 ENT))
      (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) "Color:")
      (set_tile (strcat "value" (itoa NUMBER))
         (if (dxf 62 ENT)
            (itoa (dxf 62 ENT))
            "BYLAYER"
         )
      )
      (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) "Thickness:")
      (set_tile (strcat "value" (itoa NUMBER))
         (if (dxf 39 ENT)
            (rtos (dxf 39 ENT))
            "0"
         )
      )
      (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) "Linetype:")
      (set_tile (strcat "value" (itoa NUMBER))
         (if (dxf 6 ENT)
            (dxf 6 ENT)
            "BYLAYER"
         )
      )
      (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) "Location:")
      (set_tile (strcat "value" (itoa NUMBER))
         (if
            (or
               (= (dxf 67 ENT) nil)
               (= (dxf 67 ENT) 0)
            )
            "MODEL SPACE"
            "PAPER SPACE"
         )
      )
      (foreach ITEM
         (list
            '(
               ("TRACE" "SOLID" "3DFACE")
               (
                  ("First Corner" 10)
                  ("Second Corner" 11)
                  ("Third Corner" 12)
                  ("Fourth Corner" 13)
               )
            )
            '(
               ("ATTDEF" "ATTRIB")
               (
                  ("Start Point" 10)
                  ("Height" 40)
                  ("Default Value" 1)
                  ("Prompt String" 3)
                  ("Tag String" 2)
                  ("Field Length" 73 "(itoa 0)")
                  ("Rotation Angle" 50 "(angtos 0)")
                  ("X Scale Factor" 41 "(itoa 0)")
                  ("Oblique angle" 51 "(angtos 0)")
                  ("Text Style" 7 "STANDARD")
               )
            )
            '(
               ("ARC" "CIRCLE")
               (
                  ("Center" 10)
                  ("Radius" 40)
                  ("Start Angle" 50)
                  ("End Angle" 51)
               )
            )
            '(
               ("DIMENSION")
               (
                  ("Block Name" 2)
                  ("Style Name" 3)
                  ("Definition Pt" 10)
                  ("Middle Point" 11)
                  ("Text" 1)
                  ("Leader Length" 40)
                  ("Rotation Angle" 50)
                  ("Ext. Angle" 52)
                  ("Text Angle" 53)
               )
            )
            '(
               ("INSERT")
               (
                  ("Block Name" 2)
                  ("Insertion Pt" 10)
                  ("X Scale Factor" 41)
                  ("Y Scale Factor" 42)
                  ("Z Scale Factor" 43)
                  ("Rotation Angle" 50)
                  ("Column Count" 70)
                  ("Row Count" 71)
                  ("Column Spacing" 44)
                  ("Row Spacing" 45)
               )
            )
            '(
               ("LINE")
               (
                  ("Start Point" 10)
                  ("End Point" 11)
               )
            )
            '(
               ("POINT")
               (
                  ("Coordinate" 10)
                  ("X Axis Angle" 50)
               )
            )
            '(
               ("POLYLINE")
               (
                  ("Elevation" 10)
                  ("Starting Width" 40)
                  ("Ending Width" 41)
               )
            )
            '(
               ("SHAPE")
               (
                  ("Insertion Pt" 10)
                  ("Size" 40)
                  ("Shape Name" 2)
                  ("Rotation Angle" 50)
                  ("X Scale Factor" 41)
                  ("Oblique Angle" 51)
               )
            )
            '(
               ("TEXT")
               (
                  ("Insertion Pt" 10)
                  ("Height" 40)
                  ("Text" 1)
                  ("Rotation Angle" 50)
                  ("X Scale Factor" 41)
                  ("Oblique Angle"  51)
                  ("Text Style" 7 "STANDARD")
               )
            )
            '(
               ("VERTEX")
               (
                  ("Location Pt" 10)
                  ("Starting Width" 40)
                  ("Ending Width" 41)
                  ("Bulge" 42)
               )
            )
            '(
               ("VIEWPORT")
               (
                  ("Center Pt" 10)
                  ("Width" 40)
                  ("Height" 41)
               )
            )
         )
         (if
            (member (dxf 0 ENT) (car ITEM))
            (foreach
               pair
               (cadr ITEM)
               (if (setq VAL (dxf (cadr PAIR) ENT))
                  (progn
                     (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) (strcat (car PAIR) ":"))
                     (cond
                        ((and (>= (cadr PAIR) 50) (<= (cadr PAIR) 58))
                           (set_tile (strcat "value" (itoa NUMBER)) (angtos (dxf (cadr PAIR) ENT)))
                        )
                        (
                           (= (type val) 'REAL)
                           (set_tile (strcat "value" (itoa NUMBER)) (rtos (dxf (cadr pair) ent)))
                        )
                        (
                           (= (type val) 'INT)
                           (set_tile (strcat "value" (itoa NUMBER)) (itoa (dxf (cadr PAIR) ENT)))
                        )
                        (
                           (= (type val) 'LIST)
                           (set_tile (strcat "value" (itoa NUMBER)) (_pts (dxf (cadr PAIR) ENT)))
                        )
                        (T
                           (set_tile (strcat "value" (itoa NUMBER)) VAL)
                        )
                     )
                  )
               )
            )
         )
      )
      (if
         (= (dxf 0 ENT) "LINE")
         (progn
            (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) "Angle Formed:")
            (set_tile
               (strcat "value" (itoa NUMBER))
               (angtos
                  (angle
                     (dxf 10 ENT)
                     (dxf 11 ENT)
                  )
               )
            )
            (set_tile (strcat "label" (itoa (setq NUMBER (1+ NUMBER)))) "Length:")
            (set_tile
               (strcat "value" (itoa NUMBER))
               (rtos
                  (distance
                     (dxf 10 ENT)
                     (dxf 11 ENT)
                  )
               )
            )
         )
      )
   )
   (if (= #ddlist_n "1")
      (while (not (setq ENT (nentsel)))
         (prompt "\nNo entity selected.")
      )
      (while (not (setq ENT (entsel)))
         (prompt "\nNo entity selected.")
      )
   )
   (setq
      ENT (entget (setq DDLIST_NAME (car ENT)))
      NUMBER 0)
   (redraw DDLIST_NAME 3)
   (setq ID (load_dialog "ddlist"))
   (new_dialog "ddlist" id "" #DDLIST_LOC)
   (_go! ENT)
   (if (= #DDLIST_N "1")
      (set_tile "nested" "1")
   )
   (action_tile "accept" "(setq #DDLIST_LOC (done_dialog))")
   (action_tile "nested" "(setq #DDLIST_N $VALUE)")
   (action_tile "again"  "(done_dialog 2)")
   (setq AGAIN (start_dialog))
   (unload_dialog id)
   (redraw DDLIST_NAME 4)
   (if (= AGAIN 2)
      (C:DDLIST)
   )
   (princ)
)
(princ "DDLIST Loaded.")
(princ); end ddlist.lsp
