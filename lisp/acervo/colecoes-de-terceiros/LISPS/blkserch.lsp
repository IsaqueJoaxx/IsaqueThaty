; BLKSERCH.LSP  (copyright) 1988, Barry R. Bowen

(defun c:BLKSERCH (/ search slgth blk
                      usr st)
 (setvar "cmdecho" 0)
 (initget 1)
 (setq search (strcase
 (getstring "\nSearch String: ")))
 (textscr)
 (setq slgth (strlen search))
 (if (= search "?")
   (command "block" "?")
   (progn
    (prompt "\nDefined blocks: \n\n")
    (setq blk (cdr
       (assoc 2 (tblnext "block" T))))
   (if (= (substr blk 1 slgth) search)
    (progn
     (princ (strcat "  " blk "\n"))
     (setq st T)))
 (while (setq blk (cdr
         (assoc 2 (tblnext "block"))))
  (if (= (substr blk 1 slgth) search)
   (progn (princ (strcat "  " blk "\n"))
          (setq st T))))
 (if (= st nil)
     (princ "No Blocks Defined"))
 (princ)))  ; End if "?"
)
