# Komputa Business Rules Documentation

## Overview
This document captures all business rules extracted from the current implementation to ensure they are preserved during the DDD migration. These rules define the core behavior that must be maintained in the new domain-driven architecture.

## Memory Scoring Rules

### Base Content Type Scoring
The importance score is determined by content type using the following hierarchy:

| Content Type | Base Score | Description |
|-------------|------------|-------------|
| `userpreference` | 0.9 | User preferences - highest retention priority |
| `factuallearning` | 0.8 | Personal facts about the user |
| `contextualfact` | 0.7 | Important contextual information |
| `default` | 0.5 | Standard content baseline |
| `userinput` | 0.4 | User queries - decay over time |
| `assistantresponse` | 0.3 | AI responses - lowest retention priority |

### Importance Keyword Boosting
Content containing specific importance keywords receives a +0.2 boost to the base score:

**Importance Keywords:**
- `remember` - explicit memory instruction
- `prefer` - user preference indicator
- `always` - absolute preference
- `never` - absolute negative preference
- `my name is` - personal identification
- `i am` - personal information
- `i live` - location information
- `i work` - professional information

### Score Constraints
- **Maximum Score**: 1.0 (hard cap)
- **Minimum Score**: Based on content type (no explicit minimum)
- **Calculation**: `Math.Min(baseScore + keywordBoost, 1.0)`

## Conversation Context Building Rules

### Context Selection Strategy
The system builds conversation context using a multi-source approach with specific priorities:

1. **Recent Conversation History**
   - **Priority**: Highest
   - **Count**: Last 5 exchanges (10 memory items max)
   - **Rationale**: Recent context maintains conversation flow

2. **Semantic Search Results**
   - **Priority**: Medium
   - **Count**: 3 most relevant items
   - **Method**: Search current input against existing memories
   - **Deduplication**: Excludes items already in recent history

3. **High-Importance Memories**
   - **Priority**: Medium
   - **Count**: Top 5 by importance score
   - **Deduplication**: Excludes items already selected above

### Context Constraints
- **Total Memory Limit**: 8 items maximum
- **Purpose**: Prevent token overflow in AI requests
- **Selection Order**: Recent → Semantic → Important
- **Chronological Ordering**: Memories ordered by timestamp in context

### Context Formatting Rules
1. **Web Search Results** (when available):
   - Positioned first in context
   - Prefixed with: "Here's current web search information relevant to your question:"
   - Followed by empty line separator

2. **Memory Context**:
   - Prefixed with: "Here's some relevant context from our previous conversations:"
   - Format: `- {prefix} ({timeAgo}): {content}`
   - User input prefix: "You said"
   - Assistant response prefix: "I responded"
   - Ordered chronologically by timestamp

3. **Current Input**:
   - Positioned last
   - Format: `Current user input: {userInput}`

## Web Search Triggering Rules

### Current Information Keywords
Web search is triggered when input contains any of these keywords:

**Temporal Indicators:**
- `news`, `today`, `latest`, `current`, `recent`, `now`
- `this week`, `this month`, `today's`, `recent events`
- `what's happening`, `what happened`, `breaking`, `update`

### Question Pattern Analysis
Web search is triggered for time-based questions that combine:

**Question Patterns:**
- `what is`, `what are`, `what's`
- `tell me about`, `update on`, `latest on`

**Combined with Current Context:**
- Question pattern + (`today` OR `now` OR `latest`)

### Search Decision Logic
```
IF (containsCurrentInfoKeyword(input)) 
   OR (containsQuestionPattern(input) AND containsTemporalContext(input))
THEN triggerWebSearch = true
```

## Tag Extraction Rules

### Content Categorization Algorithm
The system applies keyword-based tagging using the following categories:

| Tag Category | Keywords | Purpose |
|-------------|----------|---------|
| `weather` | weather, temperature, rain, sunny, cloudy, forecast | Weather-related queries |
| `greeting` | hello, hi, hey, good morning, good evening | Social interaction |
| `question` | what, how, why, when, where, who, ? | Information requests |
| `preference` | prefer, like, favorite, always, never, usually | User preferences |
| `personal` | my name, i am, i live, i work, my job | Personal information |
| `time` | time, clock, hour, minute, schedule, calendar | Time-related queries |
| `news` | news, today, latest, current, recent, breaking, update, happening | Current events |

### Tagging Rules
- **Multiple Tags**: A single memory item can have multiple tags
- **Case Insensitive**: All keyword matching is case-insensitive
- **Partial Matching**: Keywords can appear anywhere in content
- **Extensible**: Tag categories can be extended without code changes

## Time-Based Rules

### Relative Time Display
Memory timestamps are displayed with human-readable relative times:

