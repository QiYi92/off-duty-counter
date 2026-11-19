import AppKit

enum DockIcon {
    static func make() -> NSImage {
        NSImage(size: NSSize(width: 128, height: 128), flipped: false) { _ in
            let tile = NSBezierPath(
                roundedRect: NSRect(x: 8, y: 8, width: 112, height: 112),
                xRadius: 25, yRadius: 25
            )
            NSGradient(
                starting: NSColor(srgbRed: 0.12, green: 0.41, blue: 0.34, alpha: 1),
                ending: NSColor(srgbRed: 0.04, green: 0.19, blue: 0.18, alpha: 1)
            )?.draw(in: tile, angle: -65)

            let cream = NSColor(srgbRed: 1, green: 0.93, blue: 0.75, alpha: 1)
            cream.setStroke()
            let ring = NSBezierPath(ovalIn: NSRect(x: 32, y: 27, width: 64, height: 64))
            ring.lineWidth = 5
            ring.stroke()

            let crown = NSBezierPath()
            crown.move(to: NSPoint(x: 64, y: 91))
            crown.line(to: NSPoint(x: 64, y: 100))
            crown.move(to: NSPoint(x: 56, y: 100))
            crown.line(to: NSPoint(x: 72, y: 100))
            crown.lineWidth = 5
            crown.lineCapStyle = .round
            crown.stroke()

            let symbol = NSAttributedString(string: "¥", attributes: [
                .font: NSFont.systemFont(ofSize: 41, weight: .bold),
                .foregroundColor: cream
            ])
            let size = symbol.size()
            symbol.draw(at: NSPoint(x: 64 - size.width / 2, y: 59 - size.height / 2))
            return true
        }
    }
}
