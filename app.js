// --- FIREBASE CONFIGURATION (PASTE KEYS HERE) ---
const firebaseConfig = {
    apiKey: "AIzaSyD--YQOiniI1Rdrx8tFvtsu-ZoOZjR5BlA",
  authDomain: "stranger-99.firebaseapp.com",
  projectId: "stranger-99",
  storageBucket: "stranger-99.firebasestorage.app",
  messagingSenderId: "419674409538",
  appId: "1:419674409538:web:0b32d852a8202e6bfd3bdb"
};

// Initialize Firebase
let app, auth, db;
try {
    app = firebase.initializeApp(firebaseConfig);
    auth = firebase.auth();
    db = firebase.database();
} catch (e) {
    console.warn("Firebase not waiting for config...", e);
}

// --- APP STATE ---
const STATE = {
    myId: null,
    settings: {
        gender: "Non-binary",
        interest: "Both",
        avatarSeed: Math.floor(Math.random() * 5000)
    },
    currentTab: 'people',
    activePrivateChat: null,
    users: {}, // Cache of online users
    games: {}
};

// --- DOM ELEMENTS ---
const dom = {
    authSection: document.getElementById('auth-section'),
    chatSection: document.getElementById('chat-section'),
    btnRegister: document.getElementById('btn-register'),
    registrationInfo: document.getElementById('registration-info'),
    myIdDisplay: document.getElementById('my-id'),
    displayIdHead: document.getElementById('display-id'),
    myAvatar: document.getElementById('my-avatar'),
    genderSymbol: document.getElementById('gender-symbol'),

    // Tabs
    tabs: document.querySelectorAll('.nav-tab'),
    views: {
        people: document.getElementById('view-people'), // Using for Global Chat now based on HTML
        matches: document.getElementById('view-matches'),
        private: document.getElementById('view-private')
    },

    // Global Chat
    globalMessages: document.getElementById('messages'),
    globalInput: document.getElementById('message-input'),
    btnSendGlobal: document.getElementById('btn-send'),

    // Private Chat
    privateList: document.getElementById('private-conversations-list'),
    privateArea: document.getElementById('private-chat-area'),
    privateContainer: document.getElementById('private-messages-container'),
    privateHeaderName: document.getElementById('private-chat-partner-name'),
    btnClosePrivate: document.getElementById('btn-close-private'),
    privateMessages: document.getElementById('private-messages-list'),
    privateInput: document.getElementById('private-message-input'),
    btnSendPrivate: document.getElementById('btn-send-private'),

    // Grid
    grid: document.getElementById('profiles-grid'),

    // Settings
    btnSettings: document.getElementById('btn-settings'),
    settingsModal: document.getElementById('settings-modal'),
    btnCloseSettings: document.getElementById('btn-close-settings'),
    btnSaveSettings: document.getElementById('btn-save-settings'),
    selectGender: document.getElementById('select-gender'),
    selectInterest: document.getElementById('select-interest')
};

// --- AUTHENTICATION & STARTUP ---
dom.btnRegister.onclick = () => {
    if (firebaseConfig.apiKey === "GANTI_DENGAN_API_KEY_ANDA") {
        alert("PERHATIAN: Anda belum memasukkan Kunci Firebase di app.js! Baca PANDUAN_LENGKAP.md");
        return;
    }

    dom.btnRegister.classList.add('hidden');
    dom.registrationInfo.classList.remove('hidden');

    auth.signInAnonymously()
        .then((userCredential) => {
            // Signed in..
            const user = userCredential.user;
            console.log("Logged in as", user.uid);
        })
        .catch((error) => {
            console.error(error);
            alert("Error login: " + error.message);
            dom.btnRegister.classList.remove('hidden');
            dom.registrationInfo.classList.add('hidden');
        });
};

// Auth State Observer
if (auth) {
    auth.onAuthStateChanged((user) => {
        if (user) {
            initUser(user);
        } else {
            // Logged out
        }
    });
}

function initUser(user) {
    // Generate readable ID (first 6 chars of UID for simplicity in UI)
    const shortId = user.uid.substring(0, 6).toUpperCase();
    STATE.myId = shortId;
    STATE.uid = user.uid;

    // Load Settings
    const saved = localStorage.getItem('glowme_settings');
    if (saved) STATE.settings = JSON.parse(saved);

    // Update UI
    dom.myIdDisplay.textContent = shortId;
    dom.displayIdHead.textContent = `ID: ${shortId}`;
    updateAvatar(STATE.settings.gender);

    // Switch Scene
    setTimeout(() => {
        dom.authSection.classList.add('hidden');
        dom.chatSection.classList.remove('hidden');
        dom.chatSection.classList.add('fade-in');
        setupPresence();
        loadGlobalChat();
        loadUsersGrid();
        loadPrivateChats();
    }, 1000);
}

