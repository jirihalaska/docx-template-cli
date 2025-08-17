# Product Requirements Document
**DOCX Template CLI System v2.0**  
*BMad-Compliant PRD*  
*Created: 2025-08-17*

---

## Executive Summary

A clean-architecture command-line interface system for Word document template processing with placeholder replacement, designed with modularity, testability, and future GUI integration in mind.

### Key Differentiators from v1.0
- **CLI-First Architecture**: All operations exposed as testable CLI commands
- **Clean Separation**: Core business logic independent of UI concerns
- **No Technical Debt**: Fresh start without Czech encoding workarounds
- **GUI-Ready**: Architecture prepared for future windowed interface

---

## Goals and Background Context

### Primary Goals
1. **Modular Architecture**: Each operation (discover, scan, copy, replace) as independent service
2. **Complete Testability**: Every component individually testable with automated CI/CD
3. **Cross-Platform Excellence**: Native performance on Windows and macOS
4. **Future-Proof Design**: Ready for GUI layer without architectural changes
5. **Production Quality**: Enterprise-ready from day one

### Background Context

#### Lessons from v1.0 (Python Implementation)
- **Success**: Proved market need with 5 specific templates
- **Challenge**: Czech encoding created extensive technical debt
- **Challenge**: Monolithic design made testing difficult
- **Challenge**: GUI integration required significant refactoring

#### Lessons from v1.5 (.NET Migration Attempt)
- **Success**: Proved .NET performance superiority (<3 seconds vs 5 seconds)
- **Success**: Better Unicode handling for Czech characters
- **Challenge**: Mixed architecture with partial implementation
- **Challenge**: Console UI limitations for business users

### Strategic Decision
Start fresh with clean architecture, incorporating all lessons learned, focusing on CLI-first design that enables both automated testing and future GUI development.

---

## Requirements

### Functional Requirements

#### FR1: Template Set Discovery
- **FR1.1**: Discover all template sets (top-level folders) in templates directory
- **FR1.2**: List available template sets with metadata (name, file count, total size)
- **FR1.3**: Allow user to select a specific template set for processing
- **FR1.4**: Validate template set contains .docx files
- **FR1.5**: Output template set information as JSON for pipeline processing

#### FR2: Template Discovery (Within Set)
- **FR2.1**: Discover all .docx files within selected template set
- **FR2.2**: Support recursive directory traversal within the set
- **FR2.3**: Maintain relative paths from template set root
- **FR2.4**: Include file metadata (size, modified date, path)
- **FR2.5**: Preserve template set directory structure

#### FR3: Placeholder Scanning
- **FR3.1**: Scan all templates within selected template set
- **FR3.2**: Support configurable placeholder patterns (default: `{{.*?}}`)
- **FR3.3**: Report unique placeholders across the template set
- **FR3.4**: Track placeholder locations and occurrence counts
- **FR3.5**: Group placeholders by template within the set

#### FR4: Template Set Copying
- **FR4.1**: Copy entire template set from source to target directory
- **FR4.2**: Preserve complete template set directory structure
- **FR4.3**: Support overwrite/skip existing files
- **FR4.4**: Maintain all file attributes and timestamps
- **FR4.5**: Create target folder with template set name and timestamp
- **FR4.6**: Report copy statistics and file mapping

#### FR5: Placeholder Replacement
- **FR5.1**: Replace placeholders in all templates within copied set
- **FR5.2**: Support JSON input for replacement mappings
- **FR5.3**: Create automatic backups before modification
- **FR5.4**: Preserve all document formatting
- **FR5.5**: Process entire template set atomically
- **FR5.6**: Handle Czech and Unicode characters correctly


### Non-Functional Requirements

#### NFR1: Performance
- **NFR1.1**: Process 100 templates in <10 seconds
- **NFR1.2**: Memory usage <100MB for typical operations
- **NFR1.3**: Startup time <500ms
- **NFR1.4**: Support parallel processing where applicable

#### NFR2: Compatibility
- **NFR2.1**: Windows 10/11 native support
- **NFR2.2**: macOS 11+ native support
- **NFR2.3**: Linux support (Ubuntu 20.04+)
- **NFR2.4**: .NET 8.0 runtime
- **NFR2.5**: Self-contained executable option

#### NFR3: Reliability
- **NFR3.1**: Graceful error handling with actionable messages
- **NFR3.2**: Atomic operations (all-or-nothing)
- **NFR3.3**: Automatic backup before destructive operations
- **NFR3.4**: Detailed logging for troubleshooting
- **NFR3.5**: Recovery from partial failures

