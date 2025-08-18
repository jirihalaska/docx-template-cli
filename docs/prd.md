# Product Requirements Document
**DOCX Template CLI System v2.0 with GUI**  
*BMad-Compliant PRD*  
*Created: 2025-08-17, Updated: 2025-08-18*

---

## Executive Summary

A comprehensive command-line interface system with graphical user interface for Word document template processing with placeholder replacement. The system features a clean-architecture CLI foundation with an intuitive cross-platform GUI that enables both technical and non-technical users to process document templates efficiently.

### Key Differentiators from v1.0
- **CLI-First Architecture**: All operations exposed as testable CLI commands
- **Clean Separation**: Core business logic independent of UI concerns
- **No Technical Debt**: Fresh start without Czech encoding workarounds
- **GUI Integration**: Cross-platform graphical interface leveraging CLI services
- **Complete Solution**: Single system serving technical users (CLI) and business users (GUI)

---

## Goals and Background Context

### Primary Goals
1. **Modular Architecture**: Each operation (discover, scan, copy, replace) as independent service
2. **Complete Testability**: Every component individually testable with automated CI/CD
3. **Cross-Platform Excellence**: Native performance on Windows and macOS
4. **Dual Interface**: Both CLI for automation and GUI for business users
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
Implement a complete system with CLI foundation and GUI layer, incorporating all lessons learned, focusing on clean architecture that enables both automated testing and intuitive user experience.

---

## User Personas

### Persona 1: Technical User / DevOps Engineer
- **Role**: System Administrator / CI/CD Pipeline Developer
- **Technical Skill**: Expert command-line proficiency
- **Context**: Integrates template processing into automated workflows
- **Needs**: 
  - Scriptable CLI commands with JSON output
  - Reliable automation capabilities
  - Performance and error handling
- **Success Criteria**: "I can automate template processing in our CI/CD pipeline with confidence"

### Persona 2: Business Administrator
- **Role**: Office Administrator / Document Coordinator
- **Technical Skill**: Basic computer literacy, comfortable with Office applications
- **Language**: Primary Czech language interface required
- **Context**: Processes Word document templates regularly (contracts, invoices, reports)
- **Needs**: 
  - Process templates without learning command-line syntax
  - Replace placeholders with client/project information
  - Generate document sets quickly and reliably
  - Visual confirmation that processing succeeded
- **Pain Points**:
  - Command-line interfaces are intimidating and error-prone
  - No visual feedback during processing
- **Success Criteria**: "I can select my templates, fill in the values, click Process, and it just works - all in Czech"

---

## Requirements

### Functional Requirements - CLI Foundation

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
- **FR3.6**: **NEW**: Preserve placeholder discovery order for GUI consistency

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

### Functional Requirements - GUI Interface

#### FR6: Wizard Navigation & Flow
- **FR6.1**: Step-by-step wizard interface with 5 distinct steps
- **FR6.2**: Clear step indicators showing current position (Step 1 of 5)
- **FR6.3**: "Next" and "Back" buttons for navigation between steps
- **FR6.4**: Cannot proceed to next step without completing current step
- **FR6.5**: Standard keyboard navigation (Tab/Enter) and clipboard (Ctrl+X/C/V)

#### FR7: Step 1 - Template Set Selection
- **FR7.1**: Automatically scan `./templates` folder on wizard launch
- **FR7.2**: Display each subfolder as a clickable button/tile
- **FR7.3**: Show template count for each subfolder (e.g., "Contracts (15 files)")
- **FR7.4**: Highlight selected template set with visual indication
- **FR7.5**: "Next" button enabled only after template set selection
- **FR7.6**: Show "No templates found" message if `./templates` folder is missing or empty

#### FR8: Step 2 - Placeholder Discovery & Display
- **FR8.1**: Automatic scanning when advancing from Step 1
- **FR8.2**: Display unique placeholders in discovery order (first occurrence determines position)
- **FR8.3**: Preserve template reading sequence for logical placeholder ordering
- **FR8.4**: Show occurrence count for each placeholder
- **FR8.5**: Status message during scanning
- **FR8.6**: "Next" button enabled after scanning completes
- **FR8.7**: "Back" button to return to template selection

#### FR9: Step 3 - Placeholder Value Input
- **FR9.1**: Text input field for each discovered placeholder in discovery order
- **FR9.2**: Labels showing placeholder names clearly
- **FR9.3**: Maintain same order as Step 2 placeholder list
- **FR9.4**: Text input with automatic whitespace normalization
- **FR9.5**: Clear all values button
- **FR9.6**: "Next" button enabled only when all placeholders have values
- **FR9.7**: "Back" button to return to placeholder discovery

