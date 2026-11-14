; SEQUENCE.LSP  V1.0d (SEQUENCE/NEXT commands)
; Block insertion display sequencer for AutoCAD 2.5x +
;
; Demo/beta version of AutoSEEK, a block insertion command/process sequencer
; for AutoCAD Release 9
;
; Created 11/12/87
;
; By Tony Tanzillo, A/E Automation Systems (all rights reserved)
;
; Deposited into the public domain on 12/15/87
;
;-----------------------------------------------------------------------+
; AutoCAD, AutoLISP, AutoSHADE, AutoSKETCH, AutoFLIX, and ADI are       |
; registered trademarks of Autodesk inc.                                |
;-----------------------------------------------------------------------+
;
; *    *    *    *    *     N O T I C E     *    *    *    *    *
;                           ~~~~~~~~~~~
;
; The information contained herein is being donated to the public domain
; soley for the benefit of licensed AutoCAD END USERS everywhere. You are
; free to modify same for YOUR OWN USE, or reproduce and distribute it
; to other licensed AutoCAD users only in its unmodified original state
; for thier individual use and modification, provided that such repro-
; duction/distribution is NON-COMMERCIAL and /NON-PROFIT, and that with
; any such distribution, this NOTICE (or any portion thereof) is NOT
; REMOVED AND/OR ALTERED in ANY manner, shape, or form.
;
; You ARE NOT free to reproduce and/or modify and distribute the infor
; mation contained herein (regardless of medium or form of reproduction)
; FOR ANY COMMERCIAL USE/PUPROSE OR FOR PROFIT, or to include it or any
; portion thereof, as a part of any commercially obtainable goods regard-
; less of thier intended nature, scope, or form, or to charge for repro-
; duction of this information alone, or with other information beyond the
; REASONABLE costs of the medium of distribution, without the expressed
; written permission of the author.
;
; If you seek such permission, in most cases, it will be granted provided
; you furnish the author with ONE (1) fully-functional, licenseable sample
; of the finished product(s) in final marketed form. In return for which
; you may solict and will recieve the author's objective opinions, advice
; and insight regarding any and all aspects of said product(s), free of
; charge, with appropriate and complete written assurance of non-disclosure.
;
; Disclaimer:
;
; A/E Automation Systems and Tony Tanzillo are in no way responsible
; for the content of this information, and can not be held liable for any
; damadges or losses resulting from the direct or indirect usage of this
; information regardless of it's origin, or manner of prescription or
; usage. A/E Automation systems and Tony Tanzillo does in no way warranty
; and/or guarantee (expressed or implied) this information to operate as
; indicated, be correct, non-defective, be identical to and/or unaltered
; from it's original distrubuted form, operate properly, be non-destructive,
; safe to use, or any other stipulation. Use this information at your own
; risk.
;
; If you disagree with the terms and conditions of this notice as stated
; above, please erase (any magnetic medium) and/or destroy (any hardcopy
; medium) this information. Your retainage and/or useage of same constitutes
; your agreement, accpetance and intent to fully comply with the above
; stated terms and conditions.
;
; Thank you for your patience, Tony Tanzillo, A/E Automation Systems
;
;
;    *    *    *    *     E N D   N O T I C E    *    *    *    *
;                         ~~~~~~~~~~~~~~~~~~~
;
; Additional information can be obtained by writing to:
;
;                 A/E Automation Systems
;             c/o Tony Tanzillo
;                 32 Gould Ave.
;                 Paterson, N. J. 07503
;                 (201) 523-7944 (business hours (EST), M-F only)
;
;
; ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
;
;                       A/E Automation Systems
;                       ~~~~~~~~~~~~~~~~~~~~~~
;                    Design Automation Consultants
;                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
;
;  A/E Automation Systems is a group of dedicated design professionals
;  and experts in computer aidid design & drafting (CADD), related
;  CADD systems hardware, software, programming & customization,
;  along with thier respective disciplines to which they apply CADD
;  to professionally. Currently, these include Architecture/engineering
;  & construction.  construction detailing, structural engineering,
;  civil engineering, land planning, electronics engineering, mechinical
;  & electro-mechanical engineering, civil engineering, space planning and
;  interior & industrial design.
;
;  The firm currently offers professional CADD/design automation
;  consulting services on an hourly, fixed-fee, or contract basis,
;  as well as basic CADD design & drafting services.
;
;  The AutoCAD Design & Drafting System, and related products & services.
;
;  The professioals at A/E Automation systems are all veteran AutoCAD
;  users & application specialists, making it our natural area of
;  expertise.
;
;     The following is a brief summary of AutoCAD-related products and
;     services available from A/E Automation Systems.
;
;     New system installation/pre-purchase services
;     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
;       - CADD productivity analysis/profit cycle/feasability studies.
;
;       - CADD hardware/software product research, testing, evaluation,
;         and procurement specification.
;
;       - CADD systems installation supervision.
;
;     Post installation & existing system services
;     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
;       - User training for AutoCAD/LISP/SHADE/SKETCH, introductory thru
;         advanced.
;
;       - AutoLISP programming training and instruction.
;
;       - Independent phone-based user support (by contract,
;         including interactive support via modem).
;
;       - Ongoing CADD systems management (via modem or onsite).
;
;       - Dealer training services for AutoCAD dealers without
;         Authorized/qualified training.
;
;       - Extended dealer services for dealers specializing in
;         intensive vertical applications.
;
;     Other products & services
;     ~~~~~~~~~~~~~~~~~~~~~~~~~
;       - A/E/C CADD design, drafting, digitizing & plotting services.
;
;       - Extensive AutoCAD customization/programming to optimize
;         system to various specific disciplines.
;
;       - Professional AutoLISP programming services (contract &
;         hourly), including complete development, testing, install-
;         ation, training, and support of turnkey applications.
;
;       - Marketing partnership & profit sharing agreements for
;         commerially marketed custom-developed applications
;         software with the client for whom the product was
;         originally produced for.
;
;       - Custom symbol libraries, electronic product catalogs and
;         templates (generic and manufacturer specific) for use by
;         manufacturers and suppliers of products usually included
;         & detailed in printed catalogs and on conventional drafting
;         templates, for the convience of thier clients (or sale) and
;         others who specify and include detail drawings & technical
;         descriptions of such products in thier plans, drawings, and
;         contract documents.
;
;     AutoCAD Application Software
;     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
;       - Developers of commercial AutoCAD add-in applications
;         Including AutoLISP programming tools, utilities and
;         function libraries for application developers, user
;         customization tools, user productivity tools, And
;         descipline-specific and highly-specialized add-in
;         application software products.
;
;  For more information on the products and services available from
;  A/E Automation Systems, call during business hours, or write to
;  the address listed in this file.
;
;--------------------------------------------------------------------------+
; SEQUENCE.LSP  (11/12/87)    A/E Automation Systems (all rights reserved) |
;--------------------------------------------------------------------------+
; SEQUENCE/NEXT is a block display sequencer for AutoCAD 2.5x +.
; It will on command, sequentially zoom/pan to each insertion of a
; given block in a drawing. This is useful for inspecting, and/or
; editing each and every insertion of a particular block individually.
;
; Using SEQUENCE/NEXT
; ~~~~~~~~~~~~~~~~~~~
; To use SEQUENCE/NEXT, load the file or add to your ACAD.LSP file.
; In a drawing that has several insertions of a particular block, enter
; SEQUENCE at the AutoCAD Command: prompt. You will first be prompted
; for the name of the block which you want to sequence. Enter the name
; of a defined block for which there are any number of insertions in
; the current drawing (or press return and SELECT an insertion of that
; block if one is visible on the display).
;
; View display height
; ~~~~~~~~~~~~~~~~~~~
; Next, you will be prompted for the display height when the program
; zooms to the block. The height should be sufficient enough display
; the entire block. For example, if you wanted to sequence each door
; on a floor plan, you might specify a display height of 6-8 feet.
; This would be good enough to display the door regardless of it's angle,
; along with some of the area around it. Keep in mind that if the block
; view display height specified is small enough, when SEQUENCE/NEXT
; attempts to ZOOM to a block insertion, a REGEN will be required.
; If REGENAUTO is OFF, then you will see the that familiar prompt;
; "About to regen, proceed? <Y>: ". If you respond Yes to this question,
; the ZOOM will proceed. If you respond NO, then the SEQUENCE/NEXT will
; simply terminate. To avoid this, set REGENAUTO to ON or AUTO.
;
; A regen will also be required if SEQUENCE/NEXT attempts to zoom to
; a block which is outside of the virtual display, or drawing extents.
; We are looking into getting SEQUENCE/NEXT to detect if a regen will be
; required before zooming, and optionally SKIP that block insertion, and
; proceed to the next. This feature _may_ appear in the next version.
;
; You should also be sure that IF limits checking is on (I never use it)
; that all zooms to all occurances of the block insertion being sequenced
; are within the drawing limits.
;
; SEQUENCE will then attempt to find the first insertion of the selected
; block definition. When it finds the first insertion of the target block,
; it will by itself, zoom center on the block's insertion point, at the
; display height which you specified, and then terminate. You can now
; inspect and edit this insertion of the block if you wish, or you can
; go on to the next one.
;
; NEXT command
; ~~~~~~~~~~~~
; To proceed to the next block, simply enter NEXT at the AutoCAD command
; prompt. The sequencer will now attempt to find the next logical insertion
; of the selected block. If and when it does, it will again zoom center
; on the block's insertion point and terminate. To go on to the next block,
; again, just enter NEXT at the command prompt. When the last insertion of
; this block is sequenced, the next time you issue NEXT command, you will
; be notified that there are no more insertions of this block in the drawing.
;
; Would you believe this program is only a demo?!
; ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
; SEQUENCE/NEXT is a beta/demo version of AutoSEEK, a powerful block
; insertion command/process sequencer for AutoCAD rel. 9. The display
; sequencing (zooming to the block insertion) function of this program
; is only one of many powerful functions and operations which AutoSEEK
; can be perform on block insertions in the same manner in which
; SEQUENCE/NEXT display sequences block insertions.
;
; Look for AutoSEEK, coming soon from A/E Automation Systems.
;
; Enjoy! Tony Tanzillo, A/E Automation Systems.
;
;
; Included functions
;
; Name         Desciption
; ----------------------------------------------------------------------
; C:SEQUENCE   Sequencer initiation function
; C:NEXT       Sequencer continuation function from command prompt (2.5x +)
; NEXT         Transparent sequencer continuation function (2.6x + only)
; STRING       Null string filter
; QUERY        Logic (Yes/No) input logic function
; *ERROR*      Critical error handling function.
;
; Free global variables
;
; Name    Type     Description
; --------------------------------------------------------------------------
; SEQ:BL  STR      Name of current block being sequenced
; SEQ:NX  ENAME    ENAME of next entity to sequence
; SEQ:HT  REAL     Zoom display height.
; SEQ:HI  SYM      Block highlighting flag (NIL = highlighting off)
; SEQ:CF  SYM      Next command/transparent flag (NIL = transparent command)


