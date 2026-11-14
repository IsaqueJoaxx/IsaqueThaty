;;; ============================================================
;;; IMPSECOES.lsp
;;; Impressao em lote de secoes (folhas A4) no Model Space
;;;
;;; COMO USAR:
;;;   1. Carregue com APPLOAD (ou arraste o arquivo para o AutoCAD)
;;;   2. Digite IMPSECOES
;;;   3. Clique na MOLDURA (borda) de UMA secao de exemplo
;;;   4. A rotina localiza todas as molduras iguais (mesma camada
;;;      e mesmo tamanho), ordena por linhas (de cima para baixo,
;;;      esquerda para direita) e plota cada uma em A4
;;;
;;; DICA: faca antes UM plot manual da primeira secao para testar
;;; a impressora, o papel e o CTB. Depois informe os mesmos nomes
;;; quando a rotina pedir.
;;; ============================================================

(vl-load-com)

;; ---------- funcao auxiliar: bounding box de uma entidade ----------
(defun ims:bbox (ent / obj ll ur)
  (setq obj (vlax-ename->vla-object ent))
  (vla-getboundingbox obj 'll 'ur)
  (list (vlax-safearray->list ll) (vlax-safearray->list ur))
)

;; ---------- funcao auxiliar: formata ponto como "x,y" ----------
(defun ims:pt2str (p)
  (strcat (rtos (car p) 2 4) "," (rtos (cadr p) 2 4))
)