#### FR10: Step 4 - Output Folder Selection
- **FR10.1**: "Select Output Folder" button as primary action
- **FR10.2**: Display selected output path in read-only text field
- **FR10.3**: "Next" button enabled only after output folder selected
- **FR10.4**: "Back" button to return to placeholder input

#### FR11: Step 5 - Processing & Results
- **FR11.1**: Summary of selections (template set, output folder, placeholder count)
- **FR11.2**: "Process Templates" button to start processing
- **FR11.3**: Status messages during processing
- **FR11.4**: Success/error message display
- **FR11.5**: "Open Output Folder" button after successful completion
- **FR11.6**: "Start Over" button to return to Step 1
- **FR11.7**: Processing log for troubleshooting

#### FR12: User Assistance & Localization
- **FR12.1**: All UI text in Czech language
- **FR12.2**: Step titles and descriptions for each wizard step in Czech
- **FR12.3**: Tooltips on all interactive elements in Czech
- **FR12.4**: "Nápověda" menu with Czech user guide link
- **FR12.5**: "O aplikaci" dialog with version information
- **FR12.6**: Clear error messages in Czech with suggested actions
- **FR12.7**: Czech button labels ("Další", "Zpět", "Procházet", "Zpracovat")

### Functional Requirements - System Integration

#### FR13: GUI-CLI Integration ⭐ **NEW REQUIREMENT**
- **FR13.1**: GUI must locate and execute CLI executable (`docx-template.exe` or `docx-template`)
- **FR13.2**: GUI searches for CLI executable in same directory as GUI executable
- **FR13.3**: GUI displays clear error if CLI executable not found in same directory
- **FR13.4**: All GUI operations executed via CLI commands with JSON output parsing
- **FR13.5**: GUI passes all parameters to CLI via command-line arguments (no shared assemblies)
- **FR13.6**: GUI handles CLI process errors and timeouts gracefully

#### FR14: End-to-End Testing ⭐ **NEW REQUIREMENT**
- **FR14.1**: At least 1 automated E2E test proving GUI-CLI integration works
- **FR14.2**: E2E test covers complete happy path: template selection → placeholder scanning → value input → processing → verification
- **FR14.3**: E2E test verifies actual file outputs match expected results
- **FR14.4**: E2E test runs in CI/CD pipeline on both Windows and macOS
- **FR14.5**: E2E test includes both GUI automation and CLI command verification
- **FR14.6**: E2E test validates that GUI produces same results as direct CLI usage

#### FR15: Bundled Distribution ⭐ **NEW REQUIREMENT**
- **FR15.1**: Create single distribution package containing both GUI and CLI executables
- **FR15.2**: GUI executable and CLI executable must be in same directory in distribution
- **FR15.3**: Distribution package available for Windows (zip/installer) and macOS (app bundle/dmg)
- **FR15.4**: Both executables are self-contained (no separate .NET runtime installation required)
- **FR15.5**: Single download provides complete solution for both technical and business users
- **FR15.6**: Version numbers synchronized between GUI and CLI executables
- **FR15.7**: Distribution includes minimal documentation (README with quick start)

### Non-Functional Requirements

#### NFR1: Performance
- **NFR1.1**: Process 100 templates in <10 seconds
- **NFR1.2**: Memory usage <100MB for typical operations
- **NFR1.3**: Startup time <500ms (CLI) / <2 seconds (GUI)
- **NFR1.4**: Support parallel processing where applicable

#### NFR2: Compatibility
- **NFR2.1**: Windows 10/11 native support
- **NFR2.2**: macOS 11+ native support
- **NFR2.3**: Linux support (Ubuntu 20.04+) for CLI
- **NFR2.4**: .NET 8.0 runtime
- **NFR2.5**: Self-contained executable option

#### NFR3: Reliability
- **NFR3.1**: Graceful error handling with actionable messages
- **NFR3.2**: Atomic operations (all-or-nothing)
- **NFR3.3**: Automatic backup before destructive operations
- **NFR3.4**: Detailed logging for troubleshooting
- **NFR3.5**: Recovery from partial failures

