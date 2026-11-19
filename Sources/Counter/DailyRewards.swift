import SwiftUI

struct DailyRewards: View {
    let snapshot: Snapshot
    let theme: CounterTheme

    private struct Reward {
        let price: Int
        let name: String
        let icon: String
    }

    private static let rewards: [Reward] = [
        .init(price: 3, name: "一瓶可乐", icon: "waterbottle"),
        .init(price: 10, name: "一杯瑞幸", icon: "cup.and.saucer"),
        .init(price: 20, name: "一餐外卖", icon: "takeoutbag.and.cup.and.straw"),
        .init(price: 50, name: "一份KFC疯狂星期四", icon: "fork.knife"),
        .init(price: 99, name: "一件T恤", icon: "tshirt"),
        .init(price: 150, name: "一顿火锅", icon: "flame"),
        .init(price: 200, name: "一箱 92#汽油", icon: "fuelpump"),
        .init(price: 300, name: "一张远门动车票", icon: "tram"),
        .init(price: 400, name: "一张特价机票", icon: "airplane")
    ]

    private var reward: Reward? {
        Self.rewards.last { snapshot.amount >= Decimal($0.price) }
    }

    var body: some View {
        let name = reward?.name ?? "\(money(snapshot.amount))元"
        let highlight = Text(name).fontWeight(.semibold).foregroundColor(theme.accent)

        VStack(alignment: .leading, spacing: 8) {
            HStack(spacing: 12) {
                VStack(alignment: .leading, spacing: 5) {
                    Text("本日收获")
                        .font(.system(size: 15, weight: .semibold))
                        .accessibilityAddTraits(.isHeader)
                    Text("今天已经赚到\(highlight)啦，继续加油")
                        .font(.subheadline)
                        .fixedSize(horizontal: false, vertical: true)
                }
                Spacer(minLength: 0)
                Image(systemName: reward?.icon ?? "sparkles")
                    .font(.system(size: 21, weight: .medium))
                    .foregroundStyle(theme.accent)
                    .frame(width: 42, height: 42)
                    .background(theme.accent.opacity(0.12), in: RoundedRectangle(cornerRadius: 11))
                    .accessibilityHidden(true)
            }
            Text("时薪 \(money(snapshot.baseRate * 3600))元 · 分薪 \(money(snapshot.baseRate * 60))元 · 带薪喝水 \(money(snapshot.baseRate * 300))元/次 · 带薪拉屎 \(money(snapshot.baseRate * 1800))元/次")
                .font(.caption)
                .monospacedDigit()
                .foregroundStyle(.secondary)
                .lineLimit(1)
                .minimumScaleFactor(0.7)
        }
        .padding(12)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(theme.cardColor, in: RoundedRectangle(cornerRadius: 14))
    }

    private func money(_ amount: Decimal) -> String {
        amount.formatted(.number.precision(.fractionLength(2)))
    }
}
