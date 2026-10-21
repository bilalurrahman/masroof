import SwiftUI

/// An inclusive from/to date range as ISO `yyyy-MM-dd` strings. Empty means "no bound".
struct DateRange: Equatable {
    var from = ""
    var to = ""
    var isActive: Bool { !from.isEmpty || !to.isEmpty }
}

/// A From/To date-range control with quick presets (this month, last 30 days, year-to-date),
/// mirroring the web filter. Owns its own Date state and reports changes as a `DateRange` of
/// Gregorian ISO strings via `onChange`; the parent just reacts.
struct DateRangeFilter: View {
    var onChange: (DateRange) -> Void

    private enum Preset { case month, last30, ytd }

    @State private var from = Date()
    @State private var to = Date()
    /// Whether a range is applied. False = cleared, so the parent falls back to the current month.
    @State private var applied = false
    @State private var active: Preset?

    // Pin to the Gregorian calendar so dates read the same regardless of the device's locale
    // (an ar-SA device would otherwise render the pickers in Hijri).
    private let gregorian = Calendar(identifier: .gregorian)

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(spacing: 8) {
                chip("This month", .month)
                chip("Last 30 days", .last30)
                chip("YTD", .ytd)
                Spacer(minLength: 0)
            }

            HStack(spacing: 8) {
                field("From", selection: $from) { pickFrom($0) }
                Image(systemName: "arrow.right").font(.caption).foregroundStyle(Theme.muted)
                field("To", selection: $to) { pickTo($0) }
                Spacer(minLength: 0)
                if applied {
                    Button { clear() } label: {
                        Label("Clear", systemImage: "xmark")
                            .font(.caption).foregroundStyle(Theme.muted)
                    }
                }
            }
        }
        .padding(14)
        .background(Theme.card)
        .overlay(Rectangle().stroke(Theme.line, lineWidth: 1))
        .padding(.horizontal, 16)
    }

    private func chip(_ label: String, _ preset: Preset) -> some View {
        let on = active == preset
        return Button { apply(preset) } label: {
            Text(label)
                .font(.caption).fontWeight(.semibold)
                .padding(.horizontal, 12).padding(.vertical, 6)
                .background(on ? Theme.credit : Theme.line)
                .foregroundStyle(on ? Color(red: 0.02, green: 0.14, blue: 0.06) : Theme.text)
                .clipShape(Capsule())
        }
        .buttonStyle(.plain)
    }

    private func field(_ label: String, selection: Binding<Date>, onPick: @escaping (Date) -> Void) -> some View {
        VStack(alignment: .leading, spacing: 2) {
            MonoLabel(label)
            DatePicker("", selection: Binding(get: { selection.wrappedValue },
                                              set: { selection.wrappedValue = $0; onPick($0) }),
                       displayedComponents: .date)
                .labelsHidden()
                .datePickerStyle(.compact)
                .environment(\.calendar, gregorian)
                .tint(Theme.credit)
        }
    }

    // MARK: - Actions

    private func apply(_ preset: Preset) {
        let today = Date()
        switch preset {
        case .month:
            from = gregorian.date(from: gregorian.dateComponents([.year, .month], from: today)) ?? today
        case .ytd:
            from = gregorian.date(from: DateComponents(year: gregorian.component(.year, from: today), month: 1, day: 1)) ?? today
        case .last30:
            from = gregorian.date(byAdding: .day, value: -29, to: today) ?? today
        }
        to = today
        active = preset
        applied = true
        emit()
    }

    private func pickFrom(_ d: Date) { active = nil; applied = true; emit() }
    private func pickTo(_ d: Date) { active = nil; applied = true; emit() }

    private func clear() {
        applied = false
        active = nil
        onChange(DateRange())
    }

    private func emit() {
        onChange(DateRange(from: iso(from), to: iso(to)))
    }

    /// Gregorian `yyyy-MM-dd`, independent of the device locale/calendar.
    private func iso(_ d: Date) -> String {
        let c = gregorian.dateComponents([.year, .month, .day], from: d)
        return String(format: "%04d-%02d-%02d", c.year ?? 1970, c.month ?? 1, c.day ?? 1)
    }
}
