Feature: CQRS Command Handling
    As the Komputa application
    I want to process commands and queries separately
    So that read and write operations are properly segregated

Background:
    Given the CQRS mediator is initialized
    And the command and query handlers are registered

Scenario: Process user input command successfully
    Given a valid user input command with content "Tell me about the weather"
    When the ProcessUserInputHandler executes the command
    Then the input should be stored as memory
    And an AI response should be generated
    And domain events should be published
    And the conversation state should be updated

Scenario: Store memory command with proper scoring
    Given a store memory command with content "My favorite color is blue"
    And the content type is "user_preference"
    When the StoreMemoryHandler executes the command
    Then the memory should be stored with appropriate importance score
    And the memory should be tagged correctly
    And the conversation should be updated

Scenario: Get relevant memories query returns ranked results
    Given multiple memories exist in the store:
        | Content | Type | Importance | Age (days) |
        | "I live in Brisbane" | personal_information | 0.9 | 5 |
        | "Weather is nice" | casual_conversation | 0.3 | 1 |
        | "I prefer metric units" | user_preference | 0.8 | 10 |
    And a query for memories related to "weather in Brisbane"
    When the GetRelevantMemoriesHandler executes the query
    Then the results should include the Brisbane location memory
    And the results should include the metric preference memory
    And the results should be ordered by relevance score
    And casual conversation should have lower priority

Scenario: Get conversation history query with pagination
    Given a conversation with 20 memory items
    And a query for conversation history with page size 5
    When the GetConversationHistoryHandler executes the query
    Then the result should contain 5 most recent memories
    And the memories should be ordered by timestamp descending
    And pagination information should be included

Scenario: Command validation prevents invalid operations
    Given an invalid user input command with empty content
    When the ProcessUserInputHandler attempts to execute
    Then a validation exception should be thrown
    And no memory should be stored
    And no AI response should be generated

Scenario: Query optimization limits memory retrieval
    Given a memory store with 1000+ memories
    And a query for relevant memories with limit 10
    When the GetRelevantMemoriesHandler executes
    Then only the top 10 most relevant memories should be returned
    And the query should complete within performance thresholds
    And memory usage should remain efficient

Scenario: Command handlers publish domain events
    Given a user input command that creates new memory
    When the command is processed successfully
    Then a MemoryStored domain event should be published
    And a UserInputReceived domain event should be published
    And event handlers should be able to process these events

Scenario: Query handlers use read-only repositories
    Given a query for conversation history
    When the query handler executes
    Then it should use read-only repository methods
    And no write operations should be performed
    And the original data should remain unchanged
