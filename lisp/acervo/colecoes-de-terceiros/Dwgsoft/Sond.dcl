sond : dialog {
  label = "Sondagem          www.dwgsoft.com.br";
  :column {
    label = "Convenção de Sondagem:";
    : toggle {
      label = "Executada";
      mnemonic = "E";
      key = "exec";
    }
    : row {
      : toggle {
        label = "Programada";
        mnemonic = "P";
        key = "prog";
      }
      : button {
        label = "Nomenclatura";
        mnemonic = "N";
        key = "nome";
      }
    }
    : toggle {
      label = "Executada e Programada";
      mnemonic = "e";
      key = "pgex";
    }
  }
  :column {
    label = "Sondagem";
    : row {
      : toggle {
        label = "Tabela";
        mnemonic = "T";
        key = "tabe";
      }
      : button {
        label = "Geologia/Geotecnia";
        mnemonic = "G";
        key = "inst";
      }
    }
    : row {
      : toggle {
        label = "Legenda";
        mnemonic = "L";
        key = "lege";
      }
      : button {
        label = "   Log de Sondagem   ";
        mnemonic = "S";
        key = "logs";
      }
    }
    : toggle {
      label = "Coluna de Cotas na tabela";
      mnemonic = "C";
      key = "colu";
    }
  }
  : boxed_column {
    label = "Desenhar Sondagem, Numerar pontos:";
    : row {
      : edit_box {
        label = "Ponto:";
        edit_width = 7;
        key = "sond";
      }
      : text {
        label = "";
      }
      : edit_box {
        label = "Cota:";
        edit_width = 7;
        key = "cota";
      }
    }
    : text {
      label = "Coordenadas:";
    }
    : row {
      : edit_box {
        label = "X =";
        edit_width = 11;
        key = "X=";
      }
      : text {
        label = "   ";
      }
      : edit_box {
        label = "Y =";
        edit_width = 11;
        key = "Y=";
      }
    }
    : text {
      label = "Não separar Milhar - Ex.: X = 1207";
    }
    : text {
      label = "Separar decimal com . [ponto] - ex.: 1207.7";
    }
    : edit_box {
      label = "Geologia:";
      edit_width = 31;
      key = "obs";
    }
  }
  :row {
    ok_cancel;
    : button {
      label = "Ajuda";
      mnemonic = "A";
      key = "ajuda";
    }
  }
}
