import SwiftUI

/// The search form: airports, departure date range, direct-only, passengers and cabin.
struct SearchView: View {
    @Environment(SearchViewModel.self) private var viewModel
    @Environment(AppSettings.self) private var settings
    @State private var showResults = false
    @State private var showSettings = false

    var body: some View {
        @Bindable var viewModel = viewModel
        let today = Calendar.current.startOfDay(for: .now)

        NavigationStack {
            Form {
                if settings.dataSource == .demo {
                    Section {
                        Label("Showing demo data. Add a Duffel token in Settings to see real flights.",
                              systemImage: "info.circle")
                            .font(.footnote)
                            .foregroundStyle(.orange)
                    }
                }

                Section("Route") {
                    NavigationLink {
                        AirportPickerView(title: "From", selection: $viewModel.criteria.origin)
                    } label: {
                        airportRow(title: "From", icon: "airplane.departure", airport: viewModel.criteria.origin)
                    }
                    NavigationLink {
                        AirportPickerView(title: "To", selection: $viewModel.criteria.destination)
                    } label: {
                        airportRow(title: "To", icon: "airplane.arrival", airport: viewModel.criteria.destination)
                    }
                    Button {
                        viewModel.swapAirports()
                    } label: {
                        Label("Swap From and To", systemImage: "arrow.up.arrow.down")
                    }
                }

                Section {
                    DatePicker("From date", selection: $viewModel.criteria.fromDate,
                               in: today..., displayedComponents: .date)
                        .onChange(of: viewModel.criteria.fromDate) { _, newValue in
                            if viewModel.criteria.toDate < newValue { viewModel.criteria.toDate = newValue }
                        }
                    DatePicker("To date", selection: $viewModel.criteria.toDate,
                               in: viewModel.criteria.fromDate..., displayedComponents: .date)
                } header: {
                    Text("Departure dates")
                } footer: {
                    let days = viewModel.criteria.departureDates.count
                    Text("Flights leaving on any of these \(days) day\(days == 1 ? "" : "s") are searched (up to \(SearchCriteria.maxDays)).")
                }

                Section("Options") {
                    Toggle("Direct flights only", isOn: $viewModel.criteria.directOnly)
                    Stepper("Adults: \(viewModel.criteria.adults)", value: $viewModel.criteria.adults, in: 1...9)
                    Picker("Cabin", selection: $viewModel.criteria.cabinClass) {
                        ForEach(CabinClass.allCases) { Text($0.title).tag($0) }
                    }
                }

                Section {
                    Button {
                        viewModel.search()
                        showResults = true
                    } label: {
                        Label("Search flights", systemImage: "magnifyingglass")
                            .frame(maxWidth: .infinity)
                            .fontWeight(.semibold)
                    }
                    .disabled(viewModel.criteria.validationMessage != nil)
                } footer: {
                    if let message = viewModel.criteria.validationMessage {
                        Text(message)
                    }
                }
            }
            .navigationTitle("Flight Finder")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button {
                        showSettings = true
                    } label: {
                        Image(systemName: "gearshape")
                    }
                    .accessibilityLabel("Settings")
                }
            }
            .navigationDestination(isPresented: $showResults) {
                ResultsView()
            }
            .sheet(isPresented: $showSettings) {
                SettingsView()
            }
        }
    }

    private func airportRow(title: String, icon: String, airport: Airport?) -> some View {
        HStack {
            Label(title, systemImage: icon)
            Spacer()
            if let airport {
                VStack(alignment: .trailing) {
                    Text(airport.iataCode).font(.headline)
                    Text(airport.cityName ?? airport.name)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            } else {
                Text("Choose").foregroundStyle(.secondary)
            }
        }
    }
}

#Preview {
    let settings = AppSettings()
    return SearchView()
        .environment(settings)
        .environment(SearchViewModel(settings: settings))
}
