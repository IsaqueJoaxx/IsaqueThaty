; EZOFF.LSP (command EZOFF)                  For AutoCAD 2.5x +
; 12/4/1987
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
; EZOFF.LSP
;
; EZOFF.LSP adds a command of the same name to AutoCAD, which
; is functionally very similar to the offset command with a
; couple of exceptions. With EZOFF, you can erase the object(s) being
; offset, and you can specify the objects to be offset and the side
; or direction which they are offset to with the same pick of your
; pointing device.
;
; Deleting the object to be offset.
;
;   To do this, simply respond with Y to the "Delete orignal objects?"
;   prompt, and each time you offset an object, it will be erased
;   immediately.
;
; Single point object/offset side specification mode.
;
;   This mode permits you to select the object to be offset and the
;   side to which it is to be offset to, with a SINGLE PICK the pointing
;   device. When you select the object you must CENTER the PICKBOX
;   towards the side of the object which you want it to be offset to.
;   Avoid centering the pickbox directly on the object, since the closer
;   the center of the pickbox is to a point on the object, the more
;   difficult it is to determine what side you are indicating that
;   the object be offset to.
;
; Sample dialouge w/ EZOFF;
;
;   Command: EZOFF
;   Offset distance/<Through>: 2'0"
;   Delete original objects? <N>: N
;   Single point offset specification? <Y>: Y
;   Pickbox size: 6
;   Select object to offset: <center pickbox on offset side of object>
;   Select object to offset: <center pickbox on offset side of object>
;   Select object to offset: ....
;
; Additional functions required: QUERY (inluded)
;
; Enjoy!
;
; Tony Tanzillo, A/E Automation Systems


(defun C:EZOFF ( / sd del sp pb pt fd d)
  (setq sd (cond (  (getdist "\nOffset offset distance/<Through>: ")) (t 0.0)))
  (setq del (query "Delete original objects" nil))
  (cond (  (zerop sd) nil)
        (t (cond ((setq sp (query "\nSingle point offset specification" t))
                  (setq pb (getvar "pickbox"))
                  (setvar "pickbox" (cond (  (getint "\nPickbox size: "))
                                          (t (getvar "pickbox"))))))))
  (setq fd (not (zerop sd)))
  (setvar "cmdecho" 0)
  (command "offset" (cond ((zerop sd) "t") (t sd)))
  (while (cond ((setq e (entsel "\nSelect object to offset: "))
                (command e)
                (while (not (setq pt (cond (  (and sp fd) (cadr e))
                                           (t (getpoint (cond (fd "\nSide to offset to: ")
                                                              (t "\nThrough point: "))))))))
                (command pt)
                (cond (del (entdel (car e))) (t)))))
  (command)
  (prin1))

(defun query(p d / a)
  (setq d (cond (d "Y") (t "N")))
  (cond ( (member (setq a (substr
                            (strcase (getstring
                                       (strcat cr p " ? <" d ">: "))) 1 1))
                  '("Y" "N" ""))
          (cond ((eq a "Y"))
                (t (and (eq a "")
                        (eq d "Y")))))
        (T (prompt "Invalid response,")
           (query p d))))

; If you think that the above is too much code for too
; little gain in functionality, you can use the following
; functon along with AutoCAD's OFFSET command to provide the
; same single point entity/offset side selection that EZOFF
; provides (this one is the real "gem" here). Under no
; circumstances would you need both EZOFF, and following
; function (called "autosel") loaded simultaneously, since
; they both do the same job.
;
; To use this one, You must first issue the OFFSET command, then
; specify a fixed offset distance. When you are prompted for the
; first object to offset, instead of selecting an object, reply
; with "AUTOSEL", and you can pick the object to offset, and
; the side to offset to with a single pick. Notice the
; "transparency" with which this function works along
; with AutoCAD's OFFSET command.
;
; AUTOSEL permits user to select object to offset, and
; side to offset to with one pick (inteded for use with
; fixed-distance offset of multiple objects, not with
; through point offset method).
;

(defun AUTOSEL ( / e pb)
  (setq pb (getvar "pickbox"))
  (setvar "pickbox" (cond (  (getint "\nPickbox size: "))
                          (t (getvar "pickbox"))))
  (prompt "\nCenter pickbox towards offset side of object,")
  (while
    (cond
      ((setq e (entsel "\nOffset object to side: "))
         (command e (cadr e)) t)))
  (command "")
  (setvar "pickbox" pb)
  (prin1))

plates (generic and ma