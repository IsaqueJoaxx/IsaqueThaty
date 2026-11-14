hidrologia : dialog {
label = "Hidrologia        www.dwgsoft.com.br";
: row {
: list_box {
label = "Formato: M 1:1000";
width = 20;
 height = 7;
key = "hidr_op";
allow_accept = true;
}
: image {
key = "hidr_img";
width = 25;
color = 19;
}
}
spacer;
: row {
: popup_list {
popup_height = 1;
list = "+Esc 1:1000\n1\n2\n2.5\n5\n7.5\n10\n12.5\n20\n25\n50\n75\n100\n125\n200\n250\n500\n750\n1000\n1200\n1250\n2000\n2500\n5000\n7500\n10000\n20000\n50000\n70000\n100000\n";
key = "+esc+";
edit_width = 13;
}
: popup_list {
popup_height = 1;
list = "SIMBOLOGIA\n0\nTEXTO\nEIXO ESTUDO\nGRADE\nF-texto\n1 SPF DFADF\nPROJ.ESTACAS\nVIEWPORT\nPROJ.PNOTÁVEIS_TEXTO\nPROJ.F-NOTAVEL-TANGENTE\nPROJ.F-NOTAVEL-CURVA\nPROJ.ESTACA_MESTRA\nPROJ.GRADEINTERMEDIARIA\nPROJ.ELEVACAO_MESTRA\nPROJ.PI\nPROJ.ESTACA_MESTRA_MARCACAO\nPROJ.PERFIL_TERRENO_NATURAL\nPROJ.GREIDE\nPROJ.POLIGONAL_EXPLORACAO_VERTICAL\nPROJ.FORMATO\nPROJ_ESCALA_GRAFICA_VERTICAL\nPROJ.R140\nPROJ.R100\nSIMBOLOGIA";
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
