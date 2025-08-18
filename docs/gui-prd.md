# Product Requirements Document
**DOCX Template CLI - Windows UI (GUI) Feature**  
*BMad-Compliant PRD*  
*Created: 2025-01-18*

---

## Executive Summary

A simple, cross-platform graphical user interface for the DOCX Template CLI system, enabling non-technical users to process Word document templates through an intuitive point-and-click interface. Built with Avalonia UI for native Windows and macOS support.

### Key Value Proposition
Transform the powerful CLI tool into an accessible desktop application that business users can operate without command-line knowledge, while maintaining all existing functionality and performance.

### Success Criteria
- Zero command-line interaction required for template processing
- 5-minute learning curve for new users
- Single executable deployment for Windows and macOS
- Maintain <10 second processing for 100 templates

---

## Goals and Background Context

### Primary Goals
1. **Accessibility**: Enable non-technical users to process templates without CLI knowledge
2. **Simplicity**: Provide the simplest possible interface for core workflows
3. **Cross-Platform**: Single codebase for Windows and macOS deployment
4. **Integration**: Seamless integration with existing CLI service layer
5. **Maintainability**: Minimal UI complexity for easy maintenance

### Background Context

#### Current State (CLI v2.0)
- **Strengths**: 
  - Fully functional CLI with 5 commands (list-sets, discover, scan, copy, replace)
  - Clean architecture with separated Core/Infrastructure/CLI layers
  - 85% feature complete, production-ready core functionality
  - Excellent for automation and technical users
  
- **Limitations**:
  - Requires command-line proficiency
  - Multi-step workflow requires memorizing commands
  - No visual feedback during processing
  - Intimidating for business users

#### User Feedback
- Business users find CLI "scary" and "complicated"
- Frequent requests for "just a simple window with buttons"
- Users want to "see what's happening" during processing
- Need for "drag and drop" or "browse button" for file selection

### Strategic Decision
Implement a minimal GUI layer using Avalonia UI that wraps existing CLI services, focusing on the 80% use case of simple template processing workflows.

---

## User Persona

### Target User: Business Administrator
- **Role**: Office Administrator / Document Coordinator / Business User (Czech-speaking)
- **Technical Skill**: Basic computer literacy, comfortable with Office applications
- **Language**: Primary Czech language interface required
- **Context**: Processes Word document templates regularly (contracts, invoices, reports)
- **Needs**: 
  - Process templates without learning command-line syntax
  - Replace placeholders with client/project information
  - Generate document sets quickly and reliably
  - Visual confirmation that processing succeeded
  - Czech interface that feels natural and familiar
- **Pain Points**:
  - Command-line interfaces are intimidating and error-prone
  - Memorizing command syntax is difficult
  - No visual feedback during processing
  - Uncertainty about whether operations completed successfully
  - English-only interfaces create unnecessary barriers
- **Success Criteria**: "I can select my templates, fill in the values, click Process, and it just works - all in Czech"
- **Quote**: "Chci něco tak jednoduché jako Word - vybrat soubory, vyplnit hodnoty, kliknout na tlačítko"

---

## Requirements

### Functional Requirements

#### FR1: Wizard Navigation & Flow
- **FR1.1**: Step-by-step wizard interface with 5 distinct steps
- **FR1.2**: Clear step indicators showing current position (Step 1 of 5)
- **FR1.3**: "Next" and "Back" buttons for navigation between steps
- **FR1.4**: Cannot proceed to next step without completing current step
- **FR1.5**: Standard keyboard navigation (Tab/Enter) and clipboard (Ctrl+X/C/V)

#### FR2: Step 1 - Template Set Selection
- **FR2.1**: Automatically scan `./templates` folder on wizard launch
- **FR2.2**: Display each subfolder as a clickable button/tile
- **FR2.3**: Show template count for each subfolder (e.g., "Contracts (15 files)")
- **FR2.4**: Highlight selected template set with visual indication
- **FR2.5**: "Next" button enabled only after template set selection
- **FR2.6**: Show "No templates found" message if `./templates` folder is missing or empty

#### FR3: Step 2 - Placeholder Discovery & Display
- **FR3.1**: Automatic scanning when advancing from Step 1
- **FR3.2**: Display unique placeholders in discovery order (first occurrence determines position)
- **FR3.3**: Preserve template reading sequence for logical placeholder ordering
- **FR3.4**: Show occurrence count for each placeholder
- **FR3.5**: Status message during scanning
- **FR3.6**: "Next" button enabled after scanning completes
- **FR3.7**: "Back" button to return to template selection