(defun C:SEQUENCE( / e pr st)
  (cond
    ( (and
        (setq SEQ:BL
          (cond
            ((string (getstring (strcat "\nBlock name to sequence "
                                        "/<Select object>: "))))
            (t (cond
                 ((and (setq e (entsel "\nSelect block insertion: "))
                       (eq "INSERT" (cdr (assoc 0 (setq pr (entget (car e)))))))
                  (redraw (car e) 3)
                  (setq st (query "Begin sequencing from selected insertion" nil))
                  (cdr (assoc 2 pr)))))))
        (setq SEQ:HT
          (cond ((getdist
                   (strcat "\nView display height <"
                           (rtos (getvar "viewsize")) ">: ")))
                (t (getvar "viewsize"))))
        (not (eq SEQ:BL ""))
        (setq SEQ:HI (query "Highlight block insertions" (cond (SEQ:HI) (T))))
        (setq SEQ:BL (strcase SEQ:BL)))
      (prompt
        (strcat "\nSearching for first insertion of "
                (strcase SEQ:BL) "... "))
      (setq SEQ:NX (cond (  (and e st)
                            (redraw (car e))
                            (entnext (car e)))

                         (t (entnext))))
      (while
        (cond
          ( (and (not (eq SEQ:BL (cdr (assoc 2 (setq pr (entget SEQ:NX))))))
                 (setq SEQ:NX (entnext SEQ:NX))))))
      (cond
        ( (eq SEQ:BL (cdr (assoc 2 pr)))
          (prompt "Found... ")
          (setvar "cmdecho" 0)
          (command "zoom" "c" (cdr (assoc 10 pr)) SEQ:HT)
          (cond (SEQ:HI (redraw SEQ:NX 3)))
          (setq SEQ:NX (entnext SEQ:NX))
          (prompt "\nUse NEXT command to continue block insertion sequencing.")))))
  (prin1))


