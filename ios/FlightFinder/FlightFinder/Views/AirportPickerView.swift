import SwiftUI

/// Search for an airport or city by name or IATA code.
struct AirportPickerView: View {
    let title: String
    @Binding var selection: Airport?

    @Environment(AppSettings.self) private var settings
    @Environment(\.dismiss) private var dismiss
    @State private var query = ""
    @State private var results: [Airport] = Airport.popular
    @State private var isLoading = false
    @State private var errorMessage: String?

    var body: some View {
        List {
            if let code = typedCode, !results.contains(where: { $0.iataCode == code }) {
                Section {
                    Button("Use airport code \(code)") {
                        choose(Airport(iataCode: code, name: code, cityName: nil))
                    }
                }
            }

            Section {
                ForEach(results) { airport in
                    Button {
                        choose(airport)
                    } label: {
                        HStack {
                            VStack(alignment: .leading) {
                                Text(airport.cityName ?? airport.name)
                                    .foregroundStyle(.primary)
                                Text(airport.name)
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }
                            Spacer()
                            Text(airport.iataCode)
                                .font(.headline.monospaced())
                                .foregroundStyle(.tint)
                            if airport.iataCode == selection?.iataCode {
                                Image(systemName: "checkmark").foregroundStyle(.tint)
                            }
                        }
                    }
                }
            } header: {
                Text(query.isEmpty ? "Popular" : "Results")
            } footer: {
                if let errorMessage {
                    Text(errorMessage)
                }
            }
        }
        .overlay {
            if isLoading && results.isEmpty {
                ProgressView()
            } else if !isLoading && results.isEmpty && typedCode == nil {
                ContentUnavailableView.search(text: query)
            }
        }
        .navigationTitle(title)
        .navigationBarTitleDisplayMode(.inline)
        .searchable(text: $query, placement: .navigationBarDrawer(displayMode: .always),
                    prompt: "City, airport or code")
        .textInputAutocapitalization(.never)
        .task(id: query) { await search() }
    }

    /// Three letters typed, e.g. "lgw" -> "LGW", so any airport can be used even if search misses it.
    private var typedCode: String? {
        let trimmed = query.trimmingCharacters(in: .whitespaces)
        guard trimmed.count == 3, trimmed.allSatisfy(\.isLetter) else { return nil }
        return trimmed.uppercased()
    }

    private func search() async {
        let text = query.trimmingCharacters(in: .whitespaces)
        errorMessage = nil
        guard text.count >= 2 else {
            results = Airport.searchPopular(text)
            return
        }

        // Wait for the user to stop typing; a new keystroke cancels this task.
        try? await Task.sleep(for: .milliseconds(300))
        guard !Task.isCancelled else { return }

        isLoading = true
        defer { isLoading = false }
        do {
            let provider: any FlightProvider = (try? settings.makeProvider()) ?? DemoFlightProvider()
            let found = try await provider.searchAirports(matching: text)
            guard !Task.isCancelled else { return }
            results = found
        } catch {
            guard !Task.isCancelled else { return }
            results = Airport.searchPopular(text)
            errorMessage = "Online airport search failed, showing saved airports. \(error.localizedDescription)"
        }
    }

    private func choose(_ airport: Airport) {
        selection = airport
        dismiss()
    }
}
