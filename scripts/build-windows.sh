#!/bin/bash
set -e

PLATFORM="win-x64"
OUTPUT_DIR="${1:-dist/windows-release}"

echo "🪟 Building DocxTemplate for Windows..."

# Clean output directory
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

echo "📦 Building CLI executable..."
dotnet publish src/DocxTemplate.CLI -c Release -r "$PLATFORM" --self-contained \
    -o "$OUTPUT_DIR/cli-temp" \
    --verbosity quiet

echo "🎨 Building GUI application..."
dotnet publish src/DocxTemplate.UI -c Release -r "$PLATFORM" --self-contained \
    -p:SkipCliBuild=true \
    -o "$OUTPUT_DIR/build-temp" \
    --verbosity quiet

echo "📁 Creating distribution packages..."

# 1. Portable Package (Recommended for most users)
echo "  Creating portable package..."
mkdir -p "$OUTPUT_DIR/portable/DocxTemplate"
cp "$OUTPUT_DIR/build-temp/DocxTemplate.UI.exe" "$OUTPUT_DIR/portable/DocxTemplate/"
cp "$OUTPUT_DIR/cli-temp/DocxTemplate.CLI.exe" "$OUTPUT_DIR/portable/DocxTemplate/docx-template.exe"
cp "$OUTPUT_DIR/build-temp"/*.dll "$OUTPUT_DIR/portable/DocxTemplate/" 2>/dev/null || true

# Copy templates if they exist
if [ -d "templates" ]; then
    cp -r templates "$OUTPUT_DIR/portable/DocxTemplate/"
fi

# Create launcher batch file
cat > "$OUTPUT_DIR/portable/DocxTemplate/Launch-DocxTemplate.bat" << 'EOF'
@echo off
cd /d "%~dp0"
echo Starting DocxTemplate GUI...
start "" "DocxTemplate.UI.exe"
EOF

# Create CLI batch file for command-line users
cat > "$OUTPUT_DIR/portable/DocxTemplate/docx-template.bat" << 'EOF'
@echo off
cd /d "%~dp0"
"%~dp0docx-template.exe" %*
EOF

# Create README
cat > "$OUTPUT_DIR/portable/DocxTemplate/README.txt" << 'EOF'
DocxTemplate - Word Document Template Processor
===============================================

QUICK START:
1. Double-click "DocxTemplate.UI.exe" to start the GUI application
2. Or double-click "Launch-DocxTemplate.bat" for the same effect

COMMAND LINE USAGE:
1. Open Command Prompt in this folder
2. Run: docx-template.exe help
3. Or use the batch file: docx-template.bat help

FILES INCLUDED:
- DocxTemplate.UI.exe - Main GUI application
- docx-template.exe - Command-line tool
- docx-template.bat - Command-line wrapper
- Launch-DocxTemplate.bat - GUI launcher
- templates/ - Sample template files
- *.dll - Required runtime libraries

SYSTEM REQUIREMENTS:
- Windows 10 version 1709 or later
- x64 architecture
- No additional software installation required

SUPPORT:
- Documentation: README files in templates folder
- Issues: https://github.com/your-repo/docx-template-cli/issues

VERSION: 1.0
EOF

# 2. Single Executable (Alternative for minimal footprint)
echo "  Creating single-file package..."
mkdir -p "$OUTPUT_DIR/single-file"

# Rebuild with single file options for even smaller footprint
dotnet publish src/DocxTemplate.UI -c Release -r "$PLATFORM" --self-contained \
    -p:SkipCliBuild=true \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -o "$OUTPUT_DIR/single-file-temp" \
    --verbosity quiet > /dev/null

cp "$OUTPUT_DIR/single-file-temp/DocxTemplate.UI.exe" "$OUTPUT_DIR/single-file/"
cp "$OUTPUT_DIR/cli-temp/DocxTemplate.CLI.exe" "$OUTPUT_DIR/single-file/docx-template.exe"

if [ -d "templates" ]; then
    cp -r templates "$OUTPUT_DIR/single-file/"
fi

cat > "$OUTPUT_DIR/single-file/README.txt" << 'EOF'
DocxTemplate - Single File Distribution
=======================================

This is a minimal distribution with fewer files.

USAGE:
- Double-click "DocxTemplate.UI.exe" to start
- Command line: "docx-template.exe help"

Note: First startup may be slower due to extraction.
EOF

# 3. Developer Package (All files for debugging)
echo "  Creating developer package..."
mkdir -p "$OUTPUT_DIR/developer"
cp -r "$OUTPUT_DIR/build-temp"/* "$OUTPUT_DIR/developer/"
cp "$OUTPUT_DIR/cli-temp/DocxTemplate.CLI.exe" "$OUTPUT_DIR/developer/docx-template.exe"

if [ -d "templates" ]; then
    cp -r templates "$OUTPUT_DIR/developer/"
fi

cat > "$OUTPUT_DIR/developer/README-DEVELOPER.txt" << 'EOF'
DocxTemplate - Developer Distribution
====================================

This package contains all files including debug symbols (.pdb files)
for development and troubleshooting purposes.

Use the portable package for end-user distribution.
EOF

echo "📦 Creating ZIP archives..."
cd "$OUTPUT_DIR"

# Create user-friendly ZIP files
zip -r "DocxTemplate-Windows-Portable.zip" portable/ > /dev/null
zip -r "DocxTemplate-Windows-SingleFile.zip" single-file/ > /dev/null
zip -r "DocxTemplate-Windows-Developer.zip" developer/ > /dev/null

cd ../..

# Clean up temp directories
rm -rf "$OUTPUT_DIR/cli-temp" "$OUTPUT_DIR/build-temp" "$OUTPUT_DIR/single-file-temp"

echo "✅ Windows packages created in: $OUTPUT_DIR"
echo "📦 Portable (recommended): $OUTPUT_DIR/DocxTemplate-Windows-Portable.zip"
echo "🗜️  Single-file: $OUTPUT_DIR/DocxTemplate-Windows-SingleFile.zip"
echo "🔧 Developer: $OUTPUT_DIR/DocxTemplate-Windows-Developer.zip"
echo ""
echo "💡 For end users, distribute the Portable package."
echo "💡 For minimal footprint, use the Single-file package."