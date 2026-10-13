import Foundation

/// A source of flight prices. `DuffelFlightProvider` returns live airline offers;
/// `DemoFlightProvider` returns generated data so the app works without an API key.
protocol FlightProvider: Sendable {
    /// One-way offers departing on a single day.
    func searchOffers(from origin: String, to destination: String, on date: Date,
                      criteria: SearchCriteria) async throws -> [FlightOffer]

    /// Airports and cities matching free text such as "cai" or "London".
    func searchAirports(matching query: String) async throws -> [Airport]
}

enum FlightSearchError: LocalizedError {
    case missingToken
    case invalidCriteria(String)
    case server(status: Int, message: String)
    case allDaysFailed(String)

    var errorDescription: String? {
        switch self {
        case .missingToken:
            "Add your Duffel access token in Settings, or switch to Demo data."
        case .invalidCriteria(let message):
            message
        case .server(let status, let message):
            "The flight service returned an error (\(status)): \(message)"
        case .allDaysFailed(let message):
            "No day in the range could be searched. \(message)"
        }
    }
}
