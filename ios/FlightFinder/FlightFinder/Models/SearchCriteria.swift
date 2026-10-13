import Foundation

enum CabinClass: String, CaseIterable, Codable, Identifiable, Sendable {
    case economy
    case premiumEconomy = "premium_economy"
    case business
    case first

    var id: String { rawValue }

    var title: String {
        switch self {
        case .economy: "Economy"
        case .premiumEconomy: "Premium Economy"
        case .business: "Business"
        case .first: "First"
        }
    }
}

enum SortOption: String, CaseIterable, Identifiable {
    case priceAscending
    case priceDescending
    case durationAscending
    case departureAscending

    var id: String { rawValue }

    var title: String {
        switch self {
        case .priceAscending: "Price: low to high"
        case .priceDescending: "Price: high to low"
        case .durationAscending: "Shortest duration"
        case .departureAscending: "Earliest departure"
        }
    }
}

/// What the user is looking for: one-way flights departing on any day between `fromDate` and `toDate`.
struct SearchCriteria: Codable, Equatable, Sendable {
    /// Every day in the range is a separate live search, so the range is capped to keep searches fast.
    static let maxDays = 14

    var origin: Airport?
    var destination: Airport?
    var fromDate: Date
    var toDate: Date
    var directOnly: Bool
    var adults: Int
    var cabinClass: CabinClass

    static var `default`: SearchCriteria {
        let today = Calendar.current.startOfDay(for: .now)
        return SearchCriteria(
            origin: nil,
            destination: nil,
            fromDate: today,
            toDate: Calendar.current.date(byAdding: .day, value: 3, to: today) ?? today,
            directOnly: false,
            adults: 1,
            cabinClass: .economy
        )
    }

    /// Each calendar day from `fromDate` to `toDate`, inclusive.
    var departureDates: [Date] {
        let calendar = Calendar.current
        var day = calendar.startOfDay(for: fromDate)
        let last = calendar.startOfDay(for: toDate)
        var days: [Date] = []
        while day <= last {
            days.append(day)
            guard let next = calendar.date(byAdding: .day, value: 1, to: day) else { break }
            day = next
        }
        return days
    }

    /// Why the search can't run yet, or nil when it can.
    var validationMessage: String? {
        guard let origin else { return "Choose where you're flying from." }
        guard let destination else { return "Choose where you're flying to." }
        if origin.iataCode == destination.iataCode { return "From and To must be different airports." }
        if toDate < fromDate { return "The To date must be on or after the From date." }
        if fromDate < Calendar.current.startOfDay(for: .now) { return "The From date can't be in the past." }
        if departureDates.count > Self.maxDays { return "Pick a range of \(Self.maxDays) days or fewer." }
        return nil
    }

    /// Saved criteria may have dates that are now in the past; move them forward to today.
    func refreshedDates() -> SearchCriteria {
        var copy = self
        let today = Calendar.current.startOfDay(for: .now)
        if copy.fromDate < today { copy.fromDate = today }
        if copy.toDate < copy.fromDate { copy.toDate = copy.fromDate }
        return copy
    }
}
