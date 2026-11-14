; DIVDIST.LSP

; Written by Sri Vaidyanathan
; Bechtel Corp.
; 50 Beale St., PO Box 3965,
; San Francisco, CA 94119-3965, USA
; 415-768-1234
; 11/18/91


; Place markers or lines on a entity at equal or given intervals OR
; place markers or lines on a path specified "on the fly" at equal 
; or given intervals.

; ------------------------------------------------

;error handler

(defun *error* (s)
(if (> (length vars) 0) (progn
    (if (= s "") (command "layer" "s" (nth 0 vars) ""))
    (setvar "dragmode" (nth 1 vars))
    (setvar "coords" (nth 2 vars))
    (setvar "cmdecho" (nth 3 vars))
    (setvar "regenmode" (nth 4 vars))
    (setvar "attreq" (nth 5 vars))
    (setvar "osmode" (nth 6 vars))
))
(if (and (/= s "") (/= s "Function cancelled"))
    (princ (strcat "\nError: " s))
)
(princ)
)

; divide Entity/Path
; e,e1,e2 - elem list
; p,p0,p1 - points
; s - string
; i - index
; b - blk name
; vars - sys vars list

(defun divdist ()
(setq vars (list (getvar "clayer") (getvar "dragmode") (getvar "coords")
                 (getvar "cmdecho") (getvar "regenmode") (getvar "attreq")
                 (getvar "osmode")
))
(setvar "dragmode" 2)
(setvar "coords" 2)
(setvar "cmdecho" 0)
(setvar "regenmode" 1)
(setvar "attreq" 0)
(setvar "osmode" 0)
(setvar "highlight" 1)
(setq b "" p0 nil)

(while (/= p0 "X")  
 (setq i 0 s "" e nil e1 nil e2 nil p1 nil p0 nil p nil)
 (setq p0 (getpoint "\nDivide entity/points - press RETURN to exit "))
 (if p0 (setq e (ssget p0)) (setq p0 "X"))              ;sel or exit
 (if e (progn                                           ;entity selected
        (setq e (entget (ssname e 0)))                  ;ent list
        (setq p (cdr(assoc 0 e)))                       ;ent type
        (if (null (or (= p "LINE")(= p "ARC")(= p "CIRCLE")(= p "POLYLINE")))
            (progn
            (prompt "\ncannot divide that entity! try another")
            (setq e nil p0 nil)
        )) ;end if p
 )) ;end if e

 (cond ((and e p0)                                ;elem found
       (command "layer" "s" (cdr(assoc 8 e)) "")  ;chg to elem layer
       (divelem) 
       (command "layer" "s" (nth 0 vars) "")      ;reset layer
       )

       ((and (null e) p0 (/= p0 "X"))           ;pts given, elem not found 
        (setq p p0)                             ;start pt
        (initget 1)
        (setq p1 (getpoint p "to point: "))
        (command "pline" p "w" 0 0 p1 "")
        (setq e (entlast))
        (while p1                               ;do until close
               (setq p p1)
               (initget "Close Arc")
               (setq p1 (getpoint p "Close/Arc/to point: "))
               (cond ((null p1) p1)             ;end path
                     ((= p1 "Close")
                      (command "line" p p0 "")
                      (setq p1 nil)
                     )
                     ((= p1 "Arc")
                      (command "arc" "" pause)
                      (setq p1 (getvar "lastpoint"))
                     )
                     (T 
                      (command "line" p p1 "")
                     )  
               )  ;end cond p1

               (command "pedit" e "j" (entlast) "" "x") ;join line/arc to pline
               (setq e (entlast))
        ) ;end while p1

        (setq e2 e)             ;save elem to erase later
        (divelem) 
        (entdel e2)             ;del path
      )  ;end cond pts

 ) ;end cond
) ;end while

(*error* "")                    ;reset vars & return
) ;end func


;divide element

(defun divelem ()
       (initget "Yes No")
       (setq s (getkword "\nDivide equal? <y>:"))
       (if (= s "No") (progn
                      (getblk)
                      (setvar "cmdecho" 1)  
                      (command "measure" p0 "b" s pause pause) 
                      (setvar "cmdecho" 0)  
                      )    
                      (progn    ;yes
                      (getblk)
                      (setvar "cmdecho" 1)  
                      (command "divide" p0 "b" s pause pause) 
                      (setvar "cmdecho" 0)
       )) ;end if s
       (initget 6) 
       (setq p (getdist "\nScale factor: "))
       (if p (progn
             (setq i 0  e (ssget "p"))          ;make a set of these blks
             (while (< i (sslength e))
               (setq e1 (entget(ssname e i))    ;elem list
                     e1 (subst (cons 41 p) (assoc 41 e1) e1)  ;x scale
                     e1 (subst (cons 42 p) (assoc 42 e1) e1)  ;y scale
                     e1 (subst (cons 43 p) (assoc 43 e1) e1)  ;z scale
               )
               (entmod e1)
               (setq i (+ i 1))
             ) ;end while
       )) ;end if
) ;end func

;get blk name

(defun getblk ()
 (setq s "?")
 (while (= s "?")
  (setq s (getstring (strcat "\n<insert block " b ">/ Line/ Visual selection/ ? for list: ")))
  (cond ((= s "?") (command "block" "?" (command))) ;list blks

        ((or(= s "L")(= s "l"))                     ;place line    
         (setq s "tmpln" b s)                       ;tmp blk name for line
         (if (null (tblsearch "block" s)) (progn    ;blk undefined
          (command "line" p0 (polar p0 (/ pi 2) 1) "")
          (command "chprop" "l" "" "c" "byblock" "la" "0" "lt" "byblock" "") 
          (command "block" s p0 "l" "")             ;create tmp blk
        )))

        ((or(= s "V")(= s "v"))                     ;visual sel
         (setq e1 (entsel))
         (if e1 (setq e1 (entget (car e1))))
         (if (= (cdr(assoc 0 e1)) "INSERT")
             (setq s (cdr(assoc 2 e1)) b s e1 nil)
             (progn (prompt "\nnot a Block!") (setq s "?" e1 nil))
         ) ;end if insert
        ) ;end cond

        (T                                          ;blk name
          (if (= s "") (setq s b))
          (if (null (tblsearch "block" s))
            (if (findfile (strcat s ".dwg"))        ;external
               (command "insert" s (command))
               (progn                               ;blk undefined
                (prompt "not found!") 
                (setq s "?")
               )
          ))
          (if (/= s "?") (setq b s))
        ) ;end cond

  ) ;end cond s
 ) ;end while s
) ;end func
