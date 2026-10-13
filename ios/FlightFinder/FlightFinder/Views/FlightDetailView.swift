import SwiftUI

/// Every leg of an offer, with layovers, plus a link to book the route.
struct FlightDetailView: View {
    let offer: FlightOffer
    var isDemo = false

    @Environment(\.openURL) private var openURL

    var body: some View {
        List {
            Section {
                HStack {
                    AirlineLogo(url: offer.airlineLogoURL)
                    VStack(alignment: .leading) {
                        Text(offer.airlineName).font(.headline)
                        Text("\(offer.stopsText) · \(DateFormatting.durationText(offer.duration))")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                    Spacer()
                    Text(offer.formattedPrice)
                        .font(.title2.weight(.bold))
                }
                Text("Total price for all passengers, including taxes.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            ForEach(Array(offer.segments.enumerated()), id: \.element.id) { index, segment in
                Section {
                    segmentView(segment)
                } header: {
                    Text("Flight \(index + 1) · \(segment.flightNumber)")
                } footer: {
                    if let layover = offer.layover(after: index) {
                        Label("Layover in \(segment.destination.cityName ?? segment.destination.iataCode): \(DateFormatting.durationText(layover))",
                              systemImage: "clock")
                            .foregroundStyle(.orange)
                    }
                }
            }

            Section {
                if let url = offer.bookingSearchURL {
                    Button {
                        openURL(url)
                    } label: {
                        Label("Find this flight on Google Flights", systemImage: "safari")
                    }
                }
            } footer: {
                Text(isDemo
                     ? "This is demo data. Turn on live flights in Settings for real prices."
                     : "Prices are live and can change at any time until booked.")
            }
        }
        .navigationTitle("\(offer.origin.iataCode) → \(offer.destination.iataCode)")
        .navigationBarTitleDisplayMode(.inline)
    }

    private func segmentView(_ segment: FlightSegment) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            endpoint(time: segment.departingAt, airport: segment.origin, icon: "airplane.departure")
            HStack {
                Image(systemName: "clock").foregroundStyle(.secondary)
                Text(DateFormatting.durationText(segment.duration))
                Text("·")
                Text(segment.carrierName)
                if let aircraft = segment.aircraft {
                    Text("·")
                    Text(aircraft).lineLimit(1)
                }
            }
            .font(.caption)
            .foregroundStyle(.secondary)
            endpoint(time: segment.arrivingAt, airport: segment.destination, icon: "airplane.arrival")
        }
        .padding(.vertical, 4)
    }

    private func endpoint(time: Date, airport: Airport, icon: String) -> some View {
        HStack(alignment: .top) {
            Image(systemName: icon).foregroundStyle(.tint).frame(width: 24)
            VStack(alignment: .leading) {
                Text("\(DateFormatting.time.string(from: time)) · \(DateFormatting.day.string(from: time))")
                    .font(.headline.monospacedDigit())
                Text("\(airport.name) (\(airport.iataCode))")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
        }
    }
}
