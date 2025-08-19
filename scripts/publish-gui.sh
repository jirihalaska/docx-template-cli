#!/bin/bash
set -e

# Default to current platform if not specified
PLATFORM="${1:-osx-arm64}"
OUTPUT_DIR="${2:-dist/gui-$PLATFORM}"

echo "🖥️  Publishing DocxTemplate GUI for $PLATFORM..."

# Clean output directory
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

echo "📦 Building CLI executable..."
# Build CLI first
if [[ "$PLATFORM" == "win-x64" ]]; then
    CLI_EXE="DocxTemplate.CLI.exe"
    GUI_CLI_NAME="docx-template.exe"
else
    CLI_EXE="DocxTemplate.CLI"
    GUI_CLI_NAME="docx-template"
fi

dotnet publish src/DocxTemplate.CLI -c Release -r "$PLATFORM" --self-contained \
    -p:PublishSingleFile=false \
    -o "$OUTPUT_DIR/cli-temp" \
    --verbosity quiet

echo "🎨 Building GUI with self-contained deployment..."
# Build GUI
dotnet publish src/DocxTemplate.UI -c Release -r "$PLATFORM" --self-contained \
    -p:SkipCliBuild=true \
    -p:PublishSingleFile=false \
    -o "$OUTPUT_DIR" \
    --verbosity quiet

echo "🔗 Linking CLI and GUI..."
# Copy CLI executable
cp "$OUTPUT_DIR/cli-temp/$CLI_EXE" "$OUTPUT_DIR/$GUI_CLI_NAME"
if [[ "$PLATFORM" != "win-x64" ]]; then
    chmod +x "$OUTPUT_DIR/$GUI_CLI_NAME"
fi

# Copy templates if they exist
if [ -d "templates" ]; then
    echo "📁 Copying templates..."
    cp -r templates "$OUTPUT_DIR/"
fi

# Clean up
rm -rf "$OUTPUT_DIR/cli-temp"

# Create macOS app bundle if building for macOS
if [[ "$PLATFORM" == "osx-arm64" ]]; then
    echo "🍎 Creating macOS app bundle..."
    cd "$OUTPUT_DIR"
    
    mkdir -p DocxTemplate.app/Contents/{MacOS,Resources}
    
    # Copy all files to app bundle
    cp DocxTemplate.UI "$GUI_CLI_NAME" lib*.dylib DocxTemplate.app/Contents/MacOS/
    if [ -d "templates" ]; then
        cp -r templates DocxTemplate.app/Contents/MacOS/
    fi
    
    # Create Info.plist
    cat > DocxTemplate.app/Contents/Info.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>DocxTemplate.UI</string>
    <key>CFBundleIdentifier</key>
    <string>com.docxtemplate.ui</string>
    <key>CFBundleName</key>
    <string>DocxTemplate</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
</dict>
</plist>
EOF
    
    chmod +x DocxTemplate.app/Contents/MacOS/DocxTemplate.UI
    
    echo "✅ macOS app bundle created: $OUTPUT_DIR/DocxTemplate.app"
    cd ..
fi

echo "✅ GUI published successfully to: $OUTPUT_DIR"
echo "🚀 To run: ./$OUTPUT_DIR/DocxTemplate.UI"
if [[ "$PLATFORM" == "osx-arm64" ]]; then
    echo "🍎 Or double-click: $OUTPUT_DIR/DocxTemplate.app"
fi