import Foundation
import Observation

enum DataSource: String, CaseIterable, Identifiable {
    case live
    case demo

    var id: String { rawValue }

    var title: String {
        switch self {
        case .live: "Live flights (Duffel)"
        case .demo: "Demo data"
        }
    }
}

/// User settings: where flight data comes from, and the API token (kept in the Keychain).
@Observable
final class AppSettings {
    private static let dataSourceKey = "dataSource"
    private static let tokenKey = "duffelAccessToken"

    var dataSource: DataSource {
        didSet { UserDefaults.standard.set(dataSource.rawValue, forKey: Self.dataSourceKey) }
    }

    var duffelToken: String {
        didSet { KeychainStore.set(duffelToken.trimmingCharacters(in: .whitespacesAndNewlines), for: Self.tokenKey) }
    }

    init() {
        let token = KeychainStore.string(for: Self.tokenKey) ?? ""
        duffelToken = token
        let saved = UserDefaults.standard.string(forKey: Self.dataSourceKey).flatMap(DataSource.init(rawValue:))
        // First launch: live if a token is already saved, otherwise demo so the app works straight away.
        dataSource = saved ?? (token.isEmpty ? .demo : .live)
    }

    var isUsingTestToken: Bool { duffelToken.hasPrefix("duffel_test_") }

    func makeProvider() throws -> any FlightProvider {
        switch dataSource {
        case .demo:
            return DemoFlightProvider()
        case .live:
            let token = duffelToken.trimmingCharacters(in: .whitespacesAndNewlines)
            guard !token.isEmpty else { throw FlightSearchError.missingToken }
            return DuffelFlightProvider(token: token)
        }
    }
}
