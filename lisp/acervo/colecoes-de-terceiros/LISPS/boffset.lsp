; BOFFSET.LSP  Copyright 1992  Tony Tanzillo   All Rights Reserved
;
; ************************** Notice *********************************
;
; Permission to use, copy, modify, and distribute this software
; for any non-commerial, non-profit purpose and without fee is
; hereby granted, provided that the above copyright notice appears
; in all copies and that both that above copyright notice and this
; permission notice appear in all supporting documentation.
;
; THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED
; WARRANTY.  ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR
; PURPOSE AND OF MERCHANTABILITY ARE HEREBY DISCLAIMED.
;
; Inquiry regarding commercial licensing of this material is welcome.
; *******************************************************************
;
;                         BOFFSET.LSP
;
;                        Tony Tanzillo
;                 Design Automation Consulting
;                        (201)523-7944
;
;               (Implemented on August 20, 1992)
;
; Requires AutoCAD Release 12.
;
; BOFFSET.LSP adds the BOFFSET command to AutoCAD.  BOFFSET is a
; command that unions the functionality of BPOLY and OFFSET into
; a single, highly-intuitive operation.  Sorry, no fancy dialog
; boxes or similar bells/whistles.  BOFFSET doesn't mess around,
; it just gets right down to business.
;
; With BOFFSET, you can easily generate "offset boundries", using
; the same method that is used to generate BPOLY's.  BOFFSET uses
; a selected point to both define the boundry, and to specify the
; offset (either towards the internal point at a specified offset
; distance or Through the internal point, depending on whether you
; specify an offset distance, or "Through", just like the AutoCAD
; offset command).
;
; When you invoke BOFFSET, it acts just like the OFFSET command,
; and asks for an offset distance or "Through".  Then, after you
; specify this, you are prompted for multiple internal points,
; that define each offset boundry, and direct the offset copy of
; it toward the inside of same.
;
; BOFFSET also supports explicit boundry sets via noun/verb entity
; selection.  If PICKFIRST is enabled, and there are objects that
; are currently selected when BOFFSET is invoked, they will be used
; as the boundry set.  Otherwise, all visible entities will become
; the boundry set.
;
; An effective way to demonstrate just how useful BOFFSET is, in
; contrast to other techniques required to achieve similar results,
; is to use it to convert a single-line schematic floor plan into a
; double-line floor plan, by simply picking a point in each space.
;
; To do this, just draw a schematic floor plan, by sub-dividing a
; rectangle into several distinct spaces (no leaks, the ends of the
; lines that subdivide spaces should extend just slightly beyond the
; adjacent lines they form junctions with).
;
; Once you have this, invoke BOFFSET, and specify an offset distance
; equal to one-half of the desired wall thickness.  Then, just pick
; a point inside of each space, and BOFFSET will do the rest (well,
; okay, it won't do windows).
;
; NOTE: BOFFSET calls the ADS external subroutine C:BPOLY which is
; defined by ACADAPP.EXE, and requires ACADAPP.EXE to be loaded prior
; to execution. 
;
; Good luck!

   (defun boff_err (s)
      (cond
         (  (member s '(  "Function cancelled"
                          "console break"
                          "quit / exit abort"
                       )))
         (t (princ "\nError [BOFFSET]: ")
            (princ s)))
      (setq *error* olderr)
      (command ".redrawall")
      (princ)
   )

   (defun c:boffset ( / boundry dist off-dist p msg bpset dblast olderr)
      (setvar "cmdecho" 0)
      (if (setq bpset (ssget "i"))
          (princ "\nUsing current selection as boundry set."))
      (setq olderr *error* *error* boff_err)
      (setq off-dist (getvar "offsetdist"))
      (setq msg
         (strcat "\nOffset distance/"
            (cond (  (> off-dist 0)
                     (strcat "Through/<" (rtos off-dist)))
                  (t "<Through"))
            ">: "
         )
      )
      (initget "Through")
      (setq dist (getdist msg))
      (setvar "offsetdist"
         (cond
            (  (not dist) off-dist)
            (  (eq dist "Through") -1.0)
            (t dist)))
      (command ".undo" "g" ".point" "0,0")
      (setq dblast (entdel (entlast)))
      (setq msg (if (minusp (getvar "offsetdist"))
                    "\nSelect internal through point/Undo/<Exit>: "
                    "\nSelect internal point/Undo/<Exit>: "))

      (while (and (setq p (progn (initget "Exit Undo")
                                 (getpoint msg)))
                  (/= p "Exit"))
         (cond

            (  (eq p "Undo")
               (if (entnext dblast)
                   (entdel (entlast))
                   (princ "\nCommand completely undone.")))

            (  (setq boundry (c:bpoly p bpset))
               (if (not bpset)
                   (setq bpset (ssget "p")))
               (command ".offset" "" (list boundry p) p "")
               (entdel boundry))

            (t (princ (strcat "\nCan't generate a boundry from selected "
                               "internal point and/or boundry set.")))))

      (command ".undo" "e" ".redrawall")
      (setq *error* olderr)
      (princ)
   )

; :::::::::::::::::::::::::: EOF BOFFSET.LSP ::::::::::::::::::::::::::::::