#### NFR4: Usability
- **NFR4.1**: CLI: Intuitive command structure for technical users
- **NFR4.2**: GUI: Learnable in <5 minutes without documentation
- **NFR4.3**: Comprehensive help documentation
- **NFR4.4**: Progress indicators for long operations
- **NFR4.5**: Verbose and quiet modes (CLI)
- **NFR4.6**: All GUI actions achievable in ≤3 clicks

#### NFR5: Testability ⭐ **ENHANCED**
- **NFR5.1**: Comprehensive unit test coverage (90% target deferred to v2.1, current: >40%)
- **NFR5.2**: Integration tests for all CLI commands
- **NFR5.3**: **NEW**: End-to-end integration tests proving GUI-CLI cooperation
- **NFR5.4**: Performance benchmarks
- **NFR5.5**: Cross-platform CI/CD pipeline
- **NFR5.6**: Automated regression testing

#### NFR6: Deployment ⭐ **ENHANCED**
- **NFR6.1**: **NEW**: Single distribution package containing both CLI and GUI executables
- **NFR6.2**: Self-contained executables (no runtime installation required)
- **NFR6.3**: **NEW**: GUI can locate CLI executable automatically
- **NFR6.4**: No administrator rights required
- **NFR6.5**: Portable mode support (no installation)

#### NFR7: Internationalization
- **NFR7.1**: Full Unicode support throughout
- **NFR7.2**: Czech character preservation
- **NFR7.3**: Locale-aware date/time formatting
- **NFR7.4**: Multi-language error messages (future)

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
docx-template discover --path <directory> [options]
  --path, -p           Directory path to scan (required)
  --recursive, -r      Include subdirectories (default: true)
  --format, -f         Output format (text|json|table|csv, default: text)
  --include, -i        File patterns to include (default: *.docx)
  --exclude, -e        File patterns to exclude
  --max-depth, -d      Maximum directory depth
  --min-size           Minimum file size in bytes
  --max-size           Maximum file size in bytes
  --modified-after     Files modified after date (yyyy-MM-dd)
  --modified-before    Files modified before date (yyyy-MM-dd)
  --quiet, -q          Suppress progress messages (default: false)
```

#### Command: scan
```bash
docx-template scan --path <directory|file> [options]
  --path, -p           Directory or file path to scan (required)
  --recursive, -r      Include subdirectories (default: true)
  --pattern            Regex patterns for placeholders (default: {{.*?}})
  --format, -f         Output format (text|json|table|csv, default: text)
  --statistics, -s     Include detailed statistics (default: false)
  --case-sensitive, -c Case-sensitive matching (default: false)
  --parallelism        Number of parallel threads (default: ProcessorCount)
  --quiet, -q          Suppress progress messages (default: false)
```

#### Command: copy
```bash
docx-template copy --source <source_dir> --target <target_dir> [options]
  --source, -s         Source directory path (required)
  --target, -t         Target directory path (required)
  --preserve-structure Preserve directory structure (default: true)
  --overwrite, -f      Overwrite existing files (default: false)
  --dry-run, -d        Preview operation without copying
  --format, -o         Output format (text|json|table|csv, default: text)
  --quiet, -q          Suppress progress messages (default: false)
  --validate, -v       Validate copy operation before executing (default: false)
  --estimate, -e       Show disk space estimate (default: false)
```

#### Command: replace
```bash
docx-template replace --folder <path> --map <file> [options]
  --folder, -f         Target folder with copied templates (required)
  --map, -m           Replacement map file (required)
  --backup, -b         Create backups (default: true)
  --recursive, -r      Include subdirectories (default: true)
  --dry-run, -d        Preview replacements without modifying
  --format, -o         Output format (text|json, default: text)
  --quiet, -q          Suppress progress messages (default: false)
  --pattern, -p        Placeholder pattern (default: {{.*?}})
