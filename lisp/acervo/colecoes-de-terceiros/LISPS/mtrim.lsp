
TO: Jeff Wang

FROM: Paul Fernquist

SUBJECT: Re: Need some ideas
EID:a08a 129b5a7b
I was looking through some utilities a friend wrote and found the
following utility to do a multiple trim. Notice he only asks for one
point to determine which end of the lines to trim. Apparently the
trim command does mind that the point isn't on the lines to trim and
trims the end of the lines closest to the point. Maybe you weren't
thinking too much and I wasn't thinking enough
 
(defun C:MTRIM (/ ce ss pt ls no)
  (prompt "Select Cutting Edges: ")
  (setq ce (ssget))
  (prompt "Select Objects to Trim: ")
  (setq
    ss (ssget)
    pt (getpoint "Side to Trim: ")
    ls (sslength ss)
    no 0
  )
  (command "trim" ce "")
  (repeat ls
    (command (list (ssname ss no) pt))
    (setq no (1+ no))
  )
  (command "")
  (princ "\nTrim Complete")
)
 
---
 * Origin: Nick's Nest (612) 490-1187, (612) 490-0341 HST (Opus 1:282/3)
SEEN-BY: 103/501 105/16 17 27 35 61 69 301 400 106/386 112/28 
SEEN-BY: 116/500 900 132/101 137/12 147/9 150/503 151/4 100 1000 
SEEN-BY: 151/1003 161/2 227/1 265/3 7 364/701 371/6 382/1 3601/14 
PATH: 282/3 290/627 132/101 265/7 151/4 100 1003 1000 105/16 301 27 


