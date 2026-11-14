
;  9/14/1986 Tony Tanzillo,  A/E Automation Systems
;
;  Enhanced point, distance, angle, string, and
;  floating point entry functions
;
;  (distget) prompts the user for a distance, displays a default
;  value, and returns the distance the user enters or points to,
;  or the default value if the user replys with just enter.
;  The prompt defaults to "Distance <default>: ".
;  if this prompt is desired, pass a null string as an argument ("").
;  If a different prompt is desired, simply enclose it in quotes with
;  no leading or trailing spaces, and it will be used instead
;  of the default prompt "Distance".
;
;  The function requires an optional base point value, (a point)
;  if no base point is desired, simply use NIL as the base point
;  argument, and the user can select 2 points to indicate the distance.
;  if a base point argument is included, just as in (getdist), a
;  rubber band cursor will be anchored to the base point, and the
;  user can select one point to indicate the distance.
;
;  I use this function as an alternative to the (getdist) function
;  which is actually used in dset, along with the prompt (like getdist),
;  it adds a default mechanisim which prevents other calling functions
;  from bombing out when the user replys to (getdist) with a space or cr.
;
;  Included is other functions which get reals, integers, strings, angles
;  and points. I these in all of my AutoLISP code as excellent bomb-proof
;  alternative to thier respective primitives (getpoint, getreal, getangle
;  getstring, getdist). Most use the default provision, and optional
;  prompts, and the point entry function forces input. (like AutoCAD's
;  "Yes or No Please" !)
;
;  Functions in this file:
;
;  (distget)  - prompts for distance, with default
;  (realget)  - prompts for floating point value (real) with default
;  (angleget) - prompts for angle with default
;  (strget)   - prompts for string, with default, space or cr terminated
;  (pointget) - prompts for a point, and forces user to enter same.
;  (query)    - Asks a question, and waits for a Yes or No answer
;
;  Also included are various sample calling C:Functions which demonstrate
;  the different ways of calling the input functions. These are only
;  included for demonstration purposes. (i.a. they do practically nothing !)
;
;  Questions, suggestions, and other feedback is Welcome.
;
;  -Tony Tanzillo (70307,2556)
;
;
;
; (distget)
;
; The arguments are as follows:
;
;
;  d = default value (a real), can be a literal value or a bound symbol.
;      this value is returned to the calling function if the user replys
;      with space or <cr>.
;
;  p = The prompt to be displayed (a string), must be enclosed in quotes.
;      if the default prompt is desired, use NIL as the argument in
;      the calling funcion.
;
;  b = An optional basepoint, (just like (getdist) ) if no basepoint
;      is desired, use NIL as the argument in the calling funcion.
;
;  Calling syntax:
;
;  (distget [ basepoint / NIL ]  [ "prompt" / NIL ] [ default ] )
;

