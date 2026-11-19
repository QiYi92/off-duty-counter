import SwiftUI

struct WorkQuoteCard: View {
    let theme: CounterTheme
    @State private var index = 0

    // The source links for these online sayings are listed in the README.
    private static let quotes = [
        "早安，打工人！ (๑•̀ㅂ•́)و✧",
        "打工是不可能打工的。 ┐(￣ヘ￣)┌",
        "只有‘摸鱼’得来的工资，才算是意外之财。 (✧ω✧)",
        "打工人，打工魂，打工都是人上人！ ᕦ(ò_óˇ)ᕤ",
        "认认真真上班不叫赚钱，摸鱼才是。 (￣▽￣)ノ",
        "老板假装给我们发工资，我们假装给老板干活。 (¬‿¬)",
        "带薪摸鱼，快乐工作。 ヽ(・∀・)ﾉ"
    ]

    var body: some View {
        Button(action: next) {
            HStack(spacing: 10) {
                Image(systemName: "quote.opening")
                    .foregroundStyle(theme.accent)
                    .accessibilityHidden(true)
                Text(Self.quotes[index])
                    .font(.subheadline)
                    .foregroundStyle(.primary)
                    .fixedSize(horizontal: false, vertical: true)
                Spacer(minLength: 4)
                Image(systemName: "arrow.clockwise")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .accessibilityHidden(true)
            }
            .frame(minHeight: 24)
            .padding(12)
            .background(theme.cardColor, in: RoundedRectangle(cornerRadius: 14))
            .contentShape(RoundedRectangle(cornerRadius: 14))
        }
        .buttonStyle(.plain)
        .help("点击换一句，每 5 分钟自动切换")
        .accessibilityHint("点击切换下一句")
        .task(id: index) {
            do {
                try await Task.sleep(for: .seconds(300))
                try Task.checkCancellation()
                next()
            } catch is CancellationError {
                // The card disappeared or the user already advanced it.
            } catch {
                return
            }
        }
    }

    private func next() {
        index = (index + 1) % Self.quotes.count
    }
}
