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
// ARM + GRIPPER SIMULATION
// ======================================================

const simulationAngles = {
    shoulder: 90,
    elbow: 90,
    gripper: 90
};

let simulationFrame = null;


// ======================================================
// ARM SIMULATION
// ======================================================

function updateArmSimulation() {

    const shoulderAngle = simulationAngles.shoulder;
    const elbowAngle = simulationAngles.elbow;

    const shoulder = {
        x: 180,
        y: 232
    };

    const upperArmLength = 100;
    const lowerArmLength = 80;

    const upperArmRadians =
        (shoulderAngle - 90) * Math.PI / 180;

    const elbow = {
        x:
            shoulder.x +
            Math.cos(upperArmRadians) *
            upperArmLength,

        y:
            shoulder.y -
            Math.sin(upperArmRadians) *
            upperArmLength
    };

    const lowerArmRadians =
        upperArmRadians +
        (elbowAngle - 90) *
        Math.PI / 180;

    const wrist = {
        x:
            elbow.x +
            Math.cos(lowerArmRadians) *
            lowerArmLength,

        y:
            elbow.y -
            Math.sin(lowerArmRadians) *
            lowerArmLength
    };


    const upperArm =
        document.querySelector(
            '#simulation-upper-arm'
        );

    const lowerArm =
        document.querySelector(
            '#simulation-lower-arm'
        );

    const elbowJoint =
        document.querySelector(
            '#simulation-elbow'
        );


    if (upperArm) {

        upperArm.setAttribute(
            'x2',
            elbow.x
        );

        upperArm.setAttribute(
            'y2',
            elbow.y
        );
    }


    if (lowerArm) {

        lowerArm.setAttribute(
            'x1',
            elbow.x
        );

        lowerArm.setAttribute(
            'y1',
            elbow.y
        );

        lowerArm.setAttribute(
            'x2',
            wrist.x
        );

        lowerArm.setAttribute(
            'y2',
            wrist.y
        );
    }


    if (elbowJoint) {

        elbowJoint.setAttribute(
            'cx',
            elbow.x
        );

        elbowJoint.setAttribute(
            'cy',
            elbow.y
        );
    }


    const shoulderLabel =
        document.querySelector(
            '#shoulder-angle-label'
        );

    const elbowLabel =
        document.querySelector(
            '#elbow-angle-label'
        );


    if (shoulderLabel) {

        shoulderLabel.textContent =
            `Shoulder ${Math.round(
                shoulderAngle
            )} deg`;
    }


    if (elbowLabel) {

        elbowLabel.textContent =
            `Elbow ${Math.round(
                elbowAngle
            )} deg`;
    }


    // Gripper positie laten meegaan met einde van arm
    updateGripperSimulation(wrist, lowerArmRadians);
}


// ======================================================
// GRIPPER / VINGER SIMULATIE
// ======================================================

function getGripperState(angle) {

    if (angle <= 30) {
        return "Volledig open";
    }

    if (angle <= 75) {
        return "Grotendeels open";
    }

    if (angle <= 120) {
        return "Half gesloten";
    }

    if (angle <= 165) {
        return "Grotendeels gesloten";
    }

    return "Volledig gesloten";
}


function updateGripperSimulation(
    wrist,
    armRadians
) {

    const angle =
        simulationAngles.gripper;

    // 0 graden = helemaal open
    // 180 graden = helemaal dicht
    const progress =
        angle / 180;

    // Opening tussen vingers
    // Open = ongeveer 30 graden
    // Dicht = ongeveer 3 graden
    const fingerOpening =
        30 - progress * 27;


    const leftFinger =
        document.querySelector(
            '#simulation-gripper-left'
        );

    const rightFinger =
        document.querySelector(
            '#simulation-gripper-right'
        );

    const gripperGroup =
        document.querySelector(
            '#simulation-gripper'
        );


    // Als je SVG gripper elementen hebt
    if (gripperGroup) {

        const rotation =
            armRadians *
            180 /
            Math.PI;

        gripperGroup.setAttribute(
            'transform',
            `translate(${wrist.x} ${wrist.y}) rotate(${rotation})`
        );
    }


    if (leftFinger) {

        leftFinger.setAttribute(
            'transform',
            `rotate(${-fingerOpening})`
        );
    }


    if (rightFinger) {

        rightFinger.setAttribute(
            'transform',
            `rotate(${fingerOpening})`
        );
    }


    // Fallback voor oude HTML met .arm-gripper
    const oldGripper =
        document.querySelector(
            '.arm-gripper'
        );

    if (
        oldGripper &&
        !gripperGroup
    ) {

        const scale =
            1 -
            progress * 0.45;

        oldGripper.style.transform =
            `rotate(20deg) scaleX(${scale})`;
    }


    // Gripper status label
    const gripperLabel =
        document.querySelector(
            '#gripper-state-label'
        );

    if (gripperLabel) {

        gripperLabel.textContent =
            `Gripper: ${getGripperState(
                angle
            )}`;
    }
}