// --- PRESENCE SYSTEM ---
function setupPresence() {
    const userStatusDatabaseRef = db.ref('/status/' + STATE.uid);
    const userProfileRef = db.ref('/users/' + STATE.uid);

    const isOfflineForDatabase = {
        state: 'offline',
        last_changed: firebase.database.ServerValue.TIMESTAMP,
    };

    const isOnlineForDatabase = {
        state: 'online',
        last_changed: firebase.database.ServerValue.TIMESTAMP,
        id: STATE.myId,
        gender: STATE.settings.gender,
        interest: STATE.settings.interest
    };

    db.ref('.info/connected').on('value', (snapshot) => {
        if (snapshot.val() == false) return;

        userStatusDatabaseRef.onDisconnect().set(isOfflineForDatabase).then(() => {
            userStatusDatabaseRef.set(isOnlineForDatabase);
            userProfileRef.update({
                id: STATE.myId,
                gender: STATE.settings.gender,
                interest: STATE.settings.interest,
                last_seen: firebase.database.ServerValue.TIMESTAMP
            });
        });
    });
}

function updateAvatar(gender) {
    let color1 = "#ddd", color2 = "#999";
    let symbol = "";
    if (gender === 'Male') { color1 = "#4facfe"; color2 = "#00f2fe"; symbol = "♂"; }
    if (gender === 'Female') { color1 = "#f093fb"; color2 = "#f5576c"; symbol = "♀"; }
    if (gender === 'Non-binary') { color1 = "#f6d365"; color2 = "#fda085"; symbol = "⚧"; }

    dom.myAvatar.style.background = `linear-gradient(135deg, ${color1}, ${color2})`;
    dom.genderSymbol.textContent = symbol;
    dom.genderSymbol.className = `gender-symbol-mini gender-${gender.toLowerCase()}`;
}

// --- GLOBAL CHAT ---
function loadGlobalChat() {
    const messagesRef = db.ref('messages/global').limitToLast(50);
    messagesRef.on('child_added', (snapshot) => {
        const msg = snapshot.val();
        appendMessage(dom.globalMessages, msg.senderId, msg.text, msg.senderId === STATE.myId ? 'sent' : 'received');
    });
}

dom.btnSendGlobal.onclick = () => sendGlobalMessage();
dom.globalInput.onkeypress = (e) => { if (e.key === 'Enter') sendGlobalMessage(); };

function sendGlobalMessage() {
    const text = dom.globalInput.value.trim();
    if (!text) return;

    db.ref('messages/global').push({
        senderId: STATE.myId,
        text: text,
        timestamp: firebase.database.ServerValue.TIMESTAMP
    });
    dom.globalInput.value = "";
}

function appendMessage(container, sender, text, type) {
    // Remove empty state
    const empty = container.querySelector('.empty-state');
    if (empty) empty.remove();

    const div = document.createElement('div');
    div.className = `message-bubble message-${type}`;

    // Simple sender name
    const b = document.createElement('div');
    b.className = 'message-sender';
    b.textContent = sender;

    div.appendChild(b);
    div.appendChild(document.createTextNode(text));
    container.appendChild(div);
    container.scrollTop = container.scrollHeight;
}

// --- USERS GRID (People Tab) ---
function loadUsersGrid() {
    db.ref('/status').on('value', (snapshot) => {
        const users = snapshot.val() || {};
        dom.grid.innerHTML = "";
        STATE.users = users;

        let found = false;
        Object.keys(users).forEach(uid => {
            const user = users[uid];
            if (user.state === 'online' && uid !== STATE.uid) {
                found = true;
                createProfileCard(uid, user);
            }
        });

        if (!found) {
            dom.grid.innerHTML = `<p style="text-align:center; width:100%; grid-column:1/-1; color:var(--text-muted)">Searching for active users...</p>`;
        }
    });
}

