# DOCX Processing System

This document provides a comprehensive overview of the DOCX placeholder scanning and replacement system.

## Overview

The DOCX processing system handles the discovery and replacement of placeholders within Word documents (.docx files). It supports both text placeholders (`{{NAME}}`) and image placeholders (`{{image:logo|width:100|height:100}}`), handling Word's complex internal structure where placeholders are often split across multiple XML runs.

## Core Architecture

### Processing Flow
1. **Document Traversal** - `DocumentTraverser` systematically processes all document parts
2. **Placeholder Detection** - `PlaceholderReplacementEngine` discovers placeholders across split runs
3. **Text Reconstruction** - Rebuilds complete placeholder text from fragmented runs
4. **Replacement Logic** - Replaces placeholders while preserving formatting

### Key Components

#### PlaceholderReplacementEngine (`PlaceholderReplacementEngine.cs`)
The central engine that consolidates all placeholder processing logic:
- **Text Reconstruction**: Rebuilds paragraph text from split runs
- **Placeholder Discovery**: Finds both text and image placeholders using regex patterns
- **Replacement Strategy**: Uses right-to-left replacement to avoid index shifting
- **Processing Modes**: Scan (read-only) vs Replace (modification)

#### DocumentTraverser (`DocumentTraverser.cs`)
Systematically processes all document parts:
- Main document body
- Headers (first page, even pages, odd pages)
- Footers (first page, even pages, odd pages)
- Table cells and nested structures

## The Text Run Splitting Problem

### What is Text Run Splitting?
Word internally stores text in `<w:r>` (run) elements within `<w:p>` (paragraph) elements. A single placeholder like `{{COMPANY}}` might be split across multiple runs:

```xml
<w:p>
  <w:r><w:t>{{COM</w:t></w:r>
  <w:r><w:t>PANY}}</w:t></w:r>
</w:p>
```

### Why Does This Happen?
- **Formatting Changes**: Different parts have different formatting
- **Spell Check**: Word's spell checker creates breaks
- **Copy/Paste Operations**: Can fragment existing text
- **Track Changes**: Editing history creates run boundaries

### Our Solution
1. **Paragraph-Level Processing**: Process entire paragraphs as units
2. **Text Reconstruction**: Concatenate all run texts to find complete placeholders
3. **Position Mapping**: Track which text elements contribute to each character position
4. **Smart Replacement**: Replace text while preserving run structure

## Placeholder Types

### Text Placeholders
**Pattern**: `{{PLACEHOLDER_NAME}}`
- Case-insensitive matching
- Supports alphanumeric names with underscores
- Preserves surrounding formatting

**Example**:
```
Input: "Dear {{CUSTOMER_NAME}}, your order {{ORDER_ID}} is ready."
Mapping: {"CUSTOMER_NAME": "John Doe", "ORDER_ID": "12345"}
Output: "Dear John Doe, your order 12345 is ready."
```

### Image Placeholders
**Pattern**: `{{image:imageName|width:100|height:100}}`
- Automatically detected during scanning
- Requires width and height parameters
- Replaces with actual image from mapping

**Example**:
```
Placeholder: {{image:company_logo|width:200|height:100}}
Mapping: {"company_logo": "/path/to/logo.png"}
Result: Logo image inserted with 200x100 dimensions
```

## Processing Algorithm

### 1. Text Reconstruction (`ReconstructParagraphText`)
```csharp
private string ReconstructParagraphText(Paragraph paragraph)
{
    var textElements = paragraph.Descendants<Text>().ToList();
    return string.Concat(textElements.Select(t => t.Text ?? string.Empty));
}
```

### 2. Text Element Mapping (`BuildTextElementMap`)
Creates a map from character positions to Text elements:
- Tracks which Text element owns each character
- Handles multiple Text elements contributing to one placeholder
- Enables precise replacement without breaking document structure

