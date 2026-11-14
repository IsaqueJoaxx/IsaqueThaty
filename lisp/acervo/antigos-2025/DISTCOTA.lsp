(defun c:DISTCOTA (/ pl elist eixopt eixoX eixoY eixoCota escalaV pto alvoX alvoY alvoZ dist deltaY cotaFinal ptoTexto texto continuar)
  (vl-load-com)

  (prompt "\nSelecione a polilinha 3D (3DPOLY) da seção:")
  (setq pl (car (entsel "\nClique na polilinha 3D: ")))

  (if (and pl
           (setq elist (entget pl))
           (or (= (cdr (assoc 0 elist)) "POLYLINE")
               (= (cdr (assoc 0 elist)) "LWPOLYLINE")
               (= (cdr (assoc 0 elist)) "3DPOLY")))
    (progn
      ;; Ponto do eixo
      (setq eixopt (getpoint "\nClique no ponto do eixo (centro da seção): "))
      
      (if eixopt
        (progn
          (setq eixoX (car eixopt))
          (setq eixoY (cadr eixopt))
          
          ;; Cota real do eixo
          (setq eixoCota (getreal "\nDigite a cota real do eixo: "))
          
          ;; Escala vertical
          (setq escalaV (getreal "\nDigite o fator de escala vertical (Exemplo: Para 1:100V digite 100): "))
          
          ;; Loop para múltiplos pontos
          (setq continuar T)
          (while continuar
            (setq pto (getpoint "\nClique no ponto desejado (ou ENTER para sair): "))
            
            (if pto
              (progn
                (setq alvoX (car pto))
                (setq alvoY (cadr pto))
                (setq alvoZ (caddr pto))
                
                ;; Distância lateral no X
                (setq dist (- alvoX eixoX))
                
                ;; Diferença gráfica em Y
                (setq deltaY (- alvoY eixoY))
                
                ;; Cálculo da cota
                (setq cotaFinal (+ eixoCota (* deltaY escalaV)))

                ;; Texto
                (setq texto (strcat
                              "Distância: " (rtos dist 2 3) "\n"
                              "Cota: " (rtos cotaFinal 2 3)
                            ))

                ;; Posição do texto (deslocado pra visualizar melhor)
                (setq ptoTexto (list (+ alvoX 2.0) (+ alvoY 2.0) alvoZ))

                ;; Criar MTEXT com altura menor
                (entmakex
                  (list
                    (cons 0 "MTEXT")
                    (cons 100 "AcDbEntity")
                    (cons 100 "AcDbMText")
                    (cons 10 ptoTexto)
                    (cons 40 1.25)  ;; Altura do texto reduzida
                    (cons 1 texto)
                  )
                )

                ;; Criar Leader
                (command "LEADER" pto ptoTexto "" "")
              )
              (setq continuar nil)  ;; Sai do loop se o usuário der ENTER
            )
          )
          (prompt "\nFinalizado. Todas as cotas foram calculadas com a escala.")
        )
        (prompt "\nErro: Você não clicou no ponto do eixo.")
      )
    )
    (prompt "\nErro: Entidade selecionada não é uma polilinha válida.")
  )
  (princ)
)
