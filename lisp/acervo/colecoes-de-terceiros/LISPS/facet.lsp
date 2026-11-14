; FACET.LSP (command FACET)                  For AutoCAD 2.5x +
; 12/14/1987
;
; By Tony Tanzillo, A/E Automation Systems, all rights reserved.
;
; Donated to public domain on 2/4/1988
;
; AutoCAD, AutoLISP, AutoSHADE, AutoSKETCH, AutoFLIX, and ADI are
; registered trademarks of Autodesk inc.
;
; *    *    *    *    *     N O T I C E     *    *    *    *    *
;                           -----------
;
; The information contained herein is being donated to the public domain
; soley for the benefit of licensed AutoCAD END USERS everywhere. You are
; free to modify same for YOUR OWN USE, or reproduce and distribute it
; to other licensed AutoCAD users only in it's unmodified original state
; for thier individual use, provided that such reproduction/distribution is
; NON-COMMERCIAL, and NOT FOR PROFIT, and that with any such distribution,
; this NOTICE (or any portion thereof) is NOT REMOVED OR ALTERED from it's
; original content in ANY manner, shape, or form.
;
; You ARE NOT free to reproduce and/or distribute the information
; contained herein (regardless of medium or form of reproduction) FOR
; COMMERCIAL USE, AND/OR PROFIT, or to include it or any portion thereof,
; as a part of any commercially obtainable goods regardless of thier
; intended nature, scope, or form, or to charge for reproduction of this
; information alone, or accompanied by other information beyond the
; reasonable costs of the medium of distribution, without the expressed
; written permission of the author.
;
; If you seek such permission, it will be granted provided you furnish the
; author with ONE (1) fully-functional, licenseable sample of the finished
; product(s) in final marketed form.  In return for which you may solict
; and will recieve the author's objective opinions, advice and insight
; regarding any and all aspects of said product(s), free of charge, with
; appropriate and complete written assurance of non-disclosure.
;
; Disclaimer:
;
; A/E Automation Systems and Tony Tanzillo are in no way responsible
; for the content of this information, and can not be held liable for any
; damadges or losses resulting from the direct or indirect usage of this
; information regardless of it's origin, or manner of prescription or
; usage. Use this information at your own risk.
;
; If you do not agree with the terms and conditions stated above,
; please erase (any magnetic medium) and/or destroy (any hardcopy medium)
; this information. Your retainage and useage of same constitutes your full
; agreement and acceptance of the terms and conditions as stated above.
;
;
;   *    *    *    *     E N D   N O T I C E    *    *    *    *
;                        -------------------
;
; Additional information can be obtained by writing to:
;
;                 A/E Automation Systems
;             c/o Tony Tanzillo
;                 32 Gould Ave.
;                 Paterson, N. J. 07503
;                 (201) 523-7944 (business hours (EST), M-F only)
;
;  A Few words about A/E Automation Systems
;
;  A/E Automation Systems is a group of dedicated professionals who
;  are experts in CADD/Computers/Programming, and thier respective
;  disciplines to which they apply AutoCAD to professionally. Currently,
;  these include Archicture/Engineering/Construction, electronics
;  engineering, mechanical & electro-mechanical engineering, civil
;  engineering, space planning and industrial & interior design.
;
;  The firm offers professional CADD/Design Automation consulting
;  services on an hourly, fixed-fee, or contract basis. The following
;  is a brief summary of the products and services available from
;  A/E Automation Systems.
;
;     New installation/pre-purchase services:
;
;       - CADD productivity analysis/profit cycle/feasability studies.
;
;       - Product research, evaluation, and specification.
;
;       - Installation supervision and management.
;
;     Post installation & existing system services:
;
;       - Training (AutoCAD/LISP/SHADE/SKETCH, introductory thru advanced).
;
;       - AutoLISP programming
;
;       - Independent phone-based user support (by contract)
;         (including interactive support via modem).
;
;       - CADD management (via modem or onsite)
;
;       - Dealer training services for AutoCAD dealers without
;         Authorized/qualified training.
;
;       - Extended dealer services for dealers specializing in
;         intensive vertical applications.
;
;     Other products & services available:
;
;       - Extensive AutoCAD customization/programming to optimize
;         system to various specific disciplines.
;
;       - Professional AutoLISP programming (contract & hourly)
;         Including complete development, testing, installation,
;         training, and support of turnkey applications.
;
;       - Custom symbol libraries, electronic product catalogs and
;         templates (generic and manufacturer specific).
;
;     AutoCAD Application Software
;
;       - Developers of commercial AutoCAD add-in applications
;         Including AutoLISP programming tools and utilities,
;         function libraries for application developers, user
;         customization tools, And descipline-specific application
;         software products.
;
;  For more information on products and services available from
;  A/E Automation Systems, call during business hours, or write
;  to the address list in this file.
;
;
;   FACET.LSP
;
;   FACET.LSP adds a command called FACET to AutoCAD. The purpose of
;   facet is to draw facet-lines on a 2 dimensional projection of a
;   curved surface (such as an elevation of a cylinder, or a round
;   column on an exterior building elevation). This is commonly done
;   in my profession to indicate that a surface is curved or round
;   when viewing it in elevation, where there is no other means
;   (besides elaborate shading and shadowing) of conveying such
;   roundness.
;
;   The method used here is HATCHING (yes, I have seen dozens of
;   other ways of doing this, and use a few others myself) because
;   hatching permits faceting of irregular shapes rather than just a
;   rectangular projection of a cylindrical object. Obviously, this
;   requires hatch patterns so in the file FACET.PAT, You will find
;   3 pattern definitions, which facet objects with different
;   densities ranging from 16 lines per hemisphere to 32 lines per
;   hemisphere. I commonly use the denser one's on larger scale
;   drawings where the pen width/drawing scale ratio permits visual
;   resolution of all lines (they do not blur around the edges of
;   the cylinder).
;
;   To use FACET, append the file FACET.PAT onto your ACAD.PAT file.
;   (Do not use your only copy of ACAD.PAT, make a backup first!).
;   You can combine the two files from MS/PC-DOS like this;
;
;     D:\ >COPY ACAD.PAT + FACET.PAT TEMP.PAT
;     D:\ >DEL ACAD.PAT
;     D:\ >RENAME TEMP.PAT ACAD.PAT
;
; Using FACET
;
;   Load FACET.LSP or add it to your ACAD.LSP file. You should have
;   something hatchable available on the display to test FACET out
;   with. If not, simply draw a close polyline rectangle (make it
;   high and narrow as if it were a column or cylinder) the diameter
;   of this elevation of a cylinder doesn't really matter now. At
;   the AutoCAD command prompt, type FACET <CR>.
;
; FACETing a curved surface.
;
;   You must select 2 parallel lines which represent the 2 sides of
;   the cylinder in elevation (not the ENDS!). Then, simply select
;   all the objects which form the cylinder elevatoin (you should
;   have some kind of closed polygon that won't "leak" when it is
;   hatched). These objects to be faceted/hatched must include the 2
;   lines you specified earlier as the sides of the cylinder. Once
;   selected, you will see the hatching in place. you will notice
;   that the hatching will always align properly with your
;   cylindrical object (lines are closest together at the sides, and
;   farthest apert at the center). This is a convienent way to
;   convey a curved or round surface in elevation.
;
;
; Enjoy!  Tony Tanzillo, A/E Automation Systems
;
;



