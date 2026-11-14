(defun c:pil ()
  ;traca pilar

  (command "osnap" "none")

  (setq pa ( getpoint "\nPilar pto fixo  :"))

  (setq sn "S")
  (setq a2 0)
  (setq sn ( strcase (getstring "\nEixo 1 hor ? (<S>/N) :")))
  (if (= sn "N") (setq a2 ( getangle pa "\nEntre angulo eixo 1  :")))
  (setq a1 (+ a2 pi))

  (setq sn "S")
  (setq b2 (/ pi 2))
  (setq sn ( strcase (getstring "\nEixo 2 ver ? (<S>/N) :")))
  (if (= sn "N") (setq b2 ( getangle pa "\nEntre angulo eixo 2  :")))
  (setq b1 (+ b2 pi))

  (setq x1 0)
  (setq aux nil)
  (if (= x1 nil)
    (setq x1 (getreal "\nDimensao x- :"))
    (setq aux (getreal (strcat "\nDimensao x- <" (rtos x1) ">:")))
  )
  (if (/= aux nil) (setq x1 aux))

  (setq x2 0)
  (setq aux nil)
  (if (= x2 nil)
    (setq x2 (getreal "\nDimensao x+ :"))
    (setq aux (getreal (strcat "\nDimensao x+ <" (rtos x2) ">:")))
  )
  (if (/= aux nil) (setq x2 aux))

  (setq y1 0)
  (setq aux nil)
  (if (= y1 nil)
    (setq y1 (getreal "\nDimensao y- :"))
    (setq aux (getreal (strcat "\nDimensao y- <" (rtos y1) ">:")))
  )
  (if (/= aux nil) (setq y1 aux))

  (setq y2 0)
  (setq aux nil)
  (if (= y2 nil)
    (setq y2 (getreal "\nDimensao y+ :"))
    (setq aux (getreal (strcat "\nDimensao y+ <" (rtos y2) ">:")))
  )
  (if (/= aux nil) (setq y2 aux))

  (setq p1 (polar (polar pa a1 x1) b1 y1))
  (setq p2 (polar (polar pa a2 x2) b1 y1))
  (setq p4 (polar (polar pa a1 x1) b2 y2))
  (setq p3 (polar (polar pa a2 x2) b2 y2))

  (setvar "osmode" 0)
  (setvar "orthomode" 0)
  (command "pline" p1 p2 p3 p4 "c")

  (if (> x1 0) (setq xx1 (polar pa a1 7.5)) (setq xx1 pa) )
  (if (> x2 0) (setq xx2 (polar pa a2 7.5)) (setq xx2 pa) )
  (if (> y1 0) (setq yy1 (polar pa b1 7.5)) (setq yy1 pa) )
  (if (> y2 0) (setq yy2 (polar pa b2 7.5)) (setq yy2 pa) )

  (setq aux nil)
  (initget "H P B h p b")
  (if (= hp nil)
    (setq hp (getkword "\nHachurar,Pintar ou Branco (H/P/B) ? :"))
    (setq aux (getkword (strcat "\nHachurar,Pintar ou Branco (H/P/B) ? <" hp ">:")))
  )
  (if (/= aux nil) (setq hp aux))
  ( setq hp (strcase hp))
  ( if (= hp "H") (command "hatch" "u" "45" "7.5" "" "last" ""))
  ( if (= hp "P") (command "solid" p1 p2 p4 p3 ""))

  (if (/= hp "P")
      (progn
      (setq aux nil)
      (initget "S N s n")
      (if (= pf nil)
          (setq pf (getkword "\nPonto fixo (S/N) ? :"))
          (setq aux (getkword (strcat "\nPonto fixo (S/N) ? <" pf ">:")))
      )
      (if (/= aux nil) (setq pf aux))
      (setq pf (strcase pf))
      (if (= pf "S" ) (command "solid" yy1 xx1 xx2 yy2 ""))
      )
  )

  (setq pil (getstring "\nNo. do pilar :"))

  (setq pil (strcat "P" pil " - " (itoa(fix(+ x1 x2 0.01))) "x" (itoa(fix(+ y1 y2 0.01)))))

  (setvar "osmode" 0)
  (setq pa (getpoint "\nPto ins. texto :"))

  (command "text" pa "" "0" pil )

  (setq carg (getstring "\nCarga :"))
  (if (/= carg "")
      (progn
      (setq carg (strcat carg " kN"))
      (setq pa (polar pa (/ pi 2) -25))
      (command "text" pa "" "0" car )
      )
  )

)
;----------------------------------------------------------------------------
(defun c:sap ()
  ;traca sapata
  (command "osnap" "none")

  (setq p1 ( getpoint "\nPilar pto 1 :"))
  (setq p2 ( getpoint "\nPilar pto 2 :"))
  (setq p3 ( getpoint "\nPilar pto 3 :"))

  (setq p4 ( polar p1 (angle p2 p3) (distance p2 p3)))


  (setq dx 5)
  (setq aux nil)
  (setq aux (getreal (strcat "\nDistancia x pesc <" (rtos dx) "> : ")))
  (if (/= aux nil) (setq dx aux))

  (setq dy 5)
  (setq aux nil)
  (setq aux (getreal (strcat "\nDistancia y pesc <" (rtos dy) "> : ")))
  (if (/= aux nil) (setq dy aux))

  (setq p1y (polar p1  (angle p4 p1) dy))
  (setq p1y (polar p1y (angle p2 p1) dx))

  (setq p2y (polar p2  (angle p3 p2) dy))
  (setq p2y (polar p2y (angle p1 p2) dx))

  (setq p4y (polar p4  (angle p1 p4) dy))
  (setq p4y (polar p4y (angle p3 p4) dx))

  (setq p3y (polar p3  (angle p2 p3) dy))
  (setq p3y (polar p3y (angle p4 p3) dx))

  (setvar "orthomode" 0)
  (setvar "osmode" 0)
  (command "pline" p1y p2y p3y p4y "c")

  (setq dx (getreal "\nDistancia x  sap : "))

  (setq dy dx)
  (setq aux nil)
  (setq aux (getreal (strcat "\nDistancia y sap <" (rtos dy) "> : ")))
  (if (/= aux nil) (setq dy aux))

  (setq p1x (polar p1  (angle p4 p1) dy))
  (setq p1x (polar p1x (angle p2 p1) dx))

  (setq p2x (polar p2  (angle p3 p2) dy))
  (setq p2x (polar p2x (angle p1 p2) dx))

  (setq p4x (polar p4  (angle p1 p4) dy))
  (setq p4x (polar p4x (angle p3 p4) dx))

  (setq p3x (polar p3  (angle p2 p3) dy))
  (setq p3x (polar p3x (angle p4 p3) dx))

  (setvar "orthomode" 0)
  (setvar "osmode" 0)
  (command "pline" p1x p2x p3x p4x "c")

  (command "line" p1x p1y "")
  (command "line" p2x p2y "")
  (command "line" p3x p3y "")
  (command "line" p4x p4y "")

  (setq sap (getstring "\nNo. da sapata :"))

  (setq sap (strcat "S" sap " - " (itoa(fix(distance p1x p2x))) "x" (itoa(fix (distance p2x p3x)))))

  (setq h (getstring "\nAltura h  :"))

  (setq h (strcat "h  = " h))
  (setq ho (getstring "\nAltura ho :"))

  (setq ho (strcat "ho = " ho))

  (setq pa (getpoint "\nPto ins. texto :"))
  (command "text" pa "" "0" sap )
  (setq pa (polar pa (/ pi 2) -25))
  (command "text" pa "" "0" h )
  (setq pa (polar pa (/ pi 2) -25))
  (command "text" pa "" "0" ho )

)
;----------------------------------------------------------------------------
(defun c:estac()

(command "osnap" "none")

(setq nest (getstring "\nEntre com o nome da estaca  :"))
(setq pcg  (getpoint  "\nEntre com a origem do bloco :"))

(setq sn "S")
(setq a1 0)
(setq sn ( strcase (getstring "\nEixo x local hor ? (<S>/N) :")))
(if (= sn "N") (setq a1 ( getangle pcg "\nEntre angulo eixo x local :")))
(setq b1 (+ a1 (/ pi 2)))

(setq cont nil)

(while (= cont nil)

  (progn

  (setq aux nil)
  (if (= x1 nil)
    (setq x1 (getreal "\nDistancia x local :"))
    (setq aux (getreal (strcat "\nDistancia x <" (rtos x1) ">:")))
  )
  (if (/= aux nil) (setq x1 aux))

  (setq aux nil)
  (if (= y1 nil)
    (setq y1 (getreal "\nDistancia y local :"))
    (setq aux (getreal (strcat "\nDistancia y <" (rtos y1) ">:")))
  )
  (if (/= aux nil) (setq y1 aux))

  (setq pest (polar (polar pcg a1 x1) b1 y1))

  (setvar "orthomode" 0)
  (setvar "osmode" 0)
  (command "insert" nest pest "" "" "" )
  )
)
)

