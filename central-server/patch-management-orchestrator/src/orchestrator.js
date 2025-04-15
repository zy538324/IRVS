const express = require('express');
const bodyParser = require('body-parser');
const { checkForPatches, deployPatches } = require('./patchService');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(bodyParser.json());

app.get('/api/patches', async (req, res) => {
    try {
        const patches = await checkForPatches();
        res.status(200).json(patches);
    } catch (error) {
        res.status(500).json({ message: 'Error fetching patches', error });
    }
});

app.post('/api/patches/deploy', async (req, res) => {
    const { patchIds } = req.body;
    try {
        const result = await deployPatches(patchIds);
        res.status(200).json(result);
    } catch (error) {
        res.status(500).json({ message: 'Error deploying patches', error });
    }
});

app.listen(PORT, () => {
    console.log(`Patch Management Orchestrator running on port ${PORT}`);
});