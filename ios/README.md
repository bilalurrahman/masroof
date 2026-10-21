# Masroof iOS

A native SwiftUI client for the Masroof expense ledger. Talks to the Masroof backend API
(`/api`) — Ledger, Capture (parse a pasted SMS/note), Insights (monthly totals by category),
and an on-demand **Sync** that triggers the server's bank-SMS import.

## Build & run

Requires Xcode 16+ and [XcodeGen](https://github.com/yonaskolb/XcodeGen) (`brew install xcodegen`).

```bash
cd ios
xcodegen generate                 # creates Masroof.xcodeproj from project.yml
open Masroof.xcodeproj            # then Run (⌘R), or:

xcodebuild -project Masroof.xcodeproj -scheme Masroof \
  -sdk iphonesimulator -destination 'platform=iOS Simulator,name=iPhone 17' build
```

## Backend

The app defaults to `http://localhost:8080/api`, which the iOS **Simulator** reaches on the Mac
hosting the API. Start the backend first (API on :8080 with the dev auth bypass). Plain-HTTP to
`localhost` is permitted via an ATS exception in `Info.plist`.

To run on a physical device, change `ApiClient.baseURL` to the Mac's LAN address
(e.g. `http://192.168.1.x:8080/api`) and add that host to the ATS exceptions.

## Structure

- `MasroofApp.swift` — app entry
- `RootView.swift` — tab bar (Ledger / Capture / Insights)
- `Models.swift` — Codable DTOs matching the API contract
- `ApiClient.swift` — async/await networking
- `LedgerView.swift` / `CaptureView.swift` / `InsightsView.swift` — screens
- `Theme.swift` — shared styling (near-black canvas, hairline borders, monospace micro-labels)