```

### GUI Interface Design

#### Wizard Flow Layout (Czech Interface)

```
┌─────────────────────────────────────────┐
│  Procesor šablon DOCX - Krok 1 z 5     │
├─────────────────────────────────────────┤
│  ● ○ ○ ○ ○  [Indikátory kroků]        │
│                                         │
│  Vyberte sadu šablon                    │
│  [Smlouvy (15)]   [Faktury (8)]       │
│  [Zprávy (12)]    [Dopisy (5)]        │
│                                         │
│                   [Zpět]    [Další]    │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│  Procesor šablon DOCX - Krok 3 z 5     │
├─────────────────────────────────────────┤
│  ● ● ● ○ ○  [Indikátory kroků]        │
│                                         │
│  Zadejte hodnoty zástupných symbolů     │
│  ┌─────────────────────────────┐      │
│  │ {{NÁZEV_FIRMY}}:            │      │
│  │ [____________________]      │      │
│  │ {{DATUM_SMLOUVY}}:          │      │
│  │ [____________________]      │      │
│  └─────────────────────────────┘      │
│  [Vymazat]                             │
│                                         │
│                   [Zpět]    [Další]    │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│  Procesor šablon DOCX - Krok 5 z 5     │
├─────────────────────────────────────────┤
│  ● ● ● ● ●  [Indikátory kroků]        │
│                                         │
│  Zpracování dokončeno!                  │
│  ✓ Zpracováno 15 šablon                │
│  ✓ Výstup: C:\Vystup\Smlouvy_Dnes     │
│                                         │
│  [Otevřít složku] [Začít znovu]        │
└─────────────────────────────────────────┘
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

#### Enhanced JSON Output for Placeholder Scanning ⭐ **NEW**
```json
{
  "command": "scan",
  "success": true,
  "data": {
    "placeholders": [
      {
        "name": "NÁZEV_FIRMY",
        "firstOccurrenceIndex": 1,
        "occurrenceCount": 3,
        "locations": []
      },
      {
        "name": "DATUM_SMLOUVY", 
        "firstOccurrenceIndex": 2,
        "occurrenceCount": 1,
        "locations": []
      }
    ]
  }
}
```

---

## Technical Architecture

### Overall System Architecture ⭐ **ENHANCED**

```
┌─────────────────────────────────────────────────────────┐
│                    User Interfaces                       │
├─────────────────────────────────────────────────────────┤
│  CLI Application          │  GUI Application             │
│  ┌─────────────────┐     │  ┌─────────────────────────┐ │
│  │ DocxTemplate.CLI │◄────┼──┤ DocxTemplate.UI         │ │
│  │                 │     │  │ (Avalonia + Czech)      │ │
│  │ - Commands      │     │  │                         │ │
│  │ - JSON Output   │     │  │ Calls CLI via Process   │ │
│  │ - Validation    │     │  │ Parses JSON responses   │ │
│  └─────────────────┘     │  └─────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
           │                              │
           ▼                              │ (no shared assemblies)
┌─────────────────────────────────────────┼─────────────────┐
│              Service Layer                              │
├─────────────────────────────────────────────────────────┤
│  DocxTemplate.Core (Business Logic)                    │
│  ├── ITemplateDiscoveryService                         │
│  ├── IPlaceholderScanService                           │
│  ├── ITemplateCopyService                              │
│  ├── IPlaceholderReplaceService                        │
│  └── ITemplateSetService                               │
└─────────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────┐
│         Infrastructure Layer                            │
├─────────────────────────────────────────────────────────┤
│  DocxTemplate.Infrastructure                           │
│  ├── FileSystem Operations                             │
│  ├── Word Document Processing                          │
│  └── JSON Serialization                                │
└─────────────────────────────────────────────────────────┘
```

### Key Integration Points ⭐ **NEW**

#### GUI-CLI Communication
```bash
# GUI calls CLI with JSON output for parsing
docx-template.exe list-sets --templates ./templates --format json
docx-template.exe scan --path "./templates/Contracts" --format json  
docx-template.exe copy --source "./templates/Contracts" --target "./output" --format json
docx-template.exe replace --folder "./output" --map "./temp_values.json" --format json
```

#### CLI Executable Discovery Logic
1. Check same directory as GUI executable
2. Display user-friendly error if not found

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

DocxTemplate.UI/               # ⭐ NEW GUI Interface
├── ViewModels/                # MVVM ViewModels
├── Views/                     # Avalonia XAML Views
├── Services/                  # CLI integration services
├── Localization/              # Czech language resources
└── Integration/               # CLI process execution

