// ======================================================
// ROBOT ARM CONTROL
// ======================================================

const servos = ['Base', 'Shoulder', 'Elbow', 'Wrist', 'Gripper'];

const robotSelect = document.querySelector('#robot-select');
const servoList = document.querySelector('#servo-list');
const feedback = document.querySelector('#feedback');
const commandTable = document.querySelector('#command-table');

const values = Object.fromEntries(
    servos.map((servo) => [servo, 90])
);


// ======================================================
// AUTH ELEMENTS
// ======================================================

const authPage = document.getElementById("auth-page");
const dashboard = document.getElementById("dashboard");

const loginForm = document.getElementById("login-form");
const registerForm = document.getElementById("register-form");

const authSwitch = document.getElementById("auth-switch");
const authTitle = document.getElementById("auth-title");
const authError = document.getElementById("auth-error");

let registerMode = false;
let statusInterval = null;


// ======================================================
// AUTH SWITCH LOGIN / REGISTER
// ======================================================

authSwitch.addEventListener("click", () => {

    registerMode = !registerMode;

    loginForm.hidden = registerMode;
    registerForm.hidden = !registerMode;

    authTitle.textContent =
        registerMode
            ? "Account aanmaken"
            : "Inloggen";

    authSwitch.textContent =
        registerMode
            ? "Al een account? Inloggen"
            : "Nog geen account? Registreren";

    authError.textContent = "";
});


// ======================================================
// REGISTER
// ======================================================

registerForm.addEventListener("submit", async (event) => {

    event.preventDefault();

    authError.textContent = "";

    try {

        const response = await fetch("/api/auth/register", {
            method: "POST",

            headers: {
                "Content-Type": "application/json"
            },

            body: JSON.stringify({
                username:
                    document.getElementById("register-username").value,

                email:
                    document.getElementById("register-email").value,

                password:
                    document.getElementById("register-password").value
            })
        });

        const data = await readResponse(response);

        if (!response.ok) {

            authError.textContent =
                data?.errors?.join(" ") ??
                data?.message ??
                "Registreren mislukt.";

            return;
        }

        // Registratie gelukt -> terug naar login
        registerMode = false;

        registerForm.hidden = true;
        loginForm.hidden = false;

        authTitle.textContent = "Inloggen";

        authSwitch.textContent =
            "Nog geen account? Registreren";

        authError.textContent =
            "Account aangemaakt. Je kunt nu inloggen.";

        // Email alvast invullen
        document.getElementById("login-email").value =
            document.getElementById("register-email").value;

        registerForm.reset();

    }
    catch (error) {

        authError.textContent =
            "De API is niet bereikbaar.";
    }
});


// ======================================================
// LOGIN
// ======================================================

loginForm.addEventListener("submit", async (event) => {

    event.preventDefault();

    authError.textContent = "";

    try {

        const response = await fetch("/api/auth/login", {

            method: "POST",

            headers: {
                "Content-Type": "application/json"
            },

            body: JSON.stringify({
                email:
                    document.getElementById("login-email").value,

                password:
                    document.getElementById("login-password").value
            })
        });

        const data = await readResponse(response);

        if (!response.ok) {

            authError.textContent =
                data?.errors?.join(" ") ??
                data?.message ??
                "Inloggen mislukt.";

            return;
        }

        // Tokens opslaan
        localStorage.setItem("token", data.token);

        if (data.refreshToken) {
            localStorage.setItem(
                "refreshToken",
                data.refreshToken
            );
        }

        // Dashboard openen
        showDashboard();

    }
    catch (error) {

        authError.textContent =
            "De API is niet bereikbaar.";
    }
});


// ======================================================
// API FETCH MET JWT
// ======================================================

async function apiFetch(url, options = {}) {

    const token = localStorage.getItem("token");

    const headers = {
        ...(options.headers || {})
    };

    if (token) {
        headers["Authorization"] = `Bearer ${token}`;
    }

    const response = await fetch(url, {
        ...options,
        headers
    });

    // Token ongeldig/verlopen
    if (response.status === 401) {

        const refreshed = await tryRefreshToken();

        if (refreshed) {

            headers["Authorization"] =
                `Bearer ${localStorage.getItem("token")}`;

            return fetch(url, {
                ...options,
                headers
            });
        }

        logout();
    }

    return response;
}


// ======================================================
// REFRESH TOKEN
// ======================================================

async function tryRefreshToken() {

    const refreshToken =
        localStorage.getItem("refreshToken");

    if (!refreshToken) {
        return false;
    }

    try {

        const response = await fetch(
            "/api/auth/refresh",
            {
                method: "POST",

                headers: {
                    "Content-Type": "application/json"
                },

                body: JSON.stringify({
                    refreshToken
                })
            }
        );

        if (!response.ok) {
            return false;
        }

        const data = await readResponse(response);

        if (!data?.token) {
            return false;
        }

        localStorage.setItem(
            "token",
            data.token
        );

        if (data.refreshToken) {

            localStorage.setItem(
                "refreshToken",
                data.refreshToken
            );
        }

        return true;

    }
    catch {

        return false;
    }
}


