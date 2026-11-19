import AppKit
import SwiftUI

@main
enum CounterApp {
    @MainActor static func main() {
        let app = NSApplication.shared
        let controller = Controller()
        app.setActivationPolicy(.regular)
        app.applicationIconImage = DockIcon.make()
        app.delegate = controller
        withExtendedLifetime(controller) { app.run() }
    }
}

@MainActor
final class Controller: NSObject, NSApplicationDelegate, NSWindowDelegate, ObservableObject {
    @Published private(set) var schedule: Schedule?
    @Published private(set) var editing = false
    @Published private(set) var compact = false
    @Published private(set) var pinned = false
    @Published private(set) var visible = false
    @Published private(set) var theme: CounterTheme = .standard

    private let defaults = UserDefaults.standard
    private var item: NSStatusItem!
    private var window: NSWindow!

    func applicationDidFinishLaunching(_ notification: Notification) {
        if let data = defaults.data(forKey: "schedule"),
           let saved = try? JSONDecoder().decode(Schedule.self, from: data), saved.valid {
            schedule = saved
        }
        editing = schedule == nil
        compact = defaults.bool(forKey: "compact")
        pinned = defaults.bool(forKey: "pinned")

        item = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        item.button?.image = NSImage(systemSymbolName: "timer", accessibilityDescription: "牛马计数器")
        item.button?.target = self
        item.button?.action = #selector(toggleWindow(_:))
        item.button?.sendAction(on: [.leftMouseUp, .rightMouseUp])

        window = NSWindow(contentRect: .zero, styleMask: [.titled, .closable, .fullSizeContentView],
                          backing: .buffered, defer: false)
        window.title = "牛马计数器"
        window.titleVisibility = .hidden
        window.titlebarAppearsTransparent = true
        window.isMovableByWindowBackground = true
        window.isReleasedWhenClosed = false
        window.delegate = self
        window.level = pinned ? .floating : .normal
        let content = NSHostingView(rootView: CounterView(model: self))
        content.sizingOptions = []
        window.contentView = content
        if let saved = defaults.string(forKey: "theme"), let theme = CounterTheme(rawValue: saved) {
            self.theme = theme
        }
        applyTheme()
        resize()
        window.center()
        if let saved = defaults.string(forKey: "windowOrigin") {
            let point = NSPointFromString(saved)
            if NSScreen.screens.contains(where: { $0.visibleFrame.contains(point) }) {
                window.setFrameOrigin(point)
            }
        }
        if editing || !defaults.bool(forKey: "hidden") { show() }
    }

    @objc private func toggleWindow(_ sender: Any?) {
        if NSApp.currentEvent?.type == .rightMouseUp, let button = item.button {
            let menu = NSMenu()
            let settings = menu.addItem(withTitle: "设置…", action: #selector(openSettings), keyEquivalent: "")
            settings.target = self
            let quit = menu.addItem(withTitle: "退出牛马计数器", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
            quit.target = NSApp
            menu.popUp(positioning: nil, at: NSPoint(x: 0, y: button.bounds.maxY), in: button)
        } else if window.isVisible { hide() } else { show() }
    }

    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows: Bool) -> Bool {
        show()
        return true
    }

    func show() {
        visible = true
        defaults.set(false, forKey: "hidden")
        NSApp.activate(ignoringOtherApps: true)
        window.makeKeyAndOrderFront(nil)
    }

    func hide() {
        visible = false
        defaults.set(true, forKey: "hidden")
        window.orderOut(nil)
    }

    func windowShouldClose(_ sender: NSWindow) -> Bool {
        hide()
        return false
    }

    func windowDidMove(_ notification: Notification) {
        defaults.set(NSStringFromPoint(window.frame.origin), forKey: "windowOrigin")
    }

    @objc func openSettings() {
        editing = true
        resize()
        show()
    }

    func cancelSettings() {
        editing = false
        resize()
    }

    func save(_ value: Schedule) {
        guard value.valid, let data = try? JSONEncoder().encode(value) else { return }
        defaults.set(data, forKey: "schedule")
        schedule = value
        cancelSettings()
    }

    func toggleCompact() {
        compact.toggle()
        defaults.set(compact, forKey: "compact")
        resize()
    }

    func togglePinned() {
        pinned.toggle()
        defaults.set(pinned, forKey: "pinned")
        window.level = pinned ? .floating : .normal
    }

    func selectTheme(_ theme: CounterTheme) {
        self.theme = theme
        defaults.set(theme.rawValue, forKey: "theme")
        applyTheme()
    }

    private func applyTheme() {
        window.appearance = theme.appearance
        window.backgroundColor = theme.background
    }

    private func resize() {
        let size = editing ? NSSize(width: 420, height: 495)
            : compact ? NSSize(width: 320, height: 112) : NSSize(width: 380, height: 588)
        let top = window.frame.maxY
        window.setContentSize(size)
        var frame = window.frame
        frame.origin.y = top - frame.height
        if let bounds = window.screen?.visibleFrame, window.isVisible {
            frame.origin.x = min(max(frame.minX, bounds.minX), bounds.maxX - frame.width)
            frame.origin.y = min(max(frame.minY, bounds.minY), bounds.maxY - frame.height)
        }
        window.setFrame(frame, display: true)
    }
}

struct CounterView: View {
    @ObservedObject var model: Controller
    @Environment(\.accessibilityReduceMotion) private var reduceMotion