function createProfileCard(uid, user) {
    const card = document.createElement('div');
    card.className = 'profile-card fade-in';

    let genderColor = "#ddd";
    if (user.gender === 'Male') genderColor = "#4facfe";
    if (user.gender === 'Female') genderColor = "#f093fb";
    if (user.gender === 'Non-binary') genderColor = "#f6d365";

    card.innerHTML = `
        <div class="profile-avatar" style="background: ${genderColor}">
            <div class="profile-status-dot status-online"></div>
        </div>
        <div class="profile-name">${user.id || 'Anon'}</div>
        <div class="profile-info">
            <span>${user.gender || '?'}</span> • <span>${user.interest || '?'}</span>
        </div>
        <button class="btn-tiny" style="margin-top:8px; width:100%">Chat</button>
    `;

    card.onclick = () => startPrivateChat(uid, user.id);
    dom.grid.appendChild(card);
}

// --- PRIVATE CHAT ---
function startPrivateChat(partnerUid, partnerId) {
    // Generate Chat ID (Sorted UIDs to ensure uniqueness per pair)
    const chatId = [STATE.uid, partnerUid].sort().join('_');
    STATE.activePrivateChat = { chatId, partnerUid, partnerId };

    // Register this chat in my conversation list (and theirs mostly auto-handled by inbox listener if we had one, but we'll simplify)
    // In this simple version, we just open the view directly.

    // Switch to Private Tab
    switchTab('private');

    // Reset View
    dom.privateContainer.classList.remove('hidden');
    dom.privateArea.querySelector('.empty-state-private')?.classList.add('hidden');
    dom.privateHeaderName.textContent = partnerId;
    dom.privateMessages.innerHTML = ''; // Clear prev chat

    // Load Messages
    db.ref(`messages/private/${chatId}`).limitToLast(50).off(); // Remove old listeners
    db.ref(`messages/private/${chatId}`).limitToLast(50).on('child_added', snapshot => {
        const msg = snapshot.val();
        appendMessage(dom.privateMessages, msg.senderId, msg.text, msg.senderId === STATE.myId ? 'sent' : 'received');
    });
}

dom.btnSendPrivate.onclick = () => sendPrivateMessage();
dom.privateInput.onkeypress = (e) => { if (e.key === 'Enter') sendPrivateMessage(); };

function sendPrivateMessage() {
    if (!STATE.activePrivateChat) return;
    const text = dom.privateInput.value.trim();
    if (!text) return;

    const { chatId } = STATE.activePrivateChat;
    db.ref(`messages/private/${chatId}`).push({
        senderId: STATE.myId,
        text: text,
        timestamp: firebase.database.ServerValue.TIMESTAMP
    });

    // Add to 'inbox' logic could go here to persist the list of conversations
    addToInbox(STATE.uid, STATE.activePrivateChat.partnerUid, STATE.activePrivateChat.partnerId);
    addToInbox(STATE.activePrivateChat.partnerUid, STATE.uid, STATE.myId); // Add to theirs too

    dom.privateInput.value = "";
}

function addToInbox(ownerUid, partnerUid, partnerId) {
    db.ref(`users/${ownerUid}/conversations/${partnerUid}`).set({
        partnerId: partnerId || 'Unknown',
        lastUpdated: firebase.database.ServerValue.TIMESTAMP
    });
}

function loadPrivateChats() {
    db.ref(`users/${STATE.uid}/conversations`).on('value', snapshot => {
        const items = snapshot.val() || {};
        dom.privateList.innerHTML = '';
        Object.keys(items).forEach(partnerUid => {
            const data = items[partnerUid];
            const div = document.createElement('div');
            div.className = 'conversation-item';
            div.innerHTML = `
                <div style="width:10px; height:10px; background:var(--primary); border-radius:50%"></div>
                <div style="font-weight:600">${data.partnerId}</div>
            `;
            div.onclick = () => startPrivateChat(partnerUid, data.partnerId);
            dom.privateList.appendChild(div);
        });
    });
}

dom.btnClosePrivate.onclick = () => {
    dom.privateContainer.classList.add('hidden');
    dom.privateArea.querySelector('.empty-state-private')?.classList.remove('hidden');
    STATE.activePrivateChat = null;
    // Remove listeners
    // ...
};

// --- TAB LOGIC ---
dom.tabs.forEach(tab => {
    tab.onclick = () => {
        const target = tab.dataset.tab;
        switchTab(target);
    };
});

