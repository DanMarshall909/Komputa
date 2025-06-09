Feature: Memory Scoring and Decay
    As the Komputa system
    I want to score and manage memory importance
    So that relevant information is prioritized and less important information naturally fades

Background:
    Given the memory scoring system is initialized
    And the decay calculator is available

Scenario: Personal information receives high importance score
    Given user input contains personal information "My birthday is March 15th"
    When the memory scorer evaluates the content
    Then the importance score should be greater than 0.8
    And the content type should be "personal_information"
    And the memory should be tagged with "personal" and "birthday"

Scenario: Preferences get high importance scores
    Given user input contains "I always prefer coffee over tea"
    When the memory scorer evaluates the content
    Then the importance score should be greater than 0.7
    And the content type should be "user_preference"
    And the memory should be tagged with "preference" and "drinks"

Scenario: Casual conversation gets lower importance
    Given user input contains "Nice weather today, isn't it?"
    When the memory scorer evaluates the content
    Then the importance score should be less than 0.5
    And the content type should be "casual_conversation"
    And the memory should have appropriate casual tags

Scenario: Important memories retain high scores over time
    Given a memory with high initial importance of 0.9
    And the memory has been accessed recently within 7 days
    When the decay calculator runs after 10 days
    Then the memory score should remain above 0.8
    And the memory should stay in active context pool

Scenario: Unused memories decay over time
    Given a memory with medium importance of 0.6
    And the memory has not been accessed for 30 days
    When the decay calculator runs
    Then the memory score should decrease to below 0.4
    And the memory should be less likely to appear in context

Scenario: Frequently accessed memories get boosted
    Given a memory that has been accessed 5 times this week
    And the memory initially had importance of 0.5
    When the access frequency is calculated
    Then the effective importance should be boosted above 0.7
    And the memory should appear more often in conversation context

Scenario: Memory scoring considers content type hierarchy
    Given multiple memories of different types:
        | Content | Type | Expected Score Range |
        | "My name is Alice" | personal_information | 0.8-1.0 |
        | "I prefer metric units" | user_preference | 0.7-0.9 |
        | "Weather is nice" | casual_conversation | 0.2-0.5 |
        | "Error in last response" | error_correction | 0.6-0.8 |
    When the memory scorer evaluates all content
    Then each memory should receive a score within its expected range
    And personal information should have the highest priority
