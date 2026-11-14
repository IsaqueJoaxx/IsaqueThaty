;       This routine is designed for AutoCAD v. 2.18
;
;       Written by Brad Zehring at PC Consulting
;       1519 San Pascual  Santa Barbara  CA  93101  805-965-3160
;
;       This is an AutoLISP program which implements a new AutoCAD 
;       comand called "BLINE", which stands for "Block Line".
;
;       BLINE places copies of a block between two user-defined points.
;       The blocks may be inserted as "star" blocks.  The copies of the
;       block will be placed a user-defined distance apart.  The copies
;       of the block will be automatically rotated to the same
;       orientation as the line whose endpoints would be defined by the
;       two points picked by the user.  The total number of copies 
;       inserted will be automatically calculated by BLINE.
;
;       WARNING: This function will not test for the validity of the 
;       block name.  If this function is given an invalid block name,
;       unpredictable results may occur.
;
;       Define a new, keyboard-available command called "BLINE"
;
(DEFUN C:BLINE ()
;
;       Suppress the command line echo
;
  (SETVAR "CMDECHO" 0)
;
;       Prompt the user for the name of the block to insert
;
  (SETQ BNAME (GETSTRING "BLOCK NAME: "))
;
;       Print a newline to clear the command line
;
  (TERPRI)
;
;       Prompt the user for the first insertion point
;
  (SETQ P1 (GETPOINT "FROM POINT: "))
;
;       Print a newline
;
  (TERPRI)
;
;       Prompt the user for the end of the insertion line
;
  (SETQ P2 (GETPOINT "TO POINT: "))
;
;       Print a newline
;
  (TERPRI)
;
;       Prompt whether to insert as "*" block and set variable
;       "STARTEST" accordingly
;
  (SETQ STARTEST (GETSTRING "INSERT AS STAR BLOCK [Y/N]? "))
;
;       Find the distance from first to second point
;
  (SETQ PDIST (DISTANCE P1 P2))
;
;       Find the angle in radians from the first to the second point
;
  (SETQ PANG (ANGLE P1 P2))
;
;       Convert the real, radian value of "PANG" to a string in degrees
;       with 4 decimal places, then convert the string to a real
;
  (SETQ IANG (ATOF (ANGTOS PANG 0 4)))
;
;       Prompt for the distance between each successive block insertion
;
  (SETQ IDIST (GETDIST P1 "OFFSET DISTANCE: "))
;
;       Divide the total distance to cover by the distance between each
;       block and convert that value to an integer
;
  (SETQ INUM (FIX (/ PDIST IDIST)))
;
;       Set the counter to 0
;
  (SETQ COUNT 0)
;
;       Check whether to insert as block or starblock
;
    (COND
      ((MEMBER STARTEST '("Y" "y"))
;
;       If "STARTEST" is "y" or "Y" concatenate "*" with the name of the
;       block to insert
;
        (SETQ BNAME (STRCAT "*" BNAME))
;
;       As long as the counter is less than the total number of blocks 
;       to insert...
;
        (WHILE (< COUNT INUM)
;
;       Insert the starblock at the first point picked with a scale
;       factor of 1 and a rotation equal to the angle between P1 and P2
;
          (COMMAND "INSERT" BNAME P1 "1" IANG)
;
;       Set P1 to the next insertion point along the line from the
;       original P1 to P2
;
          (SETQ P1 (POLAR P1 PANG IDIST))
;
;       Increase the counter by 1 and repeat the "While" loop
;
          (SETQ COUNT (1+ COUNT))
        )
      )
;
;       If "STARTEST" is not "y" or "Y", insert as regular blocks --- as 
;       long as the counter is less than the total number of blocks to
;       insert...
;
      (T
        (WHILE (< COUNT INUM)
;
;       Insert the block at P1 with an X scale of 1, a Y scale of 1, and
;       a rotation equal to the angle between P1 and P2
;
          (COMMAND "INSERT" BNAME P1 "1" "1" IANG)
;
;       Set the next insertion point
;
          (SETQ P1 (POLAR P1 PANG IDIST))
;
;       Increase the counter by 1 and repeat the "While" loop
;
          (SETQ COUNT (1+ COUNT))
        )
      )
    )
)







