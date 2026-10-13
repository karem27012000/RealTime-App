import Foundation

/// An airport (or a city code such as "LON" that covers several airports).
struct Airport: Codable, Hashable, Identifiable, Sendable {
    var iataCode: String
    var name: String
    var cityName: String?

    var id: String { iataCode }

    /// "Cairo (CAI)" when the city is known, otherwise "Cairo International Airport (CAI)".
    var displayName: String {
        "\(cityName ?? name) (\(iataCode))"
    }
}

extension Airport {
    /// Shown before the user types anything, and used offline in demo mode.
    static let popular: [Airport] = [
        Airport(iataCode: "CAI", name: "Cairo International Airport", cityName: "Cairo"),
        Airport(iataCode: "HBE", name: "Borg El Arab Airport", cityName: "Alexandria"),
        Airport(iataCode: "HRG", name: "Hurghada International Airport", cityName: "Hurghada"),
        Airport(iataCode: "SSH", name: "Sharm El Sheikh International Airport", cityName: "Sharm El Sheikh"),
        Airport(iataCode: "LXR", name: "Luxor International Airport", cityName: "Luxor"),
        Airport(iataCode: "DXB", name: "Dubai International Airport", cityName: "Dubai"),
        Airport(iataCode: "AUH", name: "Zayed International Airport", cityName: "Abu Dhabi"),
        Airport(iataCode: "DOH", name: "Hamad International Airport", cityName: "Doha"),
        Airport(iataCode: "JED", name: "King Abdulaziz International Airport", cityName: "Jeddah"),
        Airport(iataCode: "RUH", name: "King Khalid International Airport", cityName: "Riyadh"),
        Airport(iataCode: "KWI", name: "Kuwait International Airport", cityName: "Kuwait City"),
        Airport(iataCode: "AMM", name: "Queen Alia International Airport", cityName: "Amman"),
        Airport(iataCode: "IST", name: "Istanbul Airport", cityName: "Istanbul"),
        Airport(iataCode: "LHR", name: "Heathrow Airport", cityName: "London"),
        Airport(iataCode: "CDG", name: "Charles de Gaulle Airport", cityName: "Paris"),
        Airport(iataCode: "FRA", name: "Frankfurt Airport", cityName: "Frankfurt"),
        Airport(iataCode: "AMS", name: "Amsterdam Airport Schiphol", cityName: "Amsterdam"),
        Airport(iataCode: "FCO", name: "Leonardo da Vinci–Fiumicino Airport", cityName: "Rome"),
        Airport(iataCode: "MAD", name: "Adolfo Suárez Madrid–Barajas Airport", cityName: "Madrid"),
        Airport(iataCode: "JFK", name: "John F. Kennedy International Airport", cityName: "New York"),
        Airport(iataCode: "LAX", name: "Los Angeles International Airport", cityName: "Los Angeles"),
        Airport(iataCode: "YYZ", name: "Toronto Pearson International Airport", cityName: "Toronto"),
        Airport(iataCode: "SIN", name: "Singapore Changi Airport", cityName: "Singapore"),
        Airport(iataCode: "BKK", name: "Suvarnabhumi Airport", cityName: "Bangkok"),
        Airport(iataCode: "KUL", name: "Kuala Lumpur International Airport", cityName: "Kuala Lumpur"),
    ]

    /// Offline search over `popular`, matching code, city or airport name.
    static func searchPopular(_ query: String) -> [Airport] {
        let q = query.trimmingCharacters(in: .whitespaces)
        guard !q.isEmpty else { return popular }
        return popular.filter {
            $0.iataCode.localizedCaseInsensitiveContains(q)
                || $0.name.localizedCaseInsensitiveContains(q)
                || ($0.cityName?.localizedCaseInsensitiveContains(q) ?? false)
        }
    }
}
