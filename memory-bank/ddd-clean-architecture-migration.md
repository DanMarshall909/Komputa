# DDD + Clean Architecture + CQRS Migration Plan

## Overview: Next Major Development Task

Migrate Komputa from current service-based architecture to Domain-Driven Design (DDD) with Clean Architecture and Command Query Responsibility Segregation (CQRS) patterns, ensuring comprehensive unit test coverage in BDD style while observing SOLID principles.

## Current State Assessment

### ✅ Strengths to Preserve
- Dependency injection architecture with proper logging
- Memory-aware conversation system with importance scoring
- Modular AI provider interface foundation
- Web search integration capabilities
- Basic tagging and context retrieval

### 🔄 Areas Requiring DDD Migration
- **Mixed Concerns**: Services performing multiple responsibilities
- **No Domain Boundaries**: Missing clear business logic separation
- **No CQRS**: Read/write operations not separated
- **Infrastructure Coupling**: External dependencies mixed with business logic
- **Missing Tests**: No unit test coverage for existing functionality

## Target Architecture Structure

```
Komputa/
├── Komputa.Domain/                 # Core business logic (no dependencies)
│   ├── Entities/
│   │   ├── Conversation.cs         # Aggregate root - manages conversation state
│   │   ├── MemoryItem.cs          # Entity with business rules and validation
│   │   └── ConversationSession.cs # Entity for session management
│   ├── ValueObjects/
│   │   ├── UserInput.cs           # Immutable user input with validation
│   │   ├── AssistantResponse.cs   # Immutable AI response with metadata
│   │   ├── MemoryScore.cs         # Value object for importance scoring
│   │   └── ConversationId.cs      # Strong-typed identifier
│   ├── DomainServices/
│   │   ├── IMemoryScorer.cs       # Business rules for memory importance
│   │   └── IConversationContextBuilder.cs # Context building logic
│   ├── Repositories/              # Repository interfaces (no implementation)
│   │   ├── IConversationRepository.cs
│   │   └── IMemoryRepository.cs
│   └── DomainEvents/
│       ├── UserInputReceived.cs   # Domain event for input processing
│       └── MemoryStored.cs        # Domain event for memory storage

├── Komputa.Application/            # Use cases and CQRS handlers
│   ├── Commands/                  # Write operations
│   │   ├── ProcessUserInputCommand.cs
│   │   ├── ProcessUserInputHandler.cs
│   │   ├── StoreMemoryCommand.cs
│   │   └── StoreMemoryHandler.cs
│   ├── Queries/                   # Read operations
│   │   ├── GetRelevantMemoriesQuery.cs
│   │   ├── GetRelevantMemoriesHandler.cs
│   │   ├── GetConversationHistoryQuery.cs
│   │   └── GetConversationHistoryHandler.cs
│   ├── Services/                  # Application services (orchestration)
│   │   ├── ConversationOrchestrator.cs
│   │   └── MemoryManagementService.cs
│   ├── Interfaces/                # Interfaces for infrastructure
│   │   ├── IAIProvider.cs
│   │   ├── IWebSearchService.cs
│   │   └── INotificationService.cs
│   └── DTOs/                      # Data transfer objects
│       ├── ConversationDto.cs
│       └── MemoryItemDto.cs

├── Komputa.Infrastructure/         # External concerns and implementations
│   ├── Persistence/
│   │   ├── JsonMemoryRepository.cs     # Current JSON storage
│   │   ├── JsonConversationRepository.cs
│   │   └── MemoryDbContext.cs          # Future EF Core context
│   ├── AI/
│   │   ├── OpenAIProvider.cs           # Enhanced OpenAI implementation
│   │   ├── OllamaProvider.cs           # Local AI provider
│   │   └── AIProviderFactory.cs        # Provider selection logic
│   ├── Search/
│   │   └── WebSearchService.cs         # Web search implementation
│   └── Configuration/
│       └── DependencyInjection.cs     # IoC container setup

├── Komputa.Presentation/           # User interfaces
│   ├── Console/
│   │   └── ConsoleInterface.cs         # Current console implementation
│   └── Voice/
│       └── VoiceInterface.cs           # Future voice interface

└── Komputa.Tests/                  # Comprehensive BDD-style testing
    ├── Domain.Tests/
    │   ├── ConversationTests.cs        # Domain entity tests
    │   └── MemoryItemTests.cs          # Memory business logic tests
    ├── Application.Tests/
    │   ├── ProcessUserInputTests.cs    # Command handler tests
    │   └── MemoryQueryTests.cs         # Query handler tests
    └── Integration.Tests/
        └── ConversationFlowTests.cs    # End-to-end scenarios
```

