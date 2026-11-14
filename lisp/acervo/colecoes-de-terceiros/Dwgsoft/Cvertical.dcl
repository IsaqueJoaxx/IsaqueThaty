vertical : dialog {
  label = "DWGsoft - Curva Circular";
    spacer;
: row {
    : edit_box {
      label = "Velocidade km/h";
          mnemonic = "V";
      edit_width = 3;
      key = "velocid";
    }
  : text {
    label = " ";
    }
    : edit_box {
      label = "Largura";
          mnemonic = "L";
      edit_width = 4;
      key = "larg";
    }
: radio_row {
: radio_button {
label = "Platô";
key = "plato";
value = "0";
}
}
    }
: row {
  : text {
    label = "Distância visibilidade 70 m";
    }
: radio_row {
: radio_button {
label = "Config";
key = "cfgrl";
value = "0";
}
}
: radio_row {
: radio_button {
label = "Tabelas";
key = "tbls";
value = "0";
}
}
    }
: row {
    : edit_box {
      label = "Estaca inicial:";
          mnemonic = "E";
      edit_width = 3;
      key = "esini";
    }
  : text {
    label = "";
    }
    : edit_box {
      label = "Cota:";
          mnemonic = "C";
      edit_width = 5;
      key = "ceini";
    }
    }
    spacer;
: row {
: radio_row {
: radio_button {
label = "Eixo";
key = "selhor";
value = "0";
}
}
: radio_row {
: radio_button {
label = "Estacas";
key = "estacas";
value = "0";
}
}
    : edit_box {
      label = "Abaulam.";
          mnemonic = "C";
      edit_width = 2;
      key = "abaul";
    }
}
: row {
: radio_row {
: radio_button {
label = "PC/PT";
key = "pcpt";
value = "0";
}
}
: radio_row {
: radio_button {
label = "TS/SC";
key = "tssc";
value = "0";
}
}
: radio_row {
: radio_button {
label = "Superlargura";
key = "superlg";
value = "0";
}
}
}
: row {
: radio_row {
: radio_button {
label = "Greide";
key = "autodes";
value = "0";
}
}
    : edit_box {
      label = "Refin.";
          mnemonic = "C";
      edit_width = 2;
      key = "ref";
    }
  : text {
    label = "";
    }
    : edit_box {
      label = "Superelev.";
          mnemonic = "C";
      edit_width = 2;
      key = "superel";
    }
}
        : row {
  ok_cancel;
        : button {
          label = "Ajuda";
          mnemonic = "A";
         key = "ajudacv";
         fixed_width = true;
        }
        }
        }
