;                          MOFFSET.LSP
;              Michael K. Weaver   Mon 11-01-1993 
;                 CIS #71461,1775 (907)488-3577
;                        1073 Badger Road
;                   North Pole, Alaska  99705
;
;This routine allows the input of a string of offset counts and
;distances then prompts the user to select an entity to offset and a
;direction for the offsets.
;
;Example:
;  command: MOFF
;  Number of offsets and distances (3@4 2@5 1@3): 3@3 2@4.5
;  Entity to offset: <pick an entity>
;  Offset Direction: <show an offset direction>
;  
;  The selected entity is now offset in the specified direction
;  3, 6, 9, 13.5, and 18 units.
;

(defun c:moff(;		Multiple OFFset
  /;			no formal arguments
  offlist;		list of strings
  offlist2;		list of lists
  strparse;		local function
  offstring;		input string
  ent;			entity to offset
  pt0;			offset base point
  pt1;			offset direction point
  ang;			offset direction
  dist;			current offset distance
  cmdecho;		value of cmdecho to restore
 );			end of local variable list
 
 (defun strparse(;		parse a string into a list of words
   str;			string to parse
   delims;		list of delimiters
   /;			end of formal arguments
   startword wordlen stringlen rtlist
  );			end of local variable list
  
  (setq
   startword 0;		start of this word
   wordlen 0;		length of this word
   stringlen (strlen str)
   rtlist nil
  )
  (while (>= stringlen (+ startword wordlen))
   (setq startword (+ wordlen startword 1))
   (while (and
     (>= stringlen (+ wordlen startword))
     (not (member (substr str (+ startword wordlen) 1) delims))
    )
    (setq wordlen (1+ wordlen))
   )
   (if (= "" (substr str startword wordlen))
    nil
    (setq rtlist (cons (substr str startword wordlen) rtlist))
   )
   (setq
    startword (+ startword wordlen)
    wordlen 0
   )
  )
  (reverse rtlist)
 );				end strparse
 

 (setq
  offstring (getstring T "\nNumber of offsets and distances (3@4 2@5 1@3): ")
  offlist (strparse offstring '(" " ","))
  offlist2 nil
 )
 (foreach pair offlist
  (setq offlist2 (cons (strparse pair '("@")) offlist2))
 )
 (setq
  offlist2 (reverse offlist2)
  ent (entsel "\nEntity to offset: ")
  pt0 (cadr ent)
  pt1 (getpoint pt0 "\nOffset Direction: ")
  ang (angle pt0 pt1)
  dist 0
 )
 (setq cmdecho (getvar "cmdecho"))
 (setvar "cmdecho" 0)
 (foreach pair offlist2
  (repeat (atoi (car pair))
   (setq dist (+ dist (distof (cadr pair))))
   (command "offset" dist ent (polar pt0 ang dist) "")
  );				end repeat
 );				end foreach
 (setvar "cmdecho" cmdecho)
 (princ)
)
