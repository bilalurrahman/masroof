import SwiftUI

struct RootView: View {
    init() {
        // Dark, flat tab/nav bars to match the app canvas.
        let appearance = UITabBarAppearance()
        appearance.configureWithOpaqueBackground()
        appearance.backgroundColor = UIColor(Theme.bg)
        UITabBar.appearance().standardAppearance = appearance
        UITabBar.appearance().scrollEdgeAppearance = appearance

        let nav = UINavigationBarAppearance()
        nav.configureWithOpaqueBackground()
        nav.backgroundColor = UIColor(Theme.bg)
        nav.titleTextAttributes = [.foregroundColor: UIColor(Theme.text)]
        nav.largeTitleTextAttributes = [.foregroundColor: UIColor(Theme.text)]
        UINavigationBar.appearance().standardAppearance = nav
        UINavigationBar.appearance().scrollEdgeAppearance = nav
    }

    var body: some View {
        TabView {
            LedgerView()
                .tabItem { Label("Ledger", systemImage: "list.bullet.rectangle") }
            CaptureView()
                .tabItem { Label("Capture", systemImage: "plus.circle") }
            InsightsView()
                .tabItem { Label("Insights", systemImage: "chart.pie") }
            SettingsView()
                .tabItem { Label("Settings", systemImage: "gearshape") }
        }
        .tint(Theme.accent)
    }
}
