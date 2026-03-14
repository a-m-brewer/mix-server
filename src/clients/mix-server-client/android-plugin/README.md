# Android NativeAudio Plugin Setup

After running `npx cap add android`:

## 1. Copy Plugin Files
Copy the `nativeaudio/` directory into:
`android/app/src/main/java/com/mixserver/app/plugins/nativeaudio/`

## 2. Add Media3 Dependencies
Add to `android/app/build.gradle` in the `dependencies` block:

```groovy
implementation 'androidx.media3:media3-exoplayer:1.5.1'
implementation 'androidx.media3:media3-exoplayer-hls:1.5.1'
implementation 'androidx.media3:media3-session:1.5.1'
```

## 3. Register the Plugin
In `android/app/src/main/java/com/mixserver/app/MainActivity.kt`:

```kotlin
import com.mixserver.app.plugins.nativeaudio.NativeAudioPlugin

class MainActivity : BridgeActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        registerPlugin(NativeAudioPlugin::class.java)
        super.onCreate(savedInstanceState)
    }
}
```

## 4. Register the Service
Add to `android/app/src/main/AndroidManifest.xml` inside `<application>`:

```xml
<uses-permission android:name="android.permission.FOREGROUND_SERVICE"/>
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_MEDIA_PLAYBACK"/>

<service
    android:name=".plugins.nativeaudio.NativeAudioService"
    android:foregroundServiceType="mediaPlayback"
    android:exported="false"/>
```

## 5. Build
```bash
npm run cap:build
npx cap sync android
npx cap open android
```
Then build and run from Android Studio.
