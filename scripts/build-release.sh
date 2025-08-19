#!/bin/bash
set -e

echo "🔨 Building DocxTemplate Release Packages..."

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf dist/release
mkdir -p dist/release

# Define platforms
CLI_PLATFORMS=("win-x64" "osx-x64" "osx-arm64" "linux-x64" "linux-arm64")
GUI_PLATFORMS=("win-x64" "osx-arm64" "linux-x64")

echo "📦 Building CLI packages..."
for platform in "${CLI_PLATFORMS[@]}"; do
    echo "  Building CLI for $platform..."
    dotnet publish src/DocxTemplate.CLI -c Release -r "$platform" --self-contained \
        -o "dist/release/cli-$platform" \
        --verbosity quiet
done

echo "🖥️  Building GUI packages with self-contained deployment..."
for platform in "${GUI_PLATFORMS[@]}"; do
    echo "  Building GUI for $platform..."
    
    # Build CLI first for this platform
    dotnet publish src/DocxTemplate.CLI -c Release -r "$platform" --self-contained \
        -p:PublishSingleFile=false \
        -o "dist/release/gui-$platform/cli-temp" \
        --verbosity quiet
    
    # Build GUI
    dotnet publish src/DocxTemplate.UI -c Release -r "$platform" --self-contained \
        -p:SkipCliBuild=true \
        -p:PublishSingleFile=false \
        -o "dist/release/gui-$platform" \
        --verbosity quiet
    
    # Copy CLI executable to GUI output
    if [[ "$platform" == "win-x64" ]]; then
        cp "dist/release/gui-$platform/cli-temp/DocxTemplate.CLI.exe" "dist/release/gui-$platform/docx-template.exe"
    else
        cp "dist/release/gui-$platform/cli-temp/DocxTemplate.CLI" "dist/release/gui-$platform/docx-template"
        chmod +x "dist/release/gui-$platform/docx-template"
    fi
    
    # Copy templates if they exist
    if [ -d "templates" ]; then
        cp -r templates "dist/release/gui-$platform/"
    fi
    
    # Clean up temp CLI build
    rm -rf "dist/release/gui-$platform/cli-temp"
    
    # Create macOS app bundle
    if [[ "$platform" == "osx-arm64" ]]; then
        echo "  Creating macOS app bundle..."
        cd "dist/release/gui-$platform"
        
        mkdir -p DocxTemplate.app/Contents/{MacOS,Resources}
        
        # Copy executable and libraries
        cp DocxTemplate.UI docx-template lib*.dylib DocxTemplate.app/Contents/MacOS/
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
        
        cd ../../..
    fi
    
    # Create Windows portable package
    if [[ "$platform" == "win-x64" ]]; then
        echo "  Creating Windows portable package..."
        cd "dist/release/gui-$platform"
        
        # Create a clean portable folder
        mkdir -p DocxTemplate-Portable
        cp DocxTemplate.UI.exe docx-template.exe *.dll DocxTemplate-Portable/
        if [ -d "templates" ]; then
            cp -r templates DocxTemplate-Portable/
        fi
        
        # Create a simple launcher batch file
        cat > DocxTemplate-Portable/Launch-DocxTemplate.bat << 'EOF'
@echo off
echo Starting DocxTemplate...
start "" "%~dp0DocxTemplate.UI.exe"
EOF
        
        # Create a README for Windows users
        cat > DocxTemplate-Portable/README.txt << 'EOF'
DocxTemplate - Word Document Template Processor
===============================================

Quick Start:
1. Double-click "DocxTemplate.UI.exe" to start the application
2. Or double-click "Launch-DocxTemplate.bat" for the same effect

Files included:
- DocxTemplate.UI.exe - Main application
- docx-template.exe - Command-line tool  
- templates/ - Sample template files
- *.dll - Required libraries

System Requirements:
- Windows 10 or later
- No additional software installation required

For support, visit: https://github.com/your-repo/docx-template-cli
EOF
        
        cd ../../..
    fi
done

echo "📁 Creating distribution archives..."
cd dist/release

# Create CLI archives
for platform in "${CLI_PLATFORMS[@]}"; do
    if [[ "$platform" == "win-x64" ]]; then
        (cd "cli-$platform" && zip -r "../docx-template-cli-$platform.zip" .)
    else
        tar -czf "docx-template-cli-$platform.tar.gz" -C "cli-$platform" .
    fi
done

# Create GUI archives
for platform in "${GUI_PLATFORMS[@]}"; do
    if [[ "$platform" == "win-x64" ]]; then
        # Create portable ZIP package
        (cd "gui-$platform" && zip -r "../DocxTemplate-Windows-Portable.zip" DocxTemplate-Portable)
        # Also create complete development package
        (cd "gui-$platform" && zip -r "../docx-template-gui-$platform.zip" .)
    elif [[ "$platform" == "osx-arm64" ]]; then
        # Create both app bundle and regular archive
        tar -czf "docx-template-gui-$platform.tar.gz" -C "gui-$platform" .
        (cd "gui-$platform" && zip -r "../DocxTemplate-macOS.zip" DocxTemplate.app)
    else
        tar -czf "docx-template-gui-$platform.tar.gz" -C "gui-$platform" .
    fi
done

cd ../..

echo "✅ Release build complete!"
echo "📦 CLI packages: dist/release/docx-template-cli-*.{zip,tar.gz}"
echo "🖥️  GUI packages: dist/release/docx-template-gui-*.{zip,tar.gz}"
echo "🍎 macOS app bundle: dist/release/DocxTemplate-macOS.zip"
echo "🪟 Windows portable: dist/release/DocxTemplate-Windows-Portable.zip"