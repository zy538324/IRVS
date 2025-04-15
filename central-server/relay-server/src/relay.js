const express = require('express');
const http = require('http');
const WebSocket = require('ws');

const app = express();
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

const clients = new Map();

wss.on('connection', (ws, req) => {
    const clientId = req.headers['sec-websocket-key'];
    clients.set(clientId, ws);

    ws.on('message', (message) => {
        // Handle incoming messages from clients
        console.log(`Received message from ${clientId}: ${message}`);
        // Broadcast the message to other clients
        clients.forEach((client, id) => {
            if (id !== clientId && client.readyState === WebSocket.OPEN) {
                client.send(message);
            }
        });
    });

    ws.on('close', () => {
        clients.delete(clientId);
        console.log(`Client ${clientId} disconnected`);
    });
});

const PORT = process.env.PORT || 8080;
server.listen(PORT, () => {
    console.log(`Relay server is running on port ${PORT}`);
});