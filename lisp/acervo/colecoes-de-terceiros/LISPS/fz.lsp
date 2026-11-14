; ==========================================================================
;                                                                          ;
; FZ.lisp              By Jeff Strickland              02/26/89            ;
; for AutoCAD             2809 Windsor Avenue                              ;
;                         Charlotte, NC  28209                             ;
;                         (704) 332-8424                                   ;
;                                                                          ;
;                         for Gotaverken Energy Systems, Inc.              ;
;                         (704) 541-1453                                   ;
;                                                                          ;
; Used to speed redraw operations on larger drawings containing many text  ;
; objects.                                                                 ;
;                                                                          ;
; ==========================================================================
;
; Description:
;
; Often times while working on larger drawings with many text objects, the
; user is forced to wait during zooms/pans since text objects consist of
; several complex vectors.  AutoCAD offers the QTEXT mode of operation, which
; displays text objects using line 'boxes' to speed this redraw operation.
;
; QTEXT is limited in that the user will find it difficult to work on
; text objects which have been 'qtexted' since these objects are unreadable.
;
; The FZ and UF commands below allow the user to selectively QTEXT parts
; of a drawing, substantially speeding the redraw during zoom/pan operations.
; FZA will 'Freeze All' text objects by setting QTEXT ON and performing a ZOOM
; ALL (forced regen).
;
; How it works:
;
; The desired action is to turn QTEXT ON and do a partial regen of a selected
; area of the drawing.  The trick is in the partial regen.  When AutoCAD is
; instructed to make new objects (like text), those objects must be 'regened'
; on the screen to become a part of the display.  MOVE and COPY also must
; perform this object regeneration.  Thus, by turning QTEXT on and moving
; objects a distance of 0, the objects are forced to regen using the new
; settings (QTEXT=1).  To UNfreeze text objects, we simply perform the reverse
; operation.
;
; Limitations and Suggestions:
;
; - The QTEXT mode is only turned ON during the regen (move) of selective
;   objects and is returned to the OFF position when done.  This prevents
;   accidental regens from causing a QTEXT to occur throughout the drawing.
;
;   Consequently, should the drawing be REGENed at any time, previously
;   frozen text objects will be returned to their normal state.  It might be
;   suggested that REGENAUTO be turned OFF such that AutoCAD will prompt
;   with the message:
;                      About to regen -- proceed? <Y>
;
;   In this way, the user may respond with (N)o before allowing the regen.
;
; - With more complex text styles such as filled fonts, and double/triple
;   struck letters, text redraw/regen operations may be accelerated by
;   temporarily setting the complex style to use the TXT font (line vector)
;   using the STYLE command, and then resetting for final preview and plot.
;
;
;
;
; General notes:
;
; The 3 commands are FZ (freeze), UF (unfreeze), and FZA (freeze all).
; FZ and UF allow selective operation, where FZA turns QTEXT on and regens
; the drawing.
;
; I have named the commands with short mnemonics, however the user may
; modify the command names to suit the application.  The implementation of
; these commands is very basic and may be enhanced for error correction,
; and user confirmation.
;
;
; =======================================================================
;   FZ.lsp -- Uses QTEXT mode to partially 'freeze' text objects such that
;   redraws are substantially speeded on larger drawings where text entities
;   are prominent throughout.
;
;   Command Descriptions:
;
;   FZ.........Used to selectively 'freeze' text objects
;   UF.........Used to selectively 'unfreeze' text objects
;   FZA........Freezes all text by setting QTEXT ON and performing a ZOOM ALL
;              (forced regen)
;
(defun c:fz ()
  (princ "\nFREEZE text: Select text to partially freeze: ")
  ; get objects from user
  (setq ss (ssget))                      ; get objects from user
  ; temporarily set QTEXT on
  (setvar "QTEXTMODE" 1)
  ; temporarily set off the CMDECHO
  (setq tmpcmd (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)
  (princ "\nFreezing text objects with QTEXT...")
  ; move selected objects a 0 distance to force a 'partial' regen
  (command "move" ss "" "0,0" "0,0")
  ; set QTEXT back off and reset CMDECHO
  (setvar "QTEXTMODE" 0)
  (setvar "CMDECHO" tmpcmd)
  (princ)
)
(defun c:fza ()
  (princ "\nFreezing all text....")
  (setq tmpcmd (getvar "CMDECHO"))
  (setvar "CMDECHO" 0)
  (setvar "QTEXTMODE" 1)
  (command "zoom" "a")
  (setvar "QTEXTMODE" 0)
  (setvar "CMDECHO" tmpcmd)
  (princ)
)
(defun c:uf ()
   (princ "\nUNFREEZE text: Select text to return to normal: ")
   (setq tmpcmd (getvar "CMDECHO"))
   (setvar "CMDECHO" 0)
   (setq ss (ssget))
   (setvar "QTEXTMODE" 0)
   (princ "\nReturning text objects to normal...")
   (command "move" ss "" "0,0" "0,0")
   (setvar "CMDECHO" tmpcmd)
   (terpri)
)
;
; Routines end   02/26/89
;
(terpri)