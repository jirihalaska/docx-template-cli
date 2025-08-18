# CLI Command Reference

This document provides precise documentation of the DocxTemplate CLI commands for programmatic integration.

## Overview

The DocxTemplate CLI provides five main commands for working with DOCX template files:
- `list-sets` - List template sets (top-level directories containing templates)
- `discover` - Find DOCX files within a directory
- `scan` - Find placeholders in DOCX files using regex patterns
- `copy` - Copy templates with performance metrics
- `replace` - Replace placeholders in DOCX files with values from a JSON mapping file

All commands support JSON output format for programmatic use.

## Commands

### discover
**Purpose:** Find all DOCX template files in a directory tree

**Syntax:**
```bash
discover --path <directory> [options]
```

**Required Parameters:**
- `--path, -p`: Directory path to scan

**Optional Parameters:**
- `--recursive, -r`: Scan subdirectories (default: true)
- `--format, -f`: Output format (text|json|table|csv, default: text)
- `--include, -i`: File patterns to include (default: *.docx)
- `--exclude, -e`: File patterns to exclude
- `--max-depth, -d`: Maximum directory depth
- `--min-size`: Minimum file size in bytes
- `--max-size`: Maximum file size in bytes
- `--modified-after`: Files modified after date (yyyy-MM-dd)
- `--modified-before`: Files modified before date (yyyy-MM-dd)
- `--quiet, -q`: Suppress progress messages (default: false)

**JSON Output Schema:**
```json
{
  "command": "discover",
  "timestamp": "2025-08-17T22:47:32.307928Z",
  "success": true,
  "data": {
    "templates": [
      {
        "full_path": "/tmp/test-cli/test.docx",
        "relative_path": "test.docx",
        "file_name": "test.docx",
        "size_bytes": 3,
        "size_formatted": "3 B",
        "last_modified": "2025-08-17T22:47:26.9833586Z",
        "directory": "/tmp/test-cli"
      }
    ],
    "total_count": 1,
    "total_size": 3,
    "total_size_formatted": "3 B"
  }
}
```

**Other Output Formats:**
- **Text**: List of relative paths with size and modification date
- **CSV**: RelativePath,FileName,SizeBytes,LastModified,Directory
- **Table**: Unicode bordered table with columns for File Path, Size, Last Modified

---

### scan
**Purpose:** Find placeholders within DOCX templates using regex patterns

**Syntax:**
```bash
scan --path <directory|file> [options]
```

**Required Parameters:**
- `--path, -p`: Directory or file path to scan

**Optional Parameters:**
- `--recursive, -r`: Scan subdirectories (default: true)
- `--pattern`: Regex patterns for placeholders (default: `{{.*?}}` - matches double curly braces)
- `--format, -f`: Output format (text|json|table|csv, default: text)
- `--statistics, -s`: Include detailed statistics (default: false)
- `--case-sensitive, -c`: Case-sensitive matching (default: false)
- `--parallelism`: Number of parallel threads (default: Environment.ProcessorCount)
- `--quiet, -q`: Suppress progress messages (default: false)

**JSON Output Schema:**
```json
{
  "command": "scan",
  "timestamp": "2025-08-17T22:47:49.434492Z",
  "success": true,
  "data": {
    "placeholders": [],
    "summary": {
      "unique_placeholders": 0,
      "total_occurrences": 0,
      "files_scanned": 1,
      "files_with_placeholders": 0,
      "failed_files": 0,
      "scan_duration_ms": 12.376,
      "coverage_percentage": 0
    },
    "statistics": null,
    "errors": []
  }
}
```

---

### copy
**Purpose:** Copy DOCX templates to target directory with performance metrics

**Syntax:**
```bash
copy --source <source_dir> --target <target_dir> [options]
```

**Required Parameters:**
- `--source, -s`: Source directory containing templates
- `--target, -t`: Target directory for copied templates

**Optional Parameters:**
- `--preserve-structure, -p`: Preserve directory structure (default: true)
- `--overwrite, --force, -f`: Overwrite existing files (default: false)
- `--dry-run, -d`: Show what would be copied (default: false)
- `--format, -o`: Output format (text|json|table|csv, default: text)
- `--quiet, -q`: Suppress progress messages (default: false)
- `--validate, -v`: Validate copy operation before executing (default: false)
- `--estimate, -e`: Show disk space estimate for the copy operation (default: false)