;; ---------- comando principal ----------
(defun c:IMPSECOES ( / *error* oldcmd oldbg sel ent laj tipo bb largura altura
                       tol ss n e b w h molduras linhas lin restante item
                       dispositivo papel ctb orient resp cont total ll ur
                       arqpdf prefixo dwgdir plotfile i)

  ;; ----- tratamento de erro: restaura variaveis -----
  (defun *error* (msg)
    (if oldcmd (setvar "CMDECHO" oldcmd))
    (if oldbg  (setvar "BACKGROUNDPLOT" oldbg))
    (if (and msg (/= msg "Function cancelled"))
      (princ (strcat "\nErro: " msg)))
    (princ)
  )

  (setq oldcmd (getvar "CMDECHO")
        oldbg  (getvar "BACKGROUNDPLOT"))
  (setvar "CMDECHO" 0)
  (setvar "BACKGROUNDPLOT" 0)   ; plota em primeiro plano, em sequencia

  ;; ================= 1. MOLDURA DE EXEMPLO =================
  (setq sel (entsel "\n>> Selecione a MOLDURA (borda) de UMA secao de exemplo: "))
  (if (null sel) (progn (*error* nil) (exit)))
  (setq ent  (car sel)
        laj  (cdr (assoc 8 (entget ent)))       ; camada da moldura
        tipo (cdr (assoc 0 (entget ent)))       ; tipo (LWPOLYLINE, INSERT...)
        bb   (ims:bbox ent)
        largura (- (car  (cadr bb)) (car  (car bb)))
        altura  (- (cadr (cadr bb)) (cadr (car bb))))

  (princ (strcat "\n   Camada: " laj "  |  Tipo: " tipo
                 "  |  Tamanho: " (rtos largura 2 1) " x " (rtos altura 2 1)))

  ;; tolerancia de tamanho para considerar "moldura igual" (5%)
  (setq tol 0.05)

  ;; ================= 2. LOCALIZA TODAS AS MOLDURAS =================
  (setq ss (ssget "_X" (list (cons 0 tipo) (cons 8 laj))))
  (if (null ss) (progn (princ "\nNenhuma moldura encontrada.") (*error* nil) (exit)))

  (setq molduras '() n 0)
  (repeat (sslength ss)
    (setq e (ssname ss n)
          b (ims:bbox e)
          w (- (car  (cadr b)) (car  (car b)))
          h (- (cadr (cadr b)) (cadr (car b))))
    ;; aceita somente molduras com o mesmo tamanho da amostra (+-5%)
    (if (and (< (abs (- w largura)) (* largura tol))
             (< (abs (- h altura))  (* altura  tol)))
      (setq molduras (cons b molduras)))
    (setq n (1+ n))
  )
  (setq total (length molduras))
  (princ (strcat "\n   Molduras localizadas: " (itoa total)))
  (if (= total 0) (progn (*error* nil) (exit)))

  ;; ================= 3. ORDENA: LINHAS (Y desc) E COLUNAS (X asc) =================
  ;; agrupa por linha usando metade da altura como tolerancia
  (setq molduras (vl-sort molduras
                   '(lambda (a b) (> (cadr (car a)) (cadr (car b))))))
  (setq linhas '())
  (while molduras
    (setq item (car molduras)
          lin (vl-remove-if-not
                '(lambda (m) (< (abs (- (cadr (car m)) (cadr (car item))))
                                (* altura 0.5)))
                molduras)
          restante (vl-remove-if
                '(lambda (m) (< (abs (- (cadr (car m)) (cadr (car item))))
                                (* altura 0.5)))
                molduras)
          lin (vl-sort lin '(lambda (a b) (< (car (car a)) (car (car b)))))
          linhas (append linhas lin)
          molduras restante)
  )
  (setq molduras linhas)

  ;; ================= 4. CONFIGURACAO DE IMPRESSAO =================
  (setq dispositivo (getstring T
    "\n>> Nome da impressora/plotter (igual ao dialogo PLOT, ex: HP LaserJet ou DWG To PDF.pc3): "))
  (setq papel (getstring T
    "\n>> Nome do papel <A4>: "))
  (if (= papel "") (setq papel "A4"))
  (setq ctb (getstring T
    "\n>> Tabela de plotagem CTB (ex: monochrome.ctb | . = nenhuma) <monochrome.ctb>: "))
  (if (= ctb "") (setq ctb "monochrome.ctb"))

  ;; orientacao automatica pela proporcao da moldura
  (setq orient (if (> largura altura) "_L" "_P"))
  (princ (strcat "\n   Orientacao detectada: "
                 (if (= orient "_L") "Paisagem" "Retrato")))

  ;; plot em arquivo (PDF)?
  (initget "Sim Nao")
  (setq resp (getkword "\n>> O dispositivo gera arquivo PDF? [Sim/Nao] <Nao>: "))
  (setq arqpdf (= resp "Sim"))
  (if arqpdf
    (progn
      (setq dwgdir (getvar "DWGPREFIX"))
      (setq prefixo (getstring T
        (strcat "\n>> Prefixo dos arquivos PDF <Secao_>: ")))
      (if (= prefixo "") (setq prefixo "Secao_"))
    )
  )

  ;; confirmacao final
  (initget "Sim Nao")
  (setq resp (getkword (strcat "\n>> Plotar " (itoa total)
                               " folha(s) agora? [Sim/Nao] <Sim>: ")))
  (if (= resp "Nao") (progn (princ "\nCancelado.") (*error* nil) (exit)))

  ;; ================= 5. LOOP DE PLOTAGEM =================
  (setq cont 0 i 0)
  (foreach b molduras
    (setq i (1+ i)
          ll (car b)
          ur (cadr b))
    (princ (strcat "\n   Plotando folha " (itoa i) " de " (itoa total) "..."))

    (if arqpdf
      ;; ----- plot gerando arquivo (PDF) -----
      (progn
        (setq plotfile (strcat dwgdir prefixo
                               (if (< i 10) "00" (if (< i 100) "0" ""))
                               (itoa i)))
        (command "-PLOT"
          "_Y"                    ; configuracao detalhada? sim
          ""                      ; layout atual (Model)
          dispositivo             ; impressora/plotter
          papel                   ; papel A4
          "_M"                    ; milimetros
          orient                  ; retrato/paisagem
          "_N"                    ; plotar de cabeca para baixo? nao
          "_W"                    ; area: janela
          (ims:pt2str ll)         ; canto inferior esquerdo
          (ims:pt2str ur)         ; canto superior direito
          "_F"                    ; escala: ajustar (Fit)
          "_C"                    ; offset: centralizado
          "_Y"                    ; usar estilos de plotagem? sim
          ctb                     ; tabela CTB
          "_Y"                    ; plotar com espessuras? sim
          "_A"                    ; shade plot: como exibido
          plotfile                ; nome do arquivo de saida
          "_N"                    ; salvar alteracoes no page setup? nao
          "_Y")                   ; prosseguir com a plotagem? sim
      )
      ;; ----- plot direto na impressora -----
      (command "-PLOT"
        "_Y" "" dispositivo papel "_M" orient "_N"
        "_W" (ims:pt2str ll) (ims:pt2str ur)
        "_F" "_C" "_Y" ctb "_Y" "_A"
        "_N"                      ; gravar em arquivo? nao
        "_N"                      ; salvar page setup? nao
        "_Y")                     ; prosseguir
    )
    (setq cont (1+ cont))
  )

  (princ (strcat "\n\n== CONCLUIDO: " (itoa cont) " folha(s) enviada(s) para "
                 dispositivo " =="))
  (*error* nil)
  (princ)
)

(princ "\nRotina carregada. Digite IMPSECOES para iniciar a plotagem em lote das secoes.")
(princ)
