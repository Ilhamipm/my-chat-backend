const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub") // Relative path when served from same host
    .withAutomaticReconnect()
    .build();

const authSection = document.getElementById('auth-section');
const chatSection = document.getElementById('chat-section');
const registrationInfo = document.getElementById('registration-info');
const btnRegister = document.getElementById('btn-register');
const btnEnterChat = document.getElementById('btn-enter-chat');
const myIdDisplay = document.getElementById('my-id');
const displayIdHead = document.getElementById('display-id');
const messagesList = document.getElementById('messages');
const messageInput = document.getElementById('message-input');
const targetIdInput = document.getElementById('target-id');
const btnSend = document.getElementById('btn-send');
const btnLogout = document.getElementById('btn-logout');

let myId = null;

// SignalR Events
connection.on("UserRegistered", (id) => {
    myId = id;
    myIdDisplay.textContent = id;
    displayIdHead.textContent = `ID: ${id}`;
    btnRegister.classList.add('hidden');
    registrationInfo.classList.remove('hidden');
});

connection.on("ReceiveMessage", (senderId, message) => {
    appendMessage(senderId, message, 'received');
});

connection.on("Error", (msg) => {
    alert(msg);
});

// UI Actions
btnRegister.onclick = async () => {
    try {
        if (connection.state === signalR.HubConnectionState.Disconnected) {
            await connection.start();
        }
        await connection.invoke("Register");
    } catch (err) {
        console.error("Connection failed: ", err);
        alert("Failed to connect to server. Make sure the backend is running.");
    }
};

btnEnterChat.onclick = () => {
    authSection.classList.add('hidden');
    chatSection.classList.remove('hidden');
    chatSection.classList.add('fade-in');
};

btnSend.onclick = sendMessage;
messageInput.onkeypress = (e) => {
    if (e.key === 'Enter') sendMessage();
};

async function sendMessage() {
    const targetId = targetIdInput.value.trim();
    const text = messageInput.value.trim();

    if (!targetId) {
        alert("Please enter a target ID.");
        return;
    }
    if (!text) return;

    try {
        await connection.invoke("SendMessageToId", targetId, text);
        appendMessage("You", text, 'sent');
        messageInput.value = "";
    } catch (err) {
        console.error(err);
    }
}

function appendMessage(sender, text, type) {
    const emptyState = document.querySelector('.empty-state');
    if (emptyState) emptyState.remove();

    const div = document.createElement('div');
    div.className = `message-bubble message-${type}`;

    const senderSpan = document.createElement('span');
    senderSpan.className = 'message-sender';
    senderSpan.textContent = sender;

    const textNode = document.createTextNode(text);

    div.appendChild(senderSpan);
    div.appendChild(textNode);

    messagesList.appendChild(div);
    messagesList.scrollTop = messagesList.scrollHeight;
}

btnLogout.onclick = () => {
    location.reload();
};
