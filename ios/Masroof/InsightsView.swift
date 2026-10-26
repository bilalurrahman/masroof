import SwiftUI

@MainActor
final class InsightsModel: ObservableObject {
    @Published var summary: MonthlySummary?
    @Published var banks: [BankTotal] = []
    @Published var loading = false
    @Published var error: String?
    @Published var range = DateRange()
    let month = currentMonthKey()

    func apply(_ range: DateRange) async {
        self.range = range
        await load()
    }

    func load() async {
        loading = true; error = nil
        defer { loading = false }
        do {
            if range.isActive {
                summary = try await ApiClient.shared.summary(from: range.from, to: range.to)
                banks = try await ApiClient.shared.byBank(from: range.from, to: range.to)
            } else {
                summary = try await ApiClient.shared.summary(month: month)
                banks = try await ApiClient.shared.byBank(month: month)
            }
        }
        catch { self.error = (error as? ApiError)?.errorDescription ?? error.localizedDescription }
    }
}

struct InsightsView: View {
    @StateObject private var model = InsightsModel()

    var body: some View {
        NavigationStack {
            ZStack {
                Theme.bg.ignoresSafeArea()
                ScrollView {
                    VStack(alignment: .leading, spacing: 16) {
                        DateRangeFilter { range in Task { await model.apply(range) } }
                            .padding(.horizontal, -16)
                        if let s = model.summary {
                            MonoLabel("◆ \(model.range.isActive ? "\(model.range.from) → \(model.range.to)" : model.month)")
                            totals(s)
                            if !model.banks.isEmpty {
                                bankSection
                            }
                            MonoLabel("◆ Spending by category")
                            categoryBars(s)
                        } else if model.loading {
                            ProgressView().tint(Theme.accent).frame(maxWidth: .infinity).padding(.top, 60)
                        } else if let error = model.error {
                            Text(error).foregroundStyle(Theme.debit)
                        }
                    }
                    .padding(16)
                }
                .refreshable { await model.load() }
            }
            .navigationTitle("Insights")
        }
        .task { await model.load() }
    }

    @ViewBuilder private var bankSection: some View {
        let maxDebit = max(1, model.banks.map(\.debit).max() ?? 1)
        let grand = model.banks.reduce(0) { $0 + $1.debit }
        Panel {
            VStack(alignment: .leading, spacing: 14) {
                HStack {
                    MonoLabel("◆ Bank accounts")
                    Spacer()
                    Text(grand.sar())
                        .font(.system(.subheadline, design: .monospaced)).fontWeight(.semibold)
                        .foregroundStyle(Theme.debit)
                }
                ForEach(model.banks) { b in
                    VStack(alignment: .leading, spacing: 5) {
                        HStack(spacing: 8) {
                            RoundedRectangle(cornerRadius: 2)
                                .fill(BankGroup.color(b.bankCode))
                                .frame(width: 9, height: 9)
                            Text(b.bankName).font(.subheadline).foregroundStyle(Theme.text)
                            Text("\(b.count) TXN").font(.system(.caption2, design: .monospaced)).foregroundStyle(Theme.muted)
                            Spacer()
                            Text("−\(b.debit.decimal())").font(.system(.caption, design: .monospaced)).foregroundStyle(Theme.debit)
                            if b.credit > 0 {
                                Text("+\(b.credit.decimal())").font(.system(.caption, design: .monospaced)).foregroundStyle(Theme.credit)
                            }
                        }
                        GeometryReader { geo in
                            ZStack(alignment: .leading) {
                                Capsule().fill(Theme.line).frame(height: 6)
                                Capsule().fill(BankGroup.color(b.bankCode))
                                    .frame(width: max(2, geo.size.width * (b.debit / maxDebit)), height: 6)
                            }
                        }
                        .frame(height: 6)
                    }
                }
            }
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }

    private func totals(_ s: MonthlySummary) -> some View {
        HStack(spacing: 12) {
            stat("Spent", s.totalDebit, Theme.debit)
            stat("Received", s.totalCredit, Theme.credit)
        }
    }

    private func stat(_ label: String, _ value: Double, _ color: Color) -> some View {
        Panel {
            VStack(alignment: .leading, spacing: 6) {
                MonoLabel(label)
                Text(value.sar())
                    .font(.system(.title3, design: .monospaced)).fontWeight(.bold)
                    .foregroundStyle(color)
                    .minimumScaleFactor(0.6).lineLimit(1)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }

    @ViewBuilder private func categoryBars(_ s: MonthlySummary) -> some View {
        let maxTotal = s.byCategory.map(\.total).max() ?? 1
        VStack(spacing: 10) {
            ForEach(s.byCategory) { c in
                VStack(alignment: .leading, spacing: 4) {
                    HStack {
                        Text(c.categoryName).font(.subheadline).foregroundStyle(Theme.text)
                        Spacer()
                        Text(c.total.sar()).font(.system(.caption, design: .monospaced)).foregroundStyle(Theme.muted)
                    }
                    GeometryReader { geo in
                        ZStack(alignment: .leading) {
                            Rectangle().fill(Theme.line).frame(height: 6)
                            Rectangle().fill(Theme.accent)
                                .frame(width: max(2, geo.size.width * (c.total / maxTotal)), height: 6)
                        }
                    }
                    .frame(height: 6)
                }
            }
        }
    }
}
