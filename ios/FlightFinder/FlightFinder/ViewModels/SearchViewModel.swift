import Foundation
import Observation

@MainActor
@Observable
final class SearchViewModel {
    enum Phase: Equatable {
        case idle
        case searching(finished: Int, total: Int)
        case loaded
        case failed(String)
    }

    private static let criteriaKey = "lastSearchCriteria"

    let settings: AppSettings

    /// Remembered between launches so your usual airports are already filled in.
    var criteria: SearchCriteria {
        didSet { save() }
    }

    var sortOption: SortOption = .priceAscending
    private(set) var phase: Phase = .idle
    private(set) var allOffers: [FlightOffer] = []
    private(set) var failedDays: [Date] = []
    /// The criteria the current results belong to (the form may have changed since).
    private(set) var searchedCriteria: SearchCriteria?

    private var searchTask: Task<Void, Never>?

    init(settings: AppSettings) {
        self.settings = settings
        if let data = UserDefaults.standard.data(forKey: Self.criteriaKey),
           let saved = try? JSONDecoder().decode(SearchCriteria.self, from: data) {
            criteria = saved.refreshedDates()
        } else {
            criteria = .default
        }
    }

    /// Results after the direct-only filter and the chosen sort order.
    var displayedOffers: [FlightOffer] {
        let filtered = criteria.directOnly ? allOffers.filter(\.isDirect) : allOffers
        return filtered.ordered(by: sortOption)
    }

    var cheapestOffer: FlightOffer? {
        let candidates = criteria.directOnly ? allOffers.filter(\.isDirect) : allOffers
        return candidates.min { $0.totalAmount < $1.totalAmount }
    }

    var isSearching: Bool {
        if case .searching = phase { return true }
        return false
    }

    func swapAirports() {
        (criteria.origin, criteria.destination) = (criteria.destination, criteria.origin)
    }

    func search() {
        searchTask?.cancel()
        let criteria = criteria
        searchTask = Task { await run(criteria) }
    }

    /// Stops a running search, e.g. when leaving the results screen.
    func cancel() {
        searchTask?.cancel()
        if isSearching { phase = .idle }
    }

    /// Awaitable version used by pull-to-refresh.
    func refresh() async {
        searchTask?.cancel()
        await run(searchedCriteria ?? criteria)
    }

    private func run(_ criteria: SearchCriteria) async {
        allOffers = []
        failedDays = []
        searchedCriteria = criteria
        phase = .searching(finished: 0, total: criteria.departureDates.count)
        do {
            let provider = try settings.makeProvider()
            let result = try await FlightSearchService.search(criteria, using: provider) { [weak self] finished, total in
                guard !Task.isCancelled else { return }
                self?.phase = .searching(finished: finished, total: total)
            }
            allOffers = result.offers
            failedDays = result.failedDays
            phase = .loaded
        } catch is CancellationError {
            // A newer search replaced this one.
        } catch {
            if Task.isCancelled { return }
            phase = .failed(error.localizedDescription)
        }
    }

    private func save() {
        if let data = try? JSONEncoder().encode(criteria) {
            UserDefaults.standard.set(data, forKey: Self.criteriaKey)
        }
    }
}
