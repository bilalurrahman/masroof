import SwiftUI

@MainActor
final class LedgerModel: ObservableObject {
    @Published var items: [Transaction] = []
    @Published var total = 0
    @Published var loading = false
    @Published var syncing = false
    @Published var banner: String?
    @Published var error: String?
    @Published var range = DateRange()
    @Published var bank = ""   // "" = all banks

    let month = currentMonthKey()

    /// What the header shows for the active period: the picked range, else the current month.
    var periodLabel: String {
        range.isActive ? "\(range.from) → \(range.to)" : month
    }

    func apply(_ range: DateRange) async {
        self.range = range
        await load()
    }

    func toggleBank(_ code: String) async {
        bank = (bank == code) ? "" : code
        await load()
    }

    func load() async {
        loading = true; error = nil
        defer { loading = false }
        do {
            let bankParam = bank.isEmpty ? nil : bank
            let page = range.isActive
                ? try await ApiClient.shared.ledger(from: range.from, to: range.to, bank: bankParam, pageSize: 200)
                : try await ApiClient.shared.ledger(month: month, bank: bankParam, pageSize: 200)
            items = page.items
            total = page.total
        } catch {
            self.error = (error as? ApiError)?.errorDescription ?? error.localizedDescription
        }
    }

    func sync() async {
        guard !syncing else { return }
        syncing = true; error = nil
        defer { syncing = false }
        do {
            let r = try await ApiClient.shared.syncSms()
            banner = "Synced — \(r.imported) new, \(r.duplicates) already there"
            await load()
        } catch {
            self.error = (error as? ApiError)?.errorDescription ?? error.localizedDescription
        }
    }
}

struct LedgerView: View {
    @StateObject private var model = LedgerModel()

    var body: some View {
        NavigationStack {
            ZStack {
                Theme.bg.ignoresSafeArea()
                content
            }
            .navigationTitle("Ledger")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button { Task { await model.sync() } } label: {
                        if model.syncing {
                            ProgressView().tint(Theme.accent)
                        } else {
                            Label("Sync", systemImage: "arrow.triangle.2.circlepath")
                        }
                    }
                    .disabled(model.syncing)
                }
            }
        }
        .task { await model.load() }
    }

    @ViewBuilder private var content: some View {
        VStack(spacing: 0) {
            DateRangeFilter { range in Task { await model.apply(range) } }
                .padding(.top, 4).padding(.bottom, 8)
            bankChips
            header
            if let banner = model.banner {
                Text(banner)
                    .font(.footnote)
                    .foregroundStyle(Theme.credit)
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(.horizontal, 16).padding(.vertical, 8)
            }
            if let error = model.error {
                Text(error)
                    .font(.footnote)
                    .foregroundStyle(Theme.debit)
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(.horizontal, 16).padding(.vertical, 8)
            }
            if model.loading && model.items.isEmpty {
                Spacer(); ProgressView().tint(Theme.accent); Spacer()
            } else if model.items.isEmpty {
                Spacer()
                VStack(spacing: 8) {
                    Image(systemName: "tray").font(.largeTitle).foregroundStyle(Theme.muted)
                    Text("No transactions this month").foregroundStyle(Theme.muted)
                    Text("Tap Sync to import your bank SMS").font(.caption).foregroundStyle(Theme.muted)
                }
                Spacer()
            } else {
                ScrollView {
                    LazyVStack(spacing: 0) {
                        ForEach(model.items) { txn in
                            TransactionRow(txn: txn)
                            Divider().overlay(Theme.line)
                        }
                    }
                }
                .refreshable { await model.load() }
            }
        }
    }

    private var bankChips: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                bankChip(code: "", label: "All banks")
                ForEach(BankGroup.members) { m in
                    bankChip(code: m.code, label: m.name, color: m.color)
                }
            }
            .padding(.horizontal, 16)
        }
        .padding(.bottom, 8)
    }

    private func bankChip(code: String, label: String, color: Color? = nil) -> some View {
        let on = model.bank == code
        return Button { Task { await model.toggleBank(code) } } label: {
            HStack(spacing: 6) {
                if let color {
                    Circle().fill(color).frame(width: 7, height: 7)
                }
                Text(label).font(.caption).fontWeight(.semibold)
            }
            .padding(.horizontal, 12).padding(.vertical, 7)
            .background(on ? Theme.accent : Theme.line)
            .foregroundStyle(on ? Color(red: 0.02, green: 0.09, blue: 0.16) : Theme.text)
            .clipShape(Capsule())
        }
        .buttonStyle(.plain)
    }

    private var header: some View {
        HStack {
            MonoLabel("◆ \(model.periodLabel) · \(model.total) rows")
            Spacer()
        }
        .padding(.horizontal, 16).padding(.bottom, 10)
    }
}

struct TransactionRow: View {
    let txn: Transaction
    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            VStack(alignment: .leading, spacing: 3) {
                Text(txn.counterparty ?? "—")
                    .font(.system(.body, design: .default)).fontWeight(.medium)
                    .foregroundStyle(Theme.text)
                HStack(spacing: 8) {
                    Text(txn.txnDate).font(.caption).foregroundStyle(Theme.muted)
                    Text(txn.category.name)
                        .font(.system(.caption2, design: .monospaced))
                        .padding(.horizontal, 6).padding(.vertical, 2)
                        .background(Theme.line)
                    if let ch = txn.channel {
                        Text(ch.uppercased()).font(.system(.caption2, design: .monospaced)).foregroundStyle(Theme.muted)
                    }
                }
            }
            Spacer()
            Text((txn.isDebit ? "−" : "+") + txn.amount.sar())
                .font(.system(.body, design: .monospaced)).fontWeight(.semibold)
                .foregroundStyle(txn.isDebit ? Theme.debit : Theme.credit)
        }
        .padding(.horizontal, 16).padding(.vertical, 12)
    }
}
