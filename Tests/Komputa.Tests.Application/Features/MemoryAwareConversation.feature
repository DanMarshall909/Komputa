Feature: Memory-Aware Conversation
    As a user of Komputa
    I want the assistant to remember our previous conversations
    So that our interactions are contextual and personalized

Background:
    Given the memory store is initialized
    And the AI provider is available

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

Scenario: Assistant remembers user corrections
    Given I previously corrected the assistant saying "I prefer Celsius not Fahrenheit"
    When I ask about temperature
    Then the assistant should use Celsius in the response
    And the correction should be stored as high-importance memory

Scenario: Conversation context is maintained across sessions
    Given I had a conversation yesterday about "planning a trip to Brisbane"
    And I start a new conversation today
    When I ask "What was that place I wanted to visit?"
    Then the assistant should reference the Brisbane trip discussion
    And provide relevant information from the previous conversation

Scenario: Memory decay affects old conversations
    Given I had a conversation 30 days ago about "favorite pizza toppings"
    And the memory has not been accessed since
    When the decay calculator runs
    Then the memory importance score should decrease
    And it should be less likely to appear in conversation context

Scenario: Frequently accessed memories stay relevant
    Given I regularly ask about "work schedule"
    And this topic has been discussed 5 times this week
    When I ask about my schedule again
    Then the work schedule memories should have high relevance
    And should appear in the conversation context
