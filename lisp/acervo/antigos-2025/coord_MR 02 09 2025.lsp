(defun c:coord (/ pt1 norte este cota txt_norte txt_este txt_cota)
  (setq pt1 (getpoint "\nIndique o ponto base: "))
  (if pt1
    (progn
      ;; Garante que a camada exista e esteja ativa
      (command "_.layer" "_m" "SETA" "_c" "3" "" "")

      ;; Extrai coordenadas
      (setq este  (car pt1))
      (setq norte (cadr pt1))
      (setq cota  (if (caddr pt1) (caddr pt1) 0.0))

      ;; Formata texto
      (setq txt_norte (strcat "N = " (rtos norte 2 3) " m"))
      (setq txt_este  (strcat "E = " (rtos este  2 3) " m"))
      (setq txt_cota  (strcat "Z = " (rtos cota  2 3) " m"))

      ;; Usa MLEADER (mais compatível)
      (command "_.mleader" pt1 pause (strcat txt_norte "\\P" txt_este "\\P" txt_cota))

      ;; Volta para camada 0
      (command "_.layer" "_s" "0" "")
    )
    (princ "\nNenhum ponto selecionado.")
  )
  (princ)
)
