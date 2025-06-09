# DDD Migration Plan - Story Points & Sprint Breakdown

## Overview
Migrate Komputa from current service-based architecture to Domain-Driven Design (DDD) with Clean Architecture and CQRS patterns. Focus on preserving all existing business rules and minimizing breaking changes while adding aggregate roots and use cases.

## Strategic Approach
- **Change as little as possible** while introducing DDD concepts
- **Cover all business rules first** before refactoring
- **Break as little as we can** during migration
- **Incremental migration** with gradual cutover

## Story Point Backlog

### Epic 1: Domain Foundation (21 points total)


**Story 1.1**: Document Business Rules (3 points)
- Extract memory scoring rules from `VoiceAssistantContentScorer`
- Document conversation context building rules from `MemoryAwareConversationService`
- Document web search triggering logic
- Document tag extraction algorithms
- Verify all rules covered by existing BDD tests

**Story 1.2**: Create Domain Project Structure (2 points)
- Create `Komputa.Domain` class library project
- Set up proper dependencies (no external refs except primitives)
- Add domain project to solution file
- Configure project references

**Story 1.3**: Rich MemoryItem Domain Entity (5 points)
- Transform current `MemoryItem` into rich domain entity
- Add business methods: `CalculateDecayedImportance()`, `BoostForUsage()`, `AddTags()`
- Keep current `Models/MemoryItem.cs` as data transfer object
- Domain entity enforces business rules, DTO handles JSON serialization
- Implement proper encapsulation and validation

**Story 1.4**: Conversation Aggregate Root (8 points)
- Create `Conversation` aggregate managing memory collection
- Business rules: memory limits, context selection, session management
- Methods: `AddMemory()`, `GetRelevantContext()`, `CleanupOldMemories()`
- Enforce invariants like conversation ID consistency
- Implement domain events for memory operations

**Story 1.5**: Value Objects (3 points)
- `MemoryScore` - encapsulates importance + decay calculations
- `ConversationId` - strong-typed identifier preventing mix-ups
- `UserInput` - immutable input with validation rules
- `ContentType` - enum with business rules for each type

### Epic 2: Domain Services (13 points total)

**Story 2.1**: MemoryScorer Domain Service (5 points)
- Extract scoring logic from `VoiceAssistantContentScorer`
- Implement business rules:
  - Personal information gets importance >0.8
  - User preferences get scores 0.7-0.9
  - Content type determines base importance
  - Time-based decay calculations
  - Usage frequency boosting
- Pure domain logic, no infrastructure dependencies

**Story 2.2**: TagExtractor Domain Service (3 points)
- Extract tagging logic from `MemoryAwareConversationService`
- Implement keyword-based categorization rules
- Support extensible tag categories
- Business rules for tag assignment priority

**Story 2.3**: ContextBuilder Domain Service (3 points)
- Extract context selection rules from conversation service
- Implement logic for relevant context building:
  - Recent conversation history (last 5 exchanges)
  - Semantically relevant memories (search-based)
  - High-importance memories
  - Memory limit of 8 items to prevent token overflow

**Story 2.4**: Domain Repository Interfaces (2 points)
- `IMemoryRepository` - domain-focused memory operations
- `IConversationRepository` - aggregate persistence contracts
- Define domain operations, not data access patterns
- Repository methods match domain language

### Epic 3: Application Layer + CQRS (21 points total)

**Story 3.1**: Application Project Setup (2 points)
- Create `Komputa.Application` class library project
- Add MediatR package for CQRS implementation
- Reference domain project only
- Set up basic application structure

**Story 3.2**: Command Models and Handlers (5 points)
- `ProcessUserInputCommand` + Handler - orchestrates input → memory → response flow
- `StoreMemoryCommand` + Handler - memory storage with domain scoring
- `UpdateMemoryScoreCommand` + Handler - usage tracking and boosting
- Commands carry data only, handlers delegate to domain services
- Proper validation and error handling

**Story 3.3**: Query Models and Handlers (5 points)
- `GetRelevantMemoriesQuery` + Handler - context retrieval with parameters
- `GetConversationHistoryQuery` + Handler - recent conversation data
- `SearchMemoriesQuery` + Handler - semantic search operations
- Return application DTOs, not domain entities
- Use domain services for business logic

**Story 3.4**: Application Services (8 points)
- `ConversationOrchestrator` - replaces `MemoryAwareConversationService`
- Coordinates CQRS commands and queries
- Implements application workflows:
  - User input processing workflow
  - Memory storage and retrieval workflow
  - Web search integration workflow
- Maintains backward compatibility with existing interfaces

**Story 3.5**: Application DTOs (1 point)
- `ConversationDto` - conversation data transfer
- `MemoryItemDto` - memory item data transfer
- Mapping between domain entities and DTOs
- JSON serialization attributes

### Epic 4: Infrastructure Integration (13 points total)

**Story 4.1**: Infrastructure Project Setup (2 points)
- Create `Komputa.Infrastructure` class library project
- Set up project references to Application and Domain
- Prepare for moving existing services

**Story 4.2**: Repository Implementations (5 points)
- `JsonMemoryRepository` - wraps existing `JsonMemoryStore`
- `JsonConversationRepository` - implements conversation persistence
- Adapts domain operations to current JSON storage format
- Maintains compatibility with existing `memories.json` structure
- No changes to storage format during migration