**JSON Output Schema:**
```json
{
  "command": "copy",
  "timestamp": "2025-08-17T22:47:54.640757Z",
  "success": true,
  "data": {
    "summary": {
      "files_copied": 1,
      "files_failed": 0,
      "total_files_attempted": 1,
      "total_bytes_copied": 3,
      "total_size_display": "3 B",
      "duration_ms": 3.235,
      "success_rate_percentage": 100,
      "throughput_display": "927 B/s",
      "files_per_second": 309.1190108191654,
      "bytes_per_second": 927.3570324574961,
      "average_file_size": 3
    },
    "copied_files": [
      {
        "source_path": "/tmp/test-cli/test.docx",
        "target_path": "/tmp/test-copy/test.docx",
        "size_bytes": 3,
        "size_display": "3 B",
        "copy_duration_ms": 0.756,
        "copied_at": "2025-08-17T22:47:54.632898Z"
      }
    ],
    "errors": []
  }
}
```

---

### replace
**Purpose:** Replace placeholders in DOCX files with values from a JSON mapping file

**Syntax:**
```bash
replace --folder <target_dir> --map <mapping_file> [options]
```

**Required Parameters:**
- `--folder, -f`: Target directory containing templates to process
- `--map, -m`: JSON file containing placeholder-to-value mappings

**Optional Parameters:**
- `--backup, -b`: Create backups before replacement (default: true)
- `--recursive, -r`: Include subdirectories (default: true)
- `--dry-run, -d`: Preview replacements without modifying files (default: false)
- `--format, -o`: Output format (text|json, default: text)
- `--quiet, -q`: Suppress progress messages (default: false)
- `--pattern, -p`: Placeholder regex pattern (default: `{{.*?}}`)

**Mapping File Format:**
```json
{
  "{{COMPANY_NAME}}": "Acme Corporation",
  "{{CONTRACT_DATE}}": "2025-08-17",
  "{{CLIENT_NAME}}": "John Doe",
  "{{AMOUNT}}": "150000"
}
```

**JSON Output Schema:**
```json
{
  "command": "replace",
  "timestamp": "2025-08-17T10:30:00Z",
  "success": true,
  "data": {
    "summary": {
      "files_processed": 15,
      "files_modified": 12,
      "files_failed": 0,
      "total_replacements": 375,
      "backup_created": true,
      "backup_directory": "/target/.backup_20250817_103000",
      "duration_ms": 2340.5
    },
    "file_results": [
      {
        "file_path": "/target/contract.docx",
        "replacements_made": 25,
        "backup_path": "/target/.backup_20250817_103000/contract.docx",
        "status": "success"
      }
    ],
    "errors": []
  }
}
```

**Other Output Formats:**
- **Text**: Summary with counts and file-by-file results
- **Dry-run**: Shows what replacements would be made without file modification

---

### list-sets
**Purpose:** List all template sets (directories) in templates directory

**Syntax:**
```bash
list-sets --templates <templates_dir> [options]
```

**Required Parameters:**
- `--templates, -t`: Path to templates root directory

**Optional Parameters:**
- `--format, -f`: Output format (text|json|table|list, default: text)
- `--details, -d`: Show detailed information (default: false)
- `--include-empty, -e`: Include empty folders (default: false)

**JSON Output Schema:**
```json
{
  "command": "list-sets",
  "timestamp": "2025-08-17T22:47:43.709199Z",
  "success": true,
  "data": {
    "template_sets": [
      {
        "name": "Set Name",
        "path": "/full/path/to/set",
        "file_count": 5,
        "total_size": 123456,
        "total_size_formatted": "120.6 KB",
        "last_modified": "2025-08-17T22:47:43.709199Z",
        "status": "valid|invalid",
        "has_subfolders": true,
        "directory_depth": 2,
        "templates": []
      }
    ],
    "total_sets": 1
  }
}
```

## Planned Commands (Not Yet Implemented)

*No planned commands - all core functionality has been implemented.*

## Error Handling

**Current Error Behavior:**
- Errors are written to stderr, not JSON format
- JSON format is only used for successful operations
- Exit codes: 0 (success), 1 (general error), 2 (partial failure for copy command)

**Error Output Example:**
```
Error: Folder path not found: /nonexistent
```

**Exit Codes:**
- `0`: Success (Note: scan command returns 0 even when path doesn't exist)
- `1`: General error or exception
- `2`: Partial failure (copy command only when some files fail to copy)

## Current Limitations

1. **No standardized error JSON format** - Errors go to stderr as text
2. **Console output mixed with JSON** - Progress messages appear before JSON in quiet mode
3. **JSON field naming** - Property names use snake_case (e.g., `template_sets`, `full_path`) despite JsonNamingPolicy.CamelCase in code

## Maintenance

**For Future CLI Changes:**
1. Test actual CLI output after any command modifications
2. Update JSON schemas in this document to match actual output
3. Verify parameter lists and defaults are accurate
4. Update error handling documentation if behavior changes
5. Ensure documentation reflects current implementation exactly