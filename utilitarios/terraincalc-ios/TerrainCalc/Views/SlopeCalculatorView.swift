import SwiftUI

enum SlopeDirection: String, CaseIterable {
    case uphill = "Subida"
    case downhill = "Descida"

    var icon: String {
        switch self {
        case .uphill:   return "arrow.up.right"
        case .downhill: return "arrow.down.right"
        }
    }

    var color: Color {
        switch self {
        case .uphill:   return Color(red: 0.20, green: 0.55, blue: 0.95)
        case .downhill: return Color(red: 0.95, green: 0.45, blue: 0.10)
        }
    }
}

struct SlopeCalculatorView: View {
    @State private var distance: String = ""
    @State private var percentage: String = ""
    @State private var cotaInicial: String = ""
    @State private var direction: SlopeDirection = .uphill
    @State private var cotaFinal: Double? = nil
    @FocusState private var focusedField: SlopeField?

    enum SlopeField { case distance, percentage, cotaInicial }

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 28) {

                    // Header
                    VStack(spacing: 6) {
                        Image(systemName: "angle")
                            .font(.system(size: 48))
                            .foregroundStyle(.blue)
                        Text("Calculadora de Inclinação")
                            .font(.title2).bold()
                            .multilineTextAlignment(.center)
                        Text("Calcule a cota final a partir de uma inclinação percentual")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                            .multilineTextAlignment(.center)
                    }
                    .padding(.top, 8)

                    // Direction picker
                    VStack(alignment: .leading, spacing: 10) {
                        Label("Direção", systemImage: "arrow.left.arrow.right")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        HStack(spacing: 0) {
                            ForEach(SlopeDirection.allCases, id: \.self) { dir in
                                Button(action: {
                                    withAnimation(.spring(response: 0.3)) {
                                        direction = dir
                                        cotaFinal = nil
                                    }
                                }) {
                                    HStack(spacing: 8) {
                                        Image(systemName: dir.icon)
                                        Text(dir.rawValue)
                                            .fontWeight(.semibold)
                                    }
                                    .frame(maxWidth: .infinity)
                                    .frame(height: 44)
                                    .background(direction == dir ? dir.color : Color(.systemGray5))
                                    .foregroundStyle(direction == dir ? .white : .primary)
                                }
                            }
                        }
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                        .overlay(
                            RoundedRectangle(cornerRadius: 12)
                                .stroke(Color(.systemGray4), lineWidth: 1)
                        )
                    }

                    // Inputs
                    VStack(spacing: 0) {
                        SlopeInputRow(
                            icon: "ruler",
                            label: "Distância (m)",
                            placeholder: "Ex: 50.00",
                            text: $distance,
                            isFocused: focusedField == .distance
                        )
                        .focused($focusedField, equals: .distance)

                        Divider().padding(.horizontal)

                        SlopeInputRow(
                            icon: "percent",
                            label: "Inclinação (%)",
                            placeholder: "Ex: 8.50",
                            text: $percentage,
                            isFocused: focusedField == .percentage
                        )
                        .focused($focusedField, equals: .percentage)

                        Divider().padding(.horizontal)

                        SlopeInputRow(
                            icon: "arrow.down.to.line.compact",
                            label: "Cota Inicial (m)",
                            placeholder: "Ex: 100.00",
                            text: $cotaInicial,
                            isFocused: focusedField == .cotaInicial
                        )
                        .focused($focusedField, equals: .cotaInicial)
                    }
                    .background(Color(.secondarySystemBackground))
                    .clipShape(RoundedRectangle(cornerRadius: 16))
                    .shadow(color: .black.opacity(0.06), radius: 8, x: 0, y: 2)

                    // Calculate button
                    Button(action: calculate) {
                        HStack(spacing: 10) {
                            Image(systemName: "equal.circle.fill")
                            Text("Calcular Cota Final")
                                .fontWeight(.semibold)
                        }
                        .frame(maxWidth: .infinity)
                        .frame(height: 52)
                        .background(canCalculate ? direction.color : Color(.systemGray4))
                        .foregroundStyle(.white)
                        .clipShape(RoundedRectangle(cornerRadius: 14))
                    }
                    .disabled(!canCalculate)
                    .animation(.easeInOut(duration: 0.2), value: canCalculate)

                    // Result
                    if let cota = cotaFinal {
                        SlopeResultView(cotaFinal: cota, direction: direction)
                            .transition(.scale(scale: 0.92).combined(with: .opacity))
                    }

                    // Clear
                    if cotaFinal != nil || !distance.isEmpty || !percentage.isEmpty || !cotaInicial.isEmpty {
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
            .navigationTitle("Inclinação")
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
        let d = Double(distance.replacingOccurrences(of: ",", with: "."))
        let p = Double(percentage.replacingOccurrences(of: ",", with: "."))
        let c = Double(cotaInicial.replacingOccurrences(of: ",", with: "."))
        return d != nil && p != nil && c != nil
    }

    func calculate() {
        guard
            let d = Double(distance.replacingOccurrences(of: ",", with: ".")),
            let p = Double(percentage.replacingOccurrences(of: ",", with: ".")),
            let ci = Double(cotaInicial.replacingOccurrences(of: ",", with: "."))
        else { return }

        focusedField = nil
        let delta = d * p / 100.0
        withAnimation(.spring(response: 0.4, dampingFraction: 0.75)) {
            switch direction {
            case .uphill:   cotaFinal = ci + delta
            case .downhill: cotaFinal = ci - delta
            }
        }
    }

    func clear() {
        withAnimation {
            distance = ""
            percentage = ""
            cotaInicial = ""
            cotaFinal = nil
        }
    }
}

// MARK: - Slope Result View

struct SlopeResultView: View {
    let cotaFinal: Double
    let direction: SlopeDirection

    var body: some View {
        VStack(spacing: 16) {
            HStack(spacing: 10) {
                Image(systemName: direction.icon)
                    .font(.system(size: 22))
                    .foregroundStyle(direction.color)
                Text(direction.rawValue)
                    .font(.headline)
                    .foregroundStyle(direction.color)
            }

            VStack(spacing: 4) {
                Text("Cota Final Calculada")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                Text(String(format: "%.4f m", cotaFinal))
                    .font(.system(size: 26, weight: .bold, design: .monospaced))
                    .foregroundStyle(.primary)
            }
        }
        .frame(maxWidth: .infinity)
        .padding(24)
        .background(direction.color.opacity(0.10))
        .overlay(
            RoundedRectangle(cornerRadius: 16)
                .stroke(direction.color.opacity(0.30), lineWidth: 1.5)
        )
        .clipShape(RoundedRectangle(cornerRadius: 16))
    }
}

// MARK: - Slope Input Row

struct SlopeInputRow: View {
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
    SlopeCalculatorView()
}
