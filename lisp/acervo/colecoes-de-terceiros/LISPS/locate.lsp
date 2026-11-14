;----------------------------------------------------------
;
; LOCATE.LSP -- Copyright 1988 by Looking Glass Microproducts
;               Author -- Philip M. Kreiker
;               Version 1.0 -- 10/13/88
;
; Locate Stations offset from an ARC, LINE, or POLYLINE
;
;----------------------------------------------------------
; Global Constants
(setq
   loclayer  "LOCATE"    ; layer on which to place locates
   locstyle  "STANDARD"  ; style of locate text
   locpdmode 3           ; pdmode after calling locate
   half-pi   (* 0.5 pi)   ; 90 degrees
   3-half-pi (* 1.5 pi)   ; -90 degrees
   fuzz      0.02         ; fuzz factor for comparisons
)
;----------------------------------------------------------
; System variable save
(princ ".")
(defun modes (a)
   (setq MLST nil)
   (repeat (length a)
      (setq MLST (append MLST (list (list (car a) (getvar (car a))))))
      (setq a (cdr a)))
   (setq oldlayer (getvar "clayer"))
)
;----------------------------------------------------------
; System variable restore
(princ ".")
(defun moder ()
   (command "layer" "set" oldlayer "")
   (repeat (length MLST)
      (setvar (caar MLST) (cadar MLST))
      (setq MLST (cdr MLST))
   )
)
;----------------------------------------------------------
; Internal error handler
;
(princ ".")
(defun myerror (s)
   (if (/= S "Function cancelled")
      (princ (strcat "\nError: " s))
   )
   (moder)
   (setq *error* olderr)
   (princ)
)
;----------------------------------------------------------
; SGN FUNCTION -- returns Y * Sgn(x)
(princ ".")
(defun sgn (x y)
   (cond
      ((minusp x) (- y))
      ((zerop  x) 0)
      (y)
   )
)
;----------------------------------------------------------
; TAN FUNCTION
(princ ".")
(defun tan (x)
   (/ (sin x) (cos x))
)
;----------------------------------------------------------
; Associated Item
(princ ".")
(defun item (n alist)
   (cdr (assoc n alist))
)
;-----------------------------------------------------------
; POLYLINE PARSE
;    Each Segment is a LIST:
;
;        (<start-point> <end-point> <bulge>).
;
;    If the bulge is 0, return a LIST:
;         (<start-point> <end-point> <bulge> <tangent-dir> <length>)
;
;    If the bulge is non-zero, return a LIST:
;         (<start-point> <end-point> <bulge> <tangent-dir> <length>
;                        <center> <radius> <starting-angle>)
;    NOTE:  Radius has the same sign as bulge.
;
(princ ".")
(defun pparse (segment / p0 p1 bulge a1 a2 a3 r c d sa sd len)
   (setq
      p0    (car segment)
      p1    (cadr segment)
      bulge (caddr segment)
      a1    (angle p0 p1)
      d  (distance p0 p1)
   )
   (append
      segment
      (if (zerop bulge)
          (progn
             (setq blen (+ blen d))
             (list a1 d)   ; line, return dirction and length
          )
          (progn
             (setq
                a2 (* 4.0 (atan bulge))
                a3 (* 0.5 (- pi a2))
                r  (/ (* 0.5 d) (cos a3))
                c  (polar p0 (+ a1 a3) r)
                sa (angle c p0)
                sd (if (minusp bulge)
                       (- sa half-pi)
                       (+ sa half-pi)
                   )
                len (abs (* r a2))
                blen (+ blen len)
             )
             (list sd len c r sa)
          )
      )
   )
)
;-----------------------------------------------------------
; Draw a Graphics arrow from point p at angle a
(princ ".")
(defun arrow (segment / len hwid p1 p2 p3 p4)
   (setq
      p       (car    segment)
      a       (cadddr segment)
      len  (* 0.05 (getvar "viewsize")) ; length of arrow
      hwid (/ len 4.0)                  ; halfwidth of arrow
      p1 (polar p a len)
      p2 (polar p (+ a (* 0.5 pi)) hwid )
      p3 (polar p (- a (* 0.5 pi)) hwid )
      p4 (polar p a hwid)
   )
   (grdraw p1 p2 -1) ; use xor ink
   (grdraw p1 p3 -1)
   (grdraw p4 p2 -1)
   (grdraw p4 p3 -1)
)
;--------------------------------------------------------------------
; XGETKWORD -- Extended Get Keyword
(princ ".")
(defun xgetkword (prmpt kwords default)
   (initget kwords)
   (cond
      (
       (getkword
          (strcat
            prmpt
            " <"
            default
            ">: "
          )
       )
      )
      (
       default
      )
   )
)
;-----------------------------------------------------
;  REPLACE -- Replace One substring with another
;
(princ ".")
(defun replace (oldstr newstr instring all / i nlen olen tempstr)
      (setq
         i       1
         nlen  (strlen newstr)
         olen  (strlen oldstr)
      )
      (while (= olen
                (strlen
                    (setq
                        tmpstr
                        (substr instring i olen)
                     )
                )
             )
         (if (= tmpstr oldstr)
             (setq
                  instring
                  (strcat
                      (substr instring 1 (1- i))
                      newstr
                      (substr instring (+ i olen))
                   )
                   i
                   (if all
                       (+ i nlen)
                       999
                   )
             )
             (setq i (1+ i))
         )
      )
      instring
)
;--------------------------------------------------------------------
; XGETSTRING -- Extended Get String
(princ ".")
(defun xgetstring (cr prmpt default / temp)
   (setq
      temp
      (getstring
         cr
         (strcat
            prmpt
            " <"
            default
            ">: "
          )
       )
   )
   (if (/= "" temp)
       temp
       default
   )
)
;--------------------------------------------------------------------
; Get Station -- get Real in form xxxx+xx.xx
(princ ".")
(defun getstation (prompt)
   (setq stat-string (getstring nil prompt))
   (if (/= "" stat-string)
      (atof (replace "+" "" stat-string T))
   )
)
;--------------------------------------------------------------------
; Get Offset -- get Real in form xxxxxx.xx
(princ ".")
(defun getoffset (prompt)
   (atof
      (setq off-string (getstring nil prompt))
   )
)
;--------------------------------------------------------------
; Create Segment List for LINE
(princ ".")
(defun linesegs (p0 bent / p1 p2)
   ; create a segment list for the line
   ; start at point closest to pick point
   (setq
       p1      (item 10 bent)
       p2      (item 11 bent)
   )
   (if (< (distance p0 p1)
           (distance p0 p2))
           (list (list p1 p2 0.0))
           (list (list p2 p1 0.0))
   )
)
;--------------------------------------------------------------
; Create Segment List for ARC
(princ ".")
(defun arcsegs(p0 bent / cen rad sa ea p1 p2 bulge)
   ; create a vertex list for the arc
   ; start at point closest to pick point
   (setq
      cen     (item 10 bent)
      rad     (item 40 bent)
      sa      (item 50 bent)
      ea      (item 51 bent)
      ea      (if (< ea sa)
                  (+ ea pi pi)
                  ea
              )
      p1      (polar cen sa rad)
      p2      (polar cen ea rad)
      bulge   (tan (* 0.25 (- ea sa)))
   )
   (if (< (distance p0 p1)
       (distance p0 p2))
       (list (list p1 p2 bulge))
       (list (list p2 p1 (- bulge)))
   )
)
;-----------------------------------------------------------
; Create Segment List for POLYLINE
;
(princ ".")
(defun polysegs (p0 bent / vlist blist vename vent forward seglist)
   ; start at point closest to pick point
   (setq
       vlist     nil                      ; vertex list
       blist     nil                      ; bulge list
       vename    (entnext (item -1 bent)) ; entity name of vertix
       vent      (entget vename)          ; vertex entity
   )
   (while
      (and
         (= (item 0  vent) "VERTEX") ; it's a vertex and
         (< (item 70 vent) 16)       ; not a spline control point
       )
       (setq
          vlist     (cons (item 10 vent) vlist) ; add the point to the list
          blist     (cons (item 42 vent) blist) ; add the bulge to the list
          vename    (entnext vename)            ; get the name and
          vent      (entget vename)             ; entity of next vertex(?)
       )
   )
   ; get the list of segments
   (setq
       seglist nil                   ; the list of segments
   )
   (setq
      forward  (< (distance p0 (last vlist)) ; Nearer to First Vertex?
                  (distance p0 (car vlist))
               )
   )
   (while (cadr vlist) ; there's a vertex before this one
       (setq
          seglist (cons  ; add this segment to the segment list
                     (if forward
                        (list
                           (cadr vlist)  ; the vertex before
                           (car  vlist)  ; the vertex
                           (cadr blist)  ; the bulge before
                        )
                        (list
                           (car vlist)      ; the vertex
                           (cadr vlist)     ; the vertex before
                           (- (cadr blist)) ; the other way
                        )
                     )
                     seglist
                  )
          vlist   (cdr vlist)
          blist   (cdr blist)
       )
   )
   (if forward               ; turn it around?
      seglist
      (reverse seglist)
   )
)
;-----------------------------------------------------------
; Check if this is the right Baseline and direction
(princ ".")
(defun bcheck (bpick bent / btype ok)
   ; compute list of segments (seglist)
   ; and length of baseline   (blen)
   (setq
      btype (item 0 bent)
      seglist (cond
                 ((= btype "LINE")     (linesegs bpick bent))
                 ((= btype "ARC")      (arcsegs  bpick bent))
                 ((= btype "POLYLINE") (polysegs bpick bent))
              )
      blen    0.0
      seglist (mapcar 'pparse seglist) ;get centers and angles and lenghts
   )
   (arrow (car seglist))      ; draw an arrow
   (redraw bename 3)          ; highlight entity
   (setq                      ; all ok?
      ok
      (xgetkword "OK?" "Y N" "Y")
    )
    (arrow (car seglist))     ; remove the arrow
    (redraw bename 4)         ; de-highlight entity
    (= "Y" ok)
)
;-----------------------------------------------------------
; STATION -- Return point at station and offset specified
;
(princ ".")
(defun station (bstat seglist stat offset side / p0 dir bulge)
   (if seglist
      (progn
         (setq
            seg   (car seglist)
            estat (+              ; ending station
                     bstat        ; starting station
                     (nth 4 seg) ; length of segment
                  )
         )
         (cond
            (
               (> stat (+ estat fuzz)) ; not on this segment
               (station estat (cdr seglist) stat offset side)
            )
            (T ; on this segment
               (setq
                  p0    (car   seg)
                  bulge (caddr seg)
                  dir   (cadddr seg)
               )
               (if (zerop bulge)
                  (polar                             ; a line segment
                     (polar p0 dir (- stat bstat))   ; the station
                     (if (= "Left" side)             ; the offset direction
                       (+ dir half-pi)
                       (- dir half-pi)
                     )
                    offset                            ; the distance
                  )
                  (polar                              ; an arc segment
                     (nth 5 seg)                      ; the center
                     (+ (nth 7 seg)                   ; starting angle
                        (/ (- stat bstat) (nth 6 seg)); included angle
                     )
                     (-
                        (abs (nth 6 seg))             ; radius
                        (if (= "Left" side)           ; offset
                            (sgn bulge offset)
                            (- (sgn bulge offset))
                        )
                     )
                  )
               )
            )
         )
      )
   )
)
;-----------------------------------------------------------
; Delta XY
(princ ".")
(defun dxy (p dx dy)
   (list
      (+ (car p)  dx)
      (+ (cadr p) dy)
   )
)
;-----------------------------------------------------------
; LEADER -- Draw leader
(princ ".")
(defun leader (p0 stat-string side off-string
                / a p1 p2 p3 p4 p5 p6 p7 rjust ltext temp)
   (setvar "blipmode" bm)
   (setq p1 (getpoint p0 "Endpoint of leader: "))
   (setvar "blipmode" 0)
   (if p1
      (progn ; draw a leader
         (setq
           a     (angle p0 p1)                       ; angle of leader
           p2    (polar p0 a arowsize)               ; origin of leader
           p3    (polar p2 a arowsize)               ; tip of arrowhead
           p4    (polar p3 (+ a half-pi) arowsize/6) ; corner of arrowhead
           p5    (polar p3 (- a half-pi) arowsize/6) ; corner of arrowhead
           rjust (< half-pi a 3-half-pi)             ; Right justify?
           p6    (if rjust                           ; end of tail
                    (dxy p1 (- arowsize) 0)
                    (dxy p1 arowsize    0)
                 )
           p7    (if rjust                          ; insertion point of text
                    (dxy p6 (- txtsize) (* -0.5 txtsize))
                    (dxy p6 txtsize     (* -0.5 txtsize))
                 )
         )
         (if (>= (distance p0 p1) (* 3.0 arowsize)) ; room for the arrowhead?
            (command                   ; yes, draw the arrowheads
               "solid" p2 p4 p5 "" ""
               "line" p3 p1 p6 ""
            )
            (command                    ; no, just draw lines
               "line" p2 p1 p6 ""
            )
         )
      )
      (setq                                      ; don't draw a leader
        rjust nil                                ; don't right justify text
        p7    (dxy p0 txtsize (* -0.5 txtsize)) ; put it here
      )
   )
   (if rjust                        ; text justification
        (command "text" "R" p7)
        (command "text"     p7)
   )
   (if (not fixdhite)              ; text height
        (command txtsize)
   )
   (command "0") ; rotation angle
   (setq
      ltext (strcat
               "STA "
               stat-string
            )
   )
   (if (/= "" off-string)
      (setq
         ltext (strcat
                  ltext
                  (if (= side "Right")
                     " RT "
                     " LT "
                  )
                  off-string

                )
       )
   )
   (setq
      temp
      (getstring
         T
         (strcat
            "\nDescription <"
            ltext
            ">: "
          )
       )
       ltext (if (= temp "")
                 ltext
                 temp
              )
   )
   (command ltext)                                        ; text
   (while                                                 ; loop for text
      (/= "" (setq ltext (getstring T "\nDescription: ")))
      (command "text" "" ltext)
   )
)
;-----------------------------------------------------------
; Main program.  Locates Objects by Station and Off-set.
;
(princ ".")
(defun c:locate( /   olderr oldlayer arowsize/6 arowsize txtsize
                     bm style fixdhite b bename bpick bent btype
                     ok bstat stat stat-string off-set off-string
                     offpoint blen)

   (setq olderr   *error*
         *error*  myerror
   )
   (modes '("CMDECHO" "BLIPMODE" "HIGHLIGHT"))
   (setq
      arowsize   (* (getvar "dimscale") (getvar "dimasz"))
      arowsize/6 (/ arowsize 6.0)
      txtsize  (* (getvar "dimscale") (getvar "dimtxt"))
   )
   (setvar "CMDECHO" 0)
   (setvar "HIGHLIGHT" 0)
   (setq bm (getvar "blipmode"))
   (setvar "blipmode" 0)
   (command "layer" "make" loclayer "")

   ; set text style to locstyle
   (if (and
          (setq style (tblsearch "style" locstyle)) ; does style exist and
          (> (item 40 style) 0.0)                   ; is the height fixed
       )
       (setq
         fixdhite T
         txtsize  (item 40 style)
       )
       (setq fixdhite nil)
   )
   (if (/= (getvar "textstyle") locstyle)
       (command "style" locstyle "" "" "" "" "" "" "")
   )

   (setvar "pdmode" locpdmode)
   (setvar "pdsize" txtsize)

   (while
      (null
         (setq
             b (entsel "\nPick the center/base line close to its origin: ")
         )
      )
   )
   (setq
       bename (car b)          ; baseline entity name
       bpick  (cadr b)         ; baseline pick point
       bent   (entget bename)  ; baseline entity
       btype  (item 0 bent)    ; baseline entity type
       ok     (member btype '("LINE" "ARC" "POLYLINE"))
   )
   (if ok
       (setq ok (bcheck bpick bent))  ; is this the right
                                      ; entity and direction?
       (princ "\nSelected entity is NOT a LINE, ARC, or POLYLINE.")
   )
   (if ok
      (progn
         (setq
            bstat (cond
                      ((getstation "\nOrigin station <0+00>: "))
                      (0.00)
                   )
         )
         (while
            (setq         ; get a station
               stat (getstation "\nStation: ")
            )
            (if (<= (- bstat fuzz)                 ; on the baseline?
                    stat
                    (+ bstat blen fuzz)
                )
               (progn
                  (setq
                     offset
                     (getoffset "Off-set: ")
                  )
                  (if (/= offset 0.0)
                     (progn
                        (initget 1 "Left Right")  ; disallow null left or right
                        (setq side (getkword "Left or Right: "))
                     )
                     (setq off-string "")
                  )
                  (setq
                    offpoint (station bstat seglist stat offset side)
                  )
                  (command "undo" "group")
                  (command "point" offpoint)
                  (leader offpoint stat-string side off-string)
                  (command "undo" "end")
               )
               (princ "\nInvalid input -- try again.")
            )
         )
      )
   )
   (moder)                            ; Restore saved modes
   (setq *error* olderr)              ; Restore old *error* handler
   (princ)
)
(princ "\nLOCATE.LSP -- Copyright 1988 by Looking Glass Microproducts\n")
(c:locate)

