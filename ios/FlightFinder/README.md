# Flight Finder (iOS)

A personal SwiftUI app for finding flights while travelling. You pick two airports and a **departure date range**,
and the app searches **every day in that range** for **live, current airline offers**. Results are sorted by **price, lowest first**.

## Features

| Filter / option | How it works |
|---|---|
| **From / To airports** | Search by city, airport name or IATA code (e.g. `cai`, `Dubai`, `LON` for all London airports). Type any 3-letter code to use it directly. |
| **From date → To date** | Each day in the range is searched (up to 14 days, 3 days at a time). One-way trips. |
| **Direct flights only** | Asks the airlines for non-stop flights only (`max_connections = 0`) and also filters the results on the device. You can turn it on or off from the results screen. |
| **Sort** | Price low→high (default), price high→low, shortest duration, earliest departure. |
| Adults / cabin | 1–9 adults; Economy, Premium Economy, Business, First. The price shown is the total for everyone. |
| Details | Every leg, flight numbers, aircraft, layover times, and a button that opens the same route on Google Flights for booking. |

Your last search (airports, options) is remembered between launches.

## Where the flight data comes from

**Live mode uses the [Duffel API](https://duffel.com/docs/api)**, which asks the airlines in real time for current
fares and seat availability. Amadeus Self-Service is not an option: it was shut down in July 2026.

1. Create a free account at <https://app.duffel.com/join>.
2. In the dashboard go to **Developers → Access tokens** and create a token.
   - `duffel_test_…` tokens return **sandbox** data (including Duffel's fake airline "Duffel Airways", code `ZZ`). Use these to try the app.
   - `duffel_live_…` tokens return **real airline prices**. Duffel asks you to verify your account before switching to live mode.
3. In the app, tap the ⚙️ button, choose **Live flights (Duffel)** and paste the token. It is saved in the iOS Keychain.

Until a token is added the app runs in **Demo data** mode. It generates fake flights so you can try every screen, and shows an orange banner so you don't mistake them for real prices.

> Searching is free, but Duffel's terms limit how many searches you can make for each booking. That's fine for
> personal use. Check their pricing page if you plan to search a lot.

## Run it

Requirements: a Mac with **Xcode 16 or newer**, and an iPhone or simulator on **iOS 17+**.

1. Open `ios/FlightFinder/FlightFinder.xcodeproj` in Xcode.
2. Select the **FlightFinder** target → *Signing & Capabilities* → choose your **Team** (a free Apple ID works).
   If the bundle id `com.karem.FlightFinder` is taken, change it to something unique.
3. Pick your iPhone (or a simulator) and press **Run** (⌘R).

On a free Apple ID the app must be re-installed from Xcode every 7 days. A paid Apple Developer account removes that limit and lets you use TestFlight.

## Code structure

```
FlightFinder/
├── FlightFinderApp.swift            # App entry; creates AppSettings + SearchViewModel
├── Models/
│   ├── Airport.swift                # Airport + built-in list of popular airports
│   ├── SearchCriteria.swift         # Airports, date range, direct-only, adults, cabin, validation
│   └── FlightOffer.swift            # Offer + segments, price formatting, layovers, sorting
├── Services/
│   ├── FlightProvider.swift         # Protocol every data source implements + errors
│   ├── DuffelFlightProvider.swift   # Live data: POST /air/offer_requests, GET /places/suggestions
│   ├── DemoFlightProvider.swift     # Fake but stable data for trying the app
│   └── FlightSearchService.swift    # Searches each day in the range in parallel and merges results
├── ViewModels/
│   ├── AppSettings.swift            # Data source + token (Keychain)
│   └── SearchViewModel.swift        # Form state, search progress, filtering and sorting
├── Views/                           # SearchView, AirportPickerView, ResultsView, FlightRow,
│                                    # FlightDetailView, SettingsView
└── Utilities/                       # Date/duration formatting, Keychain helper
```

To add another data source, write a type that implements `FlightProvider` and return it from `AppSettings.makeProvider()`.

### A note on times

Airlines publish times in each airport's **local** time. The app shows them exactly as published,
so a flight leaving Cairo at 14:35 shows 14:35 whatever time zone your phone is in. `+1` next to an arrival time means it lands the next day.
