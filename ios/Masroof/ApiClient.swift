import Foundation

enum ApiError: LocalizedError {
    case badStatus(Int, String?)
    case badURL

    var errorDescription: String? {
        switch self {
        case .badStatus(let code, let detail):
            return detail ?? "Request failed (HTTP \(code))."
        case .badURL:
            return "Invalid URL."
        }
    }
}

/// Minimal typed client for the Masroof backend. In the iOS Simulator, `localhost` resolves to the
/// Mac hosting the API, so the default base URL just works against the dev server on :8080.
final class ApiClient {
    static let shared = ApiClient()

    /// Default host: the Mac's LAN IP, so the same build works from a physical device and the
    /// Simulator (both can reach it). Change it at runtime in Settings.
    static let defaultBaseURL = "http://192.168.100.221:8080/api"
    private let key = "serverBaseURL"

    /// Persisted in UserDefaults so Settings changes survive relaunch.
    var baseURL: String {
        get { UserDefaults.standard.string(forKey: key) ?? Self.defaultBaseURL }
        set { UserDefaults.standard.set(newValue.trimmingCharacters(in: .whitespacesAndNewlines), forKey: key) }
    }

    /// Lightweight connectivity check for the Settings screen.
    func ping() async -> Bool {
        guard let host = URL(string: baseURL)?.host,
              let scheme = URL(string: baseURL)?.scheme,
              let port = URL(string: baseURL)?.port else { return false }
        guard let url = URL(string: "\(scheme)://\(host):\(port)/health/ready") else { return false }
        var req = URLRequest(url: url); req.timeoutInterval = 8
        guard let (_, resp) = try? await URLSession.shared.data(for: req) else { return false }
        return (200..<300).contains((resp as? HTTPURLResponse)?.statusCode ?? 0)
    }

    private func send(_ path: String, method: String = "GET", body: Data? = nil, timeout: TimeInterval = 300) async throws -> Data {
        guard let url = URL(string: baseURL + path) else { throw ApiError.badURL }
        var req = URLRequest(url: url)
        req.httpMethod = method
        req.timeoutInterval = timeout
        if let body {
            req.httpBody = body
            req.setValue("application/json", forHTTPHeaderField: "Content-Type")
        }
        req.setValue("application/json", forHTTPHeaderField: "Accept")

        let (data, resp) = try await URLSession.shared.data(for: req)
        let code = (resp as? HTTPURLResponse)?.statusCode ?? -1
        guard (200..<300).contains(code) else {
            let detail = (try? JSONSerialization.jsonObject(with: data) as? [String: Any])?["detail"] as? String
            throw ApiError.badStatus(code, detail)
        }
        return data
    }

    /// Lists the ledger. An inclusive `from`/`to` range (yyyy-MM-dd) takes precedence over `month`.
    /// `bank` filters to one provider in the monitored group (e.g. "D360", "STCBANK").
    func ledger(month: String? = nil, from: String? = nil, to: String? = nil,
                bank: String? = nil, page: Int = 1, pageSize: Int = 100) async throws -> PagedResult<Transaction> {
        var path = "/transactions?page=\(page)&pageSize=\(pageSize)"
        if let from, !from.isEmpty { path += "&from=\(from)" }
        if let to, !to.isEmpty { path += "&to=\(to)" }
        if (from?.isEmpty ?? true), (to?.isEmpty ?? true), let month, !month.isEmpty {
            path += "&month=\(month)"
        }
        if let bank, !bank.isEmpty { path += "&bank=\(bank)" }
        return try JSONDecoder().decode(PagedResult<Transaction>.self, from: try await send(path))
    }

    /// Spent/received per bank in the monitored group for the given range (defaults to this month).
    func byBank(month: String? = nil, from: String? = nil, to: String? = nil) async throws -> [BankTotal] {
        var path = "/reports/by-bank"
        var q: [String] = []
        if let from, !from.isEmpty { q.append("from=\(from)") }
        if let to, !to.isEmpty { q.append("to=\(to)") }
        if q.isEmpty, let month, !month.isEmpty { q.append("month=\(month)") }
        if !q.isEmpty { path += "?" + q.joined(separator: "&") }
        return try JSONDecoder().decode([BankTotal].self, from: try await send(path))
    }

    /// Monthly summary, or a range summary when `from`/`to` (yyyy-MM-dd, inclusive) are supplied.
    func summary(month: String? = nil, from: String? = nil, to: String? = nil) async throws -> MonthlySummary {
        var path = "/reports/summary"
        var q: [String] = []
        if let from, !from.isEmpty { q.append("from=\(from)") }
        if let to, !to.isEmpty { q.append("to=\(to)") }
        if q.isEmpty, let month, !month.isEmpty { q.append("month=\(month)") }
        if !q.isEmpty { path += "?" + q.joined(separator: "&") }
        return try JSONDecoder().decode(MonthlySummary.self, from: try await send(path))
    }

    @discardableResult
    func syncSms() async throws -> SmsSyncResult {
        let data = try await send("/transactions/sync-sms", method: "POST", body: Data("{}".utf8))
        return try JSONDecoder().decode(SmsSyncResult.self, from: data)
    }

    func parse(_ text: String) async throws {
        let body = try JSONEncoder().encode(["text": text])
        _ = try await send("/transactions/parse", method: "POST", body: body)
    }
}