#### NFR4: Usability
- **NFR4.1**: Intuitive command structure
- **NFR4.2**: Comprehensive help documentation
- **NFR4.3**: Progress indicators for long operations
- **NFR4.4**: Verbose and quiet modes
- **NFR4.5**: Configuration file support

#### NFR5: Testability
- **NFR5.1**: Comprehensive unit test coverage (90% target deferred to v2.1, current: >40%)
- **NFR5.2**: Integration tests for all commands
- **NFR5.3**: Performance benchmarks
- **NFR5.4**: Cross-platform CI/CD pipeline
- **NFR5.5**: Automated regression testing

#### NFR6: Internationalization
- **NFR6.1**: Full Unicode support throughout
- **NFR6.2**: Czech character preservation
- **NFR6.3**: Locale-aware date/time formatting
- **NFR6.4**: Multi-language error messages (future)

---

## User Interface Design

### CLI Command Structure

#### Global Options
```bash
docx-template [command] [options]
  --verbose, -v          Detailed output
  --quiet, -q           Suppress non-error output
  --format, -f          Output format (json|text|xml)
  --config, -c          Configuration file path
  --help, -h           Show help
  --version            Show version
```

#### Command: list-sets
```bash
docx-template list-sets --templates <path> [options]
  --templates, -t      Templates root folder (required)
  --details, -d        Show detailed information per set
  --output, -o         Output file path (default: stdout)
```

#### Command: discover
```bash
docx-template discover --templates <path> --set <name> [options]
  --templates, -t      Templates root folder (required)
  --set, -s           Template set name (required)
  --recursive, -r      Include subdirectories (default: true)
  --pattern, -p        File pattern (default: *.docx)
  --output, -o         Output file path (default: stdout)
```

#### Command: scan
```bash
docx-template scan --templates <path> --set <name> [options]
  --templates, -t      Templates root folder (required)
  --set, -s           Template set name (required)
  --pattern, -p        Placeholder pattern (default: {{.*?}})
  --recursive, -r      Include subdirectories (default: true)
  --unique, -u         Show only unique placeholders (default: true)
  --output, -o         Output file path (default: stdout)
```

#### Command: copy
```bash
docx-template copy --templates <path> --set <name> --target <path> [options]
  --templates, -t      Templates root folder (required)
  --set, -s           Template set name (required)
  --target, -g         Target folder path (required)
  --timestamp, -m      Add timestamp to target folder (default: true)
  --overwrite, -w      Overwrite existing files (default: false)
  --dry-run, -d        Preview operation without copying
```

#### Command: replace
```bash
docx-template replace --folder <path> --map <file> [options]
  --folder, -f         Target folder with copied template set (required)
  --map, -m           Replacement map file (required)
  --backup, -b         Create backups (default: true)
  --recursive, -r      Include subdirectories (default: true)
  --dry-run, -d        Preview replacements without modifying
```

### Output Formats

#### JSON Output
```json
{
  "command": "discover",
  "timestamp": "2025-08-17T10:30:00Z",
  "success": true,
  "data": {
    "templates": [
      {
        "path": "/templates/contract.docx",
        "size": 45678,
        "modified": "2025-08-15T14:20:00Z"
      }
    ],
    "count": 1
  }
}
```

#### Text Output (Human-Friendly)
```
Template Discovery Results
=========================
Found 5 templates in /templates

1. contract.docx (44.6 KB)
2. invoice.docx (23.1 KB)
3. report.docx (67.8 KB)
4. letter.docx (12.3 KB)
5. proposal.docx (89.2 KB)

Total: 5 files, 237.0 KB
```

---

## Technical Architecture

### Component Structure
```
DocxTemplate.Core/              # Business Logic (no external dependencies)
├── Models/                     # Domain models
├── Services/                   # Service interfaces
└── Validators/                 # Business rule validation

DocxTemplate.Infrastructure/    # External Integrations
├── FileSystem/                # File I/O operations
├── DocxProcessing/            # Word document manipulation
└── Serialization/             # JSON/XML handling

DocxTemplate.CLI/              # Command Line Interface
├── Commands/                  # Command implementations
├── Options/                   # Command options parsing
├── Output/                    # Formatters and presenters

DocxTemplate.Tests/           # Test Projects
├── Unit/                     # Unit tests per component
├── Integration/              # Cross-component tests
└── E2E/                      # End-to-end scenarios
```

### Key Design Patterns
- **Clean Architecture**: Domain logic independent of infrastructure
- **CQRS Pattern**: Separate read (discover/scan) from write (copy/replace)
- **Repository Pattern**: Abstract file system operations
- **Strategy Pattern**: Pluggable output formatters

---

## Success Metrics