(defun C:FACET ( ) / sp1 sp2 es1 es2 s1 sb pat)
  (cond
    ( (and (setq pat
             (cond
               ((and
                  (setq pat
                    (getint
                      "\nEnter number of facet lines (16/24/32) <24>: "))
                  (member pat '(16 24 32))) pat)
               (t 24)))

           (princ
             "\nSelect 2 parallel lines forming sides of cylindrical object")
           (setq sp1
             (osnap
               (cadr (setq es1 (entsel "\nOne side of cylinder: "))) "nea")
           )
           (setq sp2
             (osnap
               (cadr (setq es2 (entsel "Other side of cylinder: "))) "nea")
           )
           (setq sp2
             (progn (setvar "lastpoint" sp1)
                    (osnap sp2 "per")
             )
           )
           (princ "\nSelect objects forming cylinder to be faceted," )
           (setq s1 (ssget))
           (setq sb (getvar "snapbase"))
           (setq au (getvar "aunits"))
           (setvar "snapbase" sp1)
           (setvar "aunits" 1)
      )
      (setvar "cmdecho" 0)
      (command "hatch" (strcat "facet" (itoa pat))
        (distance sp1 sp2)
        (angstr (rt-ang '+ (angle sp1 sp2)) 8)
        s1 ""
      )
      (setvar "snapbase" sb)
      (setvar "aunits" au)
    )
  )
  (prin1))

(defun angstr (a p)
  (rtos (* a (/ 180.0 pi)) 2 (cond (p) (t (getvar "luprec")))))

(defun rt-ang (d a) (apply d (list a (/ pi 2.0))))

