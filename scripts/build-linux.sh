#!/bin/bash

set -e

ARCH=${1:-x64}
RID="linux-$ARCH"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

UI_DIR="$REPO_ROOT/JobTracker/UserInterface"
BUILD_OUTPUT="$UI_DIR/build"
WWWROOT="$REPO_ROOT/JobTracker/wwwroot"
RESOURCES_WWWROOT="$REPO_ROOT/JobTracker/Resources/wwwroot"
PROJECT="$REPO_ROOT/JobTracker/JobTracker.csproj"
TEMPLATES="$REPO_ROOT/templates/linux"
OUT_DIR="$REPO_ROOT/publish/out/$RID"
DIST_DIR="$REPO_ROOT/publish/dist"

VERSION="0.0.0-$(date +%Y%m%d%H%M%S)"
PACKAGE_NAME="JobTracker-$VERSION-$RID"
STAGING="$OUT_DIR/staging"

echo "=== Building JobTracker for Linux ($ARCH) ==="
echo "Version : $VERSION"
echo "Output  : $DIST_DIR/$PACKAGE_NAME.tar.gz"
echo ""

# Clean
rm -rf "$OUT_DIR"
mkdir -p "$STAGING"
mkdir -p "$DIST_DIR"

# Step 1: Build frontend
if [ -z "$CI" ]; then
    echo "[1/4] Building frontend..."
    cd "$UI_DIR"
    npm install
    npm run build
    echo "  Frontend built"
else
    echo "[1/4] Skipping frontend build (already built in CI)"
fi

# Step 2: Copy frontend to wwwroot
echo "[2/4] Copying frontend to wwwroot..."
rm -rf "$WWWROOT" && mkdir -p "$WWWROOT"
rm -rf "$RESOURCES_WWWROOT" && mkdir -p "$RESOURCES_WWWROOT"
cp -r "$BUILD_OUTPUT/." "$WWWROOT/"
cp -r "$BUILD_OUTPUT/." "$RESOURCES_WWWROOT/"
echo "✓ wwwroot populated"

# Step 3: Publish .NET project
echo "[3/4] Publishing .NET project..."
dotnet publish "$PROJECT" \
    -r $RID \
    -f net8.0 \
    -c Release \
    --output "$OUT_DIR/publish"
echo "  Published"

# Step 4: Assemble tar.gz
echo "[4/4] Assembling package..."
cp "$OUT_DIR/publish/JobTracker" "$STAGING/JobTracker"
chmod +x "$STAGING/JobTracker"
cp "$TEMPLATES/jobtracker.png"     "$STAGING/jobtracker.png"
cp "$TEMPLATES/jobtracker.desktop" "$STAGING/jobtracker.desktop"
cp "$TEMPLATES/install.sh"         "$STAGING/install.sh"
chmod +x "$STAGING/install.sh"

tar -czf "$DIST_DIR/$PACKAGE_NAME.tar.gz" -C "$STAGING" .

# Clean up
rm -rf "$OUT_DIR"

echo ""
echo "  Done!"
echo "  Output: $DIST_DIR/$PACKAGE_NAME.tar.gz"