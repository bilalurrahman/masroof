import SwiftUI

struct SettingsView: View {
    @State private var url = ApiClient.shared.baseURL
    @State private var testing = false
    @State private var result: (String, Color)?

    var body: some View {
        NavigationStack {
            ZStack {
                Theme.bg.ignoresSafeArea()
                VStack(alignment: .leading, spacing: 16) {
                    MonoLabel("◆ Server")
                    Panel {
                        VStack(alignment: .leading, spacing: 10) {
                            Text("API base URL").font(.caption).foregroundStyle(Theme.muted)
                            TextField("http://host:8080/api", text: $url)
                                .textInputAutocapitalization(.never)
                                .autocorrectionDisabled()
                                .keyboardType(.URL)
                                .foregroundStyle(Theme.text)
                                .padding(10)
                                .background(Theme.bg)
                                .overlay(Rectangle().stroke(Theme.line, lineWidth: 1))
                        }
                    }

                    HStack(spacing: 12) {
                        Button("Save") {
                            ApiClient.shared.baseURL = url
                            result = ("Saved. Pull to refresh the other tabs.", Theme.credit)
                        }
                        .buttonStyle(FilledButton(color: Theme.accent))

                        Button(testing ? "Testing…" : "Test") {
                            Task {
                                testing = true; result = nil
                                ApiClient.shared.baseURL = url
                                let ok = await ApiClient.shared.ping()
                                testing = false
                                result = ok ? ("Connected ✓", Theme.credit)
                                            : ("No response — check the IP, same Wi-Fi, and that the API is running.", Theme.debit)
                            }
                        }
                        .buttonStyle(FilledButton(color: Theme.card, fg: Theme.text))
                        .disabled(testing)
                    }

                    if let (msg, color) = result {
                        Text(msg).font(.footnote).foregroundStyle(color)
                    }

                    Panel {
                        VStack(alignment: .leading, spacing: 6) {
                            MonoLabel("Tips")
                            Text("• Phone and Mac must be on the same Wi-Fi.").font(.caption).foregroundStyle(Theme.muted)
                            Text("• Default is the Mac's LAN IP; change it if your network changes.").font(.caption).foregroundStyle(Theme.muted)
                            Text("• First connection asks for Local Network permission — tap Allow.").font(.caption).foregroundStyle(Theme.muted)
                        }
                    }
                    Spacer()
                }
                .padding(16)
            }
            .navigationTitle("Settings")
        }
    }
}

struct FilledButton: ButtonStyle {
    var color: Color
    var fg: Color = .black
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .fontWeight(.semibold)
            .frame(maxWidth: .infinity)
            .padding(.vertical, 12)
            .background(color)
            .foregroundStyle(fg)
            .overlay(Rectangle().stroke(Theme.line, lineWidth: 1))
            .opacity(configuration.isPressed ? 0.7 : 1)
    }
}