function switchTab(targetName) {
    // Update Tabs
    dom.tabs.forEach(t => t.classList.remove('active'));
    document.querySelector(`.nav-tab[data-tab="${targetName}"]`).classList.add('active');

    // Update Views
    Object.keys(dom.views).forEach(k => dom.views[k].classList.remove('active', 'hidden')); // removing active mainly
    Object.keys(dom.views).forEach(k => {
        if (k === targetName) {
            dom.views[k].classList.add('active');
        } else {
            dom.views[k].classList.remove('active'); // ensure hidden via CSS
        }
    });
}

// --- SETTINGS ---
dom.btnSettings.onclick = () => dom.settingsModal.classList.remove('hidden');
dom.btnCloseSettings.onclick = () => dom.settingsModal.classList.add('hidden');

dom.btnSaveSettings.onclick = () => {
    STATE.settings.gender = dom.selectGender.value;
    STATE.settings.interest = dom.selectInterest.value;
    localStorage.setItem('glowme_settings', JSON.stringify(STATE.settings));

    updateAvatar(STATE.settings.gender);

    // Update online status immediately
    if (STATE.uid) {
        db.ref('/status/' + STATE.uid).update({
            gender: STATE.settings.gender,
            interest: STATE.settings.interest
        });
        db.ref('/users/' + STATE.uid).update({
            gender: STATE.settings.gender,
            interest: STATE.settings.interest
        });
    }

    dom.settingsModal.classList.add('hidden');
};

// --- GAME LOGIC (Play Together) ---
// Syncing 'speed' via Firebase instead of SignalR

let gameLoopId;
let ballPosY = 50;
let ballDirection = 1;
let currentGameId = null;

const btnPlayTogether = document.getElementById('btn-play-together');
const btnStartGame = document.getElementById('btn-start-game');
const matchText = document.getElementById('match-text');
const matchDot = document.getElementById('match-dot');
const gameOverlay = document.getElementById('game-overlay');
const btnExitGame = document.getElementById('btn-exit-game');
const gameBall = document.getElementById('game-ball');
const speedSlider = document.getElementById('speed-slider');
const gameControls = document.getElementById('game-controls');
const gameRole = document.getElementById('game-role');

btnPlayTogether.onclick = () => {
    if (matchText.textContent === 'Idle') {
        startMatchmaking();
    } else {
        cancelMatchmaking();
    }
};

function startMatchmaking() {
    matchText.textContent = "Searching...";
    matchDot.className = "dot dot-searching";

    // Simple Matchmaking: Look for anyone else 'searching'
    // In a real app, use Cloud Functions. Here: Client-side logic (first found).
    db.ref('matchmaking').once('value', snapshot => {
        const others = snapshot.val() || {};
        const otherIds = Object.keys(others).filter(k => k !== STATE.uid);

        if (otherIds.length > 0) {
            // Found a partner!
            const partnerUid = otherIds[0];
            createGame(partnerUid);
            db.ref('matchmaking/' + partnerUid).remove(); // Remove them from queue
        } else {
            // No one found, add self to queue
            db.ref('matchmaking/' + STATE.uid).set({
                id: STATE.myId,
                timestamp: firebase.database.ServerValue.TIMESTAMP
            });

            // Listen for being picked
            db.ref('matchmaking/' + STATE.uid).on('child_removed', () => {
                // Determine if we were removed because we joined a game
                // We check if we are in a game node?
                // Actually easier: write an 'invite' to the user.
                // Simplified: Just polling 'users/{uid}/game' assignment.
            });
        }
    });

    // Listen for Game Assignment
    db.ref(`users/${STATE.uid}/activeGame`).on('value', snapshot => {
        const gameId = snapshot.val();
        if (gameId) {
            joinGame(gameId);
            db.ref('matchmaking/' + STATE.uid).remove(); // Stop searching
        }
    });
}

function cancelMatchmaking() {
    matchText.textContent = "Idle";
    matchDot.className = "dot dot-idle";
    db.ref('matchmaking/' + STATE.uid).remove();
    db.ref(`users/${STATE.uid}/activeGame`).off();
}

function createGame(partnerUid) {
    const gameId = `game_${Date.now()}_${Math.floor(Math.random() * 1000)}`;
    const gameData = {
        players: {
            [STATE.uid]: { role: 'controller' }, // Creator controls
            [partnerUid]: { role: 'spectator' }
        },
        speed: 10,
        status: 'playing',
        timestamp: firebase.database.ServerValue.TIMESTAMP
    };

    db.ref('games/' + gameId).set(gameData).then(() => {
        // Notify both players
        db.ref(`users/${STATE.uid}/activeGame`).set(gameId);
        db.ref(`users/${partnerUid}/activeGame`).set(gameId);
    });
}