// ======================================================
// ANIMATIE
// ======================================================

function animateArmSimulation() {

    const shoulderDifference =
        values.Shoulder -
        simulationAngles.shoulder;

    const elbowDifference =
        values.Elbow -
        simulationAngles.elbow;

    const gripperDifference =
        values.Gripper -
        simulationAngles.gripper;


    simulationAngles.shoulder +=
        shoulderDifference * 0.18;

    simulationAngles.elbow +=
        elbowDifference * 0.18;

    simulationAngles.gripper +=
        gripperDifference * 0.18;


    updateArmSimulation();


    if (
        Math.abs(shoulderDifference) > 0.1 ||
        Math.abs(elbowDifference) > 0.1 ||
        Math.abs(gripperDifference) > 0.1
    ) {

        simulationFrame =
            requestAnimationFrame(
                animateArmSimulation
            );
    }

    else {

        simulationAngles.shoulder =
            values.Shoulder;

        simulationAngles.elbow =
            values.Elbow;

        simulationAngles.gripper =
            values.Gripper;

        updateArmSimulation();

        simulationFrame = null;
    }
}


function startArmSimulation() {

    if (simulationFrame === null) {

        simulationFrame =
            requestAnimationFrame(
                animateArmSimulation
            );
    }
}


// ======================================================
// AUTH ELEMENTS
// ======================================================

const authPage =
    document.getElementById(
        "auth-page"
    );

const dashboard =
    document.getElementById(
        "dashboard"
    );

const loginForm =
    document.getElementById(
        "login-form"
    );

const registerForm =
    document.getElementById(
        "register-form"
    );

const authSwitch =
    document.getElementById(
        "auth-switch"
    );

const authTitle =
    document.getElementById(
        "auth-title"
    );

const authError =
    document.getElementById(
        "auth-error"
    );


let registerMode = false;

let statusInterval = null;


// ======================================================
// LOGIN / REGISTER SWITCH
// ======================================================

authSwitch.addEventListener(
    "click",
    () => {

        registerMode =
            !registerMode;

        loginForm.hidden =
            registerMode;

        registerForm.hidden =
            !registerMode;

        authTitle.textContent =
            registerMode
                ? "Account aanmaken"
                : "Inloggen";

        authSwitch.textContent =
            registerMode
                ? "Al een account? Inloggen"
                : "Nog geen account? Registreren";

        authError.textContent = "";
    }
);


// ======================================================
// REGISTER
// ======================================================

registerForm.addEventListener(
    "submit",
    async (event) => {

        event.preventDefault();

        authError.textContent = "";

        try {

            const response =
                await fetch(
                    "/api/auth/register",
                    {
                        method: "POST",

                        headers: {
                            "Content-Type":
                                "application/json"
                        },

                        body:
                            JSON.stringify({
                                username:
                                    document
                                        .getElementById(
                                            "register-username"
                                        )
                                        .value,

                                email:
                                    document
                                        .getElementById(
                                            "register-email"
                                        )
                                        .value,

                                password:
                                    document
                                        .getElementById(
                                            "register-password"
                                        )
                                        .value
                            })
                    }
                );


            const data =
                await readResponse(
                    response
                );


            if (!response.ok) {

                authError.textContent =
                    data?.errors?.join(" ") ??
                    data?.message ??
                    "Registreren mislukt.";

                return;
            }


            registerMode = false;

            registerForm.hidden = true;

            loginForm.hidden = false;

            authTitle.textContent =
                "Inloggen";

            authSwitch.textContent =
                "Nog geen account? Registreren";

            authError.textContent =
                "Account aangemaakt. Je kunt nu inloggen.";


            document
                .getElementById(
                    "login-email"
                )
                .value =

                document
                    .getElementById(
                        "register-email"
                    )
                    .value;


            registerForm.reset();
        }

        catch {

            authError.textContent =
                "De API is niet bereikbaar.";
        }
    }
);


// ======================================================
// LOGIN
// ======================================================

