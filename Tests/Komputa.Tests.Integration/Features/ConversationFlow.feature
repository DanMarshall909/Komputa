Feature: End-to-End Conversation Flow
    As a user of Komputa
    I want to have natural conversations with memory continuity
    So that the assistant provides a seamless experience

Background:
    Given the Komputa system is fully initialized
    And all services are properly configured
    And the memory store is clean

Scenario: Complete conversation with memory persistence
    Given I start a new conversation
    When I say "Hi, my name is Sarah and I live in Melbourne"
    Then the assistant should acknowledge my introduction
    And my name "Sarah" should be stored as personal information
    And my location "Melbourne" should be stored with high importance
    When I say "What's the weather like?"
    Then the assistant should remember I'm in Melbourne
    And provide weather information for Melbourne
    And the conversation should maintain context

Scenario: Cross-session memory continuity
    Given I had a previous conversation where I mentioned "I work as a software developer"
    And the conversation ended 2 hours ago
    When I start a new conversation session
    And I say "I'm having trouble with my work today"
    Then the assistant should remember I'm a software developer
    And provide relevant technical assistance
    And reference the previous context appropriately

Scenario: Multiple topic conversation with proper memory management
    Given I start a conversation about "planning a vacation to Japan"
    When I discuss "budget concerns" during the conversation
    And then switch to asking about "best time to visit"
    And finally ask about "visa requirements"
    Then all topics should be stored as related memories
    And future questions about the Japan trip should reference all relevant information
    And the memory should maintain topic relationships

Scenario: AI provider fallback maintains conversation continuity
    Given I'm having a conversation with the primary AI provider
    And the primary provider becomes unavailable
    When the system switches to the fallback provider
    Then the conversation context should be maintained
    And the response quality should remain consistent
    And the memory storage should continue functioning

Scenario: Web search integration with memory awareness
    Given I previously mentioned "I'm interested in climate change"
    When I ask "What's the latest news about environmental policy?"
    Then the system should perform a web search
    And include my known interest in climate change in the context
    And store the search results as temporary memory
    And provide a personalized response based on my interests

Scenario: Error handling preserves conversation state
    Given I'm in the middle of a conversation about "weekend plans"
    When a temporary system error occurs
    And the error is resolved
    Then my conversation context should be preserved
    And I should be able to continue the discussion
    And no memory should be lost

Scenario: Large conversation history management
    Given I have had 100+ interactions over several weeks
    When I ask a question that relates to information from 2 weeks ago
    Then the system should efficiently retrieve relevant old memories
    And provide contextually appropriate responses
    And maintain good performance despite large memory size

Scenario: Privacy-sensitive conversation handling
    Given I share sensitive personal information like "my social security number is 123-45-6789"
    When the conversation continues
    Then the sensitive information should be stored securely
    And should not appear in logs or debug output
    And should be accessible only when directly relevant
    And appropriate privacy controls should be applied