#### FR4: Step 3 - Placeholder Value Input
- **FR4.1**: Text input field for each discovered placeholder in discovery order
- **FR4.2**: Labels showing placeholder names clearly
- **FR4.3**: Maintain same order as Step 2 placeholder list
- **FR4.4**: Text input with automatic whitespace normalization (newlines, tabs, multiple spaces → single space)
- **FR4.5**: Clear all values button
- **FR4.6**: "Next" button enabled only when all placeholders have values
- **FR4.7**: "Back" button to return to placeholder discovery

#### FR5: Step 4 - Output Folder Selection
- **FR5.1**: "Select Output Folder" button as primary action
- **FR5.2**: Display selected output path in read-only text field
- **FR5.3**: "Next" button enabled only after output folder selected
- **FR5.4**: "Back" button to return to placeholder input

#### FR6: Step 5 - Processing & Results
- **FR6.1**: Summary of selections (template set, output folder, placeholder count)
- **FR6.2**: "Process Templates" button to start processing
- **FR6.3**: Status messages during processing
- **FR6.4**: Success/error message display
- **FR6.5**: "Open Output Folder" button after successful completion
- **FR6.6**: "Start Over" button to return to Step 1
- **FR6.7**: Processing log for troubleshooting

#### FR7: User Assistance & Localization
- **FR7.1**: All UI text in Czech language
- **FR7.2**: Step titles and descriptions for each wizard step in Czech
- **FR7.3**: Tooltips on all interactive elements in Czech
- **FR7.4**: "Nápověda" menu with Czech user guide link
- **FR7.5**: "O aplikaci" dialog with version information
- **FR7.6**: Clear error messages in Czech with suggested actions
- **FR7.7**: Czech button labels ("Další", "Zpět", "Procházet", "Zpracovat")

### Non-Functional Requirements

#### NFR1: Usability
- **NFR1.1**: Learnable in <5 minutes without documentation
- **NFR1.2**: All actions achievable in ≤3 clicks
- **NFR1.3**: Visual feedback for all user actions
- **NFR1.4**: Consistent with OS UI conventions
- **NFR1.5**: Standard keyboard navigation (Tab/Enter/Escape)

#### NFR2: Compatibility
- **NFR2.1**: Windows 10/11 native experience
- **NFR2.2**: macOS 11+ native experience  
- **NFR2.3**: Single executable per platform
- **NFR2.4**: No additional runtime installation required

#### NFR3: Reliability
- **NFR3.1**: Graceful handling of all errors
- **NFR3.2**: Validation before destructive operations
- **NFR3.3**: Clear error messages with suggested actions

#### NFR4: Deployment
- **NFR4.1**: Single-file executable deployment
- **NFR4.2**: No administrator rights required
- **NFR4.3**: Portable mode support (no installation)

---

## User Interface Design

### Wizard Flow Layout (Czech Interface)

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

### Czech Wizard Flow Steps

**Krok 1: Vyberte sadu šablon**
- Spuštění → Prohledání složky `./templates` → Zobrazení tlačítek sad šablon
- Klik na sadu šablon → Aktivace tlačítka "Další" → Klik na Další

**Krok 2: Nalezení zástupných symbolů** 
- Automatické prohledání vybraných šablon → Zobrazení seznamu zástupných symbolů → Aktivace "Další"

**Krok 3: Zadání hodnot zástupných symbolů**
- Vyplnění všech hodnot → Aktivace tlačítka "Další" → Klik na Další

**Krok 4: Výběr výstupní složky**
- Procházení a výběr cílové složky → Aktivace tlačítka "Další" → Klik na Další

**Krok 5: Zpracování a výsledky**
- Zobrazení souhrnu → Klik na "Zpracovat šablony" → Zobrazení výsledků → "Začít znovu" nebo "Otevřít složku"

### Czech Error States

- **Nenalezeny šablony**: "Ve vybrané složce nebyly nalezeny žádné soubory .docx. Vyberte prosím složku obsahující šablony Word."
- **Chyba zpracování**: "Chyba při zpracování šablony 'smlouva.docx': [konkrétní chyba]. Ostatní šablony byly zpracovány úspěšně."
- **Neplatné hodnoty**: "Před zpracováním zadejte prosím hodnoty pro všechny zástupné symboly."
- **Chyba výstupní složky**: "Nelze zapisovat do vybrané složky. Zvolte prosím jiné umístění."

---

## Technical Architecture

### Integration with CLI Architecture

**IMPORTANT**: GUI does NOT reference CLI libraries or assemblies

**Reference Documentation**: 
- CLI commands and JSON schemas: `docs/cli-reference.md`
- Avalonia implementation guide: `docs/avalonia-best-practices.md`