DocxTemplate.Tests/           # Test Projects
├── Unit/                     # Unit tests per component
├── Integration/              # Cross-component tests
├── E2E/                      # ⭐ NEW End-to-end scenarios
└── GUI/                      # ⭐ NEW GUI automation tests
```

### Key Design Patterns
- **Clean Architecture**: Domain logic independent of infrastructure
- **CQRS Pattern**: Separate read (discover/scan) from write (copy/replace)
- **Repository Pattern**: Abstract file system operations
- **Strategy Pattern**: Pluggable output formatters
- **Process Integration**: GUI-CLI communication via external processes ⭐ **NEW**
- **MVVM Pattern**: GUI separation of concerns ⭐ **NEW**

---

## Success Metrics

### Technical Metrics
- [x] All 5 CLI commands fully functional (5 of 5 implemented)
- [x] Comprehensive unit test coverage achieved (46.6%, 90% deferred to v2.1)
- [ ] **NEW**: GUI successfully integrates with CLI executable in 100% of test scenarios
- [ ] **NEW**: E2E test covers complete GUI-CLI workflow and passes on CI/CD
- [ ] <10 second processing for 100 files
- [ ] Zero memory leaks in 24-hour stress test
- [x] Cross-platform CI/CD pipeline green

### Quality Metrics
- [ ] Zero critical bugs in production
- [ ] <2% error rate in field usage
- [ ] 100% Czech character preservation
- [ ] All operations atomic and reversible
- [ ] **NEW**: GUI-CLI integration robust across different deployment scenarios

### Adoption Metrics
- [ ] CLI used in automated workflows
- [ ] **NEW**: 80% of template processing done via GUI (vs CLI) after 3 months
- [ ] Integration with existing CI/CD pipelines
- [ ] **NEW**: User satisfaction score >4.5/5 for GUI experience
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
- [x] Implement replace command (Story 03.001 - Completed)
- [x] Error handling and recovery

### Phase 4: Polish & Testing (Week 4) 🔄 IN PROGRESS
- [x] Cross-platform testing
- [x] Comprehensive testing implementation
- [x] Documentation
- [ ] Performance optimization (Story 04.004 - Draft)
- [ ] Release preparation (Story 04.005 - Draft)

### Phase 5: GUI Foundation ⭐ **NEW** (Week 5-7) 📋 PLANNED
- [ ] **Phase 5a: CLI Enhancement for GUI Integration (2-3 days)**
  - [ ] Update `scan` command JSON output to include placeholder ordering
  - [ ] Add `firstOccurrenceIndex` field to placeholder objects
  - [ ] Test enhanced JSON output format
- [ ] **Phase 5b: GUI MVP (Week 5-6)**
  - [ ] Avalonia UI project setup with Czech localization
  - [ ] Basic wizard flow implementation
  - [ ] CLI integration layer (process execution and JSON parsing)
  - [ ] Core GUI functionality
- [ ] **Phase 5c: Integration & Testing (Week 7)**
  - [ ] End-to-end integration testing
  - [ ] GUI-CLI communication validation
  - [ ] Cross-platform GUI testing
  - [ ] Production GUI polish

### Phase 6: Integration Testing ⭐ **NEW** (Week 8) 📋 PLANNED
- [ ] **E2E Test Implementation**
  - [ ] Automated GUI testing framework setup
  - [ ] Happy path E2E test: template selection → processing → verification
  - [ ] CLI command verification within E2E tests
  - [ ] CI/CD integration for automated E2E testing
- [ ] **Bundled Distribution Package Creation** (FR15 Implementation)
  - [ ] Create unified distribution package with both GUI and CLI executables
  - [ ] Windows distribution: zip file or MSI installer
  - [ ] macOS distribution: app bundle (.app) or DMG package
  - [ ] Self-contained executables (no runtime dependencies)
  - [ ] Version synchronization between GUI and CLI
  - [ ] Automated build pipeline for bundled releases
  - [ ] Distribution testing on clean systems

---

## Risk Assessment

### Technical Risks
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Word document corruption | High | Low | Comprehensive backup system |
| Czech encoding issues | High | Medium | Extensive Unicode testing |
| **NEW**: GUI-CLI integration complexity | High | Medium | Process-based integration, comprehensive E2E testing |
| **NEW**: Cross-platform GUI issues | High | Medium | Avalonia framework, early multi-platform testing |
| Performance degradation | Medium | Low | Benchmarking suite |
| Cross-platform bugs | Medium | Medium | CI/CD on all platforms |

### Business Risks
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Scope creep | High | High | Strict phase boundaries |
| **NEW**: GUI development complexity | High | Medium | Simple wizard pattern, process-based integration |
| User adoption | Medium | Low | Excellent documentation, intuitive GUI |
| **NEW**: Dual interface maintenance burden | Medium | Medium | Shared core services, automated testing |

---

## Constraints and Assumptions

### Constraints
- Must use .NET 8.0 for consistency with v1.5 attempt
- Must support existing template formats from v1.0
- Must maintain Czech character support
- Cannot break existing Python v1.0 workflows during transition
- **NEW**: GUI must not require shared assemblies with CLI (process-based integration only)
- **NEW**: Single developer resource for GUI development

### Assumptions
- Users have .NET 8.0 runtime or will use self-contained builds
- Templates follow consistent placeholder patterns
- File system has adequate permissions for operations
- Network drives accessible for template storage
- **NEW**: Process-based GUI-CLI communication is sufficient for performance requirements
- **NEW**: Czech-first interface acceptable for initial GUI release

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

### B. Complete Workflow Examples

#### CLI Workflow (Technical Users)
```bash
# Step 1: See what template sets are available
docx-template list-sets --templates /shared/templates
# Output: 
# 1. Contract_Templates (15 files, 2.3 MB)
# 2. Invoice_Templates (8 files, 1.1 MB)  
# 3. Report_Templates (12 files, 3.5 MB)

