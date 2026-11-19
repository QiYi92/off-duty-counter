import AppKit
import SwiftUI

enum CounterTheme: String, CaseIterable, Identifiable {
    case standard, system, night
    case ocean, forest, mint, amber, rose, lavender, slate, cocoa

    var id: String { rawValue }

    var name: String {
        switch self {
        case .standard: return "默认"
        case .system: return "跟随系统"
        case .night: return "黑夜"
        case .ocean: return "深海蓝"
        case .forest: return "松林绿"
        case .mint: return "薄荷青"
        case .amber: return "琥珀橙"
        case .rose: return "蔷薇红"
        case .lavender: return "薰衣草紫"
        case .slate: return "雾岩灰"
        case .cocoa: return "可可棕"
        }
    }

    var appearance: NSAppearance? {
        switch self {
        case .system: return nil
        case .night: return NSAppearance(named: .darkAqua)
        default: return NSAppearance(named: .aqua)
        }
    }

    private var palette: (accent: UInt32, background: UInt32)? {
        switch self {
        case .standard, .system, .night: return nil
        case .ocean: return (0x1D568E, 0xEFF6FF)
        case .forest: return (0x2D6246, 0xEFF6EC)
        case .mint: return (0x006B60, 0xEBF9F4)
        case .amber: return (0x984B09, 0xFFF4E2)
        case .rose: return (0xA63457, 0xFFF0F3)
        case .lavender: return (0x6543A5, 0xF5EFFF)
        case .slate: return (0x495768, 0xEEF2F5)
        case .cocoa: return (0x78513D, 0xFAF1E9)
        }
    }

    var accent: Color {
        if self == .night { return Color(nsColor: Self.color(0x88ABFF)) }
        return palette.map { Color(nsColor: Self.color($0.accent)) } ?? .accentColor
    }

    var cardColor: Color {
        palette == nil ? Color.primary.opacity(0.04) : accent.opacity(0.07)
    }

    var background: NSColor {
        palette.map { Self.color($0.background) } ?? .windowBackgroundColor
    }

    private static func color(_ rgb: UInt32) -> NSColor {
        NSColor(srgbRed: CGFloat((rgb >> 16) & 255) / 255,
                green: CGFloat((rgb >> 8) & 255) / 255,
                blue: CGFloat(rgb & 255) / 255, alpha: 1)
    }
}