```
DocxTemplate.UI (New GUI Layer)
├── ViewModels/
│   └── MainViewModel.cs ──────┐
│                              ↓ (calls via Process.Start)
├── CLI Integration Layer:
│   ├── CliCommandBuilder.cs   ← Builds command strings
│   ├── CliResultParser.cs     ← Parses JSON output
│   └── CliProcessRunner.cs    ← Executes CLI commands
│
└── Calls CLI executable directly:
    └── docx-template.exe --format json
```

**Integration Approach**: 
- GUI launches CLI executable as external process
- All communication via command-line arguments and JSON output
- No shared assemblies or direct service references
- Clean separation between GUI and CLI layers

### Required CLI Command Updates

#### CLI JSON Output Enhancement for Placeholder Ordering
**Current State**: `scan --format json` returns placeholders in unspecified order (see `docs/cli-reference.md`)

**Required Changes**:
- Preserve document reading order during scanning
- Track first occurrence position for each unique placeholder  
- Return ordered placeholder list in JSON output
- Maintain occurrence counts per placeholder

**Updated JSON Output Format**:
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

**CLI Command Integration Examples**:
```bash
# Step 1: List template sets
list-sets --templates ./templates --format json

# Step 2: Scan placeholders (with ordering enhancement needed)  
scan --path "./templates/Contracts" --format json

# Step 3: Copy templates to output folder
copy --source "./templates/Contracts" --target "./output" --format json

# Step 4: Replace placeholders with values
replace --folder "./output" --map "./temp_values.json" --format json
```

**Current CLI JSON Schemas (from cli-reference.md)**:

**list-sets output:**
```json
{
  "command": "list-sets",
  "success": true,
  "data": {
    "template_sets": [
      {
        "name": "Contracts",
        "file_count": 15,
        "total_size_formatted": "2.3 MB"
      }
    ]
  }
}
```

**scan output (needs ordering enhancement):**
```json
{
  "command": "scan",
  "success": true,
  "data": {
    "placeholders": [],
    "summary": {
      "unique_placeholders": 5,
      "total_occurrences": 25
    }
  }
}
```

### Technology Stack
- **UI Framework**: Avalonia UI 11.x (see implementation details in `docs/avalonia-best-practices.md`)
- **MVVM Framework**: ReactiveUI for property notifications and commanding
- **CLI Integration**: Process execution with JSON parsing (see `docs/cli-reference.md`)
- **Target Framework**: .NET 8.0
- **Deployment**: Self-contained single-file executables

### Key Design Decisions
1. **Avalonia over WPF/MAUI**: True cross-platform with minimal complexity (detailed analysis in `docs/ui-framework-analysis.md`)
2. **Process-based CLI Integration**: Clean separation, no shared libraries
3. **Wizard Pattern**: Step-by-step guided workflow for business users
4. **Czech Language First**: Primary interface language for target users
5. **CLI JSON Communication**: All data exchange via command-line arguments and JSON output

---

## Success Metrics

### Launch Metrics (Month 1)
- [ ] 90% of users complete first template processing without help
- [ ] <5 support tickets for GUI-specific issues
- [ ] Average time from launch to completion <3 minutes
- [ ] Zero crashes reported in normal usage

### Adoption Metrics (Month 3)
- [ ] 80% of template processing done via GUI (vs CLI)
- [ ] User satisfaction score >4.5/5
- [ ] 50% reduction in support requests
- [ ] GUI becomes default installation choice

### Quality Metrics
- [ ] 0 critical bugs in production
- [ ] <2 second response time for all actions
- [ ] 100% feature parity with CLI
- [ ] Works on 95% of Windows/Mac configurations

---

## Development Phases

### Phase 0: CLI JSON Enhancement (2-3 days) ⚠️ PREREQUISITE
- Update `scan` command JSON output to include placeholder ordering
- Add `firstOccurrenceIndex` field to placeholder objects in JSON
- Ensure `placeholders` array is ordered by discovery sequence
- Test JSON output format with GUI integration requirements
- Update cli-reference.md documentation
- **Deliverable**: Enhanced CLI JSON output ready for GUI parsing

### Phase 1: MVP (Week 1)
- Basic window with folder selection
- Placeholder discovery and display
- Simple value input form
- Process button with basic progress
- **Deliverable**: Working prototype for user feedback

### Phase 2: Polish (Week 2)
- Progress indicators and status messages
- Load/save value mappings
- Error handling and validation
- Keyboard shortcuts
- **Deliverable**: Beta release

