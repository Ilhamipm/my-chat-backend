const isLocalhost = location.hostname === "localhost" || location.hostname === "127.0.0.1";
const hubUrl = isLocalhost ? "/chatHub" : "https://my-chat-backend-production-2b56.up.railway.app/chatHub";

const connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl)
    .withAutomaticReconnect()
    .build();

const STORAGE_KEY = 'glowme_user_id';

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
const btnEditId = document.getElementById('btn-edit-id');

let myId = null;

// SignalR Events
connection.on("UserRegistered", (id) => {
    myId = id;
    localStorage.setItem(STORAGE_KEY, id); // Simpan ID agar tetap sama besok
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
        const savedId = localStorage.getItem(STORAGE_KEY);
        await connection.invoke("Register", savedId);
    } catch (err) {
        console.error("Connection failed: ", err);
        alert("Failed to connect to server. Make sure the backend is running.");
    }
};

btnEditId.onclick = async () => {
    const newId = prompt("Enter your new custom ID (max 20 chars):", myId);
    if (newId && newId !== myId) {
        try {
            await connection.invoke("ChangeId", newId);
        } catch (err) {
            console.error(err);
        }
    }
};

// Auto-start if we have a saved ID
(async () => {
    const savedId = localStorage.getItem(STORAGE_KEY);
    if (savedId) {
        try {
            await connection.start();
            await connection.invoke("Register", savedId);
            // Auto enter chat if we were already registered
            authSection.classList.add('hidden');
            chatSection.classList.remove('hidden');
        } catch (err) {
            console.warn("Auto-reconnect failed:", err);
        }
    }
})();

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
