<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<!-- saved from url=(0048)http://www.autolisp.com.br/aaa1/aa7/misc_pgp.lsp -->
<HTML><HEAD>
<META http-equiv=Content-Type content="text/html; charset=windows-1252">
<META content="MSHTML 6.00.2900.2722" name=GENERATOR></HEAD>
<BODY><PRE>;|
#########################################################################
#  www.autolisp.com.br                                                  #
#  Copyright (c)2002 Marcos Mendes                                      #
#  URL: http://www.autolisp.com.br                                      #
#  Email: mmal@estadao.com.br ou marcos@autolisp.com.br                 #
#                                                                       #
# Esta rotina pode ser distribuida livremente!                          #
# Não remova este cabeçalho!                                            #
#                                                                       #
#########################################################################
|;
;; edita o arquivo acad.pgp
(defun c:pgp (comando abrevia / w arq)
  (setq var (getvar "cmdecho"))
  (setvar "cmdecho" 0)
  (setq linhapgp (strcat abrevia ",       " "*" comando))
  (setq w (findfile "acad.pgp"))
  (setq arq (open w "a"))
  (write-line linhapgp arq)
  (close arq)
  (command "_.re-init" "16")
  (setvar "cmdecho" var)
  (princ)
)
</PRE></BODY></HTML>
