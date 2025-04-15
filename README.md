# Agent Module Documentation

## Overview
The Agent module is a critical component of the IT security solution, designed to run on client endpoints. It provides essential functionalities such as system monitoring, vulnerability scanning, antivirus protection, patch management, and remote access capabilities.

## Features
- **System Monitoring**: Monitors CPU, RAM, disk usage, and running services.
- **Vulnerability Scanning**: Identifies software inventory and correlates with known vulnerabilities (CVE).
- **Antivirus Scanning**: Offers signature-based scanning, real-time protection, and scheduled scans.
- **Patch Management**: Detects and deploys patches for the operating system and installed software.
- **Remote Shell Access**: Provides secure access to command-line interfaces (PowerShell, CMD, Bash).
- **Remote Desktop Access**: Enables screen streaming with user consent and admin override capabilities.

## Setup Instructions
1. **Prerequisites**:
   - .NET Core SDK installed on the development machine.
   - Access to the central server for API communication.

2. **Building the Agent**:
   - Navigate to the `agent/src/AgentCore` directory.
   - Run the following command to build the project:
     ```
     dotnet build
     ```

3. **Running the Agent**:
   - After building, execute the agent using:
     ```
     dotnet run
     ```

4. **Configuration**:
   - Update the configuration files as necessary to set up communication with the central server.

## Usage
- The agent will automatically start monitoring the system upon execution.
- Administrators can access the central dashboard to view real-time data and manage agent tasks.

## Future Enhancements
- Support for Linux and macOS agents.
- Integration with third-party alerting systems.
- Enhanced security features, including data encryption at rest.

## Contribution
Contributions to the Agent module are welcome. Please follow the project's contribution guidelines for submitting changes or enhancements.