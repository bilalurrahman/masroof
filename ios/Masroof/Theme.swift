import SwiftUI

/// Masroof's "design-engineered" look: near-black canvas, hairline borders, sharp corners,
/// monospace uppercase micro-labels.
enum Theme {
    static let bg = Color(red: 0.035, green: 0.035, blue: 0.045)
    static let card = Color(red: 0.075, green: 0.078, blue: 0.09)
    static let line = Color.white.opacity(0.09)
    static let text = Color(white: 0.95)
    static let muted = Color(white: 0.55)
    static let credit = Color(red: 0.49, green: 0.85, blue: 0.55)
    static let debit = Color(red: 0.96, green: 0.47, blue: 0.42)
    static let accent = Color(red: 0.55, green: 0.80, blue: 0.95)
}

extension Double {
    /// "SAR 1,234.50"
    func sar() -> String {
        let f = NumberFormatter()
        f.numberStyle = .decimal
        f.minimumFractionDigits = 2
        f.maximumFractionDigits = 2
        let n = f.string(from: NSNumber(value: self)) ?? String(format: "%.2f", self)
        return "SAR \(n)"
    }
}

struct MonoLabel: View {
    let text: String
    init(_ text: String) { self.text = text }
    var body: some View {
        Text(text.uppercased())
            .font(.system(.caption2, design: .monospaced))
            .tracking(1.5)
            .foregroundStyle(Theme.muted)
    }
}

/// A hairline-bordered card container.
struct Panel<Content: View>: View {
    @ViewBuilder var content: Content
    var body: some View {
        content
            .padding(16)
            .background(Theme.card)
            .overlay(Rectangle().stroke(Theme.line, lineWidth: 1))
    }
}

func currentMonthKey(_ date: Date = Date()) -> String {
    let f = DateFormatter()
    f.dateFormat = "yyyy-MM"
    return f.string(from: date)
}
