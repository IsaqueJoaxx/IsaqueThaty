<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<!-- saved from url=(0049)http://www.autolisp.com.br/aaa1/aa7/lst_l2arq.lsp -->
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
;;grava os itens de uma lista de strings em um arquivo externo
(defun c:L2arq (arquivo lista_linha / arq2 erro_item)
  (setq arq2 (open arquivo "w"))
  (setq item1 (nth 0 lista_linha))
  (write-line item1 arq2)
  (setq lista_linha (cdr lista_linha))
  (close arq2)
  (setq arq2 (open arquivo "a"))
  (foreach erro_item lista_linha (write-line erro_item arq2))
  (close arq2)
)</PRE></BODY></HTML>
