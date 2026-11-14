;----------------------------------------------------------
;
;   Advanced AutoLISP Concepts      December 1988 
;
;   Bill & Denise Kramer    Kramer Consulting, Inc.
;
;   Listing 1.
;
;    (DECHEX <integer>) converts integer to hex string.
;    (HEXDEC <hex string>) converts hex string to integer.
;
;    Integer range supported 0-32767
;                            0-7FFF
;
(defun dechex (dd / hx)
   (setq hx "")
   (while (> dd 0)
     (setq tt (rem dd 16) 
           dd (fix (/ dd 16.0))
           hx (strcat (cond ((> tt 9) (chr (+ tt 55))) 
                            (t (chr (+ tt 48))))
                      hx))))

(defun hexdec (hx / dd ex pp ch)
   (setq dd 0
         ex -1
         hx (strcase hx)
         pp (strlen hx))
   (while (> pp 0)
      (setq ch (ascii (substr hx pp 1)) 
            ch (cond
                 ((> ch 64) (- ch 55))
                 (t (- ch 48)))
            pp (1- pp)
            dd (+ dd (* ch (expt 16 (setq ex (1+ ex))))))))
;
;----------------------------------------------------------
;    Listing 2
;
;    (ENTPREV <entity name>)   returns previous entity name
;
;    Note: Entity HANDLES must be ON before (entprev) will
;          operate as intended.
;
; ----------------------------------------------------------
(defun entprev (en / hx)
   (cond
      ((= (getvar "HANDLES") 0) (prompt "\nEntity handles must be ON"))
      ((not (equal EN (entnext)))
         (setq hx (dechex (1- (hexdec (cdr (assoc 5 (entget en))))))) 
         (while (not (handent hx)) 
            (setq hx (dechex (1- (hexdec hx)))))
         (handent hx))
      (t nil))
)
;
;-------------------------------------------------------------
;
;   LISTING 3.   TITLE BLOCK MACRO
;
;   Bill Kramer   Kramer Consulting, Inc.
;   Requires AutoCAD Release 10 or greater
;
;   AutoCAD Command:  TITLE
;   Title block data: <drawing name>.HDL
;
;   If HDL file is not found, one is created by requesting location
;   of last revision date.  Add other variable information as 
;   required.
;
(defun C:TITLE ( / FN EL CD)
   (if (= (getvar "HANDLES") 0)
      (prompt "\nHANDLES are not currently enabled, please activate.")
      (progn
        (setq FN (open (strcat (getvar "DWGNAME") ".HDL") "r"))
        (if FN (progn
          (entmod
             (setq EL (entget (handent (read-line FN)))
                   CD (rtos (getvar "CDATE") 2 4)
                   CD (strcat (substr CD 5 2) "/"
                              (substr CD 7 2) "/"
                              (substr CD 1 4))
                   EL (subst (cons 1 CD) (assoc 1 EL) EL))) 
          ;  insert other entities here
          )
          (progn
            (setq FN (open (strcat (getvar "DWGNAME") ".HDL") "w")
                  CD (car (entsel "\nShow revision date location:"))
                  EL (entget CD))
            (write-line (cdr (assoc 5 EL)) FN)
            ; insert request for other entity locations here.
          ))
          (close FN)
      ))
      (princ)
)