**Story 4.3**: Move Existing Services (3 points)
- Move `OpenAIProvider`, `WebSearchService` to infrastructure
- Implement application interfaces defined in Epic 3
- Preserve all existing functionality
- No breaking changes to service behavior

**Story 4.4**: Dependency Injection Configuration (3 points)
- Update DI container to register new services
- Support gradual migration (both old and new services available)
- Configure CQRS pipeline with MediatR
- Maintain existing service registrations during transition

### Epic 5: Testing & Validation (8 points total)

**Story 5.1**: Update Existing Tests (3 points)
- Modify existing BDD tests to work with new architecture
- Update test dependencies and mocking
- Ensure all scenarios still pass with new implementation
- No test behavior changes, only infrastructure updates

**Story 5.2**: Domain Logic Tests (2 points)
- Add unit tests for domain entities and services
- Test business rules extracted to domain layer
- Property-based testing for complex business logic
- Edge case validation

**Story 5.3**: BDD Scenario Validation (2 points)
- Verify all existing BDD scenarios pass
- Test both old and new implementation paths
- Validate business rule preservation
- End-to-end workflow testing

**Story 5.4**: Performance Validation (1 point)
- Compare performance before and after migration
- Memory usage analysis
- Response time benchmarking
- Identify any performance regressions

### Epic 6: Migration & Cleanup (5 points total)

**Story 6.1**: Gradual Service Cutover (2 points)
- Switch from old to new services in DI configuration
- Provide feature flags for rollback capability
- Test cutover in stages
- Monitor for issues during transition

**Story 6.2**: Remove Obsolete Code (2 points)
- Remove old service implementations once migration complete
- Clean up unused interfaces and models
- Update project references
- Remove temporary compatibility code

**Story 6.3**: Documentation Updates (1 point)
- Update architecture documentation
- Document new DDD patterns and structure
- Update developer onboarding guides
- Create migration completion record

## Sprint Planning

### Sprint 1: Domain Foundation (21 points)
**Goal**: Extract core business logic into pure domain layer
- Complete Epic 1 entirely
- Foundation for all subsequent work
- All business rules documented and tested
- Domain entities with rich behavior

### Sprint 2: Domain Services + CQRS Start (22 points)
**Goal**: Complete domain services and begin application layer
- Complete Epic 2 (Domain Services) - 13 points
- Stories 3.1-3.2 from Epic 3 - 7 points
- Domain logic fully extracted
- CQRS foundation established

### Sprint 3: Complete CQRS + Infrastructure (21 points)
**Goal**: Finish application layer and integrate infrastructure
- Stories 3.3-3.5 from Epic 3 - 14 points
- Complete Epic 4 (Infrastructure) - 13 points
- Full new architecture working
- Parallel operation with existing system

### Sprint 4: Testing + Migration (13 points)
**Goal**: Validate migration and complete cutover
- Complete Epic 5 (Testing) - 8 points
- Complete Epic 6 (Migration) - 5 points
- All tests passing
- Migration complete and documented

## Risk Mitigation

### Technical Risks
- **Business Rule Loss**: Document all rules before extraction
- **Breaking Changes**: Maintain interfaces during migration
- **Performance Regression**: Continuous performance monitoring
- **Test Failures**: Update tests incrementally with code changes

### Mitigation Strategies
- **Incremental Approach**: Each epic builds on previous
- **Parallel Implementation**: Old and new systems coexist during migration
- **Comprehensive Testing**: BDD scenarios protect against regression
- **Rollback Capability**: Feature flags allow quick reversion

## Definition of Done

### Epic Level
- All stories completed and tested
- No breaking changes to external interfaces
- All existing BDD scenarios pass
- Code coverage maintained or improved
- Performance benchmarks met

### Story Level
- Implementation complete with unit tests
- Integration tests pass
- Code reviewed and approved
- Documentation updated
- Acceptance criteria verified

## Success Metrics

### Code Quality
- **Test Coverage**: Maintain existing 90%+ coverage
- **Cyclomatic Complexity**: Max 10 per method
- **Dependency Metrics**: Clean dependency graph
- **Code Duplication**: <5% duplicated code

### Business Value
- **Functionality Preservation**: All current features working
- **Extensibility**: Easy to add new AI providers and features
- **Testability**: Comprehensive test coverage
- **Maintainability**: Clear DDD architecture

### Performance
- **Response Time**: Maintain current conversation response times
- **Memory Usage**: Efficient memory management
- **Scalability**: Architecture supports future enhancements

## Current Business Rules Identified

### Memory Scoring Rules
- Personal information: importance >0.8
- User preferences: importance 0.7-0.9
- Casual conversation: importance 0.2-0.5
- Content type determines base importance
- Time-based decay reduces old memory scores
- Usage frequency boosts relevance scores

### Conversation Context Rules
- Context built from recent + relevant + important memories
- Memory limit of 8 items prevents token overflow
- Recent memories: last 5 exchanges prioritized
- Semantic search for relevant content
- High-importance memories always included

### Web Search Triggering Rules
- Current info keywords: "news", "today", "latest", "current", "recent"
- Time-based questions: "what is" + temporal indicators
- Explicit current information requests
- Breaking news and update patterns

### Tag Extraction Rules
- Keyword-based categorization: weather, greeting, question, preference, personal, time, news
- Multiple tags per memory item supported
- Tag priority based on content analysis
- Extensible tag categories

This plan provides a structured approach to DDD migration while preserving all existing functionality and business rules.
