;* C:WLAYER  scans the drawing's layer list and Erases, skips or Wblocks each
;* layer to a drawing file name of the first 8 character of the layer name.
;* It assumes that the files on disk do not exist. It will bomb if one does exist
;* It prompts with each layer name, then Erases, Wblocks (& optionally erases),
;* or Skips it.
;* It requires the DXF and UFUNS function be preloaded.
(defun C:WLAYER( / prmpt frst bpt tbdata ents lname)
  (setvar "CMDECHO" 0)
  (setq frst t                                  ;set a flag to wind table to top
    bpt  (getvar "INSBASE")                     ;get the drawing's insertion basepoint
  );setq
  (while (setq tbdata (tblnext "layer" frst))   ;start a while loop, get each layer
    (setq ents (ssget "X" (list (cons 8 (dxf 2 tbdata)))) ;make set of all entities on layer
      lname (substr (dxf 2 tbdata) 1 8)         ;parse the string to max of 8 char
      frst nil                                  ;reset rewind flag
    );setq
    (setq prmpt (strcat "Erase or Wblock layer " lname "? E/W or RETURN to skip? "))
    (if (and ents (setq prmpt (ukword 0 "Wblock Erase" prmpt nil))) ;check for entities & choice
      (if (= prmpt "Wblock")
        (if (= "Yes" (ukword 1 "Yes No" "Erase also? Y/N" nil))
          (command "wblock" lname "" bpt ents "")        ;wblock & erase
          (command "wblock" lname "" bpt ents "" "Oops") ;wblock & restore
        )
        (command "ERASE" ents "")      ;wblock them out if exist
      )
    );if
  );while
  (setvar "CMDECHO" 1)
  (princ)                                             ;finish cleanly
);defun
;*
