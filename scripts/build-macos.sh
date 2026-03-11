#!/bin/bash

set -e

ARCH=${1:-arm64}
RID="osx-$ARCH"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

UI_DIR="$REPO_ROOT/JobTracker/UserInterface"
BUILD_OUTPUT="$UI_DIR/build"
WWWROOT="$REPO_ROOT/JobTracker/wwwroot"
RESOURCES_WWWROOT="$REPO_ROOT/JobTracker/Resources/wwwroot"
PROJECT="$REPO_ROOT/JobTracker/JobTracker.csproj"
TEMPLATES="$REPO_ROOT/templates/osx"
OUT_DIR="$REPO_ROOT/publish/out/$RID"
DIST_DIR="$REPO_ROOT/publish/dist"

VERSION="0.0.0-$(date +%Y%m%d%H%M%S)"
PACKAGE_NAME="JobTracker-$VERSION-$RID"
STAGING="$OUT_DIR/$PACKAGE_NAME"
APP_BUNDLE="$STAGING/JobTracker.app"

echo "=== Building JobTracker for macOS ($ARCH) ==="
echo "Version : $VERSION"
echo "Output  : $DIST_DIR/$PACKAGE_NAME.dmg"
echo ""

# Clean
rm -rf "$OUT_DIR"
mkdir -p "$STAGING"
mkdir -p "$DIST_DIR"

# Step 1: Build frontend
if [ -z "$CI" ]; then
    echo "[1/5] Building frontend..."
    cd "$UI_DIR"
    npm install
    npm run build
    echo "  Frontend built"
else
    echo "[1/5] Skipping frontend build (already built in CI)"
fi

# Step 2: Copy frontend to wwwroot
echo "[2/5] Copying frontend to wwwroot..."
rm -rf "$WWWROOT" && mkdir -p "$WWWROOT"
rm -rf "$RESOURCES_WWWROOT" && mkdir -p "$RESOURCES_WWWROOT"
cp -r "$BUILD_OUTPUT/." "$WWWROOT/"
cp -r "$BUILD_OUTPUT/." "$RESOURCES_WWWROOT/"
echo "✓ wwwroot populated"

# Step 3: Publish .NET project
echo "[3/5] Publishing .NET project..."
dotnet publish "$PROJECT" \
    -r $RID \
    -f net8.0 \
    -c Release \
    --output "$OUT_DIR/publish"
echo "  Published"

# Step 4: Assemble .app bundle
echo "[4/5] Assembling .app bundle..."
cp -r "$TEMPLATES/JobTracker.app" "$APP_BUNDLE"
cp "$OUT_DIR/publish/JobTracker" "$APP_BUNDLE/Contents/MacOS/JobTracker"
chmod +x "$APP_BUNDLE/Contents/MacOS/JobTracker"

PLIST="$APP_BUNDLE/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $VERSION" "$PLIST"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $VERSION" "$PLIST" 2>/dev/null || \
/usr/libexec/PlistBuddy -c "Add :CFBundleShortVersionString string $VERSION" "$PLIST"
echo "  .app bundle assembled"

# Step 5: Create .dmg
echo "[5/5] Creating .dmg..."
ln -s /Applications "$STAGING/Applications"

hdiutil create \
    -volname "JobTracker" \
    -srcfolder "$STAGING" \
    -ov \
    -format UDZO \
    "$DIST_DIR/$PACKAGE_NAME.dmg"

# Clean up
rm -rf "$OUT_DIR"

echo ""
echo "  Done!"
echo "  Output: $DIST_DIR/$PACKAGE_NAME.dmg"
echo ""
echo "NOTE: App is not code-signed. Users will need to right-click > Open"
echo "      on first launch to bypass Gatekeeper."