const chatMessages = document.getElementById('chat-messages');
const userInput = document.getElementById('user-input');
const sendBtn = document.getElementById('send-btn');
const memoryBtn = document.getElementById('memory-btn');
const connectionStatus = document.getElementById('connection-status');

// API base URL - change this if your API runs on a different port
const API_BASE_URL = 'http://localhost:5000/api';

// Set welcome message time
document.getElementById('welcome-time').textContent = formatTime(new Date());

// Auto-resize textarea
userInput.addEventListener('input', function() {
    this.style.height = 'auto';
    this.style.height = Math.min(this.scrollHeight, 120) + 'px';
});

// Send message on Enter (Shift+Enter for new line)
userInput.addEventListener('keydown', function(e) {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

sendBtn.addEventListener('click', sendMessage);
memoryBtn.addEventListener('click', checkMemoryStatus);

async function sendMessage() {
    const message = userInput.value.trim();

    if (!message) return;

    // Add user message to chat
    addMessage(message, 'user');

    // Clear input
    userInput.value = '';
    userInput.style.height = 'auto';

    // Disable send button
    sendBtn.disabled = true;

    // Show typing indicator
    const typingId = showTypingIndicator();

    try {
        const response = await fetch(`${API_BASE_URL}/chat/message`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ message: message })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();

        // Remove typing indicator
        removeTypingIndicator(typingId);

        // Add assistant response
        addMessage(data.response, 'assistant');

        updateConnectionStatus(true);
    } catch (error) {
        console.error('Error sending message:', error);

        // Remove typing indicator
        removeTypingIndicator(typingId);

        // Show error message
        showError('Failed to send message. Please check if the API is running.');

        updateConnectionStatus(false);
    } finally {
        sendBtn.disabled = false;
        userInput.focus();
    }
}

async function checkMemoryStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/chat/memory`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();

        // Add memory status as a system message
        addMessage(`Memory Status: ${data.status}`, 'assistant');

        updateConnectionStatus(true);
    } catch (error) {
        console.error('Error checking memory:', error);
        showError('Failed to retrieve memory status.');
        updateConnectionStatus(false);
    }
}

function addMessage(content, sender) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${sender}-message`;

    const now = new Date();

    messageDiv.innerHTML = `
        <div class="message-header">
            <span class="message-sender">${sender === 'user' ? 'You' : 'Komputa'}</span>
            <span class="message-time">${formatTime(now)}</span>
        </div>
        <div class="message-content">${escapeHtml(content)}</div>
    `;

    chatMessages.appendChild(messageDiv);
    scrollToBottom();
}

function showTypingIndicator() {
    const typingDiv = document.createElement('div');
    typingDiv.className = 'message assistant-message';
    typingDiv.id = 'typing-indicator-' + Date.now();

    typingDiv.innerHTML = `
        <div class="message-header">
            <span class="message-sender">Komputa</span>
            <span class="message-time">typing...</span>
        </div>
        <div class="message-content typing-indicator">
            <span></span>
            <span></span>
            <span></span>
        </div>
    `;

    chatMessages.appendChild(typingDiv);
    scrollToBottom();

    return typingDiv.id;
}

function removeTypingIndicator(typingId) {
    const typingDiv = document.getElementById(typingId);
    if (typingDiv) {
        typingDiv.remove();
    }
}

function showError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'error-message';
    errorDiv.textContent = message;

    chatMessages.appendChild(errorDiv);
    scrollToBottom();

    // Remove error after 5 seconds
    setTimeout(() => {
        errorDiv.remove();
    }, 5000);
}

function updateConnectionStatus(isConnected) {
    if (isConnected) {
        connectionStatus.textContent = '🟢 Connected';
        connectionStatus.style.background = 'rgba(76, 175, 80, 0.2)';
    } else {
        connectionStatus.textContent = '🔴 Disconnected';
        connectionStatus.style.background = 'rgba(244, 67, 54, 0.2)';
    }
}

function scrollToBottom() {
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function formatTime(date) {
    return date.toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit'
    });
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Check API health on load
async function checkApiHealth() {
    try {
        const response = await fetch(`${API_BASE_URL}/chat/health`);
        if (response.ok) {
            updateConnectionStatus(true);
        } else {
            updateConnectionStatus(false);
        }
    } catch (error) {
        updateConnectionStatus(false);
        showError('Cannot connect to API. Please make sure the Komputa API is running on http://localhost:5000');
    }
}

// Check health on page load
checkApiHealth();
