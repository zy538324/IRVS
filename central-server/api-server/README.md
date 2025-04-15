# API Server Documentation

## Overview
The API Server is a crucial component of the IT security solution, responsible for handling secure communications and interactions between the client endpoints (agents) and the central server. It facilitates the management of agent requests, command dispatching, and patch management orchestration.

## Features
- **Secure Communications**: Utilizes TLS to ensure secure data transmission between agents and the central server.
- **Command Handling**: Processes requests from agents and dispatches commands accordingly.
- **Patch Management Integration**: Interfaces with the Patch Management Orchestrator to manage software updates and security patches.

## Setup Instructions
1. **Prerequisites**:
   - Node.js (version 14 or higher)
   - npm (Node Package Manager)

2. **Installation**:
   - Navigate to the `api-server` directory:
     ```
     cd central-server/api-server
     ```
   - Install the required dependencies:
     ```
     npm install
     ```

3. **Running the Server**:
   - Start the API server:
     ```
     npm start
     ```
   - The server will run on the specified port (default is 3000).

## Usage
- The API server exposes various endpoints for agent communication. Refer to the API documentation for detailed information on available endpoints and their usage.

## Contributing
Contributions to the API Server are welcome. Please follow the standard contribution guidelines for the project.

## License
This project is licensed under the MIT License. See the LICENSE file for more details.