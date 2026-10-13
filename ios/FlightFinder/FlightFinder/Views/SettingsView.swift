import SwiftUI

struct SettingsView: View {
    @Environment(AppSettings.self) private var settings
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        @Bindable var settings = settings

        NavigationStack {
            Form {
                Section {
                    Picker("Flight data", selection: $settings.dataSource) {
                        ForEach(DataSource.allCases) { Text($0.title).tag($0) }
                    }
                    .pickerStyle(.inline)
                    .labelsHidden()
                } header: {
                    Text("Flight data")
                } footer: {
                    Text("Live searches ask the airlines for current prices and seats, so each day in your range takes a few seconds.")
                }

                Section {
                    SecureField("duffel_live_…", text: $settings.duffelToken)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                    if settings.isUsingTestToken {
                        Label("This is a test token: results come from Duffel's sandbox, not real airlines.",
                              systemImage: "exclamationmark.triangle")
                            .font(.footnote)
                            .foregroundStyle(.orange)
                    }
                    Link("Get a free Duffel account", destination: URL(string: "https://app.duffel.com/join")!)
                } header: {
                    Text("Duffel access token")
                } footer: {
                    Text("Create a token in the Duffel dashboard under Developers → Access tokens. It is stored in the iOS Keychain on this device only.")
                }
            }
            .navigationTitle("Settings")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Done") { dismiss() }
                }
            }
        }
    }
}
