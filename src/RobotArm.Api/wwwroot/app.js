const servos = ['Base', 'Shoulder', 'Elbow', 'Wrist', 'Gripper'];
const robotSelect = document.querySelector('#robot-select');
const servoList = document.querySelector('#servo-list');
const feedback = document.querySelector('#feedback');
const commandTable = document.querySelector('#command-table');
const values = Object.fromEntries(servos.map((servo) => [servo, 90]));

function setConnection(online, label) {
    document.querySelector('#connection-label').textContent = label;
    document.querySelector('#footer-status').textContent = online ? 'ONLINE' : 'OFFLINE';
}

function renderServos() {
    servoList.innerHTML = servos.map((servo) => `
        <label class="servo-row">
            <span class="servo-name">${servo}<small>JOINT ${servos.indexOf(servo) + 1}</small></span>
            <span><input type="range" min="0" max="180" value="${values[servo]}" data-servo="${servo}"><span class="range-labels"><span>0</span><span>90</span><span>180</span></span></span>
            <span class="servo-value" data-value="${servo}">${values[servo]} deg</span>
            <button class="servo-send" type="button" data-send-servo="${servo}">Stuur</button>
        </label>`).join('');
    servoList.querySelectorAll('input').forEach((input) => input.addEventListener('input', (event) => {
        const servo = event.target.dataset.servo;
        values[servo] = Number(event.target.value);
        document.querySelector(`[data-value="${servo}"]`).textContent = `${values[servo]} deg`;
        document.querySelector('#active-servo').textContent = servo;
        document.querySelector('#angle-readout').textContent = `${values[servo]} deg`;
    }));
    servoList.querySelectorAll('[data-send-servo]').forEach((button) => button.addEventListener('click', async (event) => {
        const servo = event.currentTarget.dataset.sendServo;
        event.currentTarget.disabled = true;
        try {
            await sendCommand(servo, values[servo]);
            feedback.textContent = `${servo} (${values[servo]} deg) is naar de API gestuurd.`;
            document.querySelector('#last-update').textContent = new Date().toLocaleTimeString('nl-NL');
            await loadData();
        } catch (error) { feedback.textContent = error.message; }
        finally { event.currentTarget.disabled = false; }
    }));
}

async function loadData() {
    try {
        const [robotsResponse, statusResponse, commandsResponse] = await Promise.all([fetch('/api/robots'), fetch('/api/robot/status'), fetch('/api/commands')]);
        if (!robotsResponse.ok || !statusResponse.ok) throw new Error('API niet bereikbaar');
        const robots = await robotsResponse.json();
        const status = await statusResponse.json();
        robotSelect.innerHTML = robots.length ? robots.map((robot) => `<option value="${robot.id}">${robot.name}</option>`).join('') : '<option value="">Geen robot geregistreerd</option>';
        setConnection(status.status === 'online', status.status === 'online' ? 'Robot online' : 'Robot offline');
        if (commandsResponse.ok) renderCommands(await commandsResponse.json());
    } catch (error) { setConnection(false, 'Verbinding mislukt'); feedback.textContent = 'Kan de robotgegevens niet laden.'; }
}

function renderCommands(commands) {
    const recent = commands.slice(-6).reverse();
    commandTable.innerHTML = recent.length ? recent.map((command) => `<div class="command-row"><span>${command.servo}</span><span>${command.angle} deg</span><span>${new Date(command.createdAt).toLocaleTimeString('nl-NL')}</span></div>`).join('') : '<p class="empty-state">Nog geen commando\'s gevonden.</p>';
}

async function sendCommand(servo, angle) {
    const robotId = Number(robotSelect.value);
    if (!robotId) throw new Error('Selecteer eerst een robot');
    const response = await fetch('/api/commands', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ robotId, servo, angle }) });
    if (!response.ok) throw new Error('Commando kon niet worden verzonden');
}

document.querySelector('#send-all').addEventListener('click', async () => {
    try { await Promise.all(Object.entries(values).map(([servo, angle]) => sendCommand(servo, angle))); feedback.textContent = 'Alle servo-waarden zijn verzonden.'; document.querySelector('#last-update').textContent = new Date().toLocaleTimeString('nl-NL'); await loadData(); }
    catch (error) { feedback.textContent = error.message; }
});
document.querySelector('#emergency-button').addEventListener('click', () => { feedback.textContent = 'Noodstop gemarkeerd. Bewegingen zijn gestopt.'; });
document.querySelector('#refresh-button').addEventListener('click', loadData);
renderServos();
loadData();