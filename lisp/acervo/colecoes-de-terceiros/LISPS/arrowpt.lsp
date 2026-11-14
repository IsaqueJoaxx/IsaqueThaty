; =========================================================
;   ARROWPT.LSP                By:   Jeff Pilcher
;
;   This is an AutoLISP routine for use with the
;   AutoCAD drafting software.
;
;   I am providing this routine to other users of
;   AutoCAD under the SHAREWARE concept. I would
;   like to recover some of the cost of my time
;   spent developing this and other AutoLISP
;   routines for AutoCAD, by asking for voluntary
;   payments.
;
;   If you find this routine is useful to you in
;   doing paying work, please send $5 to:
;
;              Jeffrey S. Pilcher
;
;              Production Systems
;              P.O. Box 218
;              Richmond, IL  60071
;
;              (815) 678-4562
;
;   Obviously $5 is not going to make me rich, or break
;   your bank account.  It will however, show me that
;   someone out there appreciates my efforts, and give
;   me the incentive to write more useful routines.
;
;   I would be very interested in seeing any routines
;   written by you or anyone else. In other words, an
;   exchange of routines would make me as happy as a
;   $5 payment, (well, almost as happy).
;
;   Feel free to pass this routine on to other AutoCAD
;   users, but PLEASE, do not remove these comments.
;
;   Good luck, have fun, and LONG LIVE AutoDESK !!!
;
; =========================================================
;
;   ARROWPT.LSP is a program that will allow you to SNAP
;   to the endpoint of an AutoCAD ARROWHEAD. This is
;   not possible using the standard AutoCAD OSNAP
;   selections.
;
;   To use ARROWPT.LSP, anytime AutoCAD prompts you for a
;   point, and you want to SNAP to the endpoint of an
;   ARROWHEAD, just enter (arrowpt) and hit <RETURN>,
;   you will be prompted to Select arrowhead. Do so and
;   BINGO, you have SNAPPED to the exact endpoint of the
;   arrowhead.
;
;

(defun arrowpt ()
  (cdr (assoc 12 (entget (car (entsel "\nSelect arrowhead: ")))))
)
