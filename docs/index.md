# Documentation Index

## Root Documents

### [Documentation Index](./README.md)

BMad-compliant documentation structure index that provides central navigation for all project documentation, organizing strategic, tactical, and operational documents with clear hierarchy and traceability.

### [Architecture Document](./architecture.md)

Comprehensive system architecture specification using Clean Architecture principles with detailed component breakdowns, data flow diagrams, and cross-cutting concerns for the DOCX Template CLI v2.0 system.

### [CLI Command Reference](./cli-reference.md)

Precise technical documentation of all CLI commands with exact parameter specifications, JSON output schemas, and programmatic integration guidance for the DocxTemplate CLI system.

### [Implementation Plan](./implementation-plan.md)

4-week sprint implementation roadmap with detailed task breakdowns, progress tracking, and completion status showing 85% project completion with remaining work on performance optimization and release preparation.

### [Product Requirements Document](./prd.md)

Complete product requirements specification defining goals, functional/non-functional requirements, user interface design, and success metrics for the DOCX Template CLI System v2.0 with clean architecture approach.

### [Technical Specification](./technical-specification.md)

Detailed technical implementation guidance covering technology stack, OpenXML processing, error handling, performance targets, security specifications, and deployment configurations for the DOCX Template CLI system.

## Architecture Decision Records

Documents within the `adr/` directory:

### [ADR-001: Clean Architecture Implementation](./adr/001-clean-architecture.md)

Architecture Decision Record documenting the choice to implement Clean Architecture with three distinct layers (CLI, Core, Infrastructure) including rationale, alternatives considered, and implementation consequences.

### [Architecture Decision Records (ADRs)](./adr/README.md)

Guide for creating and managing Architecture Decision Records in the project, including templates, naming conventions, and guidelines for documenting significant architectural decisions.

## User Stories

Documents within the `stories/` directory:

### [User Story: Project Setup & Architecture](./stories/01.001.project-setup-architecture.md)

Foundation story for establishing complete project infrastructure with clean architecture, build system, CI/CD pipeline, and development tooling. Status: Complete with comprehensive solution structure and simplified Windows-focused pipeline.

### [User Story: Core Models & Interfaces](./stories/01.002.core-models-interfaces.md)

Core domain layer implementation with comprehensive models, service interfaces, validation, and exception hierarchy. Status: Complete with 142 tests passing and 100% coverage for all domain models.

### [User Story: Infrastructure Foundation](./stories/01.003.infrastructure-foundation.md)

Infrastructure layer implementation with file system abstraction, configuration management, and dependency injection setup. Status: Complete with 51 unit tests and comprehensive cross-platform support.

### [User Story: List Sets Command Implementation](./stories/02.001.list-sets-command.md)

CLI command for discovering and listing template sets (top-level directories) with metadata collection and multiple output formats. Status: Complete as foundational command for template operations workflow.

### [User Story: Discover Command Implementation](./stories/02.002.discover-command.md)

CLI command for recursive DOCX template discovery with filtering options, progress reporting, and comprehensive output formats. Status: Complete with full template cataloging capabilities.

### [User Story: Scan Command Implementation](./stories/02.003.scan-command.md)

CLI command for placeholder detection in DOCX templates using OpenXML parsing, supporting custom patterns, parallel processing, and split text run handling. Status: Complete with comprehensive placeholder identification.

### [User Story: Copy Command Implementation](./stories/02.004.copy-command.md)

CLI command for template copying with directory structure preservation, conflict resolution, dry-run mode, and comprehensive error handling. Status: Complete with atomic operations and backup support.

### [User Story: Replace Command Implementation](./stories/03.001.replace-command.md)

CLI command for placeholder replacement in Word documents using JSON mapping files, with atomic operations, Czech character support, and batch processing capabilities. Status: Complete implementation for template processing workflow.

### [User Story: Error Handling and Recovery Implementation](./stories/03.003.error-handling-recovery.md)

Basic error detection and user-friendly messaging system with pipeline error propagation for template processing operations. Status: Complete with graceful exception handling and clear error messages.

### [User Story: Comprehensive Testing Implementation](./stories/04.001.comprehensive-testing.md)

Complete test coverage including unit, integration, and cross-platform testing with automated CI/CD integration. Status: Complete with 268 tests, 46.6% coverage, and Windows/macOS pipeline support.

### [User Story: End-to-End Testing Implementation](./stories/04.002.end-to-end-testing.md)

Complete user workflow validation with real CLI process execution, document integrity checking, and cross-command data flow testing. Status: Complete with comprehensive E2E testing framework and real Word document validation.

### [User Story: CLI Command Reference Documentation](./stories/04.003.documentation.md)

Precise documentation of current CLI implementation with accurate parameter specifications, real JSON output schemas, and programmatic integration guidance. Status: Complete with tested output documentation.

### [User Story: Performance Optimization Implementation](./stories/04.004.performance-optimization.md)

Performance optimization to meet requirements for processing speed (<10s for 100 templates), memory usage (<100MB), startup time (<500ms), and parallel processing efficiency. Status: Draft - remaining work for completion.

### [User Story: Release Preparation Implementation](./stories/04.005.release-preparation.md)

Automated release builds, distribution packages, installation scripts, and deployment artifacts for production v2.0.0 release across all supported platforms. Status: Draft - remaining work for completion.