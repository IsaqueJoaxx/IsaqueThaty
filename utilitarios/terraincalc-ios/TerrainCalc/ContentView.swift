import SwiftUI

struct ContentView: View {
    var body: some View {
        TabView {
            CutFillCalculatorView()
                .tabItem {
                    Label("Corte/Aterro", systemImage: "arrow.up.arrow.down.circle.fill")
                }

            SlopeCalculatorView()
                .tabItem {
                    Label("Inclinação", systemImage: "angle")
                }
        }
        .accentColor(.blue)
    }
}

#Preview {
    ContentView()
}
