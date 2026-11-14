tplista : dialog {
label = "Tipo de lista:";
: radio_row {
: radio_button {
label = "Tipo1";
key = "ltp0";
value = "1";
}
: radio_button {
label = "Tipo2";
key = "ltp1";
}
: radio_button {
label = "Tipo3";
key = "ltp2";
}
: radio_button {
label = "Tipo4";
key = "ltp3";
}}
: row {
: radio_row {
: radio_button {
label = "Apagar lista(s) [layer LISTAF]";
key = "apglf";
value = "0";
}
}
: radio_row {
: radio_button {
label = "Multiplicador";
key = "mltp";
value = "0";
}
}
}
ok_only;
}