;----------------------------------------------------------------------------

(defun c:rtext (/ ent oldline newline ent1 yesno)
  (prompt "\nTexto a ser alterado ")
  (setq ent (entget (car (entsel))))
  (setq oldline (cdr (assoc 1 ent)))
  (terpri)
  (prompt oldline)
  (terpri)
  (setq newline (getstring 1))
  (setq ent1 (subst (cons (car (assoc 1 ent)) newline) (assoc 1 ent) ent))
  (entmod ent1)
  (prin1)
)

;----------------------------------------------------------------------------

(defun chgterr (s)
   (if (/= s "Function cancelled")   ; If an error (such as CTRL-C) occurs
      (princ (strcat "\nError: " s)) ; while this command is active...
   )
   (setq p nil)                      ; Free selection set
   (setq *error* olderr)             ; Restore old *error* handler
   (princ)
)

(defun C:CTEXT (/ p l n e os as ns st s nsl osl sl si chf chm olderr)
   (setq olderr  *error*             ; Initialize variables
         *error* chgterr
         chm     0)
   (setq p (ssget))                  ; Select objects
   (if p (progn                      ; If any objects selected
      (while (= 0 (setq osl (strlen (setq os (getstring t "\nOld string: ")))))
            (princ "Null input invalid")
      )
      (setq nsl (strlen (setq ns (getstring t "\nNew string: "))))
      (setq l 0 n (sslength p))
      (while (< l n)                 ; For each selected object...
         (if (= "TEXT"               ; Look for TEXT entity type (group 0)
                (cdr (assoc 0 (setq e (entget (ssname p l))))))
            (progn
               (setq chf nil si 1)
               (setq s (cdr (setq as (assoc 1 e))))
               (while (= osl (setq sl (strlen
                             (setq st (substr s si osl)))))
                  (if (= st os)
                      (progn
                        (setq s (strcat (substr s 1 (1- si)) ns
                                        (substr s (+ si osl))))
                        (setq chf t) ; Found old string
                        (setq si (+ si nsl))
                      )
                      (setq si (1+ si))
                  )
               )
               (if chf (progn        ; Substitute new string for old
                  (setq e (subst (cons 1 s) as e))
                  (entmod e)         ; Modify the TEXT entity
                  (setq chm (1+ chm))
               ))
            )
         )
         (setq l (1+ l))
      )
   ))
   (princ "Changed ")                ; Print total lines changed
   (princ chm)
   (princ " text lines.")
   (terpri)
   (setq *error* olderr)             ; Restore old *error* handler
   (princ)
)

