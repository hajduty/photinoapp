#!/bin/bash

set -e

APP_NAME="JobTracker"
BINARY_NAME="JobTracker"
DESKTOP_FILE="jobtracker.desktop"
ICON_FILE="jobtracker.png"
INSTALL_DIR_SYSTEM="/opt/jobtracker"
INSTALL_DIR_USER="$HOME/.local/share/jobtracker"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== JobTracker Installer ==="
echo ""
echo "Where do you want to install JobTracker?"
echo "  1) System-wide (requires sudo) → /opt/jobtracker"
echo "  2) Current user only           → ~/.local/share/jobtracker"
echo ""
read -rp "Choose [1/2]: " choice

if [ "$choice" == "1" ]; then
    INSTALL_DIR="$INSTALL_DIR_SYSTEM"
    DESKTOP_DEST="/usr/share/applications/$DESKTOP_FILE"
    ICON_DEST="/usr/share/icons/hicolor/256x256/apps/$ICON_FILE"
    USE_SUDO=true
else
    INSTALL_DIR="$INSTALL_DIR_USER"
    DESKTOP_DEST="$HOME/.local/share/applications/$DESKTOP_FILE"
    ICON_DEST="$HOME/.local/share/icons/$ICON_FILE"
    USE_SUDO=false
fi

echo ""
echo "Installing to: $INSTALL_DIR"

# Create directories
if [ "$USE_SUDO" = true ]; then
    sudo mkdir -p "$INSTALL_DIR"
    sudo mkdir -p "$(dirname "$ICON_DEST")"
    sudo mkdir -p "$(dirname "$DESKTOP_DEST")"
else
    mkdir -p "$INSTALL_DIR"
    mkdir -p "$(dirname "$ICON_DEST")"
    mkdir -p "$(dirname "$DESKTOP_DEST")"
fi

# Copy binary
if [ "$USE_SUDO" = true ]; then
    sudo cp "$SCRIPT_DIR/$BINARY_NAME" "$INSTALL_DIR/$BINARY_NAME"
    sudo chmod +x "$INSTALL_DIR/$BINARY_NAME"
else
    cp "$SCRIPT_DIR/$BINARY_NAME" "$INSTALL_DIR/$BINARY_NAME"
    chmod +x "$INSTALL_DIR/$BINARY_NAME"
fi

# Copy icon
if [ "$USE_SUDO" = true ]; then
    sudo cp "$SCRIPT_DIR/$ICON_FILE" "$ICON_DEST"
else
    cp "$SCRIPT_DIR/$ICON_FILE" "$ICON_DEST"
fi

# Write .desktop file with correct Exec and Icon paths
DESKTOP_CONTENT="[Desktop Entry]
Name=JobTracker
Comment=Job tracking application
Exec=$INSTALL_DIR/$BINARY_NAME
Icon=$ICON_DEST
Terminal=false
Type=Application
Categories=Utility;
StartupWMClass=JobTracker"

if [ "$USE_SUDO" = true ]; then
    echo "$DESKTOP_CONTENT" | sudo tee "$DESKTOP_DEST" > /dev/null
else
    echo "$DESKTOP_CONTENT" > "$DESKTOP_DEST"
fi

# Update icon cache if system-wide
if [ "$USE_SUDO" = true ]; then
    sudo gtk-update-icon-cache /usr/share/icons/hicolor/ 2>/dev/null || true
fi

echo ""
echo "  JobTracker installed successfully!"
echo "  Binary : $INSTALL_DIR/$BINARY_NAME"
echo "  Icon   : $ICON_DEST"
echo "  Desktop: $DESKTOP_DEST"
echo ""
echo "You can now launch JobTracker from your app menu or run:"
echo "  $INSTALL_DIR/$BINARY_NAME"