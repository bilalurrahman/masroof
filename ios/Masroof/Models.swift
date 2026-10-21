import Foundation

struct CategoryRef: Codable, Hashable {
    let code: String
    let name: String
}

struct Transaction: Codable, Identifiable, Hashable {
    let id: Int
    let direction: String        // "debit" | "credit"
    let amount: Double
    let currency: String
    let counterparty: String?
    let channel: String?
    let txnDate: String          // yyyy-MM-dd
    let category: CategoryRef
    let confidence: Double
    let source: String

    var isDebit: Bool { direction.lowercased() == "debit" }
}

struct PagedResult<T: Codable>: Codable {
    let items: [T]
    let page: Int
    let pageSize: Int
    let total: Int
    let totalPages: Int
}

struct CategoryTotal: Codable, Identifiable {
    let categoryCode: String
    let categoryName: String
    let total: Double
    let count: Int
    var id: String { categoryCode }
}

struct MonthlySummary: Codable {
    let month: String
    let totalDebit: Double
    let totalCredit: Double
    let transactionCount: Int
    let previousMonthDebit: Double
    let byCategory: [CategoryTotal]
}

struct SmsSyncResult: Codable {
    let scanned: Int
    let imported: Int
    let duplicates: Int
    let failed: Int
}
