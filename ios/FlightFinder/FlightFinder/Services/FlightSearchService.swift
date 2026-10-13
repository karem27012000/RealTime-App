import Foundation

struct FlightSearchResult: Sendable {
    var offers: [FlightOffer]
    /// Days that could not be searched (network error, airline timeout…). The rest are still shown.
    var failedDays: [Date]
}

/// Runs one search per day in the date range, a few at a time, and merges the results.
enum FlightSearchService {
    /// How many days are searched in parallel. Keeps us well under the API rate limit.
    private static let concurrentDays = 3

    static func search(_ criteria: SearchCriteria,
                       using provider: any FlightProvider,
                       onProgress: @escaping @MainActor @Sendable (_ finished: Int, _ total: Int) -> Void)
    async throws -> FlightSearchResult {
        if let message = criteria.validationMessage { throw FlightSearchError.invalidCriteria(message) }
        guard let origin = criteria.origin?.iataCode,
              let destination = criteria.destination?.iataCode else {
            throw FlightSearchError.invalidCriteria("Choose both airports.")
        }

        let days = criteria.departureDates
        var offers: [FlightOffer] = []
        var failedDays: [Date] = []
        var lastError: Error?
        var finished = 0
        await onProgress(0, days.count)

        await withTaskGroup(of: (Date, Result<[FlightOffer], Error>).self) { group in
            var pending = days.makeIterator()

            func addNext() {
                guard let day = pending.next() else { return }
                group.addTask {
                    do {
                        let dayOffers = try await provider.searchOffers(from: origin, to: destination,
                                                                        on: day, criteria: criteria)
                        return (day, .success(dayOffers))
                    } catch {
                        return (day, .failure(error))
                    }
                }
            }

            for _ in 0..<concurrentDays { addNext() }

            while let (day, result) = await group.next() {
                switch result {
                case .success(let dayOffers):
                    offers.append(contentsOf: dayOffers)
                case .failure(let error):
                    failedDays.append(day)
                    lastError = error
                }
                finished += 1
                await onProgress(finished, days.count)
                if !Task.isCancelled { addNext() }
            }
        }

        try Task.checkCancellation()
        if failedDays.count == days.count, let lastError {
            if let searchError = lastError as? FlightSearchError, case .server = searchError { throw searchError }
            throw FlightSearchError.allDaysFailed(lastError.localizedDescription)
        }
        return FlightSearchResult(offers: offers, failedDays: failedDays.sorted())
    }
}
