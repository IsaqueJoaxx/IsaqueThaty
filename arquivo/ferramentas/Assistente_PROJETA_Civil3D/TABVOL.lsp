;;; ======================================================================
;;; TABVOL.lsp - Tabela de volume de terraplenagem (PROJETA)
;;; Civil 3D 2026 e 2027 (AutoLISP + ActiveX).            Comando: TABVOL
;;;
;;; Monta no desenho a tabela "Cálculo de Volume por Comparação de Perfis":
;;;   Estaca | Área Corte | Área Aterro | Semi-Dis. | Vol.Corte | Vol.Aterro
;;; Cada estaca ocupa uma linha (áreas). Entre duas estacas vem uma linha com
;;; a semi-distância e os volumes do trecho. No fim, o resumo de áreas e volumes.
;;;
;;; Volume pelo método das áreas médias:
;;;   Vol = (área da estaca + área da estaca seguinte) x semi-distância
;;;   semi-distância = distância entre as duas estacas / 2
;;; Os totais somam os valores como aparecem na tabela (3 casas).
;;;
;;; De onde vêm as áreas:
;;;   Superficies - compara a superfície do terreno com a de projeto (ou de
;;;                 medição) em seções perpendiculares ao eixo. Corte = terreno
;;;                 acima da outra superfície; aterro = terreno abaixo. Só entra
;;;                 o trecho da seção onde existem as duas superfícies.
;;;   Arquivo     - CSV ou TXT com estaca;área corte;área aterro por linha
;;;                 (ex.: 427+10,000;46,574;0,000). Linhas que não seguem esse
;;;                 formato (cabeçalho, linhas de volume) são ignoradas.
;;; Estacas: por intervalo (20 m, 10 m...) mais estacas extras digitadas, ou
;;; as estacas das linhas de amostragem selecionadas.
;;;
;;; Estaca de 20 m: 427+10,000 = 427 x 20 + 10 = 8.550 m.
;;; Também grava um CSV com a mesma tabela, que abre direto no Excel em
;;; português. Só a tabela é inserida no desenho; um U desfaz.
;;; ======================================================================

(vl-load-com)

(setq tv:*estaca* 20.0) ; comprimento da estaca, em metros

;;; ------------------------------------------------------------ padrões lembrados
;;; Ficam no registro do Windows (getenv/setenv), só para esta rotina.

(defun tv:cfg (chave padrao / v)
  (setq v (getenv (strcat "PROJETA_TABVOL_" chave)))
  (if (and v (/= v "")) v padrao)
)

(defun tv:salva (chave valor)
  (setenv (strcat "PROJETA_TABVOL_" chave) valor)
  valor
)

;;; ------------------------------------------------------------ números e estacas

;; 1003.4 -> "1.003,400" (ponto de milhar, vírgula decimal).
;; Completa os zeros que o DIMZIN do desenho tiver suprimido.
(defun tv:num (x dec / s p int frac out)
  (setq s (rtos (abs x) 2 dec))
  (if (setq p (vl-string-search "." s))
    (setq int (substr s 1 p) frac (substr s (+ p 2)))
    (setq int s frac "")
  )
  (if (= int "") (setq int "0"))
  (while (< (strlen frac) dec) (setq frac (strcat frac "0")))
  (if (= dec 0) (setq frac nil))
  (setq out "")
  (while (> (strlen int) 3)
    (setq out (strcat "." (substr int (- (strlen int) 2)) out)
          int (substr int 1 (- (strlen int) 3)))
  )
  (setq out (strcat int out))
  (if frac (setq out (strcat out "," frac)))
  (if (and (< x 0) (/= (atof s) 0.0)) (strcat "-" out) out)
)

;; Arredonda para 3 casas, como aparece na tabela.
(defun tv:r3 (x) (atof (rtos x 2 3)))

