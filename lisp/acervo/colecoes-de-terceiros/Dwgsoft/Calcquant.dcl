calcquant : dialog {
  label = "Distribuição:";
  : boxed_column {
    label = "Calcular quantidade:";
: row {
    : edit_box {
      edit_width = 4;
      key = "dime";
    }
    : text {
      label = "CM";
    }
: radio_row {
: radio_button {
label = "Utilizar";
key = "ok3";
value = "1";
}}
: radio_row {
: radio_button {
label = "Externo";
key = "extn";
value = "0";
}}
    }
: row {
: edit_box {
label = "Ø";
edit_width = 3;
key = "diam";
}
: text {
label = "";
}
: edit_box {
label = "C/";
edit_width = 3;
key = "espa";
}
: text {
label = "    ";
}
: radio_row {
: radio_button {
label = "Pingado";
key = "pinga";
value = "0";
}}
}
: row {
: edit_box {
edit_width = 2;
key = "cobr";
}
: text {
label = "Cobrimento";
}
: edit_box {
edit_width = 2;
key = "resp";
}
: text {
label = "Quantidade";
}
}
: row {
: edit_box {
edit_width = 2;
key = "poscao";
}
: text {
label = "Posição";
}
: edit_box {
edit_width = 12;
key = "texto";
}
: text {
label = "Texto";
}
}
: row {
: edit_box {
label = "Partida:";
edit_width = 3;
key = "partida";
}
: text {
label = "";
}
: edit_box {
label = "Chegada";
edit_width = 3;
key = "chegada";
}}
: row {
  : button {
    label = "Draw";
    key = "dese1";
  }
  : text {
    label = "";
  }
: toggle {
label = "Contável";
key = "ctavel";
}
: radio_row {
: radio_button {
label = "Run";
key = "ok2";
value = "1";
}}
}
: row {
: text {
label = "Mergulha";
}
: radio_row {
: radio_button {
label = "1ª Ponta";
key = "mrg1";
value = "0";
}
: radio_button {
label = "2ª Ponta";
key = "mrg2";
value = "0";
}
}
}
: row {
: text {
label = "Aproxima";
}
: radio_row {
: radio_button {
label = "1ª Ponta";
key = "aprx1";
value = "0";
}
: radio_button {
label = "2ª Ponta";
key = "aprx2";
value = "0";
}
}
}
: row {
: edit_box {
label = "<";
edit_width = 4;
key = "var-de";
}
: edit_box {
label = "a";
edit_width = 4;
key = "var-a";
}
: edit_box {
label = "   Delta";
edit_width = 4;
key = "delta";
}}
: radio_row {
: radio_button {
label = "Limpar Variação";
key = "lpvar";
}
}
spacer;
}
: row {
ok_only;
  : button {
    label = "Ajuda:";
    key = "sos";
  }
}}
