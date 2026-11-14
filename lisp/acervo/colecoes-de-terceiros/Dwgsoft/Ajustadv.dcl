ajustadv :dialog {
label = "Select 2D/3D polyline";
: boxed_column {
label = "PONTOS:";
: radio_row {
: radio_button {
label = "Vértices da polyline";
key = "pvert";
value = "1";
}
}
: radio_row {
: radio_button {
label = "Equidistante 20 m *";
key = "pequi";
value = "0";
}
}
: edit_box {
label = "Distância entre ptos (m):";
edit_width = 2;
key = "dentpt";
}
: row {
: radio_row {
: radio_button {
label = "Inicia em P1";
key = "inip1";
value = "0";
}
}
: radio_row {
: radio_button {
label = "Automático";
key = "auto";
value = "1";
}
}
}
}
: row {
ok_cancel;
: button {
label = "Ajuda";
key = "ajuda1";
}
}
}
