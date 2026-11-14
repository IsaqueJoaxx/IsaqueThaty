; TIP983.LSP: FDIR.LSP   Print Directory   (c)1994, Robert L. Zipprich

          (defun C:FDIR ()
              (setq path (strcase 
                 (getstring "\nSpecify path to drawings (ex:C:\\ACAD\\) ?: "))
                    PROJ (strcase (getstring t "\nProject Name or Description: ")))
              (command ".shell" (strcat "dir " path "*.dwg | sort > dir.dir"))
              (setq FIL (open "dir.dir" "r"))
              (setq DWGLIST (open "fdisk.dir" "w"))
              (write-line (strcat "   " proj) dwglist)
              (write-line " ีอออออออออออออออออออออออออออออออออออออออออออออธ " dwglist)
              (write-line " ณ                                             ณ " dwglist)
              (read-line FIL)  ;moves down
              (read-line FIL)  ;the "dir.dir" file
              (read-line FIL)  ;to the first
              (read-line FIL)  ;drawing
              (read-line FIL)  ;entry
              (read-line FIL)  ;
              (read-line FIL)  ;
              (while (/= (setq dwg (read-line FIL)) nil)
               (setq str      "                                       "
                     startstr " ณ   "
                     endstr   " ณ "
                     dwg     (strcat startstr dwg str)
                     dwg     (substr dwg 1 46)
                     dwg     (strcat dwg endstr))
               (write-line dwg dwglist))
              (setq bot1  " ณ                                             ณ "
                    bot2  " ิอออออออออออออออออออออออออออออออออออออออออออออพ ")
              (write-line bot1 dwglist)
              (write-line bot2 dwglist)
              (close FIL)
              (close DWGLIST)
             ;;;;;;; (command ".del" "dir.dir")
          ) ;end fdir

