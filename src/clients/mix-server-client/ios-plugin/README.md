# iOS NativeAudio Plugin Setup

After running `npx cap add ios` on a macOS machine:

## 1. Copy Plugin Files
Copy the `NativeAudio/` directory into `ios/App/App/Plugins/NativeAudio/`.

## 2. Add Files to Xcode Project
Open `ios/App/App.xcworkspace` in Xcode and add the Swift files to the App target.

## 3. Register the Plugin
In `ios/App/App/AppDelegate.swift`, register the plugin:

```swift
import Capacitor

@UIApplicationMain
class AppDelegate: UIResponder, UIApplicationDelegate {
    // ...existing code...
}
```

The plugin is auto-registered via `CAPBridgedPlugin` conformance in Capacitor 6+.

## 4. Configure Background Audio
Add to `ios/App/App/Info.plist`:

```xml
<key>UIBackgroundModes</key>
<array>
    <string>audio</string>
</array>
```

Or in Xcode: Select the App target → Signing & Capabilities → + Capability → Background Modes → check "Audio, AirPlay, and Picture in Picture".

## 5. Build
```bash
npm run cap:build
npx cap sync ios
npx cap open ios
```
Then build and run from Xcode.
