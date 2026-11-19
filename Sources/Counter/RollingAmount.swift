import SwiftUI

struct RollingAmount: View {
    let amount: Decimal
    let size: CGFloat
    let color: Color
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    var body: some View {
        let value = amount.formatted(.currency(code: "CNY").locale(Locale(identifier: "zh_CN"))
            .precision(.fractionLength(5)))
        let symbols = Array(value.reversed())
        GeometryReader { geometry in
            let fontSize = min(size, geometry.size.width / (CGFloat(symbols.count) * 0.64))
            let cellHeight = size * 1.2
            HStack(spacing: 0) {
                ForEach(Array(symbols.indices.reversed()), id: \.self) { index in
                    let symbol = symbols[index]
                    ZStack {
                        Text(String(symbol))
                            .font(.system(size: fontSize, weight: .bold, design: .monospaced))
                            .id(symbol)
                            .transition(symbol.isNumber && !reduceMotion ? .asymmetric(
                                insertion: .offset(y: cellHeight),
                                removal: .offset(y: -cellHeight)
                            ) : .identity)
                    }
                    .frame(width: fontSize * 0.64, height: cellHeight)
                    .clipped()
                }
            }
            .foregroundStyle(color)
            .animation(reduceMotion ? nil : .easeOut(duration: 0.24), value: value)
        }
        .frame(height: size * 1.2)
        .accessibilityElement(children: .ignore)
        .accessibilityLabel("今日已赚")
        .accessibilityValue(value)
    }
}
