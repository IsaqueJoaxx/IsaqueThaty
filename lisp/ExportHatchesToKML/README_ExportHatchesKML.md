# Export Hatches to KML - AutoLISP para Civil 3D

Duas versões de rotinas AutoLISP para exportar hachuras do AutoCAD Civil 3D para formato KML (Google Earth) **com suporte a EXTRUSÃO 3D / ALTITUDE**.

## 📁 Arquivos

| Arquivo | Descrição |
|---------|-----------|
| `ExportHatchesToKML.lsp` | **Versão padrão v2.0** - funciona em qualquer AutoCAD/Civil 3D, **com extrusão 3D** |
| `ExportHatchesToKML_Advanced.lsp` | **Versão avançada v2.0** - usa API nativa do Civil 3D 2022+ para conversão precisa **+ extrusão 3D** |
| `README_ExportHatchesKML.md` | Esta documentação |

---

## 🚀 Como Instalar

### Método 1: APPLOAD (Recomendado)
1. Digite `APPLOAD` no Civil 3D
2. Clique em **Content** → **Add**
3. Selecione o arquivo `.lsp` desejado
4. Clique **Load** → **Close**

### Método 2: Arraste e Solte
Arraste o arquivo `.lsp` para a área de desenho do Civil 3D

### Método 3: Linha de Comando
```
(load "C:/caminho/para/ExportHatchesToKML.lsp")
```

### Método 4: Startup Suite (Carregamento Automático)
1. `APPLOAD` → **Startup Suite** → **Contents**
2. Adicione o arquivo → **Close**
3. O LISP carrega automaticamente ao abrir o Civil 3D

---

## 🎯 Como Usar (v2.0 - COM EXTRUSÃO 3D)

### Versão Padrão (`EXPORTHATCHESKML`)
```
Command: EXPORTHATCHESKML
```
**Fluxo interativo:**
1. **Configuração de Altitude** (nova v2.0):
   - Escolha fonte da altura: Fixo / Elevação da hachura / Property Set / Superfície / Sem extrusão
   - Defina modo: `relativeToGround` / `absolute` / `clampToGround`
   - Ative/desative paredes laterais (extrude)
2. Selecione as hachuras (ou Enter = todas no ModelSpace)
3. Escolha local/nome do arquivo `.kml`
4. Abra no **Google Earth Pro** → Ative **3D Buildings** e **Terrain**

### Versão Avançada (`EXPORTHATCHESKML2`)
```
Command: EXPORTHATCHESKML2
```
**Requisitos:**
- Civil 3D 2022 ou superior
- Drawing com **Coordinate System atribuído** (Assign Coordinate System)

**Vantagens v2.0:**
- ✅ Conversão precisa WCS → Lat/Long via `AeccCoordinateConversion`
- ✅ **Extrusão 3D nativa** com múltiplas fontes de altura
- ✅ Suporte a boundaries complexos (arcs, splines, elipses) com amostragem 3D
- ✅ Metadados completos no KML (layer, padrão, área, cor, elevação, altura)
- ✅ Estilos por layer (inclui versões extrudadas)
- ✅ Leitura de **Property Sets / XData** para altura customizada

---

## ⚙️ Configuração de Altitude/Extrusão (NOVO v2.0)

Ao executar o comando, você verá:

```
=== CONFIGURAÇÃO DE ALTITUDE / EXTRUSÃO 3D ===

Fonte da altura:
  [1] Valor fixo (mesma altura para todas)
  [2] Elevação da hachura (propriedade Elevation/Z) - RECOMENDADO
  [3] Property Set / Extended Data (Civil 3D)
  [4] Superfície/Terreno (Civil 3D Surface) - EXPERIMENTAL
  [5] Sem extrusão (2D no chão)
Escolha <2>: 

Modo de altitude KML:
  [1] relativeToGround (altura acima do terreno) - RECOMENDADO
  [2] absolute (altitude absoluta WGS84)
  [3] clampToGround (no chão, sem volume)
Escolha <1>: 

Extrudar paredes laterais? (S/N) <S>: 
```

### Fontes de Altura Detalhadas

| Opção | Descrição | Uso Ideal |
|-------|-----------|-----------|
| **FIXED** | Altura fixa para todas as hachuras | Edifícios de mesma altura, volumes padronizados |
| **ELEVATION** | Usa propriedade `Elevation` da hachura | Hachuras já posicionadas em Z (ex: cortes, plataformas) |
| **PROPERTY** | Lê de Property Set / XData customizado | Alturas armazenadas em dados estendidos |
| **SURFACE** | *(Experimental)* Baseado em superfície Civil 3D | Terrenos, cortes/aterros relativos ao ground |
| **NONE** | Sem extrusão, apenas 2D | Visualização plana, overlays |

### Modos de Altitude KML

| Modo | Comportamento | Quando Usar |
|------|---------------|-------------|
| `relativeToGround` | Altura acima do terreno do Google Earth | **Padrão recomendado** - acompanha terreno |
| `absolute` | Altitude absoluta WGS84 (nível do mar) | Dados geo-referenciados precisos, aviação |
| `clampToGround` | Colado no terreno, sem volume | Overlays 2D, imagens, limites |

---

## 🎨 Exemplos Práticos

### 1. Edifícios com altura fixa (ex: 15m)
```
Command: EXPORTHATCHESKML
Escolha: [1] Valor fixo
Altura: 15.0
Modo: [1] relativeToGround
Extrudar: S
```

