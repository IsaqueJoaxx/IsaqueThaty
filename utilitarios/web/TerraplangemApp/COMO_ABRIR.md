# Como criar o projeto Xcode

## Passo a passo

1. Abra o **Xcode** no Mac
2. Vá em **File → New → Project**
3. Selecione **iOS → App** e clique em **Next**
4. Preencha:
   - **Product Name:** TerraplangemApp
   - **Interface:** SwiftUI
   - **Language:** Swift
   - Desmarque "Include Tests" (opcional)
5. Salve em qualquer pasta e clique em **Create**

## Substituir os arquivos gerados

6. No painel esquerdo do Xcode, expanda a pasta **TerraplangemApp**
7. **Delete** o arquivo `ContentView.swift` gerado automaticamente
   - Marque "Move to Trash"
8. **Arraste** os dois arquivos desta pasta para dentro do projeto no Xcode:
   - `TerraplangemApp.swift`
   - `ContentView.swift`
9. Marque **"Copy items if needed"** ao confirmar

## Rodar o app

10. Selecione um simulador iPhone no topo (ex: iPhone 16)
11. Pressione **⌘ + R** para rodar

---

## Funcionalidades

### Corte / Aterro
- Insira **Cota do Terreno** e **Cota de Projeto** (em metros)
- O app calcula automaticamente:
  - **CORTE** se terreno > projeto → resultado × 1,00
  - **ATERRO** se terreno < projeto → resultado × 1,50

### Inclinação
- Insira **Distância** (m) e **Porcentagem** de rampa (%)
- Selecione **Subida** ou **Descida**
- O app calcula o **desnível vertical**:
  - Fórmula: `Desnível = Distância × (% ÷ 100)`
  - Subida → valor positivo (+)
  - Descida → valor negativo (−)