// ======================================================
// RESPONSE VEILIG UITLEZEN
// ======================================================

async function readResponse(response) {

    const text = await response.text();

    if (!text) {
        return null;
    }

    try {
        return JSON.parse(text);
    }
    catch {
        return null;
    }
}


// ======================================================
// DASHBOARD TONEN
// ======================================================

function showDashboard() {

    authPage.hidden = true;
    dashboard.hidden = false;

    startDashboard();
}


// ======================================================
// UITLOGGEN
// ======================================================

function logout() {

    localStorage.removeItem("token");
    localStorage.removeItem("refreshToken");

    if (statusInterval) {

        clearInterval(statusInterval);

        statusInterval = null;
    }

    dashboard.hidden = true;
    authPage.hidden = false;

    loginForm.hidden = false;
    registerForm.hidden = true;

    registerMode = false;

    authTitle.textContent = "Inloggen";

    authSwitch.textContent =
        "Nog geen account? Registreren";
}


// ======================================================
// CONNECTION STATUS
// ======================================================

function setConnection(online, label) {

    const connectionLabel =
        document.querySelector('#connection-label');

    const footerStatus =
        document.querySelector('#footer-status');

    connectionLabel.textContent = label;

    footerStatus.textContent =
        online ? 'ONLINE' : 'OFFLINE';
}


// ======================================================
// SERVO UI
// ======================================================

function renderServos() {

    servoList.innerHTML = servos.map((servo) => `

        <label class="servo-row">

            <span class="servo-name">

                ${servo}

                <small>
                    JOINT ${servos.indexOf(servo) + 1}
                </small>

            </span>

            <span>

                <input
                    type="range"
                    min="0"
                    max="180"
                    value="${values[servo]}"
                    data-servo="${servo}">

                <span class="range-labels">

                    <span>0</span>
                    <span>90</span>
                    <span>180</span>

                </span>

            </span>

            <span
                class="servo-value"
                data-value="${servo}">

                ${values[servo]} deg

            </span>

            <button
                class="servo-send"
                type="button"
                data-send-servo="${servo}">

                Stuur

            </button>

        </label>

    `).join('');


    // Slider aanpassen
    servoList
        .querySelectorAll('input')
        .forEach((input) => {

            input.addEventListener(
                'input',
                (event) => {

                    const servo =
                        event.target.dataset.servo;

                    values[servo] =
                        Number(event.target.value);

                    document.querySelector(
                        `[data-value="${servo}"]`
                    ).textContent =
                        `${values[servo]} deg`;

                    document.querySelector(
                        '#active-servo'
                    ).textContent = servo;

                    document.querySelector(
                        '#angle-readout'
                    ).textContent =
                        `${values[servo]} deg`;
                }
            );
        });


    // Individuele servo versturen
    servoList
        .querySelectorAll('[data-send-servo]')
        .forEach((button) => {

            button.addEventListener(
                'click',
                async (event) => {

                    const servo =
                        event.currentTarget.dataset.sendServo;

                    event.currentTarget.disabled = true;

                    try {

                        await sendCommand(
                            servo,
                            values[servo]
                        );

                        feedback.textContent =
                            `${servo} (${values[servo]} deg) is naar de API gestuurd.`;

                        document.querySelector(
                            '#last-update'
                        ).textContent =
                            new Date()
                                .toLocaleTimeString('nl-NL');

                        await loadData();

                    }
                    catch (error) {

                        feedback.textContent =
                            error.message;
                    }
                    finally {

                        event.currentTarget.disabled =
                            false;
                    }
                }
            );
        });
}


// ======================================================
// ROBOTS + COMMANDS LADEN
// ======================================================

async function loadData() {

    try {

        const selectedRobotId =
            robotSelect.value;

        const [
            robotsResponse,
            commandsResponse
        ] = await Promise.all([

            apiFetch('/api/robots'),

            apiFetch('/api/commands')
        ]);

        if (!robotsResponse.ok) {

            throw new Error(
                'Robotgegevens konden niet worden geladen.'
            );
        }

        const robots =
            await robotsResponse.json();

        // Robots in dropdown
        robotSelect.innerHTML =
            robots.length
                ? robots.map((robot) => `
                    <option value="${robot.id}">
                        ${robot.name}
                    </option>
                `).join('')
                : '<option value="">Geen robot geregistreerd</option>';


        // Vorige selectie behouden
        if (
            selectedRobotId &&
            robots.some(
                robot =>
                    String(robot.id) ===
                    String(selectedRobotId)
            )
        ) {

            robotSelect.value =
                selectedRobotId;
        }


        // Status bepalen op basis van geselecteerde robot
        updateRobotStatus(robots);


        // Commands laden
        if (commandsResponse.ok) {

            const commands =
                await commandsResponse.json();

            renderCommands(commands);
        }

    }
    catch (error) {

        setConnection(
            false,
            'Verbinding mislukt'
        );

        feedback.textContent =
            error.message ||
            'Kan de robotgegevens niet laden.';
    }
}


