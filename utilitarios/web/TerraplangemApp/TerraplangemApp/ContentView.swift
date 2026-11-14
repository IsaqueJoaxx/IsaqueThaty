import SwiftUI

// MARK: - Home

struct ContentView: View {
    var body: some View {
        NavigationView {
            ZStack {
                Color(.systemGroupedBackground).ignoresSafeArea()

                VStack(spacing: 24) {
                    Spacer()

                    VStack(spacing: 6) {
                        Image(systemName: "building.2.fill")
                            .font(.system(size: 52))
                            .foregroundColor(.blue)
                        Text("Terraplanagem")
                            .font(.title)
                            .fontWeight(.bold)
                        Text("Calculadora de Obras")
                            .font(.subheadline)
                            .foregroundColor(.secondary)
                    }
                    .padding(.bottom, 12)

                    NavigationLink(destination: CorteAterroView()) {
                        FeatureCard(
                            icon: "arrow.up.arrow.down.square.fill",
                            title: "Corte / Aterro",
                            subtitle: "Calcula a diferença entre cota do terreno e de projeto",
                            color: .blue
                        )
                    }
                    .buttonStyle(PlainButtonStyle())

                    NavigationLink(destination: InclinacaoView()) {
                        FeatureCard(
                            icon: "chart.line.uptrend.xyaxis",
                            title: "Inclinação",
                            subtitle: "Calcula o desnível por distância e porcentagem de rampa",
                            color: .green
                        )
                    }
                    .buttonStyle(PlainButtonStyle())

                    Spacer()
                }
                .padding(.horizontal, 20)
            }
            .navigationTitle("Terraplanagem")
            .navigationBarHidden(true)
        }
        .navigationViewStyle(StackNavigationViewStyle())
    }
}

// MARK: - Feature Card

struct FeatureCard: View {
    let icon: String
    let title: String
    let subtitle: String
    let color: Color

    var body: some View {
        HStack(spacing: 16) {
            ZStack {
                RoundedRectangle(cornerRadius: 14)
                    .fill(color.opacity(0.15))
                    .frame(width: 58, height: 58)
                Image(systemName: icon)
                    .font(.system(size: 28))
                    .foregroundColor(color)
            }

            VStack(alignment: .leading, spacing: 4) {
                Text(title)
                    .font(.headline)
                    .foregroundColor(.primary)
                Text(subtitle)
                    .font(.caption)
                    .foregroundColor(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
            }

            Spacer()

            Image(systemName: "chevron.right")
                .font(.caption)
                .foregroundColor(.tertiaryLabel)
        }
        .padding(16)
        .background(Color(.systemBackground))
        .cornerRadius(16)
        .shadow(color: Color.black.opacity(0.07), radius: 8, x: 0, y: 2)
    }
}

// MARK: - Corte / Aterro View

struct CorteAterroView: View {

    @State private var cotaTerreno = ""
    @State private var cotaProjeto = ""
    @State private var resultado: CorteAterroResult?

    struct CorteAterroResult {
        let isCorte: Bool
        let diferenca: Double
        let fator: Double

        var valorFinal: Double { diferenca * fator }
        var tipo: String { diferenca == 0 ? "NULO" : (isCorte ? "CORTE" : "ATERRO") }
        var cor: Color { isCorte ? .blue : .orange }
        var icone: String { isCorte ? "arrow.up.to.line" : "arrow.down.to.line" }
    }

    var podoCalcular: Bool {
        !cotaTerreno.isEmpty && !cotaProjeto.isEmpty
    }

    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()