;----------------------------------------------------------------------------
;itext.lsp 
;este e um programa simples c/ finalidade didatica

;le escreve uma linha
(defun esclin ()
  (setq linha (read-line ptarq))
  (if (/= linha nil) (command "text" pa "" "0" linha ))
  (setq pa (polar pa (/ pi -2) distl))
)

;le escreve varias linhas, ou seja, um texto
(defun c:itext ()

(prompt "\nPrograma Itext para importar texto  ASCII em desenho")
(prompt "\nNota : layer e style devem ser previamente escolhidos")

(setq noarq (getstring "\nEntre c/ arquivo do  texto  :"))
(setq distl (getdist   "\nEntre c/ dist. entre linhas :"))
(setq pa    (getpoint  "\nEntre c/ pto insercao texto :"))

(setq ptarq (open noarq "r"))

(setq linha " ")
(while (/= linha nil) (esclin))

(setq ptarq (close ptarq))
)

;----------------------------------------------------------------------------

(defun c:newalt ( / ne alt)
  (setq ne nil)
  (while (= ne nil)
     (setq ne (entsel "\nEscolha entidade texto com altura desejada:"))
     (if (/= (cdr (assoc '0 (entget (car ne)))) "TEXT") (setq ne nil))
  )
  (setq alt (cdr (assoc '40 (entget (car ne)))))
  (setvar "textsize" alt)
)

;----------------------------------------------------------------------------

(defun c:chgalt (/ s)
  (setq s (getvar "textsize") )
  ( while (/= s "")
    (command "change" pause "" "" "" "" s "" "")
  )
)

;----------------------------------------------------------------------------

(defun c:newlay ()
  (setq ne (entsel "\nEscolha uma entidade da camada :"))
  (if (= ne nil)
      (setq lay (getstring "\nEntre com o nome da camada : ") )
      (setq lay (cdr (assoc '8 (entget (car ne)))))
  )
  (command "layer" "s" lay "")
)
;----------------------------------------------------------------------------

(defun c:chglay ()
  (setq lay (getvar "clayer"))
  ( while (/= lay "")
    (command "chprop" pause "" "LA" lay "")
  )
)
;----------------------------------------------------------------------------
(defun C:ltext ()
   (setvar "osmode" 512)
   (setq aux nil)
   (while (/= aux "LINE")
      (setq entli (entsel "\nSelecione a linha :"))
      (if (/= entli nil)
          (progn
          (setq datli (entget (car entli )))
          (setq aux (cdr (assoc 0 datli)))
          )
      )
   )

   (setq pa (cdr (assoc 10 datli)))
   (setq pb (cdr (assoc 11 datli)))

   (if (< (car pa) (car pb))
       (setq ab ( angle pa pb ))
       (setq ab ( angle pb pa ))
   )
   (if (equal (car pa) (car pb) '0.5 )
       (setq ab (/ pi 2)))
   (setq ort (+ ab (/ pi 2)))

   (setq paux (cadr entli ))

   (setvar "osmode" 0)
   (setq Taux (getstring "\nEntre com texto :"))

   (if (= ud nil) (setq ud "Cima"))
   (initget "Cima Baixo")
   (setq aux (getkword (strcat "Cima/Baixo <" ud "> :")))
   (if (/= aux nil) (setq ud aux))
   (if (= ud "Cima")
       (progn
       (setq paux (polar paux ort '0))
       (command "text" "j" "BC" paux "" (/ (* ab 180) pi) Taux)
       )
       (progn
       (setq paux (polar paux ort (/ (getvar "textsize") '-3.0)))
       (command "text" "j" "TC" paux "" (/ (* ab 180) pi) Taux)
       )
   )
)

;----------------------------------------------------------------------------
(defun c:ren()
  (if (= tip nil)
      (while (= tip nil) (setq tip (getstring"\nDescricao inicial : ")))
      (progn
      (setq aux nil)
      (setq aux (getstring (strcat "\nDescricao inicial <" tip "> : ")))
      (if (/= aux "") (setq tip aux))
      )
  )
  (setq num (getint    "\nUltimo numeral    : "))
  (setq aren 1)
  (setq aux nil)
  (setq aux (getint (strcat "\nEspacamento<" (itoa aren) "> : ")))
  (if (/= aux nil) (setq aren aux))
  (setq s nil)
  ( while (= s nil)
    (progn
    (setq num (+ num aren))
    (setq nome (strcat tip (itoa num)))
    (command "change" pause "" "" "" "" "" "" nome)
    )
  )
)
;----------------------------------------------------------------------------
(defun c:rena()
  (setq num (ascii (getstring "\nUltima letra    : ")))
  (setq s nil)
  ( while (= s nil)
    (progn
    (setq num (+ num 1))
    (setq nome (chr num))
    (command "change" pause "" "" "" "" "" "" nome)
    )
  )
)
;----------------------------------------------------------------------------
(terpri)
