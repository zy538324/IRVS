const express = require('express');
const bodyParser = require('body-parser');
const { handleAgentRequest } = require('./handlers/agentHandler');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(bodyParser.json());

app.post('/dispatch', async (req, res) => {
    try {
        const { agentId, command, params } = req.body;
        const result = await handleAgentRequest(agentId, command, params);
        res.status(200).json({ success: true, result });
    } catch (error) {
        console.error('Error dispatching command:', error);
        res.status(500).json({ success: false, message: error.message });
    }
});

app.listen(PORT, () => {
    console.log(`Command Dispatcher listening on port ${PORT}`);
});