;;; =====================================================================
;;;  LIMITE-CORREDOR.lsp                    Civil 3D          versao 1.0
;;;  ---------------------------------------------------------------
;;;  Gera uma POLILINHA FECHADA e PLANA contornando o corredor, para
;;;  ser usada em:
;;;     Propriedades do corredor > Limites > botao direito no nome da
;;;     superficie > "Adicionar de poligono" > selecionar a polilinha
;;;
;;;  COMANDOS
;;;  --------
;;;  CLC   Fluxo completo. O programa dispara o comando nativo do
;;;        Civil 3D "Create Polyline From Corridor" duas vezes: voce
;;;        clica na feature line de UMA borda do corredor e depois na
;;;        do OUTRO lado. Ele costura as duas em um poligono fechado.
;;;
;;;  CLCP  So a costura. Use quando as duas polilinhas das bordas ja
;;;        existem no desenho (ou quando voce quer usar duas
;;;        polilinhas/feature lines desenhadas a mao).
;;;
;;;  DICAS
;;;  -----
;;;  - Escolha o codigo mais externo que seja CONTINUO em toda a
;;;    extensao. Daylight costuma sumir nos trechos em nivel; nesse
;;;    caso use o bordo/pe de talude que existir do inicio ao fim.
;;;  - Se o Civil 3D reclamar de "poligono invalido" ao adicionar o
;;;    limite, ligue a folga: altere *clc-offset* abaixo para um valor
;;;    pequeno (ex.: 0.002 em metros / 2 em milimetros). Isso afasta o
;;;    contorno alguns milimetros para fora e resolve o caso classico
;;;    do limite cair exatamente em cima da aresta da triangulacao.
;;;  - O contorno e ESTATICO: se voce mexer no corredor, rode de novo.
;;;
;;;  INSTALACAO: APPLOAD > selecione este arquivo > Load
;;;              (Startup Suite > Add para carregar sempre)
;;; =====================================================================

(vl-load-com)

;;; ------------------------- CONFIGURACAO ------------------------------

(setq *clc-camada* "LIMITE-CORREDOR") ; camada da polilinha final
(setq *clc-cor*    30)                ; cor da camada (30 = laranja)
(setq *clc-lw*     50)                ; espessura de pena (0.50 mm)
(setq *clc-offset* 0.0)               ; folga p/ fora (0 = desligado)
(setq *clc-apagar* T)                 ; T = apaga as polilinhas de apoio
(setq *clc-tol*    0.001)             ; tolerancia p/ vertices repetidos

;;; ---------------------------------------------------------------------
;;;                        FUNCOES AUXILIARES
;;; ---------------------------------------------------------------------

