import SwiftUI

enum TerrainResult {
    case cut(volume: Double)
    case fill(volume: Double)
    case level

    var label: String {
        switch self {
        case .cut:  return "CORTE"
        case .fill: return "ATERRO"
        case .level: return "NIVELADO"
        }
    }

    var color: Color {
        switch self {
        case .cut:   return Color(red: 0.15, green: 0.68, blue: 0.38)
        case .fill:  return Color(red: 0.95, green: 0.45, blue: 0.10)
        case .level: return .blue
        }
    }

    var icon: String {
        switch self {
        case .cut:   return "arrow.down.to.line.compact"
        case .fill:  return "arrow.up.to.line.compact"
        case .level: return "checkmark.circle.fill"
        }
    }

    var volume: Double? {
        switch self {
        case .cut(let v):  return v
        case .fill(let v): return v
        case .level:       return nil
        }
    }
}

struct CutFillCalculatorView: View {
    @State private var cotaTerreno: String = ""
    @State private var cotaProjeto: String = ""
    @State private var result: TerrainResult? = nil
    @FocusState private var focusedField: Field?

    enum Field { case terreno, projeto }

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 28) {

                    // Header card
                    VStack(spacing: 6) {
                        Image(systemName: "arrow.up.arrow.down.circle.fill")
                            .font(.system(size: 48))
                            .foregroundStyle(.blue)
                        Text("Calculadora de Corte e Aterro")
                            .font(.title2).bold()
                            .multilineTextAlignment(.center)
                        Text("Insira as cotas para calcular o tipo de movimento de terra")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                            .multilineTextAlignment(.center)
                    }
                    .padding(.top, 8)

                    // Input card
                    VStack(spacing: 0) {
                        InputRow(
                            icon: "square.bottomhalf.filled",
                            label: "Cota do Terreno",
                            placeholder: "Ex: 105.50",
                            text: $cotaTerreno,
                            isFocused: focusedField == .terreno
                        )
                        .focused($focusedField, equals: .terreno)

                        Divider().padding(.horizontal)

                        InputRow(
                            icon: "square.tophalf.filled",
                            label: "Cota de Projeto",
                            placeholder: "Ex: 103.00",
                            text: $cotaProjeto,
                            isFocused: focusedField == .projeto
                        )
                        .focused($focusedField, equals: .projeto)
                    }
                    .background(Color(.secondarySystemBackground))
                    .clipShape(RoundedRectangle(cornerRadius: 16))
                    .shadow(color: .black.opacity(0.06), radius: 8, x: 0, y: 2)

                    // Calculate button
                    Button(action: calculate) {
                        HStack(spacing: 10) {
                            Image(systemName: "equal.circle.fill")
                            Text("Calcular")
                                .fontWeight(.semibold)
                        }
                        .frame(maxWidth: .infinity)
                        .frame(height: 52)
                        .background(canCalculate ? Color.blue : Color(.systemGray4))
                        .foregroundStyle(.white)
                        .clipShape(RoundedRectangle(cornerRadius: 14))
                    }
                    .disabled(!canCalculate)
                    .animation(.easeInOut(duration: 0.2), value: canCalculate)

                    // Result
                    if let result {
                        ResultCardView(result: result)
                            .transition(.scale(scale: 0.92).combined(with: .opacity))
                    }

                    // Clear
                    if result != nil || !cotaTerreno.isEmpty || !cotaProjeto.isEmpty {
                        Button(action: clear) {
                            Label("Limpar", systemImage: "trash")
                                .font(.subheadline)
                                .foregroundStyle(.secondary)
                        }
                    }

                    Spacer(minLength: 40)
                }
                .padding(.horizontal, 20)
                .padding(.top, 12)
            }
            .navigationTitle("Corte / Aterro")
            .navigationBarTitleDisplayMode(.inline)
            .scrollDismissesKeyboard(.interactively)
            .toolbar {
                ToolbarItemGroup(placement: .keyboard) {
                    Spacer()
                    Button("OK") { focusedField = nil }
                }
            }
        }
    }

    var canCalculate: Bool {
        Double(cotaTerreno.replacingOccurrences(of: ",", with: ".")) != nil &&
        Double(cotaProjeto.replacingOccurrences(of: ",", with: ".")) != nil
    }

    func calculate() {
        guard
            let terreno = Double(cotaTerreno.replacingOccurrences(of: ",", with: ".")),
            let projeto = Double(cotaProjeto.replacingOccurrences(of: ",", with: "."))
        else { return }

        focusedField = nil
        withAnimation(.spring(response: 0.4, dampingFraction: 0.75)) {
            if terreno > projeto {
                let diff = terreno - projeto
                result = .cut(volume: diff * 1.0)
            } else if projeto > terreno {
                let diff = projeto - terreno
                result = .fill(volume: diff * 1.5)
            } else {
                result = .level
            }
        }
    }

    func clear() {
        withAnimation {
            cotaTerreno = ""
            cotaProjeto = ""
            result = nil
        }
    }
}

// MARK: - Result Card

struct ResultCardView: View {
    let result: TerrainResult

    var body: some View {
        VStack(spacing: 16) {
            HStack(spacing: 12) {
                Image(systemName: result.icon)
                    .font(.system(size: 28))
                    .foregroundStyle(result.color)
                Text(result.label)
                    .font(.system(size: 28, weight: .bold, design: .rounded))
                    .foregroundStyle(result.color)
            }

            if let vol = result.volume {
                VStack(spacing: 4) {
                    Text("Volume calculado")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(String(format: "%.4f m³/m", vol))
                        .font(.system(size: 22, weight: .semibold, design: .monospaced))
                        .foregroundStyle(.primary)
                }
            } else {
                Text("O terreno está nivelado")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
        }
        .frame(maxWidth: .infinity)
        .padding(24)
        .background(result.color.opacity(0.10))
        .overlay(
            RoundedRectangle(cornerRadius: 16)
                .stroke(result.color.opacity(0.30), lineWidth: 1.5)
        )
        .clipShape(RoundedRectangle(cornerRadius: 16))
    }
}

// MARK: - Input Row

struct InputRow: View {
    let icon: String
    let label: String
    let placeholder: String
    @Binding var text: String
    let isFocused: Bool

    var body: some View {
        HStack(spacing: 14) {
            Image(systemName: icon)
                .font(.system(size: 18))
                .foregroundStyle(isFocused ? .blue : .secondary)
                .frame(width: 28)

            VStack(alignment: .leading, spacing: 2) {
                Text(label)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                TextField(placeholder, text: $text)
                    .keyboardType(.decimalPad)
                    .font(.system(size: 17))
            }
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 14)
        .animation(.easeInOut(duration: 0.15), value: isFocused)
    }
}

#Preview {
    CutFillCalculatorView()
}
