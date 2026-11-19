import Foundation

struct Schedule: Codable {
    var salary = "8000"
    var rest = Set([1, 7])
    var times = [540, 720, 780, 1080]
    var fixedDays: Int? = nil
    var payday: Int? = nil

    var pay: Decimal? { Decimal(string: salary, locale: .current) }

    var valid: Bool {
        salary.range(of: #"^[0-9]+([.,][0-9]{1,2})?$"#, options: .regularExpression) != nil
        && salary.filter({ $0 == "." || $0 == "," }).allSatisfy { String($0) == Locale.current.decimalSeparator }
        && pay.map { $0 > 0 } == true
        && (1...2).contains(rest.count) && rest.allSatisfy { (1...7).contains($0) }
        && times.count == 4 && times.allSatisfy { (0..<1440).contains($0) }
        && times[0] < times[1] && times[1] <= times[2] && times[2] < times[3]
        && fixedDays.map { (1...31).contains($0) } != false
        && payday.map { (1...31).contains($0) } != false
    }

    func snapshot(at now: Date, calendar: Calendar = .current) -> Snapshot {
        let day = calendar.startOfDay(for: now)
        let boundaries = times.map {
            calendar.date(bySettingHour: $0 / 60, minute: $0 % 60, second: 0, of: day) ?? day
        }
        let intervals = [(boundaries[0], boundaries[1]), (boundaries[2], boundaries[3])]
        let planned = intervals.reduce(0.0) { $0 + max(0, $1.1.timeIntervalSince($1.0)) }
        let resting = rest.contains(calendar.component(.weekday, from: now))
        let elapsed = resting ? 0 : intervals.reduce(0.0) {
            $0 + max(0, min(now, $1.1).timeIntervalSince($1.0))
        }
        let month = calendar.dateInterval(of: .month, for: now)
        var count = 0
        var completed = 0
        if let month {
            var date = month.start
            while date < month.end {
                if !rest.contains(calendar.component(.weekday, from: date)) {
                    count += 1
                    if date < day { completed += 1 }
                }
                guard let next = calendar.date(byAdding: .day, value: 1, to: date), next > date else { break }
                date = next
            }
        }
        let dayPay = (pay ?? 0) / Decimal(max(1, fixedDays ?? count))
        let amount = planned > 0 ? dayPay * (Decimal(elapsed.rounded(.down)) / Decimal(planned)) : 0
        let state: String
        let next: (String, Date)?
        switch now {
        case _ where resting: (state, next) = ("休息日", nil)
        case ..<boundaries[0]: (state, next) = ("未上班", ("上班", boundaries[0]))
        case ..<boundaries[1]: (state, next) = ("上班中", ("午休", boundaries[1]))
        case ..<boundaries[2]: (state, next) = ("午休中", ("上班", boundaries[2]))
        case ..<boundaries[3]: (state, next) = ("上班中", ("下班", boundaries[3]))
        default: (state, next) = ("已下班", nil)
        }
        let working = intervals.contains { now >= $0.0 && now < $0.1 }
        let baseRate = planned > 0 ? dayPay / Decimal(planned) : 0
        let rate = !resting && working ? baseRate : 0
        let remaining = resting ? 0 : max(0, dayPay - amount)
        return Snapshot(state: state, next: next, amount: amount, rate: rate, baseRate: baseRate,
                        remaining: remaining, elapsed: elapsed, planned: planned,
                        monthAmount: dayPay * Decimal(completed) + amount,
                        monthFullPay: dayPay * Decimal(count),
                        daysUntilPayday: daysUntilPayday(from: day, calendar: calendar))
    }
    private func daysUntilPayday(from day: Date, calendar: Calendar) -> Int? {
        guard let payday, let month = calendar.dateInterval(of: .month, for: day) else { return nil }
        for monthStart in [month.start, month.end] {
            guard let days = calendar.range(of: .day, in: .month, for: monthStart),
                  let target = calendar.date(byAdding: .day, value: min(payday, days.count) - 1, to: monthStart),
                  target >= day else { continue }
            return calendar.dateComponents([.day], from: day, to: target).day
        }
        return nil
    }

}

struct Snapshot {
    let state: String
    let next: (String, Date)?
    let amount: Decimal
    let rate: Decimal
    let baseRate: Decimal
    let remaining: Decimal
    let elapsed: Double
    let planned: Double
    let monthAmount: Decimal
    let monthFullPay: Decimal
    let daysUntilPayday: Int?
}
