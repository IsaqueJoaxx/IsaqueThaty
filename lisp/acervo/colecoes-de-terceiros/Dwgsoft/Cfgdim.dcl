cfgdim : dialog {
label = "Configuração:";
: boxed_column {
label = "Dimension:";
: text {
label = "Estilo: Standard";
}
: text {
label = "Fonte: Romans.shx";
}
: row {
: edit_box {
label = "h texto:";
edit_width = 3;
key = "htxt";
}
: edit_box {
label = "Ângulo:";
edit_width = 3;
key = "agtxt";
}
}
: row {
: image_button {
key = "cortit3";
horizontal_margin = none;
width = 0.1;
height = 1.2;
color        = 3;
}
: text {
label = "Cor do texto da cota";
}
}
: row {
: image_button {
key = "clinha";
horizontal_margin = none;
width = 0.1;
height = 1.2;
color        = 7;
}
: text {
label = "Cor da linha da cota";
}
}
: edit_box {
label = "Layer da cota:";
edit_width = 10;
key = "lycota";
}
spacer;
spacer;
: radio_row {
: radio_button {
label = "Arrow";
key = "tarrow";
value = "0";
}
: radio_button {
label = "Donut";
key = "tdonut";
}
: radio_button {
label = "Tick";
key = "ttick";
}
}
: row {
: edit_box {
label = "Tamanho:";
edit_width = 2;
key = "tmseta";
}
: radio_row {
: radio_button {
label = "Padrão";
key = "quilop";
value = "0";
}
}
}
}
ok_only;
}
