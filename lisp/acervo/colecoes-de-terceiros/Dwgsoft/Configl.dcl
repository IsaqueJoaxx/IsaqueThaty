cfgeral : dialog {
  label = "Configuração";
: row {
: boxed_column {
label = "Taludes:";
  : text {
    label = "ATERRO [Ex.: 1.5H:1V]";
    }
    : edit_box {
      label = "Horizontal:";
          mnemonic = "C";
      edit_width = 2;
      key = "tathor";
    }
    : edit_box {
      label = "L berma";
          mnemonic = "C";
      edit_width = 2;
      key = "lbrmat";
    }
    : edit_box {
      label = "h berma";
          mnemonic = "C";
      edit_width = 2;
      key = "haterro";
    }
  : text {
    label = "CORTE:";
    }
    : edit_box {
      label = "Horizontal";
          mnemonic = "C";
      edit_width = 2;
      key = "tcthor";
    }
    : edit_box {
      label = "L berma";
          mnemonic = "C";
      edit_width = 2;
      key = "lbrmct";
    }
    : edit_box {
      label = "h berma";
          mnemonic = "C";
      edit_width = 2;
      key = "hcorte";
    }
    }
: boxed_column {
label = "Seções:";
  : text {
    label = "Faixa de domínio";
    }
    : edit_box {
      label = "Largura:";
          mnemonic = "C";
      edit_width = 2;
      key = "lgfdm";
    }
  : text {
    label = "Berma:";
    }
: radio_row {
: radio_button {
label = "Inclinada";
key = "bminc";
value = "0";
}
}
: radio_row {
: radio_button {
label = "Horizontal";
key = "bmhor";
value = "1";
}
}
    : edit_box {
      label = "Transversal (%)";
          mnemonic = "C";
      edit_width = 2;
      key = "itrv";
    }
    }
: boxed_column {
label = "Layer";
  : text {
    label = "Curva Mestra";
    }
    : edit_box {
          mnemonic = "C";
      edit_width = 13;
      key = "cmestra";
    }
  : text {
    label = "Intermediária";
    }
    : edit_box {
          mnemonic = "C";
      edit_width = 13;
      key = "cinterm";
    }
  : text {
    label = "Decomposta";
    }
    : edit_box {
          mnemonic = "C";
      edit_width = 13;
      key = "rocd";
    }
  : text {
    label = "Rocha Sã";
    }
    : edit_box {
          mnemonic = "C";
      edit_width = 13;
      key = "rocs";
    }
: radio_row {
: radio_button {
label = "Selecionar";
key = "selayer";
value = "0";
}
}
    }
    }
        : row {
  ok_only;
        : button {
          label = "Ajuda";
          mnemonic = "A";
         key = "ajudacf";
         fixed_width = true;
        }
        }
    }