# Step 2: Explore templates in a directory
docx-template discover --path /shared/templates/Contract_Templates

# Step 3: Find what needs to be filled in
docx-template scan --path /shared/templates/Contract_Templates --format json

# Step 4: Copy templates to working directory
docx-template copy --source /shared/templates/Contract_Templates --target ./working

# Step 5: Replace placeholders with actual values
docx-template replace --folder ./working --map contract-values.json
```

#### GUI Workflow (Business Users)
```
Krok 1: Vyberte sadu šablon
- Spuštění → Prohledání složky `./templates` → Zobrazení tlačítek sad šablon
- Klik na sadu šablon → Aktivace tlačítka "Další" → Klik na Další

Krok 2: Nalezení zástupných symbolů 
- Automatické prohledání vybraných šablon → Zobrazení seznamu zástupných symbolů → Aktivace "Další"

Krok 3: Zadání hodnot zástupných symbolů
- Vyplnění všech hodnot → Aktivace tlačítka "Další" → Klik na Další

Krok 4: Výběr výstupní složky
- Procházení a výběr cílové složky → Aktivace tlačítka "Další" → Klik na Další

Krok 5: Zpracování a výsledky
- Zobrazení souhrnu → Klik na "Zpracovat šablony" → Zobrazení výsledků → "Začít znovu" nebo "Otevřít složku"
```

### C. GUI-CLI Integration Examples ⭐ **NEW**

#### CLI Command Integration
```bash
# GUI internally executes these CLI commands:
list-sets --templates ./templates --format json
scan --path "./templates/Contracts" --format json
copy --source "./templates/Contracts" --target "./output" --format json
replace --folder "./output" --map "./temp_values.json" --format json
```

#### E2E Test Specification ⭐ **NEW**
```csharp
[Test]
public async Task Complete_GUI_CLI_Integration_HappyPath()
{
    // arrange
    var testTemplates = SetupTestTemplateSet();
    var testValues = CreatePlaceholderValues();
    
    // act - GUI automation
    await gui.SelectTemplateSet("TestContracts");
    await gui.AdvanceToStep(2);
    await gui.WaitForPlaceholderScanning();
    await gui.AdvanceToStep(3);
    await gui.FillPlaceholderValues(testValues);
    await gui.AdvanceToStep(4);
    await gui.SelectOutputFolder(testOutputPath);
    await gui.AdvanceToStep(5);
    await gui.ProcessTemplates();
    
    // assert - Verify GUI results
    Assert.IsTrue(gui.ShowsSuccessMessage);
    
    // assert - Verify CLI actually produced correct files
    var processedFiles = Directory.GetFiles(testOutputPath, "*.docx");
    Assert.AreEqual(expectedFileCount, processedFiles.Length);
    
    // assert - Verify content matches expected replacements
    foreach (var file in processedFiles)
    {
        var content = ExtractDocxContent(file);
        Assert.IsFalse(content.Contains("{{"), "Unreplaced placeholders found");
        AssertContainsExpectedValues(content, testValues);
    }
}
```

### D. Future GUI Integration Points
- CLI commands exposed as service methods
- JSON output parseable by GUI
- Progress callbacks for UI updates
- Cancellation token support throughout
- **NEW**: Executable discovery and validation
- **NEW**: Version compatibility checking
- **NEW**: Error state recovery and user guidance

---

*This unified PRD represents a complete solution serving both technical and business users, with robust integration between CLI and GUI components validated by comprehensive end-to-end testing. The process-based integration approach ensures clean architecture while enabling rich user experiences across both interfaces.*