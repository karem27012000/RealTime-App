import SwiftUI

/// All offers across the date range, cheapest first by default.
struct ResultsView: View {
    @Environment(SearchViewModel.self) private var viewModel
    @Environment(AppSettings.self) private var settings

    var body: some View {
        @Bindable var viewModel = viewModel

        List {
            if let searched = viewModel.searchedCriteria {
                Section {
                    summary(for: searched)
                }
            }

            switch viewModel.phase {
            case .idle:
                EmptyView()
            case .searching(let finished, let total):
                Section {
                    VStack(alignment: .leading, spacing: 8) {
                        ProgressView(value: Double(finished), total: Double(max(total, 1)))
                        Text("Searching day \(min(finished + 1, total)) of \(total)…")
                            .font(.footnote)
                            .foregroundStyle(.secondary)
                    }
                    .padding(.vertical, 4)
                }
            case .failed(let message):
                Section {
                    ContentUnavailableView {
                        Label("Search failed", systemImage: "exclamationmark.triangle")
                    } description: {
                        Text(message)
                    } actions: {
                        Button("Try again") { viewModel.search() }
                            .buttonStyle(.borderedProminent)
                    }
                }
            case .loaded:
                if !viewModel.failedDays.isEmpty {
                    Section {
                        Label("Couldn't load: \(viewModel.failedDays.map { DateFormatting.day.string(from: dayAsUTC($0)) }.joined(separator: ", ")). Pull down to retry.",
                              systemImage: "exclamationmark.circle")
                            .font(.footnote)
                            .foregroundStyle(.orange)
                    }
                }

                let offers = viewModel.displayedOffers
                let cheapestID = offers.min { $0.totalAmount < $1.totalAmount }?.id
                if offers.isEmpty {
                    Section {
                        ContentUnavailableView(
                            "No flights found",
                            systemImage: "airplane.circle",
                            description: Text(viewModel.criteria.directOnly
                                              ? "Try turning off \"Direct flights only\" or widening the dates."
                                              : "Try other dates or nearby airports.")
                        )
                    }
                } else {
                    Section {
                        ForEach(offers) { offer in
                            NavigationLink {
                                FlightDetailView(offer: offer, isDemo: settings.dataSource == .demo)
                            } label: {
                                FlightRow(offer: offer, isCheapest: offer.id == cheapestID)
                            }
                        }
                    } header: {
                        Text("\(offers.count) flights · \(viewModel.sortOption.title)")
                    }
                }
            }
        }
        .navigationTitle(title)
        .navigationBarTitleDisplayMode(.inline)
        .refreshable { await viewModel.refresh() }
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu {
                    Picker("Sort by", selection: $viewModel.sortOption) {
                        ForEach(SortOption.allCases) { Text($0.title).tag($0) }
                    }
                    Toggle("Direct flights only", isOn: $viewModel.criteria.directOnly)
                } label: {
                    Label("Sort and filter", systemImage: "line.3.horizontal.decrease.circle")
                }
            }
        }
        .onDisappear { viewModel.cancel() }
    }

    private var title: String {
        guard let searched = viewModel.searchedCriteria,
              let origin = searched.origin, let destination = searched.destination else { return "Flights" }
        return "\(origin.iataCode) → \(destination.iataCode)"
    }

    private func summary(for criteria: SearchCriteria) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            let from = criteria.fromDate.formatted(.dateTime.day().month(.abbreviated))
            let to = criteria.toDate.formatted(.dateTime.day().month(.abbreviated))
            Text(from == to ? from : "\(from) – \(to)")
                .font(.subheadline.weight(.semibold))
            Text("\(criteria.adults) adult\(criteria.adults == 1 ? "" : "s") · \(criteria.cabinClass.title)\(viewModel.criteria.directOnly ? " · Direct only" : "")")
                .font(.caption)
                .foregroundStyle(.secondary)
            if let cheapest = viewModel.cheapestOffer, viewModel.phase == .loaded {
                Text("Cheapest: \(cheapest.formattedPrice) on \(DateFormatting.day.string(from: cheapest.departingAt))")
                    .font(.caption.weight(.semibold))
                    .foregroundStyle(.green)
            }
        }
    }

    /// Re-expresses a device-time-zone day as UTC midnight so `DateFormatting.day` prints the same day.
    private func dayAsUTC(_ date: Date) -> Date {
        let parts = Calendar.current.dateComponents([.year, .month, .day], from: date)
        return DateFormatting.utcCalendar.date(from: parts) ?? date
    }
}
