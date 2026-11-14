; TIP967.LSP: SPLIT.LSP   Split a Plot into A-size Sheets
;                           (c)1994, Randy Steele

; split.lsp  split up a c or d size dwg into plot files 
; sizes 8.5 x 11   c size 4 views   d size 8 views

(defun c:split ()

   (setq olderr *error*
   *error* err)

   (setvar "CMDECHO" 1)
   ( setq size ( car ( getvar "EXTMAX")))

   ( cond 
      ((= size 22 ) (c_size) )
      ((= size 34 ) (d_size) )
      ( princ "DRAWING IS NOT C OR D SIZE")
   )
)

( defun c_size ()    ; divide in to 4 views
   (setq count 1)        ; for view names
   (command ".view" "w" count "0.0,0.0" "11.0,8.5")
   (setq count (+ 1 count))
   (command ".view" "w" count "11.0,0.0" "22.0,8.5"   )
   (setq count (+ 1 count))
   (command ".view" "w" count "0.0,8.50" "11.0,19.0" )
   (setq count (+ 1 count))
   (command ".view" "w" count "11.0,8.50"  "22.0,19.0" )
   (plots) ; create laser plots
)

( defun d_size ()  ;divide into 8 views
   (setq count 1)        ; for view names
   (command ".view" "w" count "0.0,0.0" "8.5,11.0")
   (setq count (+ 1 count))
   (command ".view" "w" count "8.5,0.0" "17.0,11.0"   )
   (setq count (+ 1 count))
   (command ".view" "w" count "17.0,0.0" "25.5,11.0" )
   (setq count (+ 1 count))
   (command ".view" "w" count "25.5,0.0"  "34.0,11.0" )
   (setq count (+ 1 count))
   (command ".view" "w" count "0.0,11.0" "8.5,22.0")
   (setq count (+ 1 count))
   (command ".view" "w" count "8.5,11.0" "17.0,22.0"  )
   (setq count (+ 1 count))
   (command ".view" "w" count "17.0,11.0" "25.5,22.0" )
   (setq count (+ 1 count))
   (command ".view" "w" count "25.5,11.0"  "34.0,22.0" )
   (plots) ; create laser plots
)

(defun plots ()
   (setq num 1)
   (setq cr (open "runplot.scr" "w" ))
   (repeat count 
      (setq conv (itoa num) )
      (write-line ".plot" cr)
      (write-line "v" cr)
      (write-line conv cr)
      (write-line "n" cr)
      (write-line conv cr)
      ;;;(write-line "" cr)
      (setq num (+ 1 num))
   ) ; repeat end

   (close cr)
   (command ".script" "runplot" )
)

; error function
(defun err (s)

   (if (/= s "SPLIT WAS CANCELLED")
      (princ (strcat "\nError: SPLIT CANCELED BECAUSE OF: " s ))
   )
   (princ)
)

(c:split) ; end split.lsp