(defun next ( / pr)
  (cond
    ( (and SEQ:BL SEQ:HT SEQ:NX)
      (prompt (strcat "\nSearching for next insertion of "
                      (strcase SEQ:BL) "... "))
      (while
        (cond
          ( (not (eq SEQ:BL (cdr (assoc 2 (setq pr (entget SEQ:NX))))))
            (setq SEQ:NX (entnext SEQ:NX))
          )
          ( (eq SEQ:BL (cdr (assoc 2 pr)))
            (prompt "Found. ")
            (setvar "cmdecho" 0)
            (command
              (cond (SEQ:CF "zoom") (t "'zoom"))
              "c" (cdr (assoc 10 pr)) SEQ:HT)
            (cond (SEQ:HI (redraw SEQ:NX 3)))
            (setq SEQ:NX (entnext SEQ:NX)) nil)))
    )
    (SEQ:BL
      (setq SEQ:BL
        (prompt (strcat "Can not find any more insertions of "
                         SEQ:BL
                        ", sequencing terminated." ))))
    (t (prompt
         (strcat "No block selected for sequencing."
                 "\nUse SEQUENCE command to select block to sequence."))))
  (cond (SEQ:CF)
        (t (prin1))))

(defun C:NEXT()
  (setq SEQ:CF t)
  (next)
  (setq SEQ:CF nil)
  (prin1))

(defun string(s) (cond ( (and s (not (zerop (strlen s)))) s)))

(defun *error* (S)
  (cond
    ((member s '("Function cancelled" "quit / exit abort" "console break")))
    (s (setq *error*
         (prompt (strcat "\nAutoLISP error: " s
                         ".\nProcedure aborted, error handler disabled.")))))
  (prin1))

(defun query(p d / a)
  (setq d (if d "Y" "N"))
  (cond
    ((member
       (setq a
         (substr
           (strcase
             (getstring
               (strcat "\n" p "? <" d ">: ")))1 1))
       '("Y" "N" ""))
       (cond
         ((eq a "Y") T)
         ((and (eq a "")(eq d "Y")) t)))
    (T (prompt "Invalid response,")
       (query p d))))


(prompt
 "\nSEQUENCE/NEXT Version 1.0d        SEQUENCE - Invoke block insertion sequencing."
)
(prompt
 "\n(c)1988, A/E Automation Systems   NEXT     - Next block insertion in sequence."
)
(prin1)

;======================<End of file SEQUENCE.LSP>==============================