(setq *clc-vars* '("CMDECHO" "OSMODE" "CLAYER" "PEDITACCEPT"))

(defun clc:gv (v / r)
  (setq r (vl-catch-all-apply 'getvar (list v)))
  (if (vl-catch-all-error-p r) nil r)
)

(defun clc:sv (v n)
  (if n (vl-catch-all-apply 'setvar (list v n)))
  nil
)

(defun clc:salva ()
  (setq *clc-old* (mapcar 'clc:gv *clc-vars*))
)

(defun clc:undo-fim ()
  (command "_.UNDO" "_E")
)

(defun clc:restaura ()
  (if *clc-undo*
    (progn
      (setq *clc-undo* nil)
      (vl-catch-all-apply 'clc:undo-fim '())
    )
  )
  (if *clc-old*
    (progn
      (mapcar 'clc:sv *clc-vars* *clc-old*)
      (setq *clc-old* nil)
    )
  )
  (princ)
)

;; cria / prepara a camada de destino
(defun clc:camada (nome cor lw / e d f)
  (if (tblsearch "LAYER" nome)
    (progn
      (setq e (tblobjname "LAYER" nome)
            d (entget e)
            f (cdr (assoc 70 d))
      )
      (if (= 1 (logand 1 f)) (setq f (- f 1)))
      (if (= 4 (logand 4 f)) (setq f (- f 4)))
      (setq d (subst (cons 70 f) (assoc 70 d) d))
      (if (minusp (cdr (assoc 62 d)))
        (setq d (subst (cons 62 (abs (cdr (assoc 62 d)))) (assoc 62 d) d))
      )
      (entmod d)
      (entupd e)
    )
    (entmake
      (list '(0 . "LAYER")
            '(100 . "AcDbSymbolTableRecord")
            '(100 . "AcDbLayerTableRecord")
            (cons 2 nome)
            '(70 . 0)
            (cons 62 cor)
            '(6 . "Continuous")
            (cons 370 lw)
      )
    )
  )
  nome
)

;; objetos criados depois da entidade "ult"
(defun clc:novos (ult / e lst)
  (setq lst '())
  (if ult
    (while (setq ult (entnext ult)) (setq lst (cons ult lst)))
    (progn
      (setq e (entnext))
      (while e (setq lst (cons e lst)) (setq e (entnext e)))
    )
  )
  (reverse lst)
)

;; comprimento de uma curva (0.0 se nao for curva)
(defun clc:comp (en / p l)
  (setq p (vl-catch-all-apply 'vlax-curve-getEndParam (list en)))
  (if (numberp p)
    (progn
      (setq l (vl-catch-all-apply 'vlax-curve-getDistAtParam (list en p)))
      (if (numberp l) l 0.0)
    )
    0.0
  )
)

;; entre varios objetos, devolve o mais comprido
(defun clc:maior (lst / best blen e l)
  (setq best nil blen -1.0)
  (foreach e lst
    (setq l (clc:comp e))
    (if (> l blen) (setq best e blen l))
  )
  best
)

;; vertices 2D de uma curva qualquer (poli 2D, poli 3D, linha, spline)
(defun clc:pontos (en / p1 p2 i pt lst fim)
  (setq p1 (vl-catch-all-apply 'vlax-curve-getStartParam (list en))
        p2 (vl-catch-all-apply 'vlax-curve-getEndParam (list en))
  )
  (if (and (numberp p1) (numberp p2) (> p2 p1))
    (progn
      (setq lst '() i p1)
      (while (<= i p2)
        (setq pt (vl-catch-all-apply 'vlax-curve-getPointAtParam (list en i)))
        (if (listp pt) (setq lst (cons (list (car pt) (cadr pt)) lst)))
        (setq i (1+ i))
      )
      ;; garante o ponto final (parametro pode nao ser inteiro)
      (setq fim (vl-catch-all-apply 'vlax-curve-getEndPoint (list en)))
      (if (listp fim)
        (progn
          (setq fim (list (car fim) (cadr fim)))
          (if (or (null lst) (> (distance fim (car lst)) *clc-tol*))
            (setq lst (cons fim lst))
          )
        )
      )
      (reverse lst)
    )
    nil
  )
)

;; tira vertices repetidos e o ultimo igual ao primeiro
(defun clc:limpa (pts / out)
  (if (null pts)
    nil
    (progn
      (setq out (list (car pts)))
      (foreach p (cdr pts)
        (if (> (distance p (car out)) *clc-tol*)
          (setq out (cons p out))
        )
      )
      (setq out (reverse out))
      (if (and (> (length out) 2)
               (<= (distance (car out) (last out)) *clc-tol*))
        (setq out (reverse (cdr (reverse out))))
      )
      out
    )
  )
)

;; cria a LWPOLYLINE fechada e plana
(defun clc:mkpoly (pts lay / d)
  (setq d (list '(0 . "LWPOLYLINE")
                '(100 . "AcDbEntity")
                (cons 8 lay)
                '(100 . "AcDbPolyline")
                (cons 90 (length pts))
                '(70 . 1)
                '(38 . 0.0)
          )
  )
  (foreach p pts (setq d (append d (list (cons 10 p)))))
  (if (entmake d) (entlast) nil)
)

;; costura duas bordas em um anel fechado
(defun clc:monta (e1 e2 / p1 p2 d1 d2)
  (setq p1 (clc:pontos e1)
        p2 (clc:pontos e2)
  )
  (cond
    ((or (null p1) (null p2) (< (length p1) 2) (< (length p2) 2))
     (princ "\n!! Nao consegui ler os vertices de uma das bordas.")
     nil
    )
    (T
     ;; descobre como parear as pontas (bordas podem estar invertidas)
     (setq d1 (+ (distance (car p1) (car p2))
                 (distance (last p1) (last p2)))
           d2 (+ (distance (car p1) (last p2))
                 (distance (last p1) (car p2)))
     )
     (if (and (< (distance (car p1) (car p2)) *clc-tol*)
              (< (distance (last p1) (last p2)) *clc-tol*))
       (princ "\n!! Aviso: as duas bordas parecem ser a MESMA linha.")
     )
     (clc:limpa (append p1 (if (< d1 d2) (reverse p2) p2)))
    )
  )
)

;; afasta o contorno para fora (opcional) - o ponto de lado e o canto
;; superior direito da caixa envolvente, que esta sempre FORA do poligono
(defun clc:folga (en dist / x mx my pt ult novo)
  (setq mx nil my nil)
  (foreach x (entget en)
    (if (= 10 (car x))
      (progn
        (if (or (null mx) (> (cadr x) mx)) (setq mx (cadr x)))
        (if (or (null my) (> (caddr x) my)) (setq my (caddr x)))
      )
    )
  )
  (if (and mx my)
    (progn
      (setq pt  (list (+ mx (* 10.0 dist) 1.0) (+ my (* 10.0 dist) 1.0))
            ult (entlast)
      )
      (command "_.OFFSET" dist en pt "")
      (setq novo (clc:maior (clc:novos ult)))
      (if novo
        (progn (entdel en) novo)
        en
      )
    )
    en
  )
)

;; dispara o comando nativo de exportar feature line -> polilinha
(defun clc:export (msg / nome ult ok k)
  (setq ult (entlast) ok nil)
  (princ (strcat "\n>> " msg))
  (foreach nome '("_CreatePolylineFromCorridor"
                  "_CreatePolylineFromCorrdor"
                  "_AeccCreatePolylineFromCorridor")
    (if (not ok)
      (progn
        (command nome)
        (if (> (getvar "CMDACTIVE") 0)
          (progn
            (setq ok T k 0)
            (while (and (> (getvar "CMDACTIVE") 0) (< k 40))
              (command pause)
              (setq k (1+ k))
            )
          )
        )
      )
    )
  )
  (if ok
    (clc:novos ult)
    (progn
      (princ "\n!! O comando 'Create Polyline From Corridor' nao foi")
      (princ "\n   encontrado nesta versao. Extraia as bordas pela faixa")
      (princ "\n   de opcoes (Inicio > Criar projeto > Criar polilinha a")
      (princ "\n   partir do corredor) e depois use o comando CLCP.")
      nil
    )
  )
)

;; parte comum: recebe as duas bordas e finaliza o contorno
(defun clc:finaliza (e1 e2 apaga / pts pl d)
  (setq pts (clc:monta e1 e2))
  (if (and pts (> (length pts) 2))
    (progn
      (setq pl (clc:mkpoly pts *clc-camada*))
      (if pl
        (progn
          (if (and *clc-offset* (> *clc-offset* 0.0))
            (setq pl (clc:folga pl *clc-offset*))
          )
          ;; garante a camada correta mesmo depois do OFFSET
          (setq d (entget pl))
          (if (/= (strcase (cdr (assoc 8 d))) (strcase *clc-camada*))
            (entmod (subst (cons 8 *clc-camada*) (assoc 8 d) d))
          )
          (if apaga
            (progn
              (if (and e1 (entget e1)) (entdel e1))
              (if (and e2 (entget e2)) (entdel e2))
            )
          )
          (sssetfirst nil (ssadd pl))
          (princ (strcat "\n>> Contorno fechado criado na camada \""
                         *clc-camada* "\" com "
                         (itoa (length pts)) " vertices."))
          (princ "\n   Agora: Propriedades do corredor > Limites >")
          (princ "\n   botao direito no NOME DA SUPERFICIE >")
          (princ "\n   \"Adicionar de poligono\" > clique nesta polilinha")
          (princ "\n   > Tipo de uso: Limite externo > OK > Reconstruir.")
          pl
        )
        (progn (princ "\n!! Falha ao criar a polilinha.") nil)
      )
    )
    (progn (princ "\n!! Nao foi possivel montar o contorno.") nil)
  )
)

;;; ---------------------------------------------------------------------
;;;      CLC - extrai as duas bordas do corredor e monta o contorno
;;; ---------------------------------------------------------------------

(defun c:CLC (/ *error* n1 n2 e1 e2)

  (defun *error* (msg / k)
    (setq k 0)
    (while (and (> (getvar "CMDACTIVE") 0) (< k 5))
      (command)
      (setq k (1+ k))
    )
    (clc:restaura)
    (if (and msg
             (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*QUIT*,*ESCAPE*")))
      (princ (strcat "\n** Erro: " msg " **"))
    )
    (princ)
  )

  (clc:salva)
  (clc:camada *clc-camada* *clc-cor* *clc-lw*)
  (setvar "CMDECHO" 0)

  (if (= 1 (logand 1 (getvar "UNDOCTL")))
    (progn
      (command "_.UNDO" "_BE")
      (setq *clc-undo* T)
    )
  )

  (princ "\n--- CLC: contorno do corredor -----------------------------")
  (setq n1 (clc:export "Clique na feature line de UM LADO do corredor:"))
  (if n1
    (progn
      (setq n2 (clc:export "Agora clique na feature line do OUTRO LADO:"))
      (setq e1 (clc:maior n1)
            e2 (clc:maior n2)
      )
      (if (and e1 e2)
        (clc:finaliza e1 e2 *clc-apagar*)
        (princ "\n!! Nenhuma polilinha foi gerada pelo Civil 3D.")
      )
    )
  )

  (clc:restaura)
  (princ)
)

;;; ---------------------------------------------------------------------
;;;      CLCP - monta o contorno a partir de 2 polilinhas existentes
;;; ---------------------------------------------------------------------

(defun c:CLCP (/ *error* s1 s2 e1 e2)

  (defun *error* (msg / k)
    (setq k 0)
    (while (and (> (getvar "CMDACTIVE") 0) (< k 5))
      (command)
      (setq k (1+ k))
    )
    (clc:restaura)
    (if (and msg
             (not (wcmatch (strcase msg) "*BREAK*,*CANCEL*,*QUIT*,*ESCAPE*")))
      (princ (strcat "\n** Erro: " msg " **"))
    )
    (princ)
  )

  (clc:salva)
  (clc:camada *clc-camada* *clc-cor* *clc-lw*)
  (setvar "CMDECHO" 0)

  (if (= 1 (logand 1 (getvar "UNDOCTL")))
    (progn
      (command "_.UNDO" "_BE")
      (setq *clc-undo* T)
    )
  )

  (princ "\n--- CLCP: costurar duas bordas ----------------------------")
  (setq s1 (entsel "\n>> Selecione a polilinha de UM LADO: "))
  (if s1
    (progn
      (setq s2 (entsel "\n>> Selecione a polilinha do OUTRO LADO: "))
      (if s2
        (progn
          (setq e1 (car s1)
                e2 (car s2)
          )
          (if (equal e1 e2)
            (princ "\n!! Voce selecionou a mesma polilinha duas vezes.")
            (clc:finaliza e1 e2 nil)
          )
        )
      )
    )
  )

  (clc:restaura)
  (princ)
)

(princ "\n---------------------------------------------------------")
(princ "\n LIMITE-CORREDOR carregado.")
(princ "\n  CLC   = extrai as 2 bordas do corredor e fecha o contorno")
(princ "\n  CLCP  = fecha o contorno a partir de 2 polilinhas prontas")
(princ "\n---------------------------------------------------------")
(princ)
