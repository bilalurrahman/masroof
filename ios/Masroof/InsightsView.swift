import SwiftUI

@MainActor
final class InsightsModel: ObservableObject {
    @Published var summary: MonthlySummary?
    @Published var loading = false
    @Published var error: String?
    let month = currentMonthKey()

    func load() async {
        loading = true; error = nil
        defer { loading = false }
        do { summary = try await ApiClient.shared.summary(month: month) }
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
                        if let s = model.summary {
                            totals(s)
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