function joinGame(gameId) {
    currentGameId = gameId;
    matchText.textContent = "Matched!";
    matchDot.className = "dot dot-matched";
    btnPlayTogether.classList.add('hidden');
    btnStartGame.classList.remove('hidden'); // Logic differs here from SignalR, let's just auto-start

    // Auto start for fluid UX
    enterGameView();
}

function enterGameView() {
    gameOverlay.classList.remove('hidden');

    // Listen to Game State
    db.ref(`games/${currentGameId}`).on('value', snapshot => {
        const list = snapshot.val();
        if (!list) {
            exitGameLocal();
            return;
        }

        // Speed Sync
        if (list.speed) {
            currentBallSpeed = list.speed;
            if (speedSlider) speedSlider.value = list.speed;
        }

        // Role
        const myRole = list.players && list.players[STATE.uid]?.role;
        gameRole.textContent = myRole === 'controller' ? "You are Controlling" : "Spectating Partner";

        if (myRole === 'controller') {
            gameControls.classList.remove('hidden');
            document.getElementById('emoticon-panel').classList.add('hidden');
        } else {
            gameControls.classList.add('hidden');
            document.getElementById('emoticon-panel').classList.remove('hidden');
        }
    });

    // Listen for Emoticons
    db.ref(`games/${currentGameId}/emoticons`).on('child_added', snapshot => {
        const data = snapshot.val();
        // Show if not from me (or show mine too for feedback)
        if (Date.now() - data.timestamp < 2000) { // Only recent
            showEmoticonNotification(data.emoji, data.sender);
        }
    });

    startGameLoop();
}

btnStartGame.onclick = enterGameView;

btnExitGame.onclick = () => {
    if (currentGameId) {
        // Destroy game for everyone (or leave)
        db.ref(`games/${currentGameId}`).remove();
        db.ref(`users/${STATE.uid}/activeGame`).remove();
    }
    exitGameLocal();
};

function exitGameLocal() {
    gameOverlay.classList.add('hidden');
    btnPlayTogether.classList.remove('hidden');
    btnStartGame.classList.add('hidden');
    matchText.textContent = "Idle";
    matchDot.className = "dot dot-idle";
    cancelMatchmaking();

    cancelAnimationFrame(gameLoopId);
    currentGameId = null;

    // Reset listeners
    if (currentGameId) db.ref(`games/${currentGameId}`).off();
}

// Game Loop
let currentBallSpeed = 10;
function startGameLoop() {
    ballPosY += (currentBallSpeed * 0.1) * ballDirection;
    if (ballPosY > 90) { ballPosY = 90; ballDirection = -1; }
    else if (ballPosY < 5) { ballPosY = 5; ballDirection = 1; }

    gameBall.style.top = ballPosY + "%";
    gameLoopId = requestAnimationFrame(startGameLoop);
}

// Slider
speedSlider.oninput = () => {
    if (currentGameId) {
        db.ref(`games/${currentGameId}`).update({
            speed: parseInt(speedSlider.value)
        });
    }
};

// Emoticons
document.querySelectorAll('.emoticon-btn-compact').forEach(btn => {
    btn.onclick = () => {
        if (!currentGameId) return;
        const emoji = btn.dataset.emoticon;
        db.ref(`games/${currentGameId}/emoticons`).push({
            emoji: emoji,
            sender: STATE.myId,
            timestamp: firebase.database.ServerValue.TIMESTAMP
        });

        // Visual feedback
        btn.style.transform = 'scale(1.3)';
        setTimeout(() => btn.style.transform = 'scale(1)', 200);
    };
});

// Helper for Emoticon
function showEmoticonNotification(emoticon, senderId) {
    const notification = document.getElementById('emoticon-notification');
    if (!notification) return;
    notification.innerHTML = `<div class="emoticon-large">${emoticon}</div><div class="sender-id-small">${senderId}</div>`;
    notification.classList.remove('hidden');
    notification.classList.add('fade-in-fast');
    setTimeout(() => {
        notification.classList.remove('fade-in-fast');
        notification.classList.add('fade-out-slow');
        setTimeout(() => {
            notification.classList.add('hidden');
            notification.classList.remove('fade-out-slow');
        }, 600);
    }, 300);
}