| Time Range | Display Format | Examples |
|------------|---------------|----------|
| < 1 minute | "just now" | just now |
| < 1 hour | "{X} minutes ago" | 15 minutes ago |
| < 1 day | "{X} hours ago" | 3 hours ago |
| < 7 days | "{X} days ago" | 2 days ago |
| ≥ 7 days | "MMM d" | Dec 15 |

### Conversation Session Rules
- **Session ID**: Generated per conversation service instance
- **Scope**: All memories in a session share the same conversation ID
- **Persistence**: Session ID persists until service restart
- **Isolation**: Different sessions have different IDs

## Memory Limit Rules

### Storage Constraints
- **Context Limit**: Maximum 8 memory items in conversation context
- **Search Results**: Maximum 3 semantic search results
- **Recent History**: Maximum 5 recent exchanges (10 items)
- **Important Memories**: Maximum 5 high-importance items

### Overflow Prevention
- **Token Management**: Context limits prevent AI token overflow
- **Priority Selection**: Higher importance memories prioritized when at limits
- **Deduplication**: Same memory never appears twice in context

## Business Invariants

### Data Consistency Rules
1. **Conversation ID Consistency**: All memories in a session must have the same conversation ID
2. **Timestamp Ordering**: Memory retrieval respects chronological order
3. **Score Boundaries**: Importance scores must be between 0.0 and 1.0
4. **Content Validation**: Memory content cannot be null or empty

### Processing Rules
1. **User Input Processing**: Every user input must be stored as memory
2. **Assistant Response Storage**: All AI responses must be stored with context tags
3. **Search Integration**: Web search results included in context when triggered
4. **Error Handling**: Processing failures must not prevent memory storage

## Integration Rules

### AI Provider Integration
- **Model Selection**: AI provider determined by configuration
- **Context Injection**: All conversation context passed to AI provider
- **Response Processing**: AI responses processed and stored as memories

### Web Search Integration
- **Search Service**: DuckDuckGo API used for current information
- **Result Processing**: Abstract text and related topics extracted
- **Fallback Handling**: Graceful degradation when search unavailable

### Memory Store Integration
- **Persistence**: JSON-based storage for all memory operations
- **Search Capability**: Semantic search across memory content
- **Retrieval Methods**: Recent, top importance, and search-based retrieval

## Validation Against BDD Tests

### Test Coverage Verification
The following BDD scenarios should cover these business rules:

1. **Memory Scoring Tests** (`MemoryScoring.feature`):
   - Content type scoring verification
   - Importance keyword boosting
   - Score constraint validation

2. **Memory-Aware Conversation Tests** (`MemoryAwareConversation.feature`):
   - Context building from multiple sources
   - Recent history prioritization
   - Semantic search integration
   - Memory limit enforcement

3. **CQRS Command Handling Tests** (`CQRSCommandHandling.feature`):
   - User input processing workflow
   - Memory storage operations
   - Web search triggering logic

4. **Conversation Flow Tests** (`ConversationFlow.feature`):
   - End-to-end conversation processing
   - Tag extraction validation
   - Time-based display formatting

### Missing Test Coverage
Areas that may need additional BDD scenarios:
- Edge cases for memory limit overflow
- Web search failure scenarios
- Invalid content type handling
- Conversation ID consistency across sessions

## Migration Validation Checklist

### Domain Layer Validation
- [ ] All scoring rules implemented in domain entities
- [ ] Context building rules preserved in domain services
- [ ] Tag extraction logic maintained in domain services
- [ ] Time-based rules implemented in value objects

### Application Layer Validation
- [ ] CQRS commands preserve user input processing workflow
- [ ] Query handlers maintain context building logic
- [ ] Web search integration preserved in application services

### Infrastructure Layer Validation
- [ ] Memory storage maintains JSON compatibility
- [ ] Search functionality preserved with same algorithms
- [ ] AI provider integration maintains context injection

### End-to-End Validation
- [ ] All existing BDD scenarios pass with new architecture
- [ ] Business rule behavior identical to current implementation
- [ ] Performance characteristics maintained or improved

## Business Rule Dependencies

### Cross-Component Dependencies
1. **Memory Scoring ↔ Context Building**: Context selection depends on importance scores
2. **Tag Extraction ↔ Search Triggering**: Tags may influence search decisions
3. **Time Display ↔ Context Ordering**: Relative time affects context presentation
4. **Web Search ↔ Context Building**: Search results integrated into context

### External Dependencies
1. **AI Provider**: Context formatting must match provider expectations
2. **Web Search API**: Search triggering depends on external service availability
3. **Memory Storage**: All rules depend on persistent storage reliability

This documentation serves as the authoritative source for business rules that must be preserved during the DDD migration, ensuring no functionality is lost during the architectural transformation.