;; 8550 -> "427+10,000"; 8500 -> "425"
(defun tv:est (s / n r)
  (setq s (tv:r3 s)
        n (fix (/ (+ s 0.0005) tv:*estaca*))
        r (tv:r3 (- s (* n tv:*estaca*))))
  (if (< r 0.0005)
    (itoa n)
    (strcat (itoa n) "+" (tv:num r 3))
  )
)

;; "427+10,000", "427+10.5" ou "427" -> metros. nil se não for estaca.
(defun tv:le-est (txt / p n r)
  (setq txt (vl-string-translate "," "." (vl-string-trim " \t\"" txt)))
  (if (setq p (vl-string-search "+" txt))
    (if (and (> p 0)
             (setq n (distof (substr txt 1 p) 2))
             (setq r (distof (substr txt (+ p 2)) 2)))
      (+ (* n tv:*estaca*) r)
    )
    (if (and (/= txt "") (setq n (distof txt 2)))
      (* n tv:*estaca*)
    )
  )
)

;; "1.003,400" ou "1003.4" -> 1003.4. nil se não for número.
(defun tv:le-num (txt)
  (setq txt (vl-string-trim " \t\"" txt))
  (if (vl-string-search "," txt)
    (setq txt (vl-string-translate "," "." (vl-list->string (vl-remove 46 (vl-string->list txt)))))
  )
  (if (/= txt "") (distof txt 2))
)