(defun distget( b p d / t)
  (if                               ; did user hit enter or space ?
    (null                           ; yes, return default value (d)
      (setq t                       ; no, continue
        (getdist
          (if (boundp 'b)           ; base point argument ?
            b                       ; yes, use it
            (list NIL NIL)          ; no, then substitute a null list
          )
          (strcat "\n"
            (if (boundp 'p)         ;prompt argument ?
               p                    ;yes, use it
               "Distance"           ;no, use default prompt
            )
            " <"
                                   ; display default value
            (rtos (float d)        ; according to current units settings
               (getvar "lunits")   ; Note: in 2.5, these 2 lines can be
               (getvar "luprec")   ; omitted, i.e. (rtos (float d))
            )
            ">: "
          )
        )
      )
    )
    (float d)                      ; float the default argument value
    t                              ; in the event it was an integer
  )
)


;  The following is 3 examples of C:functions that call (distget) in different
;  ways. They are provided as examples of the different ways in which the
;  function can be called.
;
;  The first function calls (distget) with NO BASEPOINT and a PROMPT
;  (notice the use of NIL as the basepoint argument!).
;
;  The second function calls (distget) with a BASEPOINT and a PROMPT.
;  this function also uses a literal value as the default argument
;  as opposed to a bound real.
;
;  The third calls (distget) with nither a BASEPOINT OR PROMPT,
;  causing it to use the default prompt: [ Distance: <default> ].
;
;  In all cases, the default value will be displayed, and the function
;  will test for a null response, and if detected, will return the
;  value of the default argument back to the calling function. Try
;  responding to the various prompts with enter, and it should return
;  the default value.
;
;

(defun C:DCALL1()                           ; calls distget w/o basepoint,
  (setq fp (getpoint "\nStarting point: ")) ; and with a prompt (d).
  (setq d 4.0 )
  (setq d (distget NIL "Enter a distance, or 2 points" d ))
  (command "line" fp (polar fp 0 d) "")
)

(defun C:DCALL2()                           ; calls distget with a basepoint
  (setq fp (getpoint "\nStarting point: ")) ; and with a prompt.
  (setq base (getvar "viewctr"))
  (setq d (distget base "Enter a distance, or point" 2.5 ))
  (command "line" fp (polar fp 0 d ) "")
)

(defun C:DCALL3()                            ; calls distget w/o a prompt
                                             ; or a basepoint
  (setq fp (getpoint "\nStarting point: "))  ; get a point
  (setq d 4.0 )                              ; set a default value (REAL !)
  (setq d (distget NIL NIL d ))              ; request distance from user
  (command "line" fp (polar fp 0 d) "")      ; draw line with result
)



; (angleget) -same as (distget), except for angles (getangle)



; calling syntax:

;       (angleget [ basepoint / NIL ] [ "prompt" / NIL ] [ default ] )

;       (note that the default angle is in radians, not degrees)

(defun angleget( b p d / t)
  (if
    (null
      (setq t
        (getangle
          (if (boundp 'b)
            b
            (list NIL NIL)
          )
          (strcat "\n"
            (if (boundp 'p)
              p
              "Angle"
            )
            " <"
            (angtos (float d)
              (getvar "aunits")
              (getvar "auprec")
            )
            ">: "
          )
        )
      )
    )
    (float d)
    t
  )
)

; examples of calling functions for (angleget)

(defun C:ACALL()
  (setq base (getvar "viewctr"))
  (setq d (angleget base "Enter an angle, or point" PI ))
  (command "line" base (polar base d 4) "")
)

(defun C:ACALL1()
  (setq fp (pointget NIL "Starting point"))
  (setq d (/ PI 2) )
  (setq d (angleget NIL NIL d ))
  (command "line" fp (polar fp d 4) "")
)

; (realget) same as above, except for reals
;
; p = prompt, v = default value, d = decimal places to display
;
; calling syntax:
;
;   (realget [ "prompt" / NIL ] [ default value ] [ decimal places ] )
;
; (decimal places indicate how many decimal places are used to
;  display the default value in the prompt)
;



(defun realget( p v d / t)
  (if
    (null
      (setq t
        (getreal
          (strcat "\n"
            (if (boundp 'p)
              p
              "How much is that doggie in the window ?"
            )
            " <"
            (rtos (float v) 2 d)
            ">: "
          )
        )
      )
    )
    v
    t
  )
)

; example calling function for (realget)

(defun C:RCALL()
  (setq d (realget NIL 12000.00 2))
  (prompt
     (strcat "Value returned: "
         (rtos d 2 2 ) "\n"
     )
  )
)


;  (strget) ; same as above execpt for getting strings
;
;  p = prompt
;
;  d = default string
;
;  r = indicates if string can contain spaces or not
;      use T or NIL as argument,
;          T = confirm with <CR>
;          NIL = up to first <SPACE> OR <CR>
;
;  calling syntax:
;
;   (strget [ "prompt" ] [ "default" ] [ confirm ] )

(defun strget(p d r / t)
  (if
    (zerop
      (strlen
        (setq t
          (getstring r
            (strcat "\n"
              (if (boundp 'p)
                p
                "How's the weather ?"   ; default prompt
              )
              " <" d ">: "
            )
          )
        )
      )
    )
    d
    t
  )
)


;    Example calling functions for (strget)
;    (first requires <CR> to terminate, second doesn't)


(defun C:SCALL()
  (setq d (strget NIL "Fine !" T))
  (prompt
     (strcat "The weather is " d  " \n" )
  )
)

(defun C:SPCALL()
  (setq d (strget "Terminate with <SPACE> OR <CR>" "X" NIL))
  (prompt
     (strcat "String Returned: " d  " \n" )
  )
)

;  (pointget) forces user to enter a point.
;
;  with opional base point, (2.5 only)
;
;  and prompt
;
;  b = basepoint (list)
;
;  p = prompt (optional, for default prompt, use NIL as argument )
;
;  no default value
;
;  NOTE ! the only way out for the user is to enter <^C> or <^BREAK>
;         2 TIMES IN RAPID SUCCESSION.
;
;  calling syntax:
;
;  (pointget [ basepoint / NIL ] [ "prompt" ] )


(defun pointget( b p / t)
  (setq t (list nil nil))
  (setq f (list 0 0))
  (while
    (null
      (setq t (setq f
        (getpoint
          (if (boundp 'b)     ;   (omit for 2.18)
            b                 ;           "
            (list NIL NIL)    ;           "
          )                   ;           "
          (strcat "\n"
            (if (not (null f))
              ""
              "Please enter a point,   "
            )
            (if (boundp 'p)
              p
              "Point: "
            )
          )
        )
      ))
    )
  )
  (setq t t)
)

; example of a calling function (forces point entry, to escape,
; hit ^C 2 times in rapid succession).

(defun c:PCALL()
  (setq a (pointget NIL NIL))
  (setq b (pointget NIL "Another point: "))
  (command "LINE" a b "" )
)

; QUERY.LSP  9/18/1986
;
; A. F. Tanzillo,  A/E Automation Systems
;
; (QUERY) prompts the user with a question and waits for a Yes or No Reply
; It forces the user to reply with Yes or No IF NO DEFAULT IS SPECIFIED,
; and returns T if the reply was Yes (or Y???) , and NIL if the reply was
; No (or N????)
;
; If a default IS specified, And the user replys with either <space>
; ,<enter> or anything other than Y or N, then it will return T if
; the default is Yes, and Nil Otherwise. (it really should be called
; (queryP) since it is a predicate a.k.a. logical function).
;
; To force the user to reply with Yes Or No, do not supply a default,
; (You must substitute NIL in it's place) and query will loop until
; it recieves a valid reply.
;
; One note of caution: query only looks at the first character of
; the reply, so in reality, any response which begins with Y or N
; will be construed to mean Yes or No respectively.
; (if the user replyes with the word "NEITHER", it wil be assumed
; to mean No, regardless of what the default is if present.
;
;
; Calling Syntax:
;
;             (query [ "prompt" ] [ "default" / NIL ] )
;
; Arguments:
;
;    "prompt" is required, and must be enclosed in quotes. this
;             string will be displayed (along with " ? (Y/N): ") while
;             query is waiting for a reply.
;
;   "default" is optional, the default argument MUST be either "Y" or "N"
;             enclosed in quotes as shown, or NIL (unquoted). If no default
;             is desired. If no default is provided (and replaced with NIL)
;             it will cause query to continue displaying the prompt and
;             annoying the user with "Yes or No Please" until Y or N is
;             entered (followed by <space> or <cr> ).
;
;
;
;

(defun query(p d / a b)
  (while
    (not
      (member
        (read
          (setq a
            (substr
              (if (boundp 'd)
                (setq a
                  (if
                    (member
                      (read
                        (setq a
                          (substr
                            (getstring (strcat "\n" p " ? <" d ">: ")) 1 1
                          )
                        )
                      )   '(Y N)
                    )   a d
                  )
                )
                (getstring
                  (strcat
                    (if (boundp 'a)
                      "\nYes or No Please,\n\n"  "\n"
                    )  p  " ? (Y/N): "
                  )
                )
              )   1 1
            )
          )
        ) '(Y N)
      )
    )
  )  (EQ (READ  A) 'Y)
)



; Example calling functions for (QUERY)
; the first uses a default reply, and returns same to calling function
; if response is anything other than its opposite.
; For Example:

; If the default argument is Yes, then ANY reply other than NO would
; return T to the calling function.

; Likewise, If the default argument were No, then any reply other than
; YES would return NIL to the calling function.

(defun c:Q1()
  (PROMPT
    (STRCAT "The answer was "
      (IF (QUERY "Answer Yes or No" "N")
        "Yes"
        "No"
      )
      "\n"
    )
  )
)

; This second example does not use a default, which forces a Y or N
; response from the user (or any string beginning with Y or N).

(defun c:Q2()
  (prompt
    (strcat "Reply was "
      (if (query "Answer Yes or No" nil)
        "Yes"
        "No"
      )
      "\n"
    )
  )
)


Syntax Reference:

;   (distget [ basepoint / NIL ]  [ "prompt" / NIL ] [ default ] )

;   (angleget [ basepoint / NIL ] [ "prompt" / NIL ] [ default ] )

;   (realget [ "prompt" / NIL ] [ default value ] [ decimal places ] )

;   (strget [ "prompt" ] [ "default" ] [ confirm ] )

;   (pointget [ basepoint / NIL ] [ "prompt" ] )

;   (query [ "prompt" ] [ "default" / NIL ] )
