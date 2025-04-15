# Relay Server Documentation

This README file provides an overview of the Relay Server component of the IT Security Solution project. The Relay Server is responsible for facilitating NAT traversal for remote connections, ensuring secure and efficient communication between client endpoints and the central server.

## Overview

The Relay Server is designed to handle network address translation (NAT) issues that may arise during remote access sessions. By acting as an intermediary, it allows agents and the central server to establish connections even when they are behind different NAT configurations.

## Features

- **NAT Traversal**: Supports connections from agents behind NAT devices.
- **Secure Communication**: Ensures that all data transmitted through the relay is secure.
- **Lightweight**: Designed to be efficient and minimize latency during remote sessions.

## Installation

1. Clone the repository:
   ```
   git clone <repository-url>
   ```

2. Navigate to the relay-server directory:
   ```
   cd central-server/relay-server
   ```

3. Install dependencies:
   ```
   npm install
   ```

## Usage

To start the Relay Server, run the following command:
```
node src/relay.js
```

Ensure that the server is running before attempting to connect any agents.

## Configuration

Configuration options can be set in the `src/relay.js` file. Adjust parameters such as port numbers and security settings as needed.

## Contributing

Contributions to the Relay Server are welcome. Please follow the standard contribution guidelines for the project.

## License

This project is licensed under the MIT License. See the LICENSE file for more details.