## Key DDD Concepts Implementation

### 🏗️ Aggregates
- **Conversation** (Aggregate Root)
  - Manages conversation state and lifecycle
  - Enforces business rules for memory retention
  - Coordinates memory scoring and context building
  - Publishes domain events for side effects

- **MemoryItem** (Entity within Conversation aggregate)
  - Encapsulates memory content with business rules
  - Implements importance scoring logic
  - Manages time-based decay calculations
  - Validates content type and tagging rules

### 💎 Value Objects
- **UserInput**: Immutable input with validation and metadata
- **AssistantResponse**: AI response with generation context
- **MemoryScore**: Composite scoring with importance and recency
- **ConversationId**: Strong-typed identifier preventing mixing

### 🔧 Domain Services
- **MemoryScorer**: Implements complex scoring algorithms
- **ConversationContextBuilder**: Builds relevant context from memories
- **TagExtractor**: Business logic for content categorization

## CQRS Implementation Strategy

### 📝 Commands (Write Operations)
- **ProcessUserInputCommand**
  - Handles new user input
  - Triggers memory storage
  - Initiates AI response generation
  - Publishes domain events

- **StoreMemoryCommand**
  - Stores conversation memory with scoring
  - Applies business rules for retention
  - Updates conversation state

- **UpdateMemoryScoreCommand**
  - Adjusts memory importance based on usage
  - Implements decay algorithms
  - Maintains memory relevance

### 📖 Queries (Read Operations)
- **GetRelevantMemoriesQuery**
  - Retrieves contextually relevant memories
  - Applies scoring and ranking algorithms
  - Returns optimized memory sets

- **GetConversationHistoryQuery**
  - Fetches recent conversation history
  - Applies pagination and filtering
  - Formats for display/processing

- **SearchMemoriesQuery**
  - Performs semantic memory search
  - Supports tag-based filtering
  - Returns ranked results

## BDD Test Scenarios

### Feature: Memory-Aware Conversation
```gherkin
Scenario: User asks question with relevant memory context
  Given I have previous conversation about "weather preferences"
    And my preference is set to "metric units"
  When I ask "What's the weather like?"
  Then the assistant should include my preference for "metric units"
    And the response should be contextually relevant
    And the memory score should reflect usage

Scenario: Personal information gets prioritized
  Given user input contains "My name is John"
  When the memory scorer evaluates the content
  Then the importance score should be greater than 0.8
    And the content type should be "personal_information"
    And the memory should be tagged appropriately
```

### Feature: Memory Scoring and Decay
```gherkin
Scenario: Important memories retain high scores
  Given a memory with high initial importance
    And the memory has been accessed recently
  When the decay calculator runs
  Then the memory score should remain high
    And the memory should stay in active context

Scenario: Unused memories decay over time
  Given a memory with medium importance
    And the memory has not been accessed for 30 days
  When the decay calculator runs
  Then the memory score should decrease
    And the memory should be less likely to appear in context
```

### Feature: CQRS Command Handling
```gherkin
Scenario: User input command processing
  Given a valid user input command
  When the ProcessUserInputHandler executes
  Then the input should be stored as memory
    And an AI response should be generated
    And domain events should be published
    And the conversation state should be updated
```

## Migration Implementation Phases

### Phase 1: Domain Layer Foundation (Week 1)
**Objective**: Extract core business logic into pure domain layer

**Tasks**:
- Create `Komputa.Domain` project
- Define `Conversation` aggregate root with business rules
- Implement `MemoryItem` entity with scoring logic
- Create value objects (`UserInput`, `AssistantResponse`, `MemoryScore`)
- Define repository interfaces
- Implement domain services (`MemoryScorer`, `ConversationContextBuilder`)

**Success Criteria**:
- Domain layer has no external dependencies
- All business rules encapsulated in domain entities
- Unit tests cover all domain logic
- Domain events properly defined

### Phase 2: Application Layer with CQRS (Week 2)
**Objective**: Implement CQRS patterns and application services

**Tasks**:
- Create `Komputa.Application` project
- Implement command handlers for write operations
- Implement query handlers for read operations
- Create application services for orchestration
- Define DTOs for data transfer
- Add MediatR for CQRS dispatch

**Success Criteria**:
- Clear separation between commands and queries
- Application layer orchestrates domain operations
- No direct database access in handlers
- Integration tests verify command/query flow

### Phase 3: Infrastructure Refactoring (Week 3)
**Objective**: Decouple external dependencies and implement repository pattern

**Tasks**:
- Create `Komputa.Infrastructure` project
- Refactor existing services to implement application interfaces
- Implement repository pattern for data access
- Move AI providers to infrastructure layer
- Configure dependency injection properly
- Migrate configuration management

**Success Criteria**:
- All external dependencies in infrastructure layer
- Repository implementations tested
- AI provider factory working correctly
- Configuration properly externalized

### Phase 4: Comprehensive Testing (Week 4)
**Objective**: Achieve full test coverage with BDD scenarios

**Tasks**:
- Create `Komputa.Tests` project structure
- Write domain entity unit tests
- Implement application service integration tests
- Create BDD scenarios using SpecFlow or similar
- Add performance tests for memory operations
- Set up test automation and coverage reporting

**Success Criteria**:
- 90%+ code coverage achieved
- All BDD scenarios passing
- Performance benchmarks established
- Test suite runs in CI/CD pipeline

### Phase 5: Integration and Validation (Week 5)
**Objective**: Wire everything together and validate migration success

**Tasks**:
- Update `Program.cs` with new architecture
- Migrate existing data to new structure
- Performance testing and optimization
- Documentation updates
- Backward compatibility verification
- Production deployment preparation

**Success Criteria**:
- All existing functionality preserved
- Performance maintained or improved
- Documentation updated
- Migration path validated

## SOLID Principles Compliance

### Single Responsibility Principle (SRP)
- Each class has one reason to change
- Handlers focus on single command/query
- Repositories handle only data access
- Domain entities manage only business rules

### Open/Closed Principle (OCP)
- New AI providers added without modifying existing code
- Memory scoring algorithms extensible through strategy pattern
- Query handlers can be extended for new use cases

### Liskov Substitution Principle (LSP)
- All AI providers interchangeable through common interface
- Repository implementations substitutable
- Command/query handlers follow common contracts

### Interface Segregation Principle (ISP)
- Focused interfaces for specific capabilities
- Clients depend only on interfaces they use
- No forced implementation of unused methods

### Dependency Inversion Principle (DIP)
- High-level modules don't depend on low-level modules
- Both depend on abstractions (interfaces)
- Infrastructure implements application interfaces

## Quality Metrics and Success Criteria

### Code Quality
- **Test Coverage**: Minimum 90% line coverage
- **Cyclomatic Complexity**: Maximum 10 per method
- **Dependency Metrics**: Clean dependency graph with no cycles
- **Code Duplication**: Less than 5% duplicated code

### Performance
- **Response Time**: Maintain current conversation response times
- **Memory Usage**: Efficient memory management with proper disposal
- **Scalability**: Architecture supports future enhancements
- **Maintainability**: Clear separation of concerns

### Business Value
- **Functionality Preservation**: All current features working
- **Extensibility**: Easy to add new AI providers and features
- **Testability**: Comprehensive test coverage for reliability
- **Maintainability**: Clear architecture for future development

## Risk Mitigation

### Technical Risks
- **Data Migration**: Careful migration of existing memories.json
- **Breaking Changes**: Maintain API compatibility during transition
- **Performance Impact**: Monitor and optimize during migration
- **Complexity**: Gradual migration to manage complexity

### Mitigation Strategies
- **Incremental Migration**: Phase-by-phase implementation
- **Comprehensive Testing**: Test each phase thoroughly
- **Rollback Plan**: Ability to revert to previous version
- **Documentation**: Clear migration documentation

## Expected Benefits

### Development Benefits
- **Maintainability**: Easier to understand and modify code
- **Testability**: Comprehensive test coverage with BDD scenarios
- **Extensibility**: Easy to add new features and providers
- **Team Productivity**: Clear architecture reduces development time

### Business Benefits
- **Reliability**: Better error handling and fault tolerance
- **Performance**: Optimized for conversation processing
- **Scalability**: Architecture supports growth
- **Quality**: Higher code quality and fewer bugs

This migration represents a significant architectural improvement that will set the foundation for all future Komputa development, including the planned voice interface, advanced AI provider integration, and enhanced memory capabilities.