;; Ordena e tira repetidas (tolerância de meio milímetro).
(defun tv:ordena (lst / out)
  (foreach s (vl-sort lst '<)
    (if (or (null out) (> (abs (- s (car out))) 0.0005))
      (setq out (cons s out))
    )
  )
  (reverse out)
)

;; Separa um texto em pedaços (separador: um caractere da lista).
(defun tv:separa (txt seps / out atual c)
  (setq atual "")
  (foreach c (vl-string->list txt)
    (if (member c seps)
      (setq out (cons atual out) atual "")
      (setq atual (strcat atual (chr c)))
    )
  )
  (reverse (cons atual out))
)

;;; ------------------------------------------------------------ perguntas

;; chave nil: não lembra (o padrão é calculado a cada vez).
(defun tv:texto (msg chave padrao / r)
  (if chave (setq padrao (tv:cfg chave padrao)))
  (setq r (getstring T (strcat "\n" msg " <" padrao ">: ")))
  (setq r (if (= r "") padrao r))
  (if chave (tv:salva chave r) r)
)

(defun tv:real (msg chave padrao / r)
  (setq padrao (atof (tv:cfg chave (rtos padrao 2 4))))
  (initget 6)
  (setq r (getreal (strcat "\n" msg " <" (tv:num padrao 2) ">: ")))
  (if (null r) (setq r padrao))
  (tv:salva chave (rtos r 2 4))
  r
)

(defun tv:opcao (msg chave opcoes padrao / r)
  (setq padrao (tv:cfg chave padrao))
  (initget opcoes)
  (setq r (getkword (strcat "\n" msg " [" (vl-string-translate " " "/" opcoes) "] <" padrao ">: ")))
  (tv:salva chave (if r r padrao))
)

;; Pede um objeto de um dos tipos DXF. Enter devolve nil; clique no vazio pede de novo.
(defun tv:pegar (msg tipos / e ok)
  (setvar "ERRNO" 0)
  (while (not ok)
    (setq e (entsel msg))
    (cond
      ((and (null e) (= (getvar "ERRNO") 7))
       (princ "\nNada selecionado. Tente de novo.")
       (setvar "ERRNO" 0))
      ((null e) (setq ok T))
      ((member (cdr (assoc 0 (entget (car e)))) tipos) (setq ok T))
      (T (princ "\nEsse objeto não serve aqui. Tente de novo."))
    )
  )
  (if e (vlax-ename->vla-object (car e)))
)

;;; ------------------------------------------------------------ Civil 3D

;; Documento Civil 3D (ActiveX), para escolher superfície pelo nome.
(defun tv:civil-doc (/ ver app)
  (setq ver (cdr (assoc (substr (getvar "ACADVER") 1 4)
                        '(("26.0" . "13.9") ("25.1" . "13.8") ("25.0" . "13.7")))))
  (if ver
    (setq app (vl-catch-all-apply 'vla-getinterfaceobject
                (list (vlax-get-acad-object) (strcat "AeccXUiLand.AeccApplication." ver)))))
  (if (and app (not (vl-catch-all-error-p app)))
    (vlax-get app 'ActiveDocument)
  )
)

;; Superfície de volume (tipo 3 grade, 4 TIN)? Ela guarda diferenças de cota, não cotas.
(defun tv:volume-p (s)
  (member (vl-catch-all-apply 'vlax-get (list s 'Type)) '(3 4))
)

;; Lista os objetos numerados pelo nome e devolve o escolhido.
(defun tv:escolhe (lst oque / i n)
  (cond
    ((null lst) (princ (strcat "\nNenhum(a) " oque " encontrado(a) no desenho.")) nil)
    (T
     (setq i 0)
     (foreach s lst
       (princ (strcat "\n  " (itoa (setq i (1+ i))) " - " (vlax-get s 'Name))))
     (initget 6)
     (setq n (getint (strcat "\nNúmero do(a) " oque " (1 a " (itoa i) "): ")))
     (if (and n (<= n i)) (nth (1- n) lst))
    )
  )
)

(defun tv:superficie-por-nome (/ cdoc lst)
  (if (setq cdoc (tv:civil-doc))
    (vlax-for s (vlax-get cdoc 'Surfaces)
      (if (not (tv:volume-p s)) (setq lst (cons s lst))))
  )
  (tv:escolhe (reverse lst) "superfície")
)

;; Eixos com e sem sítio.
(defun tv:eixo-por-nome (/ cdoc lst)
  (if (setq cdoc (tv:civil-doc))
    (progn
      (vl-catch-all-apply
        '(lambda () (vlax-for a (vlax-get cdoc 'AlignmentsSiteless) (setq lst (cons a lst)))))
      (vl-catch-all-apply
        '(lambda () (vlax-for st (vlax-get cdoc 'Sites)
                      (vlax-for a (vlax-get st 'Alignments) (setq lst (cons a lst))))))
    )
  )
  (tv:escolhe (reverse lst) "eixo")
)

(defun tv:eixo (/ o)
  (setq o (tv:pegar "\nSelecione o eixo (alinhamento), ou Enter para escolher pelo nome: " '("AECC_ALIGNMENT")))
  (if o o (tv:eixo-por-nome))
)

(defun tv:superficie (msg / o)
  (while (and (setq o (tv:pegar (strcat "\n" msg " (clique nela, ou Enter para escolher pelo nome): ")
                                '("AECC_TIN_SURFACE" "AECC_GRID_SURFACE")))
              (tv:volume-p o))
    (princ "\nEssa é uma superfície de volume. Escolha a do terreno ou a de projeto/medição."))
  (if o o (tv:superficie-por-nome))
)

;; Ponto (x y) do eixo na estaca e afastamento dados. Esquerda negativa.
(defun tv:ponto (oAl sta off / x y r)
  (setq r (vl-catch-all-apply 'vlax-invoke-method (list oAl 'PointLocation sta off 'x 'y)))
  (if (and (not (vl-catch-all-error-p r)) x y) (list x y))
)

;; Cota da superfície no ponto, ou nil fora dela.
(defun tv:cota (oSup p / r)
  (setq r (vl-catch-all-apply 'vlax-invoke-method (list oSup 'FindElevationAtXY (car p) (cadr p))))
  (if (and (not (vl-catch-all-error-p r)) (numberp r)) r)
)

;; Áreas de corte e aterro na seção da estaca: (corte aterro amostras-válidas).
;; Integra terreno - outra superfície pelos trapézios, cortando onde troca de sinal.
(defun tv:secao (oAl oT oP sta esq dir passo / n w i o p zt zp d dl corte aterro f validas)
  (setq n (max 1 (fix (+ 0.999999 (/ (+ esq dir) passo))))
        w (/ (+ esq dir) n)
        corte 0.0 aterro 0.0 validas 0 i 0)
  (while (<= i n)
    (setq o (+ (- esq) (* i w))
          d nil)
    (if (and (setq p (tv:ponto oAl sta o))
             (setq zt (tv:cota oT p))
             (setq zp (tv:cota oP p)))
      (setq d (- zt zp) validas (1+ validas))
    )
    (if (and dl d)
      (if (or (and (>= dl 0.0) (>= d 0.0)) (and (<= dl 0.0) (<= d 0.0)))
        (setq corte  (+ corte  (* w 0.5 (+ (max dl 0.0) (max d 0.0))))
              aterro (+ aterro (* w 0.5 (+ (max (- dl) 0.0) (max (- d) 0.0)))))
        (progn
          (setq f (/ dl (- dl d))) ; fração do passo até o ponto de passagem
          (if (> dl 0.0)
            (setq corte (+ corte (* 0.5 dl w f))
                  aterro (+ aterro (* 0.5 (- d) w (- 1.0 f))))
            (setq aterro (+ aterro (* 0.5 (- dl) w f))
                  corte (+ corte (* 0.5 d w (- 1.0 f))))
          )
        )
      )
    )
    (setq dl d i (1+ i))
  )
  (list corte aterro validas)
)

;; Estacas a cada "passo" entre ini e fim, mais as extras dentro do trecho.
(defun tv:estacas-intervalo (ini fim passo extras / s lst)
  (setq lst (list ini fim)
        s (* (1+ (fix (/ (+ ini 0.0005) passo))) passo))
  (while (< s (- fim 0.0005))
    (setq lst (cons s lst) s (+ s passo))
  )
  (foreach e extras
    (if (and (> e (- ini 0.0005)) (< e (+ fim 0.0005))) (setq lst (cons e lst)))
  )
  (tv:ordena lst)
)

;; Estacas das linhas de amostragem selecionadas.
(defun tv:estacas-linhas (/ ss i s lst)
  (princ "\nSelecione as linhas de amostragem (pode ser uma janela sobre todas): ")
  (if (setq ss (ssget '((0 . "AECC_SAMPLE_LINE"))))
    (repeat (setq i (sslength ss))
      (setq s (vl-catch-all-apply 'vlax-get
                (list (vlax-ename->vla-object (ssname ss (setq i (1- i)))) 'Station)))
      (if (and (not (vl-catch-all-error-p s)) (numberp s)) (setq lst (cons s lst)))
    )
  )
  (tv:ordena lst)
)

;;; ------------------------------------------------------------ arquivo de áreas

(defun tv:campos (l)
  (cond
    ((vl-string-search ";" l) (tv:separa l '(59)))
    ((vl-string-search "\t" l) (tv:separa l '(9)))
    (T (tv:separa l '(44)))
  )
)

;; ((estaca corte aterro) ...) do arquivo; ignora o que não for estaca;número;número.
(defun tv:le-arquivo (arq / f l c s a b lst)
  (if (setq f (open arq "r"))
    (progn
      (while (setq l (read-line f))
        (setq c (tv:campos l))
        (if (and (>= (length c) 3)
                 (setq s (tv:le-est (nth 0 c)))
                 (setq a (tv:le-num (nth 1 c)))
                 (setq b (tv:le-num (nth 2 c))))
          (setq lst (cons (list s a b) lst))
        )
      )
      (close f)
    )
  )
  (vl-sort lst '(lambda (x y) (< (car x) (car y))))
)

;;; ------------------------------------------------------------ montagem

;; Linhas da tabela, 6 textos cada, e os totais.
;; dados: ((estaca corte aterro) ...) em ordem.  Devolve (linhas tAc tAa tVc tVa).
(defun tv:modelo (dados / linhas ant semi vc va tac taa tvc tva)
  (setq tac 0.0 taa 0.0 tvc 0.0 tva 0.0)
  (foreach e dados
    (setq e (list (car e) (tv:r3 (cadr e)) (tv:r3 (caddr e))))
    (if ant
      (setq semi (/ (- (car e) (car ant)) 2.0)
            vc (tv:r3 (* (+ (cadr ant) (cadr e)) semi))
            va (tv:r3 (* (+ (caddr ant) (caddr e)) semi))
            tvc (+ tvc vc) tva (+ tva va)
            linhas (cons (list "" "" "" (tv:num semi 3) (tv:num vc 3) (tv:num va 3)) linhas))
    )
    (setq tac (+ tac (cadr e)) taa (+ taa (caddr e))
          linhas (cons (list (tv:est (car e)) (tv:num (cadr e) 3) (tv:num (caddr e) 3) "" "" "") linhas)
          ant e)
  )
  (list (reverse linhas) tac taa tvc tva)
)

(defun tv:hoje (fmt) (menucmd (strcat "M=$(edtime,$(getvar,date)," fmt ")")))

;; Formata uma célula: texto, altura e alinhamento (4 esq., 5 centro, 6 dir.).
(defun tv:cel (tb r c txt alin h)
  (vla-SetText tb r c txt)
  (vla-SetCellTextHeight tb r c h)
  (vla-SetCellAlignment tb r c alin)
)

(defun tv:fundo (tb r c / cor)
  (setq cor (vla-GetCellBackgroundColor tb r c))
  (vla-SetRGB cor 217 217 217)
  (vla-SetCellBackgroundColorNone tb r c :vlax-false)
  (vla-SetCellBackgroundColor tb r c cor)
)

(defun tv:junta (tb r c1 c2) (vl-catch-all-apply 'vla-MergeCells (list tb r r c1 c2)))

;; Tabela vazia com nl linhas e as larguras dadas (em alturas de texto), sem
;; título nem cabeçalho automáticos do estilo.
(defun tv:nova (spc pt nl larg h / tb r c)
  (setq tb (vla-AddTable spc (vlax-3d-point pt) nl (length larg) (* 1.8 h) (* 9.0 h)))
  (vla-put-RegenerateTableSuppressed tb :vlax-true)
  (vl-catch-all-apply 'vla-put-TitleSuppressed (list tb :vlax-true))
  (vl-catch-all-apply 'vla-put-HeaderSuppressed (list tb :vlax-true))
  (setq c 0)
  (foreach w larg (vla-SetColumnWidth tb c (* w h)) (setq c (1+ c)))
  (setq r 0)
  (repeat nl
    (vl-catch-all-apply 'vla-UnmergeCells (list tb r r 0 (1- (length larg))))
    (vla-SetRowHeight tb r (* 1.8 h))
    (setq c 0)
    (repeat (length larg) (tv:cel tb r c "" 4 h) (setq c (1+ c)))
    (setq r (1+ r))
  )
  tb
)

;; Texto (MText) com o canto dado: 1 = superior esquerdo, 3 = superior direito.
(defun tv:mtexto (spc p largura txt h canto / m)
  (setq m (vla-AddMText spc (vlax-3d-point p) largura txt))
  (vla-put-Height m h)
  (vla-put-AttachmentPoint m canto)
  (vla-put-InsertionPoint m (vlax-3d-point p))
  m
)

;; Cabeçalho acima da tabela, como no relatório: responsável e data; volume,
;; projeto e local; uma linha embaixo. Devolve a cota (y) onde a tabela começa.
(defun tv:cabecalho (spc pt w h cab / x y z y2 ymin p1 p2 textos)
  (setq x (car pt) y (cadr pt) z (caddr pt) y2 (- y (* 1.7 h)))
  (setq textos
    (list
      (tv:mtexto spc (list x y z) (* 0.6 w) (nth 0 cab) h 1)
      (tv:mtexto spc (list (+ x w) y z) (* 0.38 w)
                 (strcat "Data: " (tv:hoje "DD/MO/YY") "   Hora: " (tv:hoje "HH:MM")) h 3)
      (tv:mtexto spc (list x y2 z) (* 0.5 w) (strcat "Volume: " (nth 1 cab)) h 1)
      (tv:mtexto spc (list (+ x w) y2 z) (* 0.48 w)
                 (strcat "Projeto: " (nth 3 cab) "   Local: " (nth 4 cab)) h 3)))
  ;; o texto mais baixo (pode ter quebrado em duas linhas) decide onde vai a linha
  (setq ymin y2)
  (foreach m textos
    (vla-GetBoundingBox m 'p1 'p2)
    (setq ymin (min ymin (cadr (vlax-safearray->list p1)))))
  (setq ymin (- ymin (* 0.5 h)))
  (vla-AddLine spc (vlax-3d-point (list x ymin z)) (vlax-3d-point (list (+ x w) ymin z)))
  (- ymin (* 1.5 h))
)

;; Insere cabeçalho, tabela principal e, logo abaixo, o resumo.
;; cab: (responsável volume comparação projeto local).  Devolve a tabela principal.
(defun tv:tabela (spc pt h cab mod / linhas larg tb rs r c y)
  (setq linhas (car mod)
        larg '(8.5 9.0 9.0 7.5 9.0 9.0)
        y (tv:cabecalho spc pt (* h (apply '+ larg)) h cab)
        tb (tv:nova spc (list (car pt) y (caddr pt)) (+ 2 (length linhas)) larg h))
  ;; título e nomes das colunas, com fundo cinza
  (tv:junta tb 0 0 5)
  (tv:cel tb 0 0 (strcat "TABELA DE VOLUMES — IFM: " (nth 2 cab)) 5 h)
  (tv:fundo tb 0 0)
  (setq c 0)
  (foreach t1 '("Estaca" "Área Corte" "Área Aterro" "Semi-Dis." "Vol.Corte" "Vol.Aterro")
    (tv:cel tb 1 c t1 5 h) (tv:fundo tb 1 c) (setq c (1+ c)))
  ;; linhas de estaca (áreas) e de trecho (semi-distância e volumes)
  (setq r 2)
  (foreach l linhas
    (setq c 0)
    (foreach t1 l (tv:cel tb r c t1 (if (= c 0) 4 6) h) (setq c (1+ c)))
    (setq r (1+ r))
  )
  (vla-put-RegenerateTableSuppressed tb :vlax-false)
  (vla-Update tb)
  ;; resumo: tabela própria, abaixo
  (setq rs (tv:nova spc
             (list (car pt) (- y (vla-get-Height tb) (* 2.0 h)) (caddr pt))
             3 '(17.5 16.5 18.0) h))
  (tv:cel rs 0 0 "" 4 h) (tv:cel rs 0 1 "Corte" 5 h) (tv:cel rs 0 2 "Aterro" 5 h)
  (tv:fundo rs 0 0) (tv:fundo rs 0 1) (tv:fundo rs 0 2)
  (tv:cel rs 1 0 "Áreas" 4 h)
  (tv:cel rs 1 1 (strcat (tv:num (nth 1 mod) 3) " m\\U+00B2") 6 h)
  (tv:cel rs 1 2 (strcat (tv:num (nth 2 mod) 3) " m\\U+00B2") 6 h)
  (tv:cel rs 2 0 "Volumes" 4 h)
  (tv:cel rs 2 1 (strcat (tv:num (nth 3 mod) 3) " m\\U+00B3") 6 h)
  (tv:cel rs 2 2 (strcat (tv:num (nth 4 mod) 3) " m\\U+00B3") 6 h)
  (vla-put-RegenerateTableSuppressed rs :vlax-false)
  (vla-Update rs)
  tb
)

;; Mesmo conteúdo em CSV (ponto e vírgula, vírgula decimal), para o Excel.
(defun tv:grava-csv (arq cab mod / f)
  (setq f (vl-catch-all-apply 'open (list arq "w" "utf8-bom")))
  (if (or (null f) (vl-catch-all-error-p f)) (setq f (open arq "w")))
  (if f
    (progn
      (write-line (strcat (nth 0 cab) ";;;;Data: " (tv:hoje "DD/MO/YY") ";Hora: " (tv:hoje "HH:MM")) f)
      (write-line (strcat "Volume: " (nth 1 cab) ";;;Projeto: " (nth 3 cab) ";;Local: " (nth 4 cab)) f)
      (write-line "" f)
      (write-line (strcat "TABELA DE VOLUMES — IFM: " (nth 2 cab)) f)
      (write-line "Estaca;Área Corte;Área Aterro;Semi-Dis.;Vol.Corte;Vol.Aterro" f)
      (foreach l (car mod)
        (write-line (strcat (nth 0 l) ";" (nth 1 l) ";" (nth 2 l) ";" (nth 3 l) ";" (nth 4 l) ";" (nth 5 l)) f))
      (write-line "" f)
      (write-line ";Corte;Aterro" f)
      (write-line (strcat "Áreas (m²);" (tv:num (nth 1 mod) 3) ";" (tv:num (nth 2 mod) 3)) f)
      (write-line (strcat "Volumes (m³);" (tv:num (nth 3 mod) 3) ";" (tv:num (nth 4 mod) 3)) f)
      (close f)
      arq
    )
  )
)

;;; ------------------------------------------------------------ comando

(defun c:TABVOL (/ *error* doc marca fonte arq dados oAl oT oP modo ini fim passo extras
                   esq dir amostra estacas sec semdado cab spc pt h mod tb csv csvbase csvn)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (defun *error* (msg)
    (if tb (vl-catch-all-apply 'vla-put-RegenerateTableSuppressed (list tb :vlax-false)))
    (if marca (vla-EndUndoMark doc))
    (if (and msg (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*EXIT*,*QUIT*")))
      (princ (strcat "\nTABVOL: " msg)))
    (princ)
  )

  (setq fonte (tv:opcao "Áreas de onde" "Fonte" "Superficies Arquivo" "Superficies"))
  (if (= fonte "Arquivo")
    ;; ---------- áreas de um arquivo
    (progn
      (setq arq (getfiled "Áreas por estaca (estaca;corte;aterro)" (tv:cfg "Arquivo" (getvar "DWGPREFIX")) "csv;txt" 0))
      (if (null arq) (exit))
      (tv:salva "Arquivo" arq)
      (setq dados (tv:le-arquivo arq))
      (princ (strcat "\n" (itoa (length dados)) " estaca(s) lida(s) do arquivo."))
    )
    ;; ---------- áreas comparando duas superfícies
    (progn
      (if (null (setq oAl (tv:eixo))) (exit))
      (if (null (setq oT (tv:superficie "Superfície do terreno"))) (exit))
      (if (null (setq oP (tv:superficie "Superfície de projeto ou de medição"))) (exit))
      (if (= (vla-get-Handle oT) (vla-get-Handle oP))
        (progn (princ "\nEscolha duas superfícies diferentes.") (exit)))
      (setq modo (tv:opcao "Estacas" "Modo" "Intervalo Linhas" "Intervalo"))
      (if (= modo "Linhas")
        (setq estacas (tv:estacas-linhas))
        (progn
          (setq ini (tv:le-est (tv:texto "Estaca inicial" nil (tv:est (vlax-get oAl 'StartingStation))))
                fim (tv:le-est (tv:texto "Estaca final" nil (tv:est (vlax-get oAl 'EndingStation))))
                passo (tv:real "Intervalo entre seções, em metros" "Passo" 20.0))
          (if (or (null ini) (null fim) (<= fim ini))
            (progn (princ "\nEstacas inválidas. Use o formato 425 ou 427+10,000.") (exit)))
          (setq ini (max ini (vlax-get oAl 'StartingStation))
                fim (min fim (vlax-get oAl 'EndingStation))
                extras (vl-remove nil (mapcar 'tv:le-est
                         (tv:separa (getstring T "\nEstacas extras, separadas por espaço (ex.: 437+2,178 442+2,593) <nenhuma>: ") '(32 59))))
                estacas (tv:estacas-intervalo ini fim passo extras))
        )
      )
      (if (< (length estacas) 2) (progn (princ "\nSão precisas pelo menos 2 estacas.") (exit)))
      (setq esq (tv:real "Largura da seção à esquerda do eixo (m)" "Esq" 20.0)
            dir (tv:real "Largura da seção à direita do eixo (m)" "Dir" 20.0)
            amostra (tv:real "Passo de leitura das cotas na seção (m)" "Amostra" 0.5))
      (princ (strcat "\nCalculando " (itoa (length estacas)) " seções..."))
      (foreach s estacas
        (setq sec (tv:secao oAl oT oP s esq dir amostra))
        (if (< (caddr sec) 2) (setq semdado (cons (tv:est s) semdado)))
        (setq dados (cons (list s (car sec) (cadr sec)) dados))
      )
      (setq dados (reverse dados))
      (if semdado
        (princ (strcat "\nAtenção: sem as duas superfícies na seção das estacas "
                       (apply 'strcat (mapcar '(lambda (x) (strcat x " ")) (reverse semdado)))
                       "(área zero).")))
    )
  )
  (if (< (length dados) 2) (progn (princ "\nSão precisas pelo menos 2 estacas.") (exit)))

  ;; ---------- cabeçalho
  (setq cab (list
              (tv:texto "Responsável" "Resp" "ISAQUE FERRAZ MOREIRA")
              (tv:texto "Nome do volume" nil
                        (strcat "VOLUME EST. " (tv:est (car (car dados))) " A " (tv:est (car (last dados)))))
              (tv:texto "Comparação" "Titulo" "Terreno x Projeto")
              (tv:texto "Projeto" "Projeto" "")
              (tv:texto "Local" "Local" "")))

  ;; ---------- tabela
  (setq mod (tv:modelo dados)
        spc (if (and (= (getvar "TILEMODE") 0) (= (getvar "CVPORT") 1))
              (vla-get-PaperSpace doc) (vla-get-ModelSpace doc))
        h (tv:real "Altura do texto da tabela" "Altura" 2.0))
  (if (null (setq pt (getpoint "\nCanto superior esquerdo do cabeçalho da tabela: "))) (exit))
  ;; tudo o que entra no desenho fica num grupo só: um U desfaz
  (vla-StartUndoMark doc)
  (setq marca T
        tb (tv:tabela spc (trans pt 1 0) h cab mod))
  (vla-EndUndoMark doc)
  (setq marca nil)

  ;; ---------- CSV ao lado do desenho (desenho ainda sem nome: em Documentos)
  (setq csv (strcat (if (= (getvar "DWGTITLED") 1)
                      (getvar "DWGPREFIX")
                      (strcat (vl-string-right-trim "\\" (getvar "MYDOCUMENTSPREFIX")) "\\"))
                    (vl-filename-base (getvar "DWGNAME")) " - "
                    (vl-string-right-trim ". " (vl-string-translate "/\\:*?\"<>|" "---------" (nth 1 cab))) ".csv")
        csvbase (vl-filename-base csv)
        csvn 1)
  (while (findfile csv)
    (setq csv (strcat (vl-filename-directory csv) "\\" csvbase " (" (itoa csvn) ").csv") csvn (1+ csvn)))
  (setq csv (tv:grava-csv csv cab mod))

  (princ (strcat "\nTABVOL: " (itoa (length dados)) " estacas. Corte "
                 (tv:num (nth 3 mod) 3) " m³, aterro " (tv:num (nth 4 mod) 3) " m³."
                 (if csv (strcat "\nCSV: " csv) "\nNão foi possível gravar o CSV.")))
  (princ)
)

(princ "\nTABVOL carregado. Digite TABVOL para montar a tabela de volume.")
(princ)
