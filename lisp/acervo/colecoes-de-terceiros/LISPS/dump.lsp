===========================================================================
 BBS: The Engineer's Studio
Date: 03-27-93 (22:50)             Number: 29
From: ERIC OLSEN                   Refer#: NONE
  To: BARRY WILLIAMS                Recvd: NO  
Subj: Unwanted blocks                Conf: (2) A_Cad
---------------------------------------------------------------------------
BW> How do you remove unwanted blocks from a drawing block library ??
BW> We want to reduce the amount of memory that the block library uses.

Put the 'DUMP.lsp' clip in your ACAD.LSP and then call it so that
every time you edit a drawing, all unused blocks are automatically
purged.  Will not affect linetypes, etc.

-----------------------
; DUMP.lsp
; Purges all unreferenced blocks from drawing.
(Defun C:DUMP (/ SCMDE TD COUNT)
        (Setq SCMDE (Getvar "CMDECHO"))
        (Setq TD (Tblnext "BLOCK" t))
        (Setq COUNT 0)
        (While (/= TD nil)
                (If (or (= 0 (cdr (assoc 70 TD)))
                        (= 2 (cdr (assoc 70 TD))))
                (Setq COUNT (1+ COUNT))
        )
        (Setq TD (Tblnext "BLOCK"))
        )
        (Repeat COUNT
                (Command ".PURGE" "B" "Y" ^C)
        )
        (Setvar "CMDECHO" SCMDE)
        (Print " All unreferenced blocks purged from drawing list....!")
        (princ)
);EOF dump
-------------------------

 * SLMR 2.1a *  ...Make...the Taglines go awaaaaaaaaaay....

--- Squish v1.01
 * Origin: PerryHall BBS  (410) 529-1822 *V32bis * Balto. Md. (1:261/1087)
