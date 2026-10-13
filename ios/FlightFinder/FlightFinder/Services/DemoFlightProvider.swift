import Foundation

/// Generates realistic-looking but fake flights so the app can be tried without an API key.
/// Results are stable: the same route and day always produce the same flights.
struct DemoFlightProvider: FlightProvider {
    private static let airlines: [(name: String, code: String)] = [
        ("EgyptAir", "MS"), ("Emirates", "EK"), ("Qatar Airways", "QR"), ("Turkish Airlines", "TK"),
        ("Saudia", "SV"), ("flydubai", "FZ"), ("Lufthansa", "LH"), ("Air Arabia", "G9"),
    ]
    private static let hubs = ["IST", "DXB", "DOH", "JED", "FRA", "AMM"]

    func searchOffers(from origin: String, to destination: String, on date: Date,
                      criteria: SearchCriteria) async throws -> [FlightOffer] {
        try await Task.sleep(for: .milliseconds(400))

        let dayString = DateFormatting.apiDay.string(from: date)
        var rng = SeededGenerator(seed: "\(origin)-\(destination)-\(dayString)")
        guard let day = DateFormatting.apiDayUTC.date(from: dayString) else { return [] }

        let cabinMultiplier: Double = switch criteria.cabinClass {
        case .economy: 1
        case .premiumEconomy: 1.7
        case .business: 3.2
        case .first: 5.5
        }
        let baseFare = Double.random(in: 90...420, using: &rng)

        return (0..<Int.random(in: 6...12, using: &rng)).compactMap { index -> FlightOffer? in
            let airline = Self.airlines.randomElement(using: &rng)!
            let stops = criteria.directOnly ? 0 : [0, 0, 1, 1, 2].randomElement(using: &rng)!
            var departure = day.addingTimeInterval(Double(Int.random(in: 10...92, using: &rng)) * 15 * 60)

            var segments: [FlightSegment] = []
            var from = origin
            for leg in 0...stops {
                let to = leg == stops ? destination : Self.hubs.filter { $0 != origin && $0 != destination }
                    .randomElement(using: &rng)!
                let flightTime = Double(Int.random(in: 5...22, using: &rng)) * 15 * 60
                let arrival = departure.addingTimeInterval(flightTime)
                segments.append(FlightSegment(
                    id: "\(index)-\(leg)",
                    origin: Self.airport(from),
                    destination: Self.airport(to),
                    departingAt: departure,
                    arrivingAt: arrival,
                    carrierName: airline.name,
                    flightNumber: "\(airline.code)\(Int.random(in: 100...999, using: &rng))",
                    aircraft: ["Airbus A320neo", "Boeing 737-800", "Boeing 787-9", "Airbus A350-900"]
                        .randomElement(using: &rng),
                    duration: flightTime
                ))
                from = to
                departure = arrival.addingTimeInterval(Double(Int.random(in: 4...16, using: &rng)) * 15 * 60)
            }

            // Direct flights cost a bit more; each stop knocks the price down.
            let fare = baseFare * cabinMultiplier * Double.random(in: 0.8...1.6, using: &rng)
                * (stops == 0 ? 1.15 : 1 - 0.08 * Double(stops)) * Double(criteria.adults)
            return FlightOffer(
                id: "demo-\(dayString)-\(index)",
                totalAmount: Decimal(Int(fare.rounded())),
                currency: "USD",
                airlineName: airline.name,
                airlineLogoURL: nil,
                segments: segments,
                duration: segments.last!.arrivingAt.timeIntervalSince(segments[0].departingAt)
            )
        }
    }

    func searchAirports(matching query: String) async throws -> [Airport] {
        Airport.searchPopular(query)
    }

    private static func airport(_ code: String) -> Airport {
        Airport.popular.first { $0.iataCode == code } ?? Airport(iataCode: code, name: code, cityName: nil)
    }
}

/// Small deterministic random generator (SplitMix64) seeded from a string.
private struct SeededGenerator: RandomNumberGenerator {
    private var state: UInt64

    init(seed: String) {
        // FNV-1a; String.hashValue changes every launch so it can't be used here.
        var hash: UInt64 = 0xcbf2_9ce4_8422_2325
        for byte in seed.utf8 {
            hash ^= UInt64(byte)
            hash = hash &* 0x100_0000_01b3
        }
        state = hash
    }

    mutating func next() -> UInt64 {
        state &+= 0x9e37_79b9_7f4a_7c15
        var z = state
        z = (z ^ (z >> 30)) &* 0xbf58_476d_1ce4_e5b9
        z = (z ^ (z >> 27)) &* 0x94d0_49bb_1331_11eb
        return z ^ (z >> 31)
    }
}