### 2. Plataformas/cortes com elevação real
```
Command: EXPORTHATCHESKML2
Escolha: [2] Elevação da hachura
Modo: [1] relativeToGround
Extrudar: S
```
*Requer hachuras com propriedade `Elevation` definida (Propriedades → Geometry → Elevation)*

### 3. Alturas em Property Set Civil 3D
```
Command: EXPORTHATCHESKML2
Escolha: [3] Property Set
Nome: BuildingHeight
Modo: [1] relativeToGround
```

### 4. Apenas visualização 2D (sem volume)
```
Command: EXPORTHATCHESKML
Escolha: [5] Sem extrusão
```

---

## 🔧 Personalização Avançada

### Alterar Cores/Estilos no KML
Edite a função `write-kml-header` no arquivo:

```lisp
;; Verde semi-transparente (AABBGGRR - KML usa ABGR)
(write-line "        <color>7f00ff00</color>" file)  ;; 7f=50% opacity, 00ff00=green

;; Vermelho
(write-line "        <color>7fff0000</color>" file)  ;; 7f=50%, 0000ff=red

;; Azul
(write-line "        <color>7f0000ff</color>" file)  ;; 7f=50%, ff0000=blue

;; Opacidade: 00=transparente, 7f=50%, ff=opaco
```

### Adicionar Inner Boundaries (Furos/Ilhas)
As hachuras com ilhas (boundaries internos) são suportadas automaticamente - o KML gera `<innerBoundaryIs>` para cada loop interno.

### Altitude Mode por Feature
Para misturar modos no mesmo KML, edite `write-kml-placemark-3d` e passe `mode` por feature.

---

## 🐛 Solução de Problemas

| Problema | Solução |
|----------|---------|
| "Civil 3D API não disponível" | Use Civil 3D 2022+; verifique se `AeccXUiLand` está registrado |
| Coordenadas erradas no Google Earth | Atribua Coordinate System correto no drawing (`MAPCSASSIGN`) |
| Hachura não exportada | Verifique se tem boundary válido (`HATCHEDIT` → **Recreate Boundary**) |
| Erro "Automation error" | Reinicie o Civil 3D; registre DLL: `regsvr32 "C:\Program Files\Autodesk\Civil 3D 2024\AeccXUiLand.dll"` |
| KML abre mas vazio | Verifique se hachuras têm área > 0 e boundary fechado |
| **Extrusão não aparece no GE** | Ative **3D Buildings** (Ctrl+Alt+B) e **Terrain** (Ctrl+T) no Google Earth Pro |
| **Altura errada** | Verifique unidade do drawing (UNITS) - deve ser metros para KML |
| **Paredes não extrudam** | Confirme `extrude=1` e `altitudeMode != clampToGround` no KML gerado |

---

## 🔧 Funções Internas (Para Desenvolvedores) - v2.0

| Função | Descrição |
|--------|-----------|
| `configure-altitude-settings` | Menu interativo de configuração 3D |
| `get-hatch-boundary-pts-3d` | Extrai vértices 3D (X,Y,Z) de todos os loops |
| `get-lwpolyline-pts-3d` | Vértices 3D de LWPOLYLINE (com elevação 38) |
| `get-polyline-pts-3d` | Vértices 3D de POLYLINE antiga |
| `sample-curve-obj-3d` | Amostragem 3D de curvas (arc, spline, ellipse) |
| `get-hatch-height` | Obtém altura baseada na fonte configurada |
| `get-hatch-property-height` | Lê altura de Property Set / XData |
| `wcs-to-latlong-c3d` | Conversão WCS→LatLong via Civil 3D API |
| `get-hatch-properties` | Dicionário completo de propriedades |
| `write-kml-placemark-3d` | Escreve `<Placemark>` com Polygon 3D + extrude |

---

## 📝 Changelog

### v2.0 (2026-10-29) - **EXTRUSÃO 3D**
- ✅ **NOVO**: Menu interativo de configuração de altitude
- ✅ **NOVO**: 5 fontes de altura (Fixo, Elevação, Property Set, Superfície, None)
- ✅ **NOVO**: 3 modos de altitude KML (relativeToGround, absolute, clampToGround)
- ✅ **NOVO**: Extrusão de paredes laterais (`<extrude>1</extrude>`)
- ✅ **NOVO**: Suporte a Z nativo nos vértices (LWPOLYLINE elevation 38, POLYLINE 3D)
- ✅ **NOVO**: Amostragem 3D para arcs, splines, elipses
- ✅ **NOVO**: Leitura de Property Sets / XData para altura customizada
- ✅ **NOVO**: Estilos KML dedicados para features extrudadas
- ✅ Metadados enriquecidos: elevação base, altura extrusão, vértices 3D
- ✅ Comando `TESTHATCHKML` mostra Z dos vértices

### v1.0 (2026-10-29)
- ✅ Versão padrão com seleção interativa
- ✅ Versão avançada com Civil 3D API
- ✅ Suporte a boundaries complexos
- ✅ Metadados no KML (descrição HTML)
- ✅ Estilos customizáveis
- ✅ Auto-abrir no Google Earth

---

## 📄 Licença

Livre para uso comercial e pessoal. Atribuição apreciada.

---

## 🤝 Suporte e Referências

- [Civil 3D .NET API](https://aps.autodesk.com/developer-guide/overview/)
- [AutoLISP Developer's Guide](https://help.autodesk.com/view/OARX/2024/PTB/)
- [KML Reference - Google Developers](https://developers.google.com/kml/documentation)
- [KML Altitude Modes](https://developers.google.com/kml/documentation/altitudemode)