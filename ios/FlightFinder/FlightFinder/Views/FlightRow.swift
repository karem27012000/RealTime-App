import SwiftUI

struct FlightRow: View {
    let offer: FlightOffer
    var isCheapest = false

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack(alignment: .firstTextBaseline) {
                AirlineLogo(url: offer.airlineLogoURL)
                Text(offer.airlineName)
                    .font(.subheadline)
                    .lineLimit(1)
                Spacer()
                Text(offer.formattedPrice)
                    .font(.headline)
                    .foregroundStyle(isCheapest ? Color.green : Color.primary)
            }

            HStack {
                timeColumn(offer.departingAt, code: offer.origin.iataCode)
                VStack(spacing: 2) {
                    Text(DateFormatting.durationText(offer.duration))
                        .font(.caption2)
                        .foregroundStyle(.secondary)
                    Rectangle()
                        .frame(height: 1)
                        .foregroundStyle(Color.secondary.opacity(0.5))
                    Text(offer.stopsText)
                        .font(.caption2.weight(.semibold))
                        .foregroundStyle(offer.isDirect ? Color.green : Color.orange)
                }
                .frame(maxWidth: .infinity)
                timeColumn(offer.arrivingAt, code: offer.destination.iataCode,
                           dayOffset: offer.arrivalDayOffset)
            }

            HStack {
                Text(DateFormatting.day.string(from: offer.departingAt))
                if isCheapest {
                    Text("Cheapest")
                        .font(.caption2.weight(.bold))
                        .padding(.horizontal, 6)
                        .padding(.vertical, 2)
                        .background(.green.opacity(0.15), in: Capsule())
                        .foregroundStyle(.green)
                }
            }
            .font(.caption)
            .foregroundStyle(.secondary)
        }
        .padding(.vertical, 4)
        .accessibilityElement(children: .combine)
    }

    private func timeColumn(_ date: Date, code: String, dayOffset: Int = 0) -> some View {
        VStack(spacing: 2) {
            HStack(alignment: .top, spacing: 1) {
                Text(DateFormatting.time.string(from: date))
                    .font(.title3.weight(.semibold).monospacedDigit())
                if dayOffset > 0 {
                    Text("+\(dayOffset)")
                        .font(.caption2)
                        .foregroundStyle(.red)
                }
            }
            Text(code)
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }
}

struct AirlineLogo: View {
    let url: URL?

    var body: some View {
        AsyncImage(url: url) { image in
            image.resizable().scaledToFit()
        } placeholder: {
            Image(systemName: "airplane")
                .foregroundStyle(.tint)
        }
        .frame(width: 20, height: 20)
    }
}