            ScrollView {
                VStack(spacing: 20) {

                    // Explicação
                    InfoBanner(
                        text: "Informe as cotas para identificar se há corte (terreno acima do projeto) ou aterro (terreno abaixo do projeto).",
                        color: .blue
                    )

                    // Inputs
                    VStack(spacing: 0) {
                        InputRow(label: "Cota do Terreno", unit: "m",
                                 value: $cotaTerreno, placeholder: "Ex: 125,50")
                        Divider().padding(.leading, 16)
                        InputRow(label: "Cota de Projeto", unit: "m",
                                 value: $cotaProjeto, placeholder: "Ex: 122,00")
                    }
                    .background(Color(.systemBackground))
                    .cornerRadius(16)

                    // Botão
                    Button(action: calcular) {
                        Label("Calcular", systemImage: "equal.square.fill")
                            .font(.headline)
                            .foregroundColor(.white)
                            .frame(maxWidth: .infinity)
                            .padding()
                            .background(podoCalcular ? Color.blue : Color.gray.opacity(0.5))
                            .cornerRadius(14)
                    }
                    .disabled(!podoCalcular)

                    // Resultado
                    if let res = resultado {
                        VStack(spacing: 16) {
                            // Badge
                            HStack {
                                Spacer()
                                HStack(spacing: 8) {
                                    Image(systemName: res.icone)
                                        .font(.system(size: 18, weight: .bold))
                                        .foregroundColor(res.cor)
                                    Text(res.tipo)
                                        .font(.system(size: 26, weight: .bold))
                                        .foregroundColor(res.cor)
                                }
                                .padding(.horizontal, 28)
                                .padding(.vertical, 10)
                                .background(res.cor.opacity(0.12))
                                .cornerRadius(50)
                                Spacer()
                            }

                            Divider()

                            VStack(spacing: 14) {
                                ResultRow(label: "Diferença de cotas",
                                          value: String(format: "%.3f m", res.diferenca))
                                ResultRow(label: "Fator aplicado",
                                          value: String(format: "× %.2f", res.fator))
                                Divider()
                                ResultRow(
                                    label: "Resultado",
                                    value: String(format: "%.3f m", res.valorFinal),
                                    highlight: true,
                                    cor: res.cor
                                )
                            }
                        }
                        .padding(18)
                        .background(Color(.systemBackground))
                        .cornerRadius(16)
                        .shadow(color: .black.opacity(0.07), radius: 8, x: 0, y: 2)
                        .transition(.move(edge: .bottom).combined(with: .opacity))
                    }

                    // Legenda
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Fatores utilizados")
                            .font(.caption)
                            .fontWeight(.semibold)
                            .foregroundColor(.secondary)
                        HStack(spacing: 16) {
                            LegendaItem(cor: .blue, texto: "Corte → × 1,00")
                            LegendaItem(cor: .orange, texto: "Aterro → × 1,50")
                        }
                    }
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(14)
                    .background(Color(.systemBackground))
                    .cornerRadius(12)
                }
                .padding()
            }
            .toolbar {
                ToolbarItemGroup(placement: .keyboard) {
                    Spacer()
                    Button("OK") { hideKeyboard() }
                }
            }
        }
        .navigationTitle("Corte / Aterro")
        .navigationBarTitleDisplayMode(.large)
        .onTapGesture { hideKeyboard() }
    }

    func calcular() {
        hideKeyboard()
        guard
            let terreno = Double(cotaTerreno.normalizado),
            let projeto = Double(cotaProjeto.normalizado)
        else { return }

        let diff = terreno - projeto
        withAnimation(.spring()) {
            if diff > 0 {
                resultado = CorteAterroResult(isCorte: true, diferenca: diff, fator: 1.0)
            } else {
                resultado = CorteAterroResult(isCorte: false, diferenca: abs(diff), fator: 1.5)
            }
        }
    }
}

// MARK: - Inclinação View

struct InclinacaoView: View {

    @State private var distancia = ""
    @State private var porcentagem = ""
    @State private var isSubida = true
    @State private var resultado: InclinacaoResult?

    struct InclinacaoResult {
        let desnivel: Double
        let isSubida: Bool

