import Foundation

/// One leg flown on a single aircraft.
///
/// `departingAt` / `arrivingAt` hold the *airport-local* clock time stored as if it were UTC,
/// so always format them with `DateFormatting` (which uses UTC) to show the time printed on the ticket.
struct FlightSegment: Identifiable, Hashable, Sendable {
    let id: String
    let origin: Airport
    let destination: Airport
    let departingAt: Date
    let arrivingAt: Date
    let carrierName: String
    let flightNumber: String
    let aircraft: String?
    let duration: TimeInterval?
}

/// A bookable price for a one-way trip, made of one or more segments.
struct FlightOffer: Identifiable, Hashable, Sendable {
    let id: String
    let totalAmount: Decimal
    let currency: String
    let airlineName: String
    let airlineLogoURL: URL?
    let segments: [FlightSegment]
    /// Total journey time including layovers, when the provider reports it.
    let duration: TimeInterval?

    init?(id: String, totalAmount: Decimal, currency: String, airlineName: String,
          airlineLogoURL: URL?, segments: [FlightSegment], duration: TimeInterval?) {
        guard !segments.isEmpty else { return nil }
        self.id = id
        self.totalAmount = totalAmount
        self.currency = currency
        self.airlineName = airlineName
        self.airlineLogoURL = airlineLogoURL
        self.segments = segments
        self.duration = duration
    }

    var origin: Airport { segments[0].origin }
    var destination: Airport { segments[segments.count - 1].destination }
    var departingAt: Date { segments[0].departingAt }
    var arrivingAt: Date { segments[segments.count - 1].arrivingAt }
    var stops: Int { segments.count - 1 }
    var isDirect: Bool { stops == 0 }

    var stopsText: String {
        switch stops {
        case 0: "Direct"
        case 1: "1 stop (\(segments[0].destination.iataCode))"
        default: "\(stops) stops"
        }
    }

    var formattedPrice: String {
        totalAmount.formatted(.currency(code: currency).precision(.fractionLength(0...2)))
    }

    /// Days between departure and arrival in local times, e.g. "+1" for an overnight flight.
    var arrivalDayOffset: Int {
        let calendar = DateFormatting.utcCalendar
        return calendar.dateComponents([.day],
                                       from: calendar.startOfDay(for: departingAt),
                                       to: calendar.startOfDay(for: arrivingAt)).day ?? 0
    }

    /// Time on the ground between segment `index` and `index + 1`.
    func layover(after index: Int) -> TimeInterval? {
        guard index + 1 < segments.count else { return nil }
        // Both times are local to the same connecting airport, so the difference is exact.
        return segments[index + 1].departingAt.timeIntervalSince(segments[index].arrivingAt)
    }

    /// Opens the same route and day on Google Flights so it can be booked there.
    var bookingSearchURL: URL? {
        let day = DateFormatting.apiDayUTC.string(from: departingAt)
        let query = "Flights from \(origin.iataCode) to \(destination.iataCode) on \(day) one way"
        var components = URLComponents(string: "https://www.google.com/travel/flights")
        components?.queryItems = [URLQueryItem(name: "q", value: query)]
        return components?.url
    }
}

extension Array where Element == FlightOffer {
    func ordered(by option: SortOption) -> [FlightOffer] {
        sorted { a, b in
            switch option {
            case .priceAscending:
                if a.totalAmount != b.totalAmount { return a.totalAmount < b.totalAmount }
            case .priceDescending:
                if a.totalAmount != b.totalAmount { return a.totalAmount > b.totalAmount }
            case .durationAscending:
                let da = a.duration ?? .greatestFiniteMagnitude
                let db = b.duration ?? .greatestFiniteMagnitude
                if da != db { return da < db }
            case .departureAscending:
                if a.departingAt != b.departingAt { return a.departingAt < b.departingAt }
            }
            // Ties: cheaper first, then earlier.
            if a.totalAmount != b.totalAmount { return a.totalAmount < b.totalAmount }
            return a.departingAt < b.departingAt
        }
    }
}