// ======================================================
// ROBOT STATUS
// ======================================================

function updateRobotStatus(robots) {

    const robotId =
        Number(robotSelect.value);

    const robot =
        robots.find(
            robot => robot.id === robotId
        );

    if (!robot) {

        setConnection(
            false,
            'Geen robot geselecteerd'
        );

        return;
    }

    if (robot.isOnline) {

        setConnection(
            true,
            `${robot.name} online`
        );
    }
    else {

        setConnection(
            false,
            `${robot.name} offline`
        );
    }
}


// ======================================================
// REALTIME / POLLING
// ======================================================

function startRealtimeUpdates() {

    // Voorkom meerdere intervals
    if (statusInterval) {

        clearInterval(statusInterval);
    }

    // Iedere 3 seconden opnieuw ophalen
    statusInterval = setInterval(
        async () => {

            // Alleen uitvoeren wanneer dashboard zichtbaar is
            if (!dashboard.hidden) {

                await loadData();
            }

        },
        3000
    );
}


// ======================================================
// COMMANDS TONEN
// ======================================================

function renderCommands(commands) {

    const recent =
        commands
            .slice(-6)
            .reverse();

    commandTable.innerHTML =
        recent.length
            ? recent.map((command) => `

                <div class="command-row">

                    <span>
                        ${command.servo}
                    </span>

                    <span>
                        ${command.angle} deg
                    </span>

                    <span>
                        ${new Date(
                command.createdAt
            ).toLocaleTimeString('nl-NL')}
                    </span>

                </div>

            `).join('')

            : '<p class="empty-state">Nog geen commando\'s gevonden.</p>';
}


// ======================================================
// COMMAND VERSTUREN
// ======================================================

async function sendCommand(
    servo,
    angle
) {

    const robotId =
        Number(robotSelect.value);

    if (!robotId) {

        throw new Error(
            'Selecteer eerst een robot'
        );
    }

    const response =
        await apiFetch(
            '/api/commands',
            {
                method: 'POST',

                headers: {
                    'Content-Type':
                        'application/json'
                },

                body: JSON.stringify({
                    robotId,
                    servo,
                    angle
                })
            }
        );

    if (!response.ok) {

        const data =
            await readResponse(response);

        throw new Error(
            data?.message ??
            data?.errors?.join(" ") ??
            'Commando kon niet worden verzonden'
        );
    }
}


// ======================================================
// ALLE SERVO'S VERSTUREN
// ======================================================

document
    .querySelector('#send-all')
    .addEventListener(
        'click',
        async () => {

            try {

                await Promise.all(

                    Object.entries(values)
                        .map(
                            ([servo, angle]) =>
                                sendCommand(
                                    servo,
                                    angle
                                )
                        )
                );

                feedback.textContent =
                    'Alle servo-waarden zijn verzonden.';

                document.querySelector(
                    '#last-update'
                ).textContent =
                    new Date()
                        .toLocaleTimeString('nl-NL');

                await loadData();

            }
            catch (error) {

                feedback.textContent =
                    error.message;
            }
        }
    );


// ======================================================
// NOODSTOP
// ======================================================

document
    .querySelector('#emergency-button')
    .addEventListener(
        'click',
        () => {

            feedback.textContent =
                'Noodstop gemarkeerd. Bewegingen zijn gestopt.';
        }
    );


// ======================================================
// HANDMATIG VERNIEUWEN
// ======================================================

document
    .querySelector('#refresh-button')
    .addEventListener(
        'click',
        loadData
    );


// ======================================================
// ROBOT SELECTIE VERANDERD
// ======================================================

robotSelect.addEventListener(
    'change',
    loadData
);


// ======================================================
// DASHBOARD STARTEN
// ======================================================

async function startDashboard() {

    renderServos();

    await loadData();

    startRealtimeUpdates();
}


// ======================================================
// APP START
// ======================================================

async function initialiseApp() {

    const token =
        localStorage.getItem("token");

    // Geen token -> loginpagina
    if (!token) {

        dashboard.hidden = true;
        authPage.hidden = false;

        return;
    }

    // Controleren of opgeslagen JWT nog geldig is
    try {

        const response =
            await apiFetch(
                "/api/auth/me"
            );

        if (!response.ok) {

            logout();

            return;
        }

        showDashboard();

    }
    catch {

        logout();
    }
}


initialiseApp();