;This routine writes out the layer name, layer color, and layer
;linetype to a file that is named using the drawing name with the
;extension ".lyr". Use this file to create a look-up table for LAY_CONV
;to set your layers to a new layer standard. You must edit the file to
;include the desired new layer names and optionally change the color and
;linetype prior to running LAY_CONV. This routine ignores XREF layers.

(prompt "\nEnter LAY_OUT at the AutoCAD command prompt to execute...")

(defun C:LAY_OUT ()
;(/ dw_nm sl_ctr ctr ch layfil lay_lst)
 (setq dw_nm (getvar "dwgname"))
 (setq sl_ctr 1 
       ctr 1
 )
 (while (/= "" (setq ch (substr dw_nm ctr 1)));check for path in dw_nm and
  (if (= ch "\\")                             ;parse if path included
   (setq sl_ctr ctr)
  )
  (setq ctr (1+ ctr))
 )
 (if (> sl_ctr 1)
  (setq dw_nm (substr dw_nm (1+ sl_ctr) (- (strlen dw_nm) sl_ctr)))
 )
 (setq layfil (open (strcat dw_nm ".lyr") "w"));open layer data file
 (if layfil
  (progn
   (while (setq lay_lst (tblnext "layer"))
    (setq ctr 1)
    (while (/= "" (setq ch (substr (cdr (assoc 2 lay_lst)) ctr 1)))
     (if (= ch "|");test for xref layer
      (setq xr_tst T)
     )
     (setq ctr (1+ ctr))
    )
    (if (not xr_tst);if not xref layer write to file
     (write-line (strcat (cdr (assoc 2 lay_lst)) " " 
                  (itoa (cdr (assoc 62 lay_lst))) " " 
                  (cdr (assoc 6 lay_lst))) layfil
     )
    )
    (setq xr_tst nil)
   )
  )
  (prompt "Unable to open data file for writing...")
 )
 (setq layfil (close layfil));close the file
 (princ)
)