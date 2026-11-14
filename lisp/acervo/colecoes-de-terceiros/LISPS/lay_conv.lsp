;This routine reads a ".lyr" file that must be created from the drawing
;before you execute LAY_CONV by using LAY-OUT. You must edit the lines
;in the ".lyr" file to include the new layer names before the existing
;names like so:
;before edit: ARWALL 7 CONTINUOUS
;after edit: A-WALL-FULL ARWALL 7 CONTINUOUS
;
;You may change the layer color and linetype, also. Make sure that you
;do not alter the number of spaces in the line beyond one additional
;space between the new layer name and the existing layer name. LAY-CONV
;will move entities to a new layer if you wish to combine two layers
;into one new layer.
;
;LAY_CONV does not alter layer names within blocks. You must write the
;block out to a file, convert the block to the new layer name, and
;re-define the block. To delete layer names that are no longer used but
;remain in the file due to a combining of layers you must use the
;AutoCAD PURGE command or write-block all entities out to a new file.

(prompt "\nTo execute type LAY_CONV at the AutoCAD command prompt.")

(defun c:lay_conv (/ ocme ex_pt dw_nm sl_ctr ch ly_fil lin_no ly_str
                   ly_clr ctr st_no sp_ctr nly_nm oly_nm ly_clr ly_lt ly_set)
 (setq ocme (getvar "cmdecho"))
 (setvar "cmdecho" 0)
 (setq ex_pt (getvar "expert"))
 (setvar "expert" 5)
 (prompt "\nChecking layers... please wait.")
 (command "'layer" "off" "*" "");turns off layers to speed up a redraw
 (setq dw_nm (getvar "dwgname"))
 (setq sl_ctr 1 
       ctr 1
 )
 (while (/= "" (setq ch (substr dw_nm ctr 1)));parses drawing name 
  (if (= ch "\\")                             ;from any path.
   (setq sl_ctr ctr)
  )
  (setq ctr (1+ ctr))
 )
 (if (> sl_ctr 1)
  (setq dw_nm (substr dw_nm (1+ sl_ctr) (- (strlen dw_nm) sl_ctr)))
 )
 (if (setq ly_fil (open (strcat dw_nm ".lyr") "r"));looks for the associated
  (progn                                           ;layer data file
   (setq lin_no 1)
   (while (setq ly_str (read-line ly_fil))
    (setq sp_ctr 0 ctr 1 st_no 0)
    (while (/= "" (setq ch (substr ly_str ctr 1)))
     (if (= " " ch)
      (progn
       (cond 
        ((= st_no 0) (setq nly_nm (substr ly_str (1+ sp_ctr) 
                      (- ctr (1+ sp_ctr)))))
        ((= st_no 1) (setq oly_nm (substr ly_str (1+ sp_ctr)
                      (- ctr (1+ sp_ctr)))))
        ((= st_no 2) (setq ly_clr (substr ly_str (1+ sp_ctr)
                      (- ctr (1+ sp_ctr)))))
       )
       (setq st_no (1+ st_no) sp_ctr ctr)
      )
     )
     (setq ctr (1+ ctr))
    )
    (if (= st_no 3);makes sure that the layer file has been edited
     (setq ly_lt (substr ly_str (1+ sp_ctr) (- (strlen ly_str) sp_ctr)))
    )
    (if (and nly_nm oly_nm ly_clr ly_lt);tests to make sure the proper
     (progn                             ;data was found.
      (if (not (tblsearch "layer" nly_nm));makes sure layer doesn't exist
       (command "rename" "la" oly_nm nly_nm)
       (progn
        (setq ly_set (ssget "x" (list (cons 8 oly_nm))));finds entities
        (if ly_set                                      ;on duplicate layers.
         (command "chprop" ly_set "" "la" nly_nm "")
        )
        (setq ly_set nil)
       )
      )
      (command "'layer" "c" ly_clr nly_nm "lt" ly_lt nly_nm "");sets color
     )                                                         ;and linetype.
     (prompt 
      (strcat "Error found in " (strcat dw_nm ".lyr") " on line " 
        (itoa lin_no) ".");notifies user of improperly edited data file.
     )
    )
    (setq lin_no (1+ lin_no) nly_nm nil oly_nm nil ly_clr nil ly_lt nil)
   )
   (command "layer" "on" "*" "")
   (command "regenall")
  )
  (prompt "\nThe proper layer data file for this drawing was not found...")
 )
 (setvar "cmdecho" ocme)
 (setvar "expert" ex_pt)
 (princ)
)