### 3. Placeholder Discovery (`FindAllPlaceholders`)
- Uses regex to find placeholder patterns in reconstructed text
- Extracts placeholder names and positions
- Handles both text and image placeholder patterns
- Returns `PlaceholderMatch` objects with position and type information

### 4. Replacement Strategy (`ReplaceTextPlaceholders`)
- **Right-to-Left Processing**: Replaces placeholders from end to start to avoid index shifting
- **Text Element Resolution**: Maps placeholder positions back to original Text elements
- **Content Replacement**: Updates Text element content while preserving formatting
- **Fallback Mechanism**: Handles complex cases where mapping fails

## Key Methods Reference

### `ProcessDocumentAsync`
Main entry point for processing a document part:
- **Scan Mode**: Discovers and records placeholder information
- **Replace Mode**: Performs actual placeholder replacement

### `ProcessParagraph`
Processes a single paragraph for placeholders:
1. Reconstructs full paragraph text
2. Builds text element mapping
3. Finds all placeholders in reconstructed text
4. Executes replacement or recording based on mode

### `ReplaceTextPlaceholders`
Handles the actual replacement of text placeholders:
- Processes matches in reverse order (right-to-left)
- Maps placeholder positions to Text elements
- Replaces content while preserving document structure
- Implements fallback for edge cases

### `ProcessImagePlaceholder`
Handles image placeholder replacement:
- Validates image file existence
- Converts dimensions to EMU (English Metric Units)
- Creates new image runs with proper formatting
- Replaces placeholder runs with image content

## Error Handling and Edge Cases

### Null Safety
The code includes extensive null checks because:
- **OpenXML Optional Elements**: Many elements can be null
- **Public API Safety**: Methods validate input parameters
- **Graceful Degradation**: Handles missing replacement values
- **Runtime Protection**: Prevents NullReferenceExceptions

### Complex Document Structures
- **Nested Tables**: Handles placeholders within table cells
- **Headers/Footers**: Processes all document sections uniformly
- **Multiple Sections**: Supports documents with different headers per section
- **Mixed Content**: Handles paragraphs with both text and image placeholders

### Fallback Mechanisms
When normal text element mapping fails:
1. **Paragraph-Level Search**: Searches entire paragraph for placeholder text
2. **Partial Matching**: Handles cases where placeholder spans multiple elements
3. **Content Preservation**: Maintains document structure even during complex replacements

## Performance Considerations

### Parallel Processing
- `PlaceholderScanService` uses parallel processing for multiple files
- Semaphore limits concurrent operations to available CPU cores
- Individual document processing remains single-threaded for safety

### Memory Management
- Uses `using` statements for proper disposal of OpenXML documents
- Minimizes string allocations during text reconstruction
- Efficient regex compilation with timeout protection

### Optimization Strategies
- **Single-Pass Processing**: Combines scanning and replacement when possible
- **Efficient Text Mapping**: Uses arrays for O(1) position lookups
- **Right-to-Left Replacement**: Avoids expensive index recalculation

## Testing Strategy

The system includes comprehensive tests in `PlaceholderReplaceSplitRunsTests.cs`:

### Test Scenarios
- **Split Placeholders**: Placeholders divided across 2-3 runs
- **Multiple Placeholders**: Several placeholders in same paragraph
- **Mixed Content**: Combination of text and formatting
- **Edge Cases**: Empty elements, null values, malformed placeholders

### Test Pattern
```csharp
// arrange - Create test document with split placeholders
// act - Scan for placeholders, then replace them
// assert - Verify discovery, replacement, and final content
```

## Integration Points

### Service Layer Integration
- **PlaceholderScanService**: Uses engine in scan mode
- **PlaceholderReplaceService**: Uses engine in replace mode
- **DocumentTraverser**: Provides systematic document part processing

### UI Integration
- Progress reporting through logging interfaces
- Cancellation support via CancellationToken
- Error handling with structured error information

This processing system handles the complexity of Word document structure while providing a clean, reliable interface for placeholder operations.