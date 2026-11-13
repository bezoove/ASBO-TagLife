# ASBO: Life on Tag - Android wrapper

A [Capacitor](https://capacitorjs.com/) 7 Android shell around the single-file web game
"ASBO: Life on Tag" (`www/index.html`). App id `com.bezoove.asbo`. Needs Java 17 to build.

**Status: the APK has not been built or run on a device yet.** This project was assembled in a sandbox
without the Android SDK; the first GitHub Actions run is the first real build.

## Updating the game
1. Replace `www/index.html` with the new single-file build, or run
   `npm run sync-game -- /path/to/eggy-duck.html` (copies the file and adds the small HUD loader; safe to repeat).
2. Commit and push to `main`. The workflow builds a new debug APK.

## Getting the APK
GitHub repo -> **Actions** tab -> latest "Android build" run -> **Artifacts** -> `asbo-debug-apk` (a zip containing `app-debug.apk`).
You can also start a run by hand with "Run workflow" (workflow_dispatch).

## Installing
Copy the APK to the phone, open it, and allow "Install unknown apps" for your file manager/browser when asked.
It is a debug-signed build, so Play Protect may warn.

## Performance HUD
Shows FPS, frame time p50/p95, long-task count, JS heap (Chromium only), and device info; logs a 60 s summary to the console.
- Browser/URL: add `?hud=1`.
- Persistent: `localStorage.setItem('asbo_hud','1')` and reload (use `0` to turn off). With the debug APK on USB, open `chrome://inspect` on a PC to run this and read the console.
- On the phone without a PC: tap the top-left corner 7 times within 4 seconds; the page reloads with the HUD toggled.

The HUD lives in `www/perf-hud.js`; the loader is a small block appended to `www/index.html` by `sync-game`. The game's own code is untouched.

## What the Android layer does
Locked sensor-landscape, immersive sticky full screen, screen kept on, hardware acceleration, short-edge display cutouts,
autoplay audio allowed, DOM storage on, WebView remote debugging only in debuggable builds. The Back button does not exit:
it sends an Escape key press to the game (its pause/menu handling). See `android/app/src/main/java/com/bezoove/asbo/MainActivity.java`.
The icon is an original generated duck (`scripts/make-icons.py`, needs Pillow).

## Known limitations
- Unverified on a real phone: performance, touch controls, audio, and layout are untested in the wrapper.
- The cloud-save connection from the artifact version is absent here, so saves use the WebView's local storage.
  Check that saves persist after closing and reopening the app. Uninstalling the app erases them.
- The game's HTML has no real viewport meta tag (only `claude:viewport-fit-sides`), so safe-area insets around the cutout may be 0; HUD or UI near a notch could be clipped.
- Google Fonts are linked remotely; offline the game falls back to system fonts.
- The Back-button behaviour assumes the game reacts to Escape; confirm this on device.
- The optional release job is documented only: it needs keystore secrets and a signing config in `android/app/build.gradle`, and the repo variable `ENABLE_RELEASE=true`.

## Local build (needs Android SDK)
```
npm ci
npx cap sync android
cd android && ./gradlew assembleDebug
```

## Licence
No licence has been chosen yet; all rights reserved by the repository owner until one is added. Capacitor is MIT licensed.
Game assets and the generated icon belong to the project owner.
