pquadro :dialog {
label = "PQ  e  CPQ";
: boxed_column {
label = "Layers: Cabeçalho da lista";
: row {
: edit_box {
label = "1ª linha (h 4mm): ";
edit_width = 15;
key = "ltit4";
}
: radio_row {
: radio_button {
label = " ";
key = "stx1";
value = "0";
}}
: image_button {
key = "cortit4";
horizontal_margin = none;
width = 2.5;
color        = 4;
}
}
: row {
: edit_box {
label = "2ª linha (h 3mm): ";
edit_width = 15;
key = "ltit3";
}
: radio_row {
: radio_button {
label = " ";
key = "stx3";
value = "0";
}}
: image_button {
key = "cortit3";
horizontal_margin = none;
width = 2.5;
height = 1;
color        = 3;
}
}
: row {
: edit_box {
label = "Linhas externas: ";
edit_width = 15;
key = "lline";
}
: radio_row {
: radio_button {
label = " ";
key = "sline";
value = "0";
}}
: image_button {
key = "corle";
horizontal_margin = none;
width = 2.5;
height = 1;
color        = 4;
}
}
}
: boxed_column {
label = "Layers da Lista:";
: row {
: edit_box {
label = "Textos (h 2.5mm):";
edit_width = 15;
key = "ltxl";
}
: radio_row {
: radio_button {
label = " ";
key = "stx3l";
value = "0";
}}
: image_button {
key = "cort3mm";
horizontal_margin = none;
width = 2.5;
height = 1;
color        = 3;
}
}
: row {
: edit_box {
label = "Linhas internas:   ";
edit_width = 15;
key = "llini";
}
: radio_row {
: radio_button {
label = " ";
key = "stx4";
value = "0";
}}
: image_button {
key = "corli";
horizontal_margin = none;
width = 2.5;
height = 1;
color        = 1;
}
}
}
: boxed_column {
label = "Layers Leader e Texto: ";
: row {
: edit_box {
label = "Layer Linha guia: ";
edit_width = 15;
key = "lyguia";
}
: radio_row {
: radio_button {
label = " ";
key = "stx5";
value = "0";
}}
: image_button {
key = "corld";
horizontal_margin = none;
width = 2.5;
height = 1;
color        = 1;
}
}
: row {
: edit_box {
label = "Texto Linha guia: ";
edit_width = 15;
key = "txguia";
}
: radio_row {
: radio_button {
label = " ";
key = "stx6";
value = "0";
}}
: image_button {
key = "corldt";
horizontal_margin = none;
width = 2.5;
height = 1;
color        = 3;
}
}
}
: boxed_column {
label = "Ponto: ";
: row {
: radio_row {
: radio_button {
label = "Leader";
key = "leade";
value = "0";
}}
: radio_row {
: radio_button {
label = "Donut";
key = "diamet";
value = "1";
}}
: radio_row {
: radio_button {
label = "Arrow";
key = "seta";
value = "0";
}}
: radio_row {
: radio_button {
label = "Pick";
key = "pmanu";
value = "1";
}
}
: radio_row {
: radio_button {
label = "Auto";
key = "auto";
value = "1";
}
}
}
: row {
: edit_box {
label = "Oblique";
edit_width = 1;
key = "txinc";
}
: edit_box {
label = "   Ø donut";
edit_width = 2;
key = "diamd";
}
: edit_box {
label = "   L arrow";
edit_width = 2;
key = "larow";
}
}
}
: row {
ok_cancel;
: button {
label = "Ajuda";
key = "ajuda";
}
}
}