    private static let clockFormatter: DateFormatter = {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "zh_CN")
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.timeZone = .autoupdatingCurrent
        formatter.dateFormat = "yyyy-MM-dd EEEE HH:mm:ss"
        return formatter
    }()

    var body: some View {
        Group {
            if !model.visible {
                Color.clear
            } else if model.editing {
                SettingsView(draft: model.schedule ?? Schedule(), canCancel: model.schedule != nil,
                             save: model.save, cancel: model.cancelSettings)
                    .padding(24)
            } else if let schedule = model.schedule {
                TimelineView(.periodic(from: .now, by: 1)) { _ in
                    let now = Date()
                    let result = schedule.snapshot(at: now)
                    VStack(alignment: .leading, spacing: model.compact ? 5 : 13) {
                        if model.compact {
                            HStack {
                                header(result)
                                Spacer(minLength: 8)
                                windowControls
                            }
                        } else {
                            HStack {
                                Text(Self.clockFormatter.string(from: now))
                                    .font(.caption)
                                    .monospacedDigit()
                                    .foregroundStyle(.secondary)
                                Spacer(minLength: 8)
                                windowControls
                            }
                            header(result)
                        }
                        if !model.compact {
                            Text("今日已赚").font(.subheadline).foregroundStyle(.secondary)
                        }
                        Text(result.amount.formatted(.currency(code: "CNY").locale(Locale(identifier: "zh_CN"))
                            .precision(.fractionLength(5))))
                            .font(.system(size: model.compact ? 27 : 43, weight: .bold, design: .monospaced))
                            .foregroundStyle(model.theme.accent)
                            .lineLimit(1).minimumScaleFactor(0.35)
                            .accessibilityLabel("今日已赚")
                        if !model.compact {
                            if let next = result.next {
                                let seconds = Int(max(0, next.1.timeIntervalSince(now)).rounded(.up))
                                Text("距离\(next.0)还有 \(seconds / 3600) 小时 \(seconds / 60 % 60) 分 \(seconds % 60) 秒")
                                    .monospacedDigit()
                            }
                            VStack(alignment: .leading, spacing: 4) {
                                Text("当前赚钱速度 ¥\(result.rate, format: .number.precision(.fractionLength(5)))/秒 · ¥\(result.rate * 60, format: .number.precision(.fractionLength(2)))/分钟")
                                Text("本日还有 \(result.remaining, format: .number.precision(.fractionLength(2))) 元待领取")
                            }
                            .font(.subheadline)
                            .monospacedDigit()
                            .foregroundStyle(.secondary)
                            VStack(spacing: 5) {
                                HStack {
                                    Text("今日工时进度 \(result.elapsed / max(1, result.planned), format: .percent.precision(.fractionLength(1)))")
                                    Spacer(minLength: 8)
                                    Text("\(result.elapsed / 3600, format: .number.precision(.fractionLength(2))) / \(result.planned / 3600, format: .number.precision(.fractionLength(2))) 小时")
                                }
                                .font(.subheadline)
                                .monospacedDigit()
                                .foregroundStyle(.secondary)
                                ProgressView(value: result.elapsed, total: max(1, result.planned))
                            }
                            Divider()
                            DailyRewards(snapshot: result, theme: model.theme)
                            MonthlyRewards(snapshot: result, theme: model.theme)
                            WorkQuoteCard(theme: model.theme)
                        }
                        Spacer(minLength: 0)
                    }
                    .padding(.horizontal, 22)
                    .padding(.top, model.compact ? 27 : 5)
                    .padding(.bottom, model.compact ? 12 : 8)
                }
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
        .tint(model.theme.accent)
    }

    private func header(_ result: Snapshot) -> some View {
        HStack {
            Text(result.state)
                .font(.system(size: model.compact ? 16 : 23, weight: .semibold))
            if result.state == "上班中" {
                if reduceMotion {
                    cycleIcon
                } else {
                    TimelineView(.animation(minimumInterval: 1.0 / 30)) { context in
                        let phase = context.date.timeIntervalSinceReferenceDate
                            .truncatingRemainder(dividingBy: 3)
                        cycleIcon.rotationEffect(.degrees(phase * 120))
                    }
                }
            } else {
                Image(systemName: "zzz")
                    .foregroundStyle(.secondary)
                    .accessibilityHidden(true)
            }
        }
    }

    private var windowControls: some View {
        HStack(spacing: 10) {
            control(model.pinned ? "pin.fill" : "pin", "切换置顶", model.togglePinned)
            control("gearshape", "设置", model.openSettings)
            themeMenu
            control(model.compact ? "arrow.up.left.and.arrow.down.right" : "arrow.down.right.and.arrow.up.left",
                    model.compact ? "展开" : "折叠", model.toggleCompact)
            control("menubar.rectangle", "收进菜单栏", model.hide)
        }
        .frame(height: 24)
    }

    private var themeMenu: some View {
        Menu {
            Picker("主题", selection: Binding(get: { model.theme }, set: model.selectTheme)) {
                ForEach(CounterTheme.allCases) { theme in
                    Text(theme.name).tag(theme)
                }
            }
            .pickerStyle(.inline)
        } label: {
            Image(systemName: "paintpalette")
        }
        .menuStyle(.borderlessButton)
        .menuIndicator(.hidden)
        .fixedSize()
        .help("切换主题")
        .accessibilityLabel("切换主题")
    }

    private var cycleIcon: some View {
        Image(systemName: "arrow.triangle.2.circlepath")
            .font(.system(size: 14, weight: .semibold))
            .foregroundStyle(.green)
            .frame(width: 20, height: 20)
            .accessibilityHidden(true)
    }

    private func control(_ icon: String, _ label: String, _ action: @escaping () -> Void) -> some View {
        Button(action: action) { Image(systemName: icon) }
            .buttonStyle(.borderless).help(label).accessibilityLabel(label)
    }
}

struct SettingsView: View {
    @State var draft: Schedule
    @State private var restCount = 2
    let canCancel: Bool
    let save: (Schedule) -> Void
    let cancel: () -> Void

    private let days = [(2, "一"), (3, "二"), (4, "三"), (5, "四"), (6, "五"), (7, "六"), (1, "日")]
    private let labels = ["上午上班", "上午下班", "下午上班", "下午下班"]

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text(canCancel ? "计薪设置" : "设置你的工作时间").font(.title2.bold())
            Text("金额按排班估算，设置仅保存在这台 Mac。").foregroundStyle(.secondary)
            TextField("月薪（元）", text: $draft.salary)
                .textFieldStyle(.roundedBorder)
                .accessibilityLabel("月薪（元）")
            Picker("休息安排", selection: $restCount) {
                Text("单休").tag(1)
                Text("双休").tag(2)
            }
            .pickerStyle(.segmented)
            .onChange(of: restCount) { count in
                if draft.rest.count > count {
                    draft.rest = Set(draft.rest.sorted().prefix(count))
                }
            }
            HStack {
                Text("休息日")
                ForEach(days, id: \.0) { day, label in
                    Toggle(label, isOn: Binding(get: { draft.rest.contains(day) }, set: { selected in
                        if !selected { draft.rest.remove(day) }
                        else if draft.rest.count < restCount { draft.rest.insert(day) }
                    }))
                    .toggleStyle(.button)
                    .disabled(!draft.rest.contains(day) && draft.rest.count == restCount)
                }
            }
            Grid(alignment: .leading, horizontalSpacing: 12, verticalSpacing: 8) {
                GridRow { timePicker(0); timePicker(1) }
                GridRow { timePicker(2); timePicker(3) }
            }
            Toggle("手动指定每月计薪天数", isOn: Binding(get: { draft.fixedDays != nil }, set: {
                draft.fixedDays = $0 ? 22 : nil
            }))
            if let fixed = draft.fixedDays {
                Stepper("计薪天数：\(fixed) 天", value: Binding(get: { draft.fixedDays ?? 22 },
                        set: { draft.fixedDays = $0 }), in: 1...31)
            } else {
                Text("按当月日历和休息日自动计算").foregroundStyle(.secondary)
            }
            Picker("每月发薪日", selection: Binding(get: { draft.payday ?? 0 }, set: {
                draft.payday = $0 == 0 ? nil : $0
            })) {
                Text("未设置").tag(0)
                ForEach(1...31, id: \.self) { day in
                    Text("每月 \(day) 日").tag(day)
                }
            }
            .help("月份天数不足时，按当月最后一天计算")
            if !draft.valid || draft.rest.count != restCount {
                Text("请填写有效月薪，选满休息日，并确保上下班时间有序且不重叠。")
                    .font(.caption).foregroundStyle(.secondary)
            }
            Spacer(minLength: 0)
            HStack {
                Button("退出应用") { NSApp.terminate(nil) }
                Spacer()
                if canCancel { Button("取消", action: cancel).keyboardShortcut(.cancelAction) }
                Button("保存") { save(draft) }
                    .keyboardShortcut(.defaultAction)
                    .disabled(!draft.valid || draft.rest.count != restCount)
            }
        }
        .onAppear { restCount = draft.rest.count }
    }

    private func timePicker(_ index: Int) -> some View {
        DatePicker(labels[index], selection: Binding(get: {
            let minute = draft.times[index]
            return Calendar.current.date(from: DateComponents(year: 2001, month: 1, day: 15,
                hour: minute / 60, minute: minute % 60)) ?? .now
        }, set: {
            let parts = Calendar.current.dateComponents([.hour, .minute], from: $0)
            draft.times[index] = (parts.hour ?? 0) * 60 + (parts.minute ?? 0)
        }), displayedComponents: .hourAndMinute)
    }
}
