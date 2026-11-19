#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
version=1.0.0
build=.build/distribution
stage="$build/stage"
app="$stage/牛马计数器.app"
binary="$app/Contents/MacOS/Counter"
mkdir -p "$app/Contents/MacOS"
for arch in arm64 x86_64; do
  swift build -c release --arch "$arch" --scratch-path "$build/$arch"
done
lipo -create "$build/arm64/release/Counter" "$build/x86_64/release/Counter" -output "$binary"
cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleIdentifier</key><string>com.qiyi92.off-duty-counter</string>
<key>CFBundleName</key><string>牛马计数器</string>
<key>CFBundleExecutable</key><string>Counter</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>$version</string>
<key>CFBundleVersion</key><string>$version</string>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
PLIST
codesign --force --sign - "$app"
codesign --verify --strict "$app"
archive="$build/OffDutyCounter-v$version-macOS-universal.zip"
ditto -c -k --keepParent "$app" "$archive"
(cd "$build" && shasum -a 256 "$(basename "$archive")" > SHA256SUMS.txt)
