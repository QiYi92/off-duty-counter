import SwiftUI

struct MonthlyRewards: View {
    let snapshot: Snapshot
    let theme: CounterTheme

    private var paydayText: String {
        guard let days = snapshot.daysUntilPayday else { return "发薪日未设置" }
        return days == 0 ? "今天发薪" : "距离发薪日还有 \(days) 天"
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 7) {
            HStack {
                Text("本月收获")
                    .font(.system(size: 15, weight: .semibold))
                    .accessibilityAddTraits(.isHeader)
                Spacer(minLength: 8)
                Text(money(snapshot.monthAmount))
                    .font(.system(size: 19, weight: .bold))
                    .monospacedDigit()
                    .lineLimit(1)
                    .minimumScaleFactor(0.7)
                    .foregroundStyle(theme.accent)
            }
            Text("已过的工作日按满勤计，本月满勤约 \(money(snapshot.monthFullPay)) · \(paydayText)")
                .font(.caption)
                .foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(12)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(theme.cardColor, in: RoundedRectangle(cornerRadius: 14))
    }

    private func money(_ amount: Decimal) -> String {
        amount.formatted(.currency(code: "CNY").locale(Locale(identifier: "zh_CN")))
    }
}
