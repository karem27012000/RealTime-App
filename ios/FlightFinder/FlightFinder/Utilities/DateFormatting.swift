import Foundation

/// Formatters shared across the app.
///
/// Flight times are airport-local and carry no time zone, so they are parsed and shown in UTC
/// to keep the clock time exactly as the airline publishes it.
enum DateFormatting {
    static let utc = TimeZone(identifier: "UTC")!

    static let utcCalendar: Calendar = {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = utc
        return calendar
    }()

    /// "2026-11-01" for a date the user picked (device time zone).
    static let apiDay: DateFormatter = makeFormatter("yyyy-MM-dd", timeZone: .current)

    /// "2026-11-01" for a flight time (local time stored as UTC).
    static let apiDayUTC: DateFormatter = makeFormatter("yyyy-MM-dd", timeZone: utc)

    /// Parses Duffel's "2026-11-01T14:35:00".
    static let localDateTime: DateFormatter = makeFormatter("yyyy-MM-dd'T'HH:mm:ss", timeZone: utc)

    static let time: DateFormatter = {
        let formatter = DateFormatter()
        formatter.timeZone = utc
        formatter.dateStyle = .none
        formatter.timeStyle = .short
        return formatter
    }()

    static let day: DateFormatter = {
        let formatter = DateFormatter()
        formatter.timeZone = utc
        formatter.setLocalizedDateFormatFromTemplate("EEE d MMM")
        return formatter
    }()

    static func parseLocalDateTime(_ string: String) -> Date? {
        // Drop fractional seconds or a trailing offset if present; the clock time is what matters.
        localDateTime.date(from: String(string.prefix(19)))
    }

    /// Parses ISO-8601 durations such as "PT7H5M" or "P1DT2H30M".
    static func parseISODuration(_ string: String) -> TimeInterval? {
        guard string.hasPrefix("P") else { return nil }
        var total: TimeInterval = 0
        var number = ""
        var inTimePart = false
        for character in string.dropFirst() {
            if character == "T" {
                inTimePart = true
            } else if character.isNumber || character == "." {
                number.append(character)
            } else {
                guard let value = Double(number) else { return nil }
                switch (character, inTimePart) {
                case ("D", false): total += value * 86_400
                case ("W", false): total += value * 604_800
                case ("H", true): total += value * 3_600
                case ("M", true): total += value * 60
                case ("S", true): total += value
                default: return nil
                }
                number = ""
            }
        }
        return total
    }

    /// "7h 05m"
    static func durationText(_ interval: TimeInterval?) -> String {
        guard let interval, interval >= 0 else { return "—" }
        let minutes = Int(interval / 60)
        let hours = minutes / 60
        let rest = minutes % 60
        return hours > 0 ? String(format: "%dh %02dm", hours, rest) : "\(rest)m"
    }

    private static func makeFormatter(_ format: String, timeZone: TimeZone) -> DateFormatter {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.timeZone = timeZone
        formatter.dateFormat = format
        return formatter
    }
}
