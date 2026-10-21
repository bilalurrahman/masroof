import SwiftUI

struct CaptureView: View {
    @State private var text = ""
    @State private var busy = false
    @State private var message: (String, Color)?
    @FocusState private var focused: Bool

    var body: some View {
        NavigationStack {
            ZStack {
                Theme.bg.ignoresSafeArea()
                VStack(alignment: .leading, spacing: 16) {
                    MonoLabel("◆ Paste a bank SMS or a note")
                    Panel {
                        ZStack(alignment: .topLeading) {
                            if text.isEmpty {
                                Text("e.g. \"Purchase of SAR 95.50 at NOON\" or \"paid the plumber 150\"")
                                    .foregroundStyle(Theme.muted)
                                    .padding(.top, 8).padding(.leading, 5)
                            }
                            TextEditor(text: $text)
                                .focused($focused)
                                .frame(minHeight: 130)
                                .scrollContentBackground(.hidden)
                                .foregroundStyle(Theme.text)
                        }
                    }

                    Button {
                        Task { await submit() }
                    } label: {
                        HStack {
                            if busy { ProgressView().tint(.black) }
                            Text(busy ? "Parsing…" : "Parse & Save")
                                .fontWeight(.semibold)
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 14)
                        .background(text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty ? Theme.muted : Theme.accent)
                        .foregroundStyle(.black)
                    }
                    .disabled(busy || text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)

                    if let (msg, color) = message {
                        Text(msg).font(.footnote).foregroundStyle(color)
                    }
                    Spacer()
                }
                .padding(16)
            }
            .navigationTitle("Capture")
            .onTapGesture { focused = false }
        }
    }

    private func submit() async {
        busy = true; message = nil
        defer { busy = false }
        do {
            try await ApiClient.shared.parse(text)
            message = ("Saved to the ledger.", Theme.credit)
            text = ""
            focused = false
        } catch {
            let detail = (error as? ApiError)?.errorDescription ?? error.localizedDescription
            message = (detail, Theme.debit)
        }
    }
}
