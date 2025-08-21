# Documentation Index
**DOCX Template Processing System**  
*Avalonia UI with Direct Service Integration*  
*Updated: 2025-08-21*

---

## 📚 System Overview

A cross-platform (Windows/macOS) DOCX template processing application built with Avalonia UI that directly integrates with Core and Infrastructure services for efficient placeholder replacement and document generation.

---

## 🎯 Core Documents (Strategic Level)

### Product Definition
- **[prd.md](prd.md)** - Product Requirements Document ✅
  - Complete PRD with goals, requirements, and success metrics
  - *Status: Implemented with Avalonia UI*

### Architecture
- **[architecture.md](architecture.md)** - System Architecture ✅
  - Clean Architecture with direct UI → Core/Infrastructure integration
  - *Status: Refactored, CLI layer removed from UI*

---

## 📋 Specifications (Tactical Level)

### Technical Specifications
- **[technical-specification.md](technical-specification.md)** - Implementation Details ✅
  - Core services and infrastructure implementation
  - *Status: Implemented*

### UI Framework
- **[ui-framework-analysis.md](ui-framework-analysis.md)** - Avalonia UI Selection ✅
  - Cross-platform GUI framework analysis and selection
  - *Status: Avalonia implemented*

### Avalonia Best Practices
- **[avalonia-best-practices.md](avalonia-best-practices.md)** - UI Guidelines ✅
  - MVVM patterns and Avalonia-specific practices
  - *Status: Applied in implementation*

---

## 🚀 Implementation Status

### Completed Features
- ✅ **Core Architecture**: Domain models, services, and interfaces
- ✅ **Infrastructure Layer**: DOCX processing with DocumentFormat.OpenXml
- ✅ **Avalonia UI**: Wizard-based interface with Czech localization
- ✅ **Direct Service Integration**: UI uses Core/Infrastructure directly (no CLI dependency)
- ✅ **Template Processing**: Discovery, scanning, copying, and placeholder replacement
- ✅ **File Prefix Support**: SOUBOR_PREFIX placeholder for dynamic file naming
- ✅ **Czech Character Support**: Full Unicode support throughout pipeline

### User Stories Archive
- **[stories/](stories/)** - Completed implementation stories
  - 01.xxx: Foundation and architecture setup
  - 02.xxx: Core command implementations
  - 03.xxx: Processing and error handling
  - 04.xxx: Testing and optimization
  - 05.xxx: Placeholder enhancements
  - 06.xxx: Avalonia UI implementation
  - 07.xxx: Integration and distribution
  - 08.xxx: Advanced features
  - 09.001: Architecture refactoring (CLI removal)

---

## 📊 Document Status

| Document | Type | Status | Notes |
|----------|------|--------|-------|
| PRD | Strategic | Complete | ✅ Implemented |
| Architecture | Strategic | Updated | ✅ Direct service integration |
| Technical Spec | Tactical | Complete | ✅ Core/Infrastructure |
| UI Framework Analysis | Tactical | Complete | ✅ Avalonia selected |
| Avalonia Best Practices | Operational | Complete | ✅ Applied |
| User Stories | Execution | Archived | ✅ 09 epics completed |

---

## 🔄 Documentation Workflow

### Creating New Documents
1. Use appropriate BMad template
2. Follow naming conventions
3. Link from this index
4. Update status tracking

### Document Reviews
- **Strategic**: Review on major pivots
- **Tactical**: Review before implementation
- **Operational**: Review before release

### Version Control
- All documents versioned with Git
- Major changes documented in commit messages
- Archive outdated documents to `/docs/archive/`

---

## 🎨 BMad Principles Applied

### 1. Hierarchy
- Strategic → Tactical → Operational
- High-level → Detailed → Executable

### 2. Traceability  
- Requirements trace to implementation
- Tests trace to specifications
- Code traces to design

### 3. Living Documentation
- Documents updated as project evolves
- Single source of truth per topic
- Clear ownership and status

---

## 🚦 Quick Start for Developers

1. **Understand System**: [Architecture](architecture.md) - Direct service integration design
2. **UI Framework**: [Avalonia Best Practices](avalonia-best-practices.md) - MVVM patterns
3. **Core Services**: [Technical Spec](technical-specification.md) - Service interfaces
4. **Implementation History**: [stories/](stories/) - Completed work reference

---

## 📝 Documentation Standards

### File Naming
- Use lowercase with hyphens: `technical-specification.md`
- Version in filename if needed: `prd-v2.md`
- Archive with date: `archive/prd-2025-08-17.md`

### Document Structure
```markdown
# Document Title
**System Name**  
*Document Type*  
*Date*

## Executive Summary
Brief overview

## Main Content
Detailed sections

## Appendix
Supporting information
```

### Markdown Conventions
- Use headers for hierarchy (`#`, `##`, `###`)
- Use tables for structured data
- Use code blocks with language hints
- Use checklists for task tracking

---

## 🔍 Finding Information

### By Topic
- **Requirements**: See [PRD](prd.md)
- **Design**: See [Architecture](architecture.md)
- **Implementation**: See [Technical Spec](technical-specification.md)
- **Timeline**: See [Implementation Plan](implementation-plan.md)

### By Role
- **Product Manager**: PRD, Implementation Plan
- **Architect**: Architecture, Technical Spec
- **Developer**: All documents
- **Tester**: PRD (requirements), Technical Spec (test cases)

---

## 📅 Maintenance Schedule

- **Weekly**: Update implementation progress
- **Sprint End**: Review and update all tactical docs
- **Release**: Update all documentation
- **Quarterly**: Archive outdated documents

---

## 🤝 Contributing

When contributing documentation:
1. Follow BMad structure
2. Use provided templates
3. Link from this index
4. Update status tracking
5. Commit with clear messages

---

*This index serves as the central navigation point for all project documentation, following BMad Method principles for clarity and organization.*