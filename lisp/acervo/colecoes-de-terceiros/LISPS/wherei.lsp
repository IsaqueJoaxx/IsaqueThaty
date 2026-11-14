;|
                 Michael K. Weaver CIS# 71461,1775
                          1073 Badger Road
                     North Pole, Alaska  99705
                           (907)488-3577

This routine will find and display the full pathname for any file found
on the ACAD search path.  This is especially usefull for ensuring
that the right copy of font, menu, lisp or other acad utility files
is being loaded.

|;


(defun c:whereis();	Determines the location of the file 
 (findfile(getstring "\nFilename: "))
)