### Phase 3: Production (Week 3)
- Cross-platform testing
- Installer creation
- Documentation
- Performance optimization
- **Deliverable**: v1.0 release

### Phase 4: Enhancement (Future)
- Drag-and-drop support
- Template preview
- Batch processing queue
- Value templates/profiles
- **Deliverable**: v1.1 features

---

## Risk Assessment

### Technical Risks
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Avalonia learning curve | Medium | Low | Comprehensive best practices doc created |
| Cross-platform issues | High | Medium | Early testing on both platforms |
| Performance degradation | Medium | Low | Async operations, progress feedback |
| Integration complexity | High | Low | Clean service interfaces already exist |

### User Adoption Risks
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Feature creep requests | High | High | Clear MVP scope, defer to v1.1 |
| Training requirements | Medium | Low | Intuitive design, video tutorial |
| CLI user resistance | Low | Low | GUI is addition, not replacement |

---

## Constraints and Assumptions

### Constraints
- Must maintain all existing CLI functionality
- Cannot modify Core/Infrastructure layers
- Must use existing service interfaces
- Single developer resource
- 3-week development timeline

### Assumptions
- Users have .NET 8.0 runtime or will use self-contained builds
- Standard OS file dialogs are sufficient for file selection
- Basic form inputs meet user needs (no rich text editing)
- Users process one template set at a time
- English-only interface is acceptable for v1.0

---

## Out of Scope (v1.0)

### Explicitly Excluded
- Multi-language support
- Cloud storage integration
- Multiple template set processing
- Template preview/editing
- User authentication/profiles
- Network/shared drive optimization
- Mobile or web versions
- Plugin architecture
- Macro recording/playback
- Database integration

### Deferred to v1.1
- Drag-and-drop file selection
- Recent folders history
- Value template library
- Batch processing queue
- Advanced placeholder validation
- Custom placeholder patterns
- Export to different formats
- Undo/redo functionality

---

## Acceptance Criteria

### Definition of Done
The GUI feature is complete when:

1. **Functional Completeness**
   - All five CLI commands accessible via GUI
   - Values can be input and saved
   - Processing completes successfully

2. **Quality Standards**
   - No crashes in 100 processing operations
   - All error cases handled gracefully
   - UI responsive during processing

3. **Documentation**
   - User guide with screenshots
   - Installation instructions
   - Troubleshooting guide

4. **Deployment**
   - Windows executable created and tested
   - macOS executable created and tested
   - File size <50MB per platform

5. **User Validation**
   - 3 non-technical users successfully process templates
   - Feedback incorporated into final build
   - Video tutorial recorded

---

## Reference Documentation

### Supporting Documents Created for This Project
1. **`docs/cli-reference.md`** - Complete CLI command reference with JSON schemas
2. **`docs/avalonia-best-practices.md`** - Avalonia UI implementation guide for simple cross-platform apps
3. **`docs/ui-framework-analysis.md`** - Technology evaluation comparing Avalonia, MAUI, WPF, WinUI 3

### Key Integration Points
- **CLI Commands**: Use exact syntax and parameters documented in `cli-reference.md`
- **JSON Parsing**: Parse output schemas as specified in CLI documentation  
- **Avalonia Implementation**: Follow patterns and project structure from `avalonia-best-practices.md`
- **Error Handling**: Account for CLI limitations noted in `cli-reference.md` (no JSON error format)

---

## Appendix

### A. Competitive Analysis Summary
- **WPF**: Windows-only, eliminated
- **WinUI 3**: Windows-only, too complex
- **MAUI**: Cross-platform but heavyweight
- **Avalonia**: Optimal for simple cross-platform GUI ✓

### B. User Story Map

```
Epic: GUI for Template Processing
├── Select Templates
│   ├── Browse for folder
│   ├── Validate templates
│   └── Show statistics
├── Input Values  
│   ├── Display placeholders
│   ├── Accept text input
│   ├── Load from JSON
│   └── Save to JSON
└── Process Templates
    ├── Select output location
    ├── Show progress
    ├── Handle errors
    └── Open results
```

### C. Sample Error Messages

| Scenario | Message | Action |
|----------|---------|--------|
| No templates | "No Word documents found" | "Select different folder" |
| Missing values | "5 placeholders need values" | "Fill in highlighted fields" |
| Write permission | "Cannot write to selected folder" | "Choose different location" |
| Processing error | "Failed to process 2 files" | "View log for details" |

---

*This PRD defines a minimal, user-focused GUI that leverages existing CLI architecture to deliver maximum value with minimum complexity. The focus on simplicity and cross-platform support via Avalonia ensures rapid development and easy maintenance.*