import SwiftUI

/// The bank/wallet providers Masroof monitors as a group: D360, STC Bank, SABB, Alinma, tiqmo.
/// Codes match `Account.BankCode` on the server; names and colors are for display.
enum BankGroup {
    struct Member: Identifiable {
        let code: String
        let name: String
        let color: Color
        var id: String { code }
    }

    static let members: [Member] = [
        .init(code: "D360", name: "D360", color: Color(red: 0.38, green: 0.65, blue: 0.98)),
        .init(code: "STCBANK", name: "STC Bank", color: Color(red: 0.66, green: 0.55, blue: 0.98)),
        .init(code: "SABB", name: "SABB", color: Color(red: 0.96, green: 0.62, blue: 0.04)),
        .init(code: "ALINMA", name: "Alinma", color: Color(red: 0.20, green: 0.83, blue: 0.60)),
        .init(code: "TIQMO", name: "tiqmo", color: Color(red: 0.96, green: 0.45, blue: 0.71)),
    ]

    static func color(_ code: String) -> Color {
        members.first { $0.code == code }?.color ?? Theme.muted
    }
}