        var desnivelFinal: Double { isSubida ? desnivel : -desnivel }
        var tipo: String { isSubida ? "SUBIDA" : "DESCIDA" }
        var cor: Color { isSubida ? .green : .red }
        var icone: String { isSubida ? "arrow.up.right" : "arrow.down.right" }
        var sinal: String { isSubida ? "+" : "−" }
    }

    var podoCalcular: Bool {
        !distancia.isEmpty && !porcentagem.isEmpty
    }

    var corBotao: Color { isSubida ? .green : .red }

    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()

            ScrollView {
                VStack(spacing: 20) {

                    // Explicação
                    InfoBanner(
                        text: "Informe a distância horizontal e a porcentagem de rampa. O app calcula o desnível vertical.",
                        color: .green
                    )

                    // Inputs + seletor
                    VStack(spacing: 0) {
                        InputRow(label: "Distância", unit: "m",
                                 value: $distancia, placeholder: "Ex: 50,00")
                        Divider().padding(.leading, 16)
                        InputRow(label: "Porcentagem", unit: "%",
                                 value: $porcentagem, placeholder: "Ex: 8,5")
                        Divider().padding(.leading, 16)

                        // Subida / Descida
                        HStack(spacing: 12) {
                            Text("Tipo")
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                            Spacer()
                            HStack(spacing: 2) {
                                ToggleButton(label: "Subida", icon: "arrow.up",
                                             isSelected: isSubida, cor: .green) {
                                    withAnimation(.easeInOut(duration: 0.2)) { isSubida = true }
                                }
                                ToggleButton(label: "Descida", icon: "arrow.down",
                                             isSelected: !isSubida, cor: .red) {
                                    withAnimation(.easeInOut(duration: 0.2)) { isSubida = false }
                                }
                            }
                            .background(Color(.systemGray5))
                            .cornerRadius(10)
                        }
                        .padding(16)
                    }
                    .background(Color(.systemBackground))
                    .cornerRadius(16)

                    // Botão
                    Button(action: calcular) {
                        Label("Calcular", systemImage: "equal.square.fill")
                            .font(.headline)
                            .foregroundColor(.white)
                            .frame(maxWidth: .infinity)
                            .padding()
                            .background(podoCalcular ? corBotao : Color.gray.opacity(0.5))
                            .cornerRadius(14)
                            .animation(.easeInOut, value: isSubida)
                    }
                    .disabled(!podoCalcular)

                    // Resultado
                    if let res = resultado {
                        VStack(spacing: 16) {
                            // Badge
                            HStack {
                                Spacer()
                                HStack(spacing: 8) {
                                    Image(systemName: res.icone)
                                        .font(.system(size: 18, weight: .bold))
                                        .foregroundColor(res.cor)
                                    Text(res.tipo)
                                        .font(.system(size: 26, weight: .bold))
                                        .foregroundColor(res.cor)
                                }
                                .padding(.horizontal, 28)
                                .padding(.vertical, 10)
                                .background(res.cor.opacity(0.12))
                                .cornerRadius(50)
                                Spacer()
                            }

                            Divider()

                            VStack(spacing: 14) {
                                ResultRow(label: "Desnível calculado",
                                          value: String(format: "%.3f m", res.desnivel))
                                ResultRow(label: "Direção",
                                          value: res.isSubida ? "Somar (+)" : "Subtrair (−)")
                                Divider()
                                ResultRow(
                                    label: "Desnível final",
                                    value: String(format: "%@%.3f m", res.sinal, res.desnivel),
                                    highlight: true,
                                    cor: res.cor
                                )
                            }
                        }
                        .padding(18)
                        .background(Color(.systemBackground))
                        .cornerRadius(16)
                        .shadow(color: .black.opacity(0.07), radius: 8, x: 0, y: 2)
                        .transition(.move(edge: .bottom).combined(with: .opacity))
                    }

                    // Fórmula
                    VStack(alignment: .leading, spacing: 6) {
                        Text("Fórmula")
                            .font(.caption)
                            .fontWeight(.semibold)
                            .foregroundColor(.secondary)
                        Text("Desnível = Distância × (% ÷ 100)")
                            .font(.caption)
                            .foregroundColor(.secondary)
                            .fontDesign(.monospaced)
                    }
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(14)
                    .background(Color(.systemBackground))
                    .cornerRadius(12)
                }
                .padding()
            }
            .toolbar {
                ToolbarItemGroup(placement: .keyboard) {
                    Spacer()
                    Button("OK") { hideKeyboard() }
                }
            }
        }
        .navigationTitle("Inclinação")
        .navigationBarTitleDisplayMode(.large)
        .onTapGesture { hideKeyboard() }
    }

    func calcular() {
        hideKeyboard()
        guard
            let dist = Double(distancia.normalizado),
            let perc = Double(porcentagem.normalizado)
        else { return }

        let desnivel = dist * (perc / 100.0)
        withAnimation(.spring()) {
            resultado = InclinacaoResult(desnivel: desnivel, isSubida: isSubida)
        }
    }
}

// MARK: - Componentes Compartilhados

struct InputRow: View {
    let label: String
    let unit: String
    @Binding var value: String
    let placeholder: String

    var body: some View {
        HStack(spacing: 12) {
            Text(label)
                .font(.subheadline)
                .foregroundColor(.secondary)
                .frame(minWidth: 110, alignment: .leading)

            TextField(placeholder, text: $value)
                .keyboardType(.decimalPad)
                .multilineTextAlignment(.trailing)
                .font(.body)

            Text(unit)
                .font(.subheadline)
                .foregroundColor(.secondary)
                .frame(width: 28, alignment: .trailing)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 14)
    }
}

struct ResultRow: View {
    let label: String
    let value: String
    var highlight: Bool = false
    var cor: Color = .primary

    var body: some View {
        HStack {
            Text(label)
                .font(highlight ? .headline : .subheadline)
                .foregroundColor(highlight ? .primary : .secondary)
            Spacer()
            Text(value)
                .font(highlight ? .title3 : .subheadline)
                .fontWeight(highlight ? .bold : .regular)
                .foregroundColor(highlight ? cor : .primary)
        }
    }
}

struct ToggleButton: View {
    let label: String
    let icon: String
    let isSelected: Bool
    let cor: Color
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 5) {
                Image(systemName: icon)
                    .font(.caption.weight(.semibold))
                Text(label)
                    .font(.subheadline)
                    .fontWeight(.medium)
            }
            .padding(.horizontal, 14)
            .padding(.vertical, 9)
            .background(isSelected ? cor : Color.clear)
            .foregroundColor(isSelected ? .white : .secondary)
            .cornerRadius(9)
        }
    }
}

struct InfoBanner: View {
    let text: String
    let color: Color

    var body: some View {
        HStack(alignment: .top, spacing: 10) {
            Image(systemName: "info.circle.fill")
                .foregroundColor(color)
                .font(.subheadline)
            Text(text)
                .font(.caption)
                .foregroundColor(.secondary)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(12)
        .background(color.opacity(0.08))
        .cornerRadius(12)
    }
}

struct LegendaItem: View {
    let cor: Color
    let texto: String

    var body: some View {
        HStack(spacing: 6) {
            Circle()
                .fill(cor)
                .frame(width: 8, height: 8)
            Text(texto)
                .font(.caption)
                .foregroundColor(.secondary)
        }
    }
}

// MARK: - Extensões Utilitárias

extension View {
    func hideKeyboard() {
        UIApplication.shared.sendAction(
            #selector(UIResponder.resignFirstResponder),
            to: nil, from: nil, for: nil
        )
    }
}

extension String {
    var normalizado: String {
        self.replacingOccurrences(of: ",", with: ".")
    }
}
