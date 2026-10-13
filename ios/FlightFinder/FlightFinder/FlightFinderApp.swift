import SwiftUI

@main
struct FlightFinderApp: App {
    @State private var settings: AppSettings
    @State private var viewModel: SearchViewModel

    init() {
        let settings = AppSettings()
        _settings = State(initialValue: settings)
        _viewModel = State(initialValue: SearchViewModel(settings: settings))
    }

    var body: some Scene {
        WindowGroup {
            SearchView()
                .environment(settings)
                .environment(viewModel)
        }
    }
}
