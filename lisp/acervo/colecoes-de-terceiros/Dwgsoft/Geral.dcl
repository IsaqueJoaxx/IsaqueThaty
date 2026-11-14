geral : dialog {
label = "Geral             www.dwgsoft.com.br";
: row {
: list_box {
label = "Formato: M 1:1000";
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
list = "+Esc 1:1000\n1\n2\n2.5\n5\n7.5\n10\n12.5\n20\n25\n50\n75\n100\n125\n200\n250\n500\n750\n1000\n1200\n1250\n2000\n2500\n5000\n7500\n10000\n20000\n50000\n70000\n100000\n";
key = "+esc+";
edit_width = 13;
}
: popup_list {
popup_height = 1;
list = "PRJ-TER-CORTE\n0\nPRJ-TER-EIXO\nPRJ-TER-ESTACA\nPRJ-TER-ASSEMBLY\nPRJ-TER-PERFIL\nPRJ-SURFACE\nPRJ-TER-CORTE\nPRJ-TER-ALINHAMENTO-SEÇÃO\nPRJ-TER-PI\nPRJ-LEGENDA-PERFIL\nPRJ-CAIXA-PERFIL\nPRJ-TER-PERFIL-MALHA\nPRJ-TER-TERRENO\nTOP_CURVA-INTER\nTOP_CURVA-MESTRA\nTOP_TX 04\nTOP_POSTE_EM_CONCRETO\nTOP_MEIO-FIO\nTOP_PASSEIO\nTOP_MALHA-COORDENADAS\nTESTADA\nF-HZ-GUIA\nTOP_ARVORE\nEIXO\nHATCH_PASSEIO\nASSINATURA\nVP\nMALHA\nMALHA 01\nMALHA 02\nDefpoints";
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
