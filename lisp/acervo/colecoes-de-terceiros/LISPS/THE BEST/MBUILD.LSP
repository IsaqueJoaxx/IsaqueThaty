;===============================================================================
;BTN Better Than Nothing AutoLISP                                    1999
;by Leonid Nemirovsky
;===============================================================================
(defun c:mbuild (/ menuname menufile opfile des topic func funcc funccc des1)
(setq menuname (getstring T "\nEnter Name For Your Menu <no ext.>: "))
   (if (= menuname "")
     (progn
       (alert "\nName For The Menu Was Not Selected")
       (quit)
     )
   )
;-------------------------------------------------------------------------------
(setq menufile (strcat menuname ".mnu"))
(setq opfile (open menufile "a"))
(write-line (strcat "***MENUGROUP=" menuname) opfile)
(write-line "***POP15" opfile)
(write-line "**MYGROUP" opfile)
(write-line (strcat "ID_" menuname "   [" menuname "]") opfile)
    (setq des "Y")
      (while
         (= des "Y")
         (setq topic (getstring T "\nType name for your topic in pulldown: "))
         (if (= topic "")(setq tpoic "BLANC"))
         (setq func (getstring "\nType Command related to pulldown: "))
         (if (= func "")(setq func "BLANC"))
         (initget 1 "Lisp Acad")
         (setq funcc (getkword "\nIs it Lisp or Acad command? <L/A>: "))
         (setq funccc (strcase funcc))
         (if
           (= funccc "LISP")(setq function (strcat "(c:" func ")"))
           (setq function func)
         )
         (write-line (strcat "**" menuname "   [" topic "]^C^C" function) opfile)
         (initget 1 "Yes No")
         (setq des1 (getkword "\nWant repeat procedure/ <Yes/No>: "))
         (setq des1 (strcase des1))
         (if (= des1 "YES")(setq des "Y")(setq des "N"))
      )
(alert (strcat "\nMenu " menuname ".mnu is created in current directory"))
(close opfile)
(princ)
)



 