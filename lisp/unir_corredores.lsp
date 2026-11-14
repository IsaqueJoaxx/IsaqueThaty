;=========================================================================
; UNIRCORREDORES - Une varios corredores do Civil 3D em um so corredor
;
; Como funciona:
;   1. Lista todos os corredores do desenho com numero
;   2. Voce informa quais numeros quer unir (ex: 1 2 3)
;   3. Cria um corredor destino (novo) OU usa o primeiro da lista
;   4. Copia todas as Baselines (Alignment + Profile) e as regioes
;      (assembly + estacao inicial/final) para o corredor destino
;   5. Opcional: apaga os corredores de origem
;
; Limitacoes conhecidas da API COM do Civil 3D via LISP:
;   - Frequencia de estacoes das regioes: usa o padrao da assembly
;     (revisar no Target/Region parameters apos a uniao)
;   - Baselines do tipo Offset (rail/trilhos) nao sao copiadas
;   - Parametros de targets (superficie alvo etc.) nao sao copiados
;
; Comandos: UNIRCORREDORES  (atalho: UNCOR)
; Testado em logica compativel com Civil 3D 2013+ (versao detectada)
;=========================================================================

(vl-load-com)

;--- Conecta no objeto AeccApplication tentando varias versoes -----------
(defun c3d:getAeccApp (/ versoes app achou)
  (setq versoes '("13.0" "13.3" "13.4" "13.5" "13.6" "12.0" "11.0" "10.5"
                  "10.4" "10.3" "10.0" "9.0"))
  (foreach v versoes
    (if (and (not achou)
             (not (vl-catch-all-error-p
                    (setq app
                      (vl-catch-all-apply
                        'vla-GetInterfaceObject
                        (list (vlax-get-acad-object)
                              (strcat "AeccXUiLand.AeccApplication." v)))))))
      (setq achou app)
    )
  )
  achou
)

;--- Pega o documento Civil ativo ----------------------------------------
(defun c3d:getDoc (aeccApp / doc)
  (setq doc (vlax-get aeccApp 'ActiveDocument))
  (if (= (type doc) 'VLA-OBJECT) doc nil)
)

;--- Lista os corredores do desenho --------------------------------------
(defun c3d:listaCorredores (doc / corrs n i lst)
  (setq corrs (vlax-get doc 'Corridors)
        n     (vla-get-Count corrs)
        i     0
        lst   '())
  (while (< i n)
    (setq lst (append lst (list (vlax-invoke corrs 'Item i))))
    (setq i (1+ i))
  )
  lst
)

;--- Converte "1 3 4" em lista de indices (1-based) ----------------------
(defun c3d:parseNumeros (str maxN / lst n val)
  (setq lst '())
  (foreach tok (c3d:split str " ")
    (if (> (strlen tok) 0)
      (progn
        (setq n (atoi tok))
        (if (and (>= n 1) (<= n maxN))
          (if (not (member n lst)) (setq lst (append lst (list n))))
        )
      )
    )
  )
  (vl-sort lst '<)
)

;--- Split simples de string ---------------------------------------------
(defun c3d:split (str sep / pos lst)
  (setq pos 1 lst '())
  (while (and pos (<= pos (strlen str)))
    (setq pos (vl-string-search sep pos))
    (if pos
      (progn
        (setq lst (append lst (list (substr str 1 pos))))
        (setq str (substr str (+ pos 2)))
        (setq pos 1)
      )
      (setq lst (append lst (list str)))
    )
  )
  lst
)

;--- Nome unico para baseline ---------------------------------------------
(defun c3d:nomeUnico (base existentes / nome i)
  (setq i 1 nome base)
  (while (member nome existentes)
    (setq i (1+ i))
    (setq nome (strcat base " (" (itoa i) ")"))
  )
  nome
)

;--- Copia uma baseline (alignment + profile + regioes) -------------------
(defun c3d:copiaBaseline (blOrig corrsDestino nomeExistentes / al pr novo
                           nome regions j reg asm s0 s1)
  (setq al (vlax-get blOrig 'Alignment)
        pr (vlax-get blOrig 'Profile))
  (if (and al pr)
    (progn
      (setq nome (c3d:nomeUnico (vlax-get blOrig 'Name) nomeExistentes))
      (setq novo (vlax-invoke (vlax-get corrsDestino 'Baselines)
                              'Add nome al pr))
      (if (and novo (not (vl-catch-all-error-p novo)))
        (progn
          ; Copia as regioes (assembly + estacoes)
          (setq regions (vlax-get blOrig 'BaselineRegions)
                j 0)
          (while (< j (vla-get-Count regions))
            (setq reg (vlax-invoke regions 'Item j))
            (setq asm (vlax-get reg 'Assembly)
                  s0  (vlax-get reg 'StartStation)
                  s1  (vlax-get reg 'EndStation))
            (if asm
              (vl-catch-all-apply
                '(lambda ()
                   (vlax-invoke (vlax-get novo 'BaselineRegions)
                                'Add asm s0 s1))))
            (setq j (1+ j))
          )
          T ; sucesso
        )
        nil
      )
    )
    nil
  )
)

;=========================================================================
; Comando principal
;=========================================================================
(defun c:UNIRCORREDORES (/ aeccApp doc corrs lista i escolha numeros
                           origens destino usarPrimeiro resposta
                           apagar nomeDestino copiadas falhas
                           baseNome bl k nomesUsados)
  (setq aeccApp (c3d:getAeccApp))
  (if (not aeccApp)
    (progn
      (princ "\nERRO: Nao foi possivel conectar ao Civil 3D.")
      (princ "\nRode este comando dentro de um desenho Civil 3D ativo.")
      (princ)
      (exit)
    )
  )
  (setq doc (c3d:getDoc aeccApp))
  (setq lista (c3d:listaCorredores doc))
  (if (< (length lista) 2)
    (progn
      (princ "\nERRO: E preciso ter pelo menos 2 corredores no desenho.")
      (princ)
      (exit)
    )
  )

  ;--- Mostra a lista ---
  (princ "\n==============================================")
  (princ "\nCORREDORES NO DESENHO:")
  (setq i 1)
  (foreach c lista
    (princ (strcat "\n  [" (itoa i) "] " (vlax-get c 'Name)))
    (setq i (1+ i))
  )
  (princ "\n==============================================")

  ;--- Escolha dos corredores a unir ---
  (setq escolha
    (getstring
      "\nDigite os numeros dos corredores a UNIR (ex: 1 2 3): "))
  (setq numeros (c3d:parseNumeros escolha (length lista)))
  (if (< (length numeros) 2)
    (progn
      (princ "\nSelecione pelo menos 2 corredores. Comando cancelado.")
      (princ)
      (exit)
    )
  )

  ;--- Coleta os objetos de origem ---
  (setq origens '())
  (foreach n numeros
    (setq origens (append origens (list (nth (1- n) lista))))
  )

  ;--- Destino: novo corredor ou o primeiro da lista ---
  (initget "Novo Primeiro")
  (setq resposta
    (getkword
      "\nDestino da uniao [Novo corredor/Usar o PRIMEIRO selecionado] <Novo>: "))

  (if (= resposta "Primeiro")
    (progn
      (setq destino (car origens)
            origens (cdr origens))
      (princ (strcat "\nDestino: " (vlax-get destino 'Name)
                     " (recebera as baselines dos demais)"))
    )
    (progn
      (setq nomeDestino
        (getstring T "\nNome do novo corredor unificado <Corredor Unificado>: "))
      (if (= nomeDestino "") (setq nomeDestino "Corredor Unificado"))
      (setq destino
        (vlax-invoke (vlax-get doc 'Corridors) 'Add nomeDestino))
      (if (vl-catch-all-error-p destino)
        (progn
          (princ "\nERRO ao criar o corredor destino. Cancelado.")
          (princ)
          (exit)
        )
      )
    )
  )

  ;--- Copia baselines de cada origem ---
  (setq copiadas 0 falhas 0)
  (foreach corr origens
    (setq baseNome (vlax-get corr 'Name))
    (princ (strcat "\nCopiando baselines de: " baseNome))
    (setq k 0)
    (while (< k (vla-get-Count (vlax-get corr 'Baselines)))
      (setq bl (vlax-invoke (vlax-get corr 'Baselines) 'Item k))
      (if (c3d:copiaBaseline bl destino
            (list (vlax-get destino 'Name)))
        (setq copiadas (1+ copiadas))
        (setq falhas (1+ falhas))
      )
      (setq k (1+ k))
    )
  )

  (princ (strcat "\n\nBaselines copiadas: " (itoa copiadas)))
  (if (> falhas 0)
    (princ (strcat "\nFalhas (baseline offset/sem perfil): " (itoa falhas))))

  ;--- Reconstroi o corredor destino ---
  (vl-catch-all-apply
    '(lambda () (vlax-invoke destino 'Rebuild)))

  ;--- Opcao de apagar as origens ---
  (initget "Sim Nao")
  (setq apagar
    (getkword
      "\nApagar os corredores de origem? [Sim/Nao] <Nao>: "))
  (if (= apagar "Sim")
    (foreach corr origens
      (vl-catch-all-apply
        '(lambda () (vlax-invoke (vlax-get doc 'Corridors)
                                 'Remove corr)))
      (princ (strcat "\nRemovido: " baseNome))
    )
  )

  (princ "\n\n>>> UNIAO CONCLUIDA <<<")
  (princ (strcat "\nCorredor final: " (vlax-get destino 'Name)))
  (princ "\n\nATENCAO: revise no destino:")
  (princ "\n  - Frequencia de estacoes das regioes (usa padrao da assembly)")
  (princ "\n  - Targets (superficies/alvos) de cada baseline")
  (princ "\n  - Codigos/feature lines se houver referencias externas")
  (princ)
)

;--- Atalho ---
(defun c:UNCOR () (c:UNIRCORREDORES))

(princ "\nLISP carregada. Comando: UNIRCORREDORES (ou UNCOR)")
(princ)
