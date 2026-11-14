;----------------------------------------------------------------------
;J.A.Nunes
;11-02-2000
;www.janunes.homepage.com
;calcula a área de um poligono fechado e escreve a area calculada
;observação: o texto será escrito com altura do ultimo texto escrito.
;----------------------------------------------------------------------
(setvar "cmdecho" 0)
(defun c:as()
(prompt"\nwww.janunes.homepage.com")
(setq p1(getpoint"\nLocalize o centro do poligono fechado a ser calculado:"))
(command"_bpoly"p1"")(setq ent(entlast))
(command"_area""_o"ent)
(entdel ent)
;                                                       * este numero 3 controla o numero de casa depois do ponto
(command"_text""_m"p1""""(strcat (rtos (getvar"area") 2 2) "m2"))
(princ)
)