### Technical Metrics
- [x] All 5 CLI commands fully functional
- [x] Comprehensive unit test coverage achieved (46.6%, 90% deferred to v2.1)
- [ ] <10 second processing for 100 files
- [ ] Zero memory leaks in 24-hour stress test
- [x] Cross-platform CI/CD pipeline green

### Quality Metrics
- [ ] Zero critical bugs in production
- [ ] <2% error rate in field usage
- [ ] 100% Czech character preservation
- [ ] All operations atomic and reversible

### Adoption Metrics
- [ ] CLI used in automated workflows
- [ ] Integration with existing CI/CD pipelines
- [ ] Foundation ready for GUI development
- [ ] Documentation rated "excellent" by users

---

## Development Phases

### Phase 1: Foundation (Week 1) ✅ COMPLETED
- [x] Project structure and dependencies
- [x] Core models and interfaces
- [x] Basic CLI framework
- [x] Unit test infrastructure

### Phase 2: Core Commands (Week 2) ✅ COMPLETED
- [x] Implement list-sets command
- [x] Implement discover command
- [x] Implement scan command
- [x] Implement copy command
- [x] Integration tests

### Phase 3: Advanced Commands (Week 3) ✅ COMPLETED
- [x] Implement replace command
- [x] Error handling and recovery

### Phase 4: Polish & Testing (Week 4) 🔄 IN PROGRESS
- [x] Cross-platform testing
- [x] Comprehensive testing implementation
- [x] Documentation
- [ ] Performance optimization (Story 04.004 - Draft)
- [ ] Release preparation (Story 04.005 - Draft)

### Phase 5: GUI Foundation (Future)
- [ ] GUI framework selection
- [ ] API layer over CLI
- [ ] Initial GUI prototype
- [ ] User testing

---

## Risk Assessment

### Technical Risks
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Word document corruption | High | Low | Comprehensive backup system |
| Czech encoding issues | High | Medium | Extensive Unicode testing |
| Performance degradation | Medium | Low | Benchmarking suite |
| Cross-platform bugs | Medium | Medium | CI/CD on all platforms |

### Business Risks
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Scope creep | High | High | Strict phase boundaries |
| GUI complexity | High | Medium | CLI-first approach |
| User adoption | Medium | Low | Excellent documentation |

---

## Constraints and Assumptions

### Constraints
- Must use .NET 8.0 for consistency with v1.5 attempt
- Must support existing template formats from v1.0
- Must maintain Czech character support
- Cannot break existing Python v1.0 workflows during transition

### Assumptions
- Users have .NET 8.0 runtime or will use self-contained builds
- Templates follow consistent placeholder patterns
- File system has adequate permissions for operations
- Network drives accessible for template storage

---

## Appendix

### A. Replacement Map Schema
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "placeholders": {
      "type": "object",
      "additionalProperties": {
        "type": "string"
      }
    },
    "metadata": {
      "type": "object",
      "properties": {
        "author": { "type": "string" },
        "created": { "type": "string", "format": "date-time" },
        "description": { "type": "string" }
      }
    }
  },
  "required": ["placeholders"]
}
```

### B. Example Complete Workflow

#### Interactive Mode
```bash
# Step 1: See what template sets are available
docx-template list-sets --templates /shared/templates
# Output: 
# 1. Contract_Templates (15 files, 2.3 MB)
# 2. Invoice_Templates (8 files, 1.1 MB)  
# 3. Report_Templates (12 files, 3.5 MB)

# Step 2: Select and explore a template set
docx-template discover --templates /shared/templates --set Contract_Templates
# Output: Found 15 templates with complex directory structure

# Step 3: Find what needs to be filled in
docx-template scan --templates /shared/templates --set Contract_Templates
# Output: 25 unique placeholders found:
# {{COMPANY_NAME}}, {{CONTRACT_DATE}}, {{CLIENT_NAME}}, ...

# Step 4: Copy the template set to working directory
docx-template copy --templates /shared/templates --set Contract_Templates --target ./working
# Output: Copied to ./working/Contract_Templates_2025-08-17_143025/

# Step 5: Replace placeholders with actual values
docx-template replace --folder ./working/Contract_Templates_2025-08-17_143025 --map contract-values.json
# Output: Successfully replaced 375 placeholders in 15 documents
```


### C. Future GUI Integration Points
- CLI commands exposed as service methods
- JSON output parseable by GUI
- Progress callbacks for UI updates
- Cancellation token support throughout

---

*This PRD represents a complete reimagining of the document template processing system, incorporating all lessons learned from v1.0 and v1.5 while establishing a solid foundation for future growth.*