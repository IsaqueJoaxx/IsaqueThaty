geral : dialog {
label = "Geral             www.dwgsoft.com.br";
: row {
: list_box {
label = "Formato: M 1:72";
width = 20;
 height = 7;
key = "gera_op";
allow_accept = true;
}
: image {
key = "gera_img";
width = 25;
color = 19;
}
}
spacer;
: row {
: popup_list {
popup_height = 1;
list = "+Esc 1:72\n1\n2\n2.5\n5\n7.5\n10\n12.5\n20\n25\n50\n75\n100\n125\n200\n250\n500\n750\n1000\n1200\n1250\n2000\n2500\n5000\n7500\n10000\n20000\n50000\n70000\n100000\n";
key = "+esc+";
edit_width = 13;
}
: popup_list {
popup_height = 1;
list = "1\n0\nInter5\nMestra\nInter\nDEFPOINTS\n1\nESTRADA PRINCIPAL\nML3\nAL2\nAL5\nAC2\nF-talude\nALOJA_ARRANQUE\nLIMITE_ALOJA_VALE\nPORTARIA_ALOJAS\nLIMITE_ACESSOS_ALOJAS\nFRR120\nFRR080\nFRR175\nFRL1\nFRL3\nFRL2\nFRL4\nFRR100\nFRR140\nFRL8\nLOGO VALE\nLOGO SEI CONTORNO\nLOGO SEI SOLIDO\nMALHA\nTEXTO-MALHA\n1-etapa 2\nPROJ.NORTE\nML5\nML2\nSIMBOLOGIA\nBR80-3\nDESENHO_PROJETO BORDO_04\nAloja_Vale\nACESSOS_ALOJAS\nBDR\nBBO\nBFX\nVLC\nBGR100\nBCO\nBTAL\nVCAAR\nBGMET\nPROJ-EIXO\nBGMEL\nTALUDES\nReambulação\n04\nML4\nVCABO\nBGR80\nBAT\nVCAMATA\nEIXO\nTEXTO\nLIMITE_ACESSOS\nBL2\nBR175\nBTP1\nBL4\nDESENHO_PROJETO CANAL_01\nEIXO_LINHA CENTR0_00\n_TEXTO03\nCX_PASSAGEM\nDIREÇÃO DA ÁGUA\nDESENHO_PROJETO VALETA DE CORTE_01\ncanal_pe\nESCADA_HIDRAULICA\nEIXO-ACESSOS\n02\n2\nDR-VISTA01\nDR-TALUDE-PROJETADO\nCI-SECAO\nCORTE\nDRENAGEM NATURAL COMPLETA\nFRR80\nlegenda\nTEXTOX\nSARJETA\ndsuperficial";
key = "popuplst";
edit_width = 14;
}
: popup_list {
popup_height = 1;
list = "web Serviços\nAmarração\nCurva de Nível\nPerfil/Seções\nPontos/Tabela\nVolume Geológico\nSuporte\nSugestão\nHome page\n";
key = "online";
edit_width = 12;
}
}
      : text {
label = " Desenho                   Current Layer";
}
: row {
ok_cancel;
: button {
label = "Ajuda";
key = "ajudaeletro";
}}
}
