(defun c:GEOINF_MALHA (/ p1 p2 p3 p4 prefixOption prefixX prefixY
                          esp angX angY layerName
                          divX divY i j ptA ptB ptC ptD
                          distAB distDC distAD distBC
                          nDivX nDivY
                          addThousandSep
                          makeDivPts
                          formatCoord
                          txtPtX txtPtY
                          )

  ;; Função para formatar número sem decimais e com ponto como separador de milhar
  (defun addThousandSep (num / str len res i)
    (setq str (itoa num))
    (setq len (strlen str))
    (setq res "")
    (setq i 0)
    (while (< i len)
      (if (and (> i 0) (= (rem (- len i) 3) 0))
        (setq res (strcat res "."))
      )
      (setq res (strcat res (substr str (+ i 1) 1)))
      (setq i (1+ i))
    )
    res
  )

  ;; Função para formatar texto do ponto
  (defun formatCoord (label val)
    (strcat label "=" (addThousandSep (fix val)))
  )

  ;; Divide segmento AB em n partes, devolve lista de pontos
  (defun makeDivPts (A B n / i result)
    (setq result nil)
    (repeat (+ n 1)
      (setq i (if result (1+ i) 0))
      (setq result (cons
                      (polar A (angle A B) (* (/ i n) (distance A B)))
                      result)))
    (reverse result)
  )

  ;; 1. Seleção dos 4 pontos
  (prompt "\nSelecione o 1º ponto do retângulo:")
  (setq p1 (getpoint))
  (prompt "\nSelecione o 2º ponto do retângulo:")
  (setq p2 (getpoint))
  (prompt "\nSelecione o 3º ponto do retângulo:")
  (setq p3 (getpoint))
  (prompt "\nSelecione o 4º ponto do retângulo:")
  (setq p4 (getpoint))

  ;; 2. Escolha de prefixo
  (princ "\nSelecione o prefixo dos textos:")
  (princ "\n 1. E/N")
  (princ "\n 2. X/Y")
  (setq prefixOption (getint "\nDigite 1 ou 2: "))

  (cond
    ((= prefixOption 1)
      (setq prefixX "E=")
      (setq prefixY "N=")
    )
    ((= prefixOption 2)
      (setq prefixX "X=")
      (setq prefixY "Y=")
    )
    (T
      (prompt "\nOpção inválida. Encerrando.")
      (exit)
    )
  )

  ;; 3. Espaçamento
  (setq esp (getreal "\nInforme o espaçamento entre as linhas da malha: "))

  ;; 4. Ângulos dos textos
  (setq angX (getreal "\nInforme o ângulo dos textos E ou X (graus): "))
  (setq angY (getreal "\nInforme o ângulo dos textos N ou Y (graus): "))

  ;; 5. Layer
  (setq layerName "GeoInf_Malha")
  (if (not (tblsearch "LAYER" layerName))
    (command "-layer" "make" layerName "")
    (command "-layer" "set" layerName "")
  )

  ;; 6. Desenhar contorno
  (command "pline" p1 p2 p3 p4 "c")

  ;; 7. Calcular distâncias para divisões
  (setq distAB (distance p1 p2))
  (setq distDC (distance p4 p3))
  (setq distAD (distance p1 p4))
  (setq distBC (distance p2 p3))

  ;; Número de divisões para espaçamento exato
  (setq nDivX (fix (/ (min distAB distDC) esp)))
  (setq nDivY (fix (/ (min distAD distBC) esp)))

  ;; Evitar divisão por zero
  (if (< nDivX 1) (setq nDivX 1))
  (if (< nDivY 1) (setq nDivY 1))

  ;; 8. Gerar malha

  ;; Linhas "horizontais" (X / E)
  (setq divA (makeDivPts p1 p2 nDivX))
  (setq divD (makeDivPts p4 p3 nDivX))
  (setq i 0)
  (repeat (+ nDivX 1)
    (command "line" (nth i divA) (nth i divD) "")
    ;; Texto nas extremidades
    (setq txtPtX (nth i divA))
    (command "text" "j" "mc" txtPtX 2.5 (angX) (formatCoord prefixX (car txtPtX)))
    (setq txtPtX (nth i divD))
    (command "text" "j" "mc" txtPtX 2.5 (angX) (formatCoord prefixX (car txtPtX)))
    (setq i (1+ i))
  )

  ;; Linhas "verticais" (Y / N)
  (setq divB (makeDivPts p2 p3 nDivY))
  (setq divC (makeDivPts p1 p4 nDivY))
  (setq i 0)
  (repeat (+ nDivY 1)
    (command "line" (nth i divB) (nth i divC) "")
    ;; Texto nas extremidades
    (setq txtPtY (nth i divB))
    (command "text" "j" "mc" txtPtY 2.5 (angY) (formatCoord prefixY (cadr txtPtY)))
    (setq txtPtY (nth i divC))
    (command "text" "j" "mc" txtPtY 2.5 (angY) (formatCoord prefixY (cadr txtPtY)))
    (setq i (1+ i))
  )

  (princ "\nMalha criada com sucesso na layer GeoInf_Malha.")
  (princ)
)
