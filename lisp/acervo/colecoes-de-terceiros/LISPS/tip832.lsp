; TIP #832:  BACKGRND.LSP (c)1993, Mark H. Miller

; Creates solid blue background for slides
; by Mark H. Miller, released to public domain.

(command ".solid" '(-1 -1) '(22 -1) '(-1 13) '(22 13) "")
(setq ENTCOL (assoc  62 (setq ELIST (entget (entlast)))))
(cond (entcol (entmod (subst '(62 . 5) entcol ELIST)))
(t (entmod (cons '(62 . 5) ELIST))))
;end backgrnd.lsp
