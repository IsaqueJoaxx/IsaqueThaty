;ADDQUE.LSP copyright 1988 Ron Phillips

(defun listq ()                                     ;THIS FUNCTION READS A LIST
   (setq addlist nil)                               ;FROM A FILE.  IN AutoCAD
   (if (null (setq f (open "plotlist.dat" "r")))    ;V2.6, THE FUNCTION (open
      (progn                                        ;"filename" "a") CAN BE USED
         (setq f (open "plotlist.dat" "w"))         ;TO ADD TO THE LIST OF DRAWINGS
         (close f)                                  ;WHICH ARE TO BE PLOTTED.  FOR
      )                                             ;EARLIER VERSIONS, HOWEVER, THIS
      (close f)                                     ;FUNCTION MAKES A LIST OUT OF
   )                                                ;WHATEVER IS IN THE FILE.
   (setq f (open "plotlist.dat" "r"))
   (while (setq temp (read-line f))
      (setq addlist (cons (read temp) addlist))
   )
   (close f)
   (setq addlist (reverse addlist))
)


(defun adddwg ()                                    ;THIS FUNCTION WILL ADD A
   (setq newdwg "")                                 ;DRAWING (AND SIZE) TO THE LIST.
   (while (null (open (strcat newdwg ".dwg") "r"))
      (setq newdwg (getstring (strcat "\nEnter drawing to go in queue <"
                                      (getvar "dwgname")
                                      ">: "
                              )                     ;IF THE DRAWING DOES NOT EXIST,
                   )                                ;IT WILL ASK AGAIN.
      )
      (if (= newdwg "") (setq newdwg (getvar "dwgname")))
      (if (null (open (strcat newdwg ".dwg") "r")) (print "Cannot find drawing"))
   )
   (if (= "A" (strcase (getstring "\nEnter size of drawing <B>: ")))
      (setq plotsize "A")                           ;IT WILL ASK THE SIZE
      (setq plotsize "B")                           ;TO PLOT THE DRAWING.  WE ONLY
   )                                                ;USE "A" & "B" SIZE, BUT IF DESIRED
   (setq f (open "plotlist.dat" "w"))               ;THE ENTIRE AutoCAD PLOT OPTION LIST
   (prin1 newdwg f)                                 ;COULD BE DUPLICATED.
   (princ "\n" f)
   (prin1 plotsize f)                               ;THE VARIABLES ARE ADDED TO THE LIST
   (princ "\n" f)                                   ;AND THE LIST IS WRITTEN TO THE
   (foreach temp addlist                            ;PLOT LIST FILE
      (progn
      (prin1 temp f)
      (princ "\n" f)
      )
   )
   (close f)
)


(defun C:ADDQUE ()                                  ;ADDQUE IS THE COMMAND TO ADD
   (listq)                                          ;A DRAWING OR DRAWINGS (AND
   (adddwg)                                         ;PLOT OPTIONS) TO THE PLOT LIST
)