loginForm.addEventListener(
    "submit",
    async (event) => {

        event.preventDefault();

        authError.textContent = "";

        try {

            const response =
                await fetch(
                    "/api/auth/login",
                    {
                        method: "POST",

                        headers: {
                            "Content-Type":
                                "application/json"
                        },

                        body:
                            JSON.stringify({
                                email:
                                    document
                                        .getElementById(
                                            "login-email"
                                        )
                                        .value,

                                password:
                                    document
                                        .getElementById(
                                            "login-password"
                                        )
                                        .value
                            })
                    }
                );


            const data =
                await readResponse(
                    response
                );


            if (!response.ok) {

                authError.textContent =
                    data?.errors?.join(" ") ??
                    data?.message ??
                    "Inloggen mislukt.";

                return;
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


            showDashboard();
        }

        catch {

            authError.textContent =
                "De API is niet bereikbaar.";
        }
    }
);


// ======================================================
// API FETCH MET JWT
// ======================================================

async function apiFetch(
    url,
    options = {}
) {

    const token =
        localStorage.getItem(
            "token"
        );


    const headers = {
        ...(options.headers || {})
    };


    if (token) {

        headers["Authorization"] =
            `Bearer ${token}`;
    }


    const response =
        await fetch(
            url,
            {
                ...options,
                headers
            }
        );


    if (response.status === 401) {

        const refreshed =
            await tryRefreshToken();


        if (refreshed) {

            headers["Authorization"] =
                `Bearer ${localStorage.getItem(
                    "token"
                )
                }`;


            return fetch(
                url,
                {
                    ...options,
                    headers
                }
            );
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
        localStorage.getItem(
            "refreshToken"
        );


    if (!refreshToken) {

        return false;
    }


    try {

        const response =
            await fetch(
                "/api/auth/refresh",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json"
                    },

                    body:
                        JSON.stringify({
                            refreshToken
                        })
                }
            );


        if (!response.ok) {

            return false;
        }


        const data =
            await readResponse(
                response
            );


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
// RESPONSE UITLEZEN
// ======================================================

async function readResponse(
    response
) {

    const text =
        await response.text();


    if (!text) {

        return null;
    }


    try {

        return JSON.parse(
            text
        );
    }

    catch {

        return null;
    }
}


// ======================================================
// DASHBOARD
// ======================================================

function showDashboard() {

    authPage.hidden = true;

    dashboard.hidden = false;

    startDashboard();
}


// ======================================================
// LOGOUT
// ======================================================

function logout() {

    localStorage.removeItem(
        "token"
    );

    localStorage.removeItem(
        "refreshToken"
    );


    if (statusInterval) {

        clearInterval(
            statusInterval
        );

        statusInterval = null;
    }


    dashboard.hidden = true;

    authPage.hidden = false;

    loginForm.hidden = false;

    registerForm.hidden = true;

    registerMode = false;

    authTitle.textContent =
        "Inloggen";

    authSwitch.textContent =
        "Nog geen account? Registreren";
}


// ======================================================
// CONNECTION STATUS
// ======================================================

function setConnection(
    online,
    label
) {

    const connectionLabel =
        document.querySelector(
            '#connection-label'
        );

    const footerStatus =
        document.querySelector(
            '#footer-status'
        );


    if (connectionLabel) {

        connectionLabel.textContent =
            label;
    }


    if (footerStatus) {

        footerStatus.textContent =
            online
                ? 'ONLINE'
                : 'OFFLINE';
    }
}


// ======================================================
// SERVO UI
// ======================================================

function renderServos() {

    servoList.innerHTML =
        servos
            .map(
                (servo) => `

        <label class="servo-row">

            <span class="servo-name">

                ${servo}

                <small>
                    JOINT ${servos.indexOf(
                    servo
                ) + 1
                    }
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

    `
            )
            .join('');


    // ==================================================
    // SLIDERS
    // ==================================================

    servoList
        .querySelectorAll(
            'input'
        )
        .forEach(
            (input) => {

                input.addEventListener(
                    'input',
                    (event) => {

                        const servo =
                            event
                                .target
                                .dataset
                                .servo;


                        values[servo] =
                            Number(
                                event
                                    .target
                                    .value
                            );


                        const valueLabel =
                            document.querySelector(
                                `[data-value="${servo}"]`
                            );


                        if (valueLabel) {

                            valueLabel.textContent =
                                `${values[servo]} deg`;
                        }


                        const activeServo =
                            document.querySelector(
                                '#active-servo'
                            );


                        if (activeServo) {

                            activeServo.textContent =
                                servo;
                        }


                        const angleReadout =
                            document.querySelector(
                                '#angle-readout'
                            );


                        if (angleReadout) {

                            if (
                                servo ===
                                "Gripper"
                            ) {

                                angleReadout.textContent =
                                    `Gripper ${values[servo]} deg`;
                            }

                            else {

                                angleReadout.textContent =
                                    `${servo} ${values[servo]} deg`;
                            }
                        }


                        // Arm + vingers bewegen
                        startArmSimulation();
                    }
                );
            }
        );


    // ==================================================
    // INDIVIDUELE SERVO VERSTUREN
    // ==================================================

    servoList
        .querySelectorAll(
            '[data-send-servo]'
        )
        .forEach(
            (button) => {

                button.addEventListener(
                    'click',
                    async (event) => {

                        // Dit ontbrak in je vorige versie
                        const servo =
                            event
                                .currentTarget
                                .dataset
                                .sendServo;


                        event
                            .currentTarget
                            .disabled = true;


                        try {

                            await sendCommand(
                                servo,
                                values[servo]
                            );


                            feedback.textContent =
                                `${servo} (${values[servo]} deg) is naar de API gestuurd.`;


                            const lastUpdate =
                                document.querySelector(
                                    '#last-update'
                                );


                            if (lastUpdate) {

                                lastUpdate.textContent =
                                    new Date()
                                        .toLocaleTimeString(
                                            'nl-NL'
                                        );
                            }


                            await loadData();
                        }

                        catch (error) {

                            feedback.textContent =
                                error.message;
                        }

                        finally {

                            event
                                .currentTarget
                                .disabled = false;
                        }
                    }
                );
            }
        );
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
        ] =
            await Promise.all([
                apiFetch(
                    '/api/robots'
                ),

                apiFetch(
                    '/api/commands'
                )
            ]);


        if (!robotsResponse.ok) {

            throw new Error(
                'Robotgegevens konden niet worden geladen.'
            );
        }


        const robots =
            await robotsResponse.json();


        robotSelect.innerHTML =
            robots.length

                ? robots
                    .map(
                        (robot) => `

                    <option value="${robot.id}">
                        ${robot.name}
                    </option>

                `
                    )
                    .join('')

                : '<option value="">Geen robot geregistreerd</option>';


        if (
            selectedRobotId &&
            robots.some(
                (robot) =>
                    String(
                        robot.id
                    ) ===
                    String(
                        selectedRobotId
                    )
            )
        ) {

            robotSelect.value =
                selectedRobotId;
        }


        updateRobotStatus(
            robots
        );


        if (commandsResponse.ok) {

            const commands =
                await commandsResponse.json();

            renderCommands(
                commands
            );
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

function updateRobotStatus(
    robots
) {

    const robotId =
        Number(
            robotSelect.value
        );


    const robot =
        robots.find(
            (robot) =>
                robot.id === robotId
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
// REALTIME POLLING
// ======================================================

function startRealtimeUpdates() {

    if (statusInterval) {

        clearInterval(
            statusInterval
        );
    }


    statusInterval =
        setInterval(
            async () => {

                if (
                    !dashboard.hidden
                ) {

                    await loadData();
                }
            },

            3000
        );
}


// ======================================================
// COMMAND LOG
// ======================================================

function renderCommands(
    commands
) {

    const recent =
        commands
            .slice(-6)
            .reverse();


    commandTable.innerHTML =
        recent.length

            ? recent
                .map(
                    (command) => `

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
                    )
                            .toLocaleTimeString(
                                'nl-NL'
                            )
                        }
                    </span>

                </div>

            `
                )
                .join('')

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
        Number(
            robotSelect.value
        );


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

                body:
                    JSON.stringify({
                        robotId,
                        servo,
                        angle
                    })
            }
        );


    if (!response.ok) {

        const data =
            await readResponse(
                response
            );


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
    .querySelector(
        '#send-all'
    )
    .addEventListener(
        'click',
        async () => {

            try {

                await Promise.all(

                    Object
                        .entries(
                            values
                        )
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


                const lastUpdate =
                    document.querySelector(
                        '#last-update'
                    );


                if (lastUpdate) {

                    lastUpdate.textContent =
                        new Date()
                            .toLocaleTimeString(
                                'nl-NL'
                            );
                }


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
    .querySelector(
        '#emergency-button'
    )
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
    .querySelector(
        '#refresh-button'
    )
    .addEventListener(
        'click',
        loadData
    );


// ======================================================
// ROBOT SELECTIE
// ======================================================

robotSelect.addEventListener(
    'change',
    loadData
);


// ======================================================
// DASHBOARD START
// ======================================================

async function startDashboard() {

    renderServos();

    simulationAngles.shoulder =
        values.Shoulder;

    simulationAngles.elbow =
        values.Elbow;

    simulationAngles.gripper =
        values.Gripper;

    updateArmSimulation();

    await loadData();

    startRealtimeUpdates();
}


// ======================================================
// APP START
// ======================================================

async function initialiseApp() {

    const token =
        localStorage.getItem(
            "token"
        );


    if (!token) {

        dashboard.hidden = true;

        authPage.hidden = false;

        return;
    }


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