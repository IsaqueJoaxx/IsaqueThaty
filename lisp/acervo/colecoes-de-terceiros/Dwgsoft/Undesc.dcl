undesc : dialog {
label = "Unidade e Escalas:";
: boxed_column {
label = "Unidades e Padrão";
: row  {
: toggle {
label = "Metro      ";
key = "metro";
}
: toggle {
label = "Centímetro";
key = "centi";
}
}
: row  {
: toggle {
label = "Milímetro";
key = "milim";
}
: radio_row {
: radio_button {
label = "Padrão      ";
key = "quilop";
value = "0";
}}
}
allow_accept = true;
}
: boxed_column {
label = "Escala 1:";
: row {
: edit_box {
label = "Formato:";
edit_width = 6;
key = "escale";
}
: text {
label = "  ";
}
: edit_box {
label = "Desenho:";
edit_width = 6;
key = "+escale";
}
}
spacer;
allow_accept = true;
}
: row {
: radio_row {
: radio_button {
label = "Layout";
key = "paper";
value = "0";
}}
: radio_row {
: radio_button {
label = "Dimension";
key = "cdims";
value = "0";
}}
: radio_row {
: radio_button {
label = "Linetype";
key = "lntype";
value = "0";
}}
: radio_row {
: radio_button {
label = "Menu";
key = "ltoolbar";
value = "0";
}}
}
: row {
 fixed_width = true;
alignment = centered;
ok_only;
: button {
label = "Ajuda";
mnemonic = "A";
key = "ajuda";
}
: text {
label =" ";
}
: edit_box {
label = "Decimal:";
edit_width = 2;
key = "vdec";
}
allow_accept = true;
}
}
