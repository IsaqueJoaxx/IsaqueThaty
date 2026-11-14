estaq : dialog {
  label = "Configuração:";
  : boxed_column {
    label = "Parámetros:";
      : row {
      : toggle {
        label = "PC's e PT's     ";
        key = "marca";
      }
      : toggle {
        label = "PCE PCD e PT";
        key = "marcaed";
      }
      }
      : row {
      : toggle {
        label = "TE, EC, CE, ET";
        key = "marcate";
      }
      : toggle {
        label = "TS, SC, CS, ST";
        key = "marcats";
      }
      }
  spacer;
        : text {
          label = "Texto a cada:";
        }
      :row {
    : edit_box {
      edit_width = 3;
      key = "txtam";
    }
: radio_row {
: radio_button {
label = "m(s)";
key = "txtms";
value = "0";
}
}
: radio_row {
: radio_button {
label = "Estaca(s)";
key = "txtest";
value = "1";
}
}
: radio_row {
: radio_button {
label = "km(s)";
key = "txtkm";
value = "0";
}
}
      }
        : text {
          label = "Estacas no eixo a cada";
        }
      :row {
    : edit_box {
      edit_width = 3;
      key = "estam";
    }
: radio_row {
: radio_button {
label = "m(s)";
key = "estms";
value = "0";
}
}
: radio_row {
: radio_button {
label = "Estaca(s)";
key = "estest";
value = "1";
}
}
: radio_row {
: radio_button {
label = "km(s)";
key = "estkm";
value = "0";
}
}
      }
  spacer;
  spacer;
      : toggle {
        label = "Elementos geométricos";
        key = "tabela";
      }
      : toggle {
        label = "Numerar curva na planta";
        key = "merar";
      }
    : edit_box {
      label = "Estaca Inicial";
      edit_width = 5;
      key = "inicia";
    }
    : edit_box {
      label = "Inicia em C1";
      edit_width = 5;
      key = "demm";
    }
    : edit_box {
      label = "[Plotagem]  Esc. 1:";
      edit_width = 5;
      key = "escal";
    }
  }
        : row {
  ok_cancel;
        : button {
          label = "Ajuda";
          mnemonic = "A";
         key = "ajuda";
         fixed_width = true;
        }
        }
}
