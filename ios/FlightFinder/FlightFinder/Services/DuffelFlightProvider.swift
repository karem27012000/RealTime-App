import Foundation

/// Live, real-time airline offers from the Duffel API (https://duffel.com/docs/api).
///
/// A `duffel_test_…` token returns sandbox data; a `duffel_live_…` token returns real airline prices.
struct DuffelFlightProvider: FlightProvider {
    let token: String
    var session: URLSession = .shared

    private static let baseURL = URL(string: "https://api.duffel.com")!

    func searchOffers(from origin: String, to destination: String, on date: Date,
                      criteria: SearchCriteria) async throws -> [FlightOffer] {
        var components = URLComponents(url: Self.baseURL.appendingPathComponent("air/offer_requests"),
                                       resolvingAgainstBaseURL: false)!
        components.queryItems = [
            URLQueryItem(name: "return_offers", value: "true"),
            // Give airlines up to 20 s; slower ones are skipped rather than failing the whole day.
            URLQueryItem(name: "supplier_timeout", value: "20000"),
        ]

        let body = OfferRequestBody(data: .init(
            slices: [.init(origin: origin, destination: destination,
                           departureDate: DateFormatting.apiDay.string(from: date))],
            passengers: Array(repeating: .init(type: "adult"), count: max(criteria.adults, 1)),
            cabinClass: criteria.cabinClass.rawValue,
            maxConnections: criteria.directOnly ? 0 : nil
        ))

        var request = makeRequest(url: components.url!)
        request.httpMethod = "POST"
        request.timeoutInterval = 60
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try Self.encoder.encode(body)

        let response: Envelope<OfferRequest> = try await send(request)
        return response.data.offers.compactMap(Self.makeOffer)
    }

    func searchAirports(matching query: String) async throws -> [Airport] {
        var components = URLComponents(url: Self.baseURL.appendingPathComponent("places/suggestions"),
                                       resolvingAgainstBaseURL: false)!
        components.queryItems = [URLQueryItem(name: "query", value: query)]

        let response: Envelope<[Place]> = try await send(makeRequest(url: components.url!))
        var airports: [Airport] = []
        for place in response.data {
            // A city (e.g. "LON") searches all its airports at once; list it first, then each airport.
            if let airport = place.asAirport { airports.append(airport) }
            airports.append(contentsOf: (place.airports ?? []).compactMap(\.asAirport))
        }
        var seen = Set<String>()
        return airports.filter { seen.insert($0.iataCode).inserted }
    }

    // MARK: - HTTP

    private func makeRequest(url: URL) -> URLRequest {
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.setValue("v2", forHTTPHeaderField: "Duffel-Version")
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        return request
    }

    private func send<T: Decodable>(_ request: URLRequest) async throws -> T {
        let (data, response) = try await session.data(for: request)
        let status = (response as? HTTPURLResponse)?.statusCode ?? 0
        guard (200..<300).contains(status) else {
            let errors = try? Self.decoder.decode(ErrorEnvelope.self, from: data)
            let message = errors?.errors.first.map { $0.message ?? $0.title ?? "" } ?? ""
            throw FlightSearchError.server(status: status,
                                           message: message.isEmpty ? HTTPURLResponse.localizedString(forStatusCode: status) : message)
        }
        return try Self.decoder.decode(T.self, from: data)
    }

    private static let encoder: JSONEncoder = {
        let encoder = JSONEncoder()
        encoder.keyEncodingStrategy = .convertToSnakeCase
        return encoder
    }()

    private static let decoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.keyDecodingStrategy = .convertFromSnakeCase
        return decoder
    }()

    // MARK: - Mapping

    private static func makeOffer(_ offer: Offer) -> FlightOffer? {
        guard let slice = offer.slices.first, let amount = Decimal(string: offer.totalAmount) else { return nil }

        let segments: [FlightSegment] = slice.segments.compactMap { segment in
            guard let departing = DateFormatting.parseLocalDateTime(segment.departingAt),
                  let arriving = DateFormatting.parseLocalDateTime(segment.arrivingAt),
                  let origin = segment.origin.asAirport,
                  let destination = segment.destination.asAirport else { return nil }
            let carrier = segment.marketingCarrier ?? segment.operatingCarrier
            return FlightSegment(
                id: segment.id,
                origin: origin,
                destination: destination,
                departingAt: departing,
                arrivingAt: arriving,
                carrierName: carrier?.name ?? offer.owner.name ?? "Unknown airline",
                flightNumber: "\(carrier?.iataCode ?? "")\(segment.marketingCarrierFlightNumber ?? "")",
                aircraft: segment.aircraft?.name,
                duration: segment.duration.flatMap(DateFormatting.parseISODuration)
            )
        }
        // Skip offers whose segments couldn't all be read rather than showing a wrong itinerary.
        guard segments.count == slice.segments.count else { return nil }

        return FlightOffer(
            id: offer.id,
            totalAmount: amount,
            currency: offer.totalCurrency,
            airlineName: offer.owner.name ?? segments[0].carrierName,
            airlineLogoURL: offer.owner.logoSymbolUrl.flatMap(URL.init(string:)),
            segments: segments,
            duration: slice.duration.flatMap(DateFormatting.parseISODuration)
        )
    }
}

// MARK: - Duffel JSON

private struct OfferRequestBody: Encodable {
    struct Payload: Encodable {
        struct Slice: Encodable {
            let origin: String
            let destination: String
            let departureDate: String
        }
        struct Passenger: Encodable {
            let type: String
        }
        let slices: [Slice]
        let passengers: [Passenger]
        let cabinClass: String
        /// 0 asks airlines for direct flights only; omitted means any number of stops.
        let maxConnections: Int?
    }
    let data: Payload
}

private struct Envelope<T: Decodable>: Decodable {
    let data: T
}

private struct ErrorEnvelope: Decodable {
    struct APIError: Decodable {
        let title: String?
        let message: String?
    }
    let errors: [APIError]
}

private struct OfferRequest: Decodable {
    let offers: [Offer]
}

private struct Offer: Decodable {
    let id: String
    let totalAmount: String
    let totalCurrency: String
    let owner: Carrier
    let slices: [Slice]
}

private struct Carrier: Decodable {
    let name: String?
    let iataCode: String?
    let logoSymbolUrl: String?
}

private struct Slice: Decodable {
    let duration: String?
    let segments: [Segment]
}

private struct Segment: Decodable {
    let id: String
    let origin: Place
    let destination: Place
    let departingAt: String
    let arrivingAt: String
    let marketingCarrier: Carrier?
    let marketingCarrierFlightNumber: String?
    let operatingCarrier: Carrier?
    let aircraft: Aircraft?
    let duration: String?
}

private struct Aircraft: Decodable {
    let name: String?
}

private struct Place: Decodable {
    let iataCode: String?
    let name: String?
    let cityName: String?
    let airports: [Place]?

    var asAirport: Airport? {
        guard let iataCode, !iataCode.isEmpty else { return nil }
        return Airport(iataCode: iataCode, name: name ?? iataCode, cityName: cityName ?? name)
    }
}
