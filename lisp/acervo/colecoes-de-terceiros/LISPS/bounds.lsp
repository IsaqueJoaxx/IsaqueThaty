;
; ßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßß
; A/E Automation Systems          ²² ²² ²²²²²  °°°°°°°°°°°°°°°°°°°°°°°°°
;                               ²²²  ²²
;                             ²²²    ²² ²²²       32 Gould Avenue
;                           ²²²   ²² ²²           Paterson, N.J.  07503
; °°°°°°°°°°°°°°°°°°°°°°  ²²²        ²² ²²²²²     (201) 523-7944
;
; ßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßßß
; d e s i g n          a u t o m a t i o n         c o n s u l t a n t s
;
; ----------------------------------------------------------------------
; BOUNDS.LSP
; 12/07/88
; For AutoCAD/AutoLISP Rel. 9 or greater.
;
; By Tony Tanzillo, A/E Automation Systems  (all rights reserved).
;
; Shareware/demo version (NOT PUBLIC DOMAIN).
;
; You may not SELL this file or any information from within it,
; or distribute it free to others IF this file or any portion
; herein has been modified an any way.
;
; You may distribute it freely if it is not modified in any way.
;
; Purpose:
;
;   Tabulates meets and bounds of a closed polygon on drawing,
;   and writes them onto a meets and bounds table at a user
;   specified location on the drawing. Optionally, it also places
;   DONUTS at each stake location (vertex).
;
;   This program can be used by A/E's to generate a table of
;   meets and bounds on a Site plan, plat, survey or other
;   similar document.
;
; Limitations:
;
;   This Shareware version does not tabulate curved segments,
;   or label meets and bounds on each segment.
;
;   To obtain standard version (supports radial segments and
;   segment labeling, and the AutoCAD degrees symbol %%D),
;   contact A/E Automation Systems.
;
; Usage:
;
;   New AutoCAD command: TABLE
;
;   Load this file by entering (load"bounds") <CR> at the AutoCAD
;   Command prompt.
;
; Required user input:
;
;     1. Select the closed polygon (polyline) boundry to be processed.
;     2. Specify the upper left corner of the boundry table.
;     3. Specify the right side of the table (dragging the box).
;     4. Specify the text height for the table (if current style = 0).
;     5. Specify the text linefeed spacing for the table.
;     6. Specify the donut inner diameter (or enter for NO DONUTS).
;     7. Specify the donut outer diameter.
;
; Result:
;
;   TABLE will print the bearing and distance of each segment of
;   the polyline, and place a donut at each vertex. Bearings are
;   printed along the left side of the table area, and distances
;   are right-justified along the right side of the table area.
;   A foot mark (') is added to the distance to represent decimal feet.
;
;   It is recommended that you use a NON-PROPORTIONALLY SPACED text font
;   such as MONOTXT.SH? or MONOSIMP.SH? to permit proper decimal alignment
;   of numeric table data.
;
; Program listing:
; =======================================================================

(defun C:TABLE ( / pline col1 col2 txtht txtlf env row txt
                   vertex done vert dist ang vertlast i
               )

; --[ Plug author! ]---------------------------------------------------------

(prompt "\nBOUNDS.LSP ----------------------------------- by A/E Automation Systems, 1988")

; --[ Get and validate user input ]------------------------------------------

  (cond
     (  (and (setq pline (car (entsel "\nSelect boundry polygon: ")))
             (eq (get 0 pline) "POLYLINE")
             (cond ( (zerop (get 70 pline))
                     (prompt "\nBoundry must be a closed polyline.")
                   )
                   (t)
             )
             (setq col1 (getpoint "\nUpper left corner of boundry table: "))
             (setq col2 (getcorner col1 "\nRight side of boundary table: "))
        )
        (setq txtht (cond
                       (  (zerop (setq txt
                                       (get 40 (tblsearch "STYLE"
                                                          (getvar "textstyle")
                                               )
                                       )
                                 )
                          )
                          (cond
                             (  (getdist (strcat "\nBoundry table text height"
                                                 " <"
                                                 (rtos (getvar "textsize"))
                                                 ">: "
                                         )
                                )
                             )
                             (t (getvar "textsize"))
                          )
                       )
                    )
              txt   (cond (txtht) (t txt))
              txtlf (getdist "\nTable text linefeed spacing <NORMAL>: ")
              txtlf (cond ( txtlf
                            (cond (  (> txt txtlf)
                                     (+ txt txtlf)
                                  )
                                  (t txtlf)
                             )
                          )
                          (t (* txt 1.666))
                    )

              tbl:did (cond ( (getdist (strcat "\nDonut I.D"
                                               (cond ( tbl:did
                                                       (strcat ". <"
                                                               (rtos tbl:did)
                                                               ">: "
                                                       )
                                                     )
                                                     (t ": ")
                                               )
                                       )
                              )
                            )
                            (t tbl:did)
                      )

              tbl:dod (cond ( (not tbl:did) nil)
                            ( (getdist (strcat "\nDonut O.D"
                                               (cond ( tbl:dod
                                                       (strcat ". <"
                                                               (rtos tbl:dod)
                                                               ">: "
                                                       )
                                                     )
                                                     (t ": ")
                                               )
                                       )
                              )
                            )
                            (t tbl:dod)
                      )

              row  (- (cadr col1) txtlf)
              col1 (car col1)
              col2 (car col2)
        )

; --[ Push environment ]-----------------------------------------------------

        (setq env (mapcar '(lambda (x / a)
                             (setq a (getvar (car x)))
                             (apply 'setvar x)
                             (list (car x) a)
                           )
                          '( ("CMDECHO"   0)
                             ("HIGHLIGHT" 0)
                             ("BLIPMODE"  0)
                           )
                  )
        )

        (command)                      ; *CANCEL* anything in progress
        (command "undo" "m")           ; mark start of UNDO
        group

        (setq i 0)                     ; initialize bell and whistle counter
        (terpri)

; --[ Process polyline vertices ]-------------------------------------------

        (while (not done)
               (setq vertex (entnext (cond (vertex (get -1 vertex))
                                           (t pline)
                                     )
                            )
               )
               (setq vertex
                     (cond ( (eq "VERTEX" (get 0 vertex) )  ; more vertices?
                             (entget vertex)                ; yes
                           )
                           (t (setq done (entget (entnext pline)))) ; no
                     )
               )

; --[ Handle vertex ]-------------------------------------------------------

               (cond ( vertlast       ; if this is not the first vertex,

                       (setq vert     (get 10 vertex)          ; vertex point
                             dist     (distance vertlast vert) ; distance
                             ang      (angle vertlast vert)    ; bearing
                             vertlast vert                     ; make current
                       )                                       ; vertex last

; ------------------------- increment and display the bell and wistle counter

                       (prompt (strcat "\rProcessing vertex "
                                       (itoa (setq i (1+ i)))
                               )
                       )


; -------------------------------- draw donut if both diamaters are specified

                       (cond ( (and tbl:did tbl:dod)
                               (command "donut" tbl:did tbl:dod vert "")
                             )
                       )

; -------------------------------------------------------------- bearing text

                       (command "text"  (list col1 row)) ; insertion point
                       (cond (txtht (command txtht)))    ; height if not fixed
                       (command 0.0                      ; rotation angle
                                (angtos ang)             ; bearing text
                       )

; ------------------------------------------------------------- distance text

                       (command "text"
                                "R"                      ; right aligned
                                (list col2 row)          ; insertion point
                       )
                       (cond (txtht (command txtht)))    ; height if not fixed

                       (command 0.0                      ; rotation angle
                                (strcat (rtos dist)      ; distance text
                                        "'"              ; decimal feet (')
                                )
                       )

; ------------------------------------------------ move down to next text row

                       (setq row (- row txtlf))

                     )

; ----------------------------------------- setup if this is the first vertex ---------------------------------

                     (t (setq vertlast (get 10 vertex)))
               )
        )
        (prompt "\r                              \rDone.")
     )

; -------------------------------------------------- end of vertex processing



; --[ User input no good, complain ]----------------------------------------

     (t (prompt "\n\007Error, invalid input. "))
  )

; --[ Finished, pop environment and exit quietly ]--------------------------

  (mapcar '(lambda (x) (apply 'setvar x)) env)

  (prin1)

)

; Retrive entity data
;
; (get <key> <entity ENAME | assoc LIST> )
;

(defun get (k l)
  (cond ( (not (listp l))
          (setq l (cond ( (and *GET* (eq l (cdr (assoc -1 *GET* )))) *GET*)
                        (t (setq *GET* (entget l)))))))
  (cond ( (listp k) (mapcar '(lambda (x) (cdr (assoc x l))) k))
        (t (cdr (assoc k l)))))




(prompt "\nBOUNDS.LSP ")
(repeat 36
  (prompt ">")
  (repeat 10 t)
)
(prompt " By A/E Automation Systems, 1988")
(prompt "\nTABLE command loaded. ")
(prin1)

