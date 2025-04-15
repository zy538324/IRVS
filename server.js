const express = require('express');
const cors = require('cors');
const path = require('path');
const fs = require('fs');
const app = express();
const PORT = process.env.PORT || 5000;

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Serve static files from the React app in production
if (process.env.NODE_ENV === 'production') {
  app.use(express.static(path.join(__dirname, 'client/build')));
}

// API endpoints
app.get('/api/vulnerabilities', (req, res) => {
  // This would normally fetch from a database
  // For now, we'll return mock data
  const mockData = [
    {
      id: 1,
      name: 'CVE-2023-1234',
      severity: 'High',
      affectedSystems: ['Windows Server 2019', 'Windows 10'],
      description: 'Remote code execution vulnerability in Windows',
      remediation: 'Apply Microsoft Security Update KB5025224',
      dateDiscovered: '2023-06-15'
    },
    {
      id: 2,
      name: 'CVE-2023-5678',
      severity: 'Critical',
      affectedSystems: ['Apache 2.4.x'],
      description: 'Buffer overflow vulnerability in Apache HTTP Server',
      remediation: 'Upgrade to Apache 2.4.57 or later',
      dateDiscovered: '2023-07-20'
    }
  ];
  
  res.json(mockData);
});

app.get('/api/systems', (req, res) => {
  // Mock data for systems inventory
  const mockSystems = [
    {
      id: 1,
      hostname: 'srv-web-01',
      ipAddress: '192.168.1.101',
      operatingSystem: 'Ubuntu 22.04 LTS',
      lastScan: '2023-08-15',
      vulnerabilityCount: 3
    },
    {
      id: 2,
      hostname: 'srv-db-01',
      ipAddress: '192.168.1.102',
      operatingSystem: 'Windows Server 2019',
      lastScan: '2023-08-14',
      vulnerabilityCount: 5
    },
    {
      id: 3,
      hostname: 'srv-app-01',
      ipAddress: '192.168.1.103',
      operatingSystem: 'CentOS 8',
      lastScan: '2023-08-15',
      vulnerabilityCount: 2
    }
  ];
  
  res.json(mockSystems);
});

// API endpoint for scan results
app.get('/api/scan-results', (req, res) => {
  const mockScanResults = {
    lastScanDate: '2023-08-15',
    scanDuration: '45 minutes',
    systemsScanned: 24,
    vulnerabilitiesFound: {
      critical: 3,
      high: 8,
      medium: 15,
      low: 22
    },
    topVulnerabilities: [
      {
        id: 'CVE-2023-1234',
        name: 'Remote Code Execution Vulnerability',
        affectedCount: 5
      },
      {
        id: 'CVE-2023-5678',
        name: 'SQL Injection Vulnerability',
        affectedCount: 3
      }
    ]
  };
  
  res.json(mockScanResults);
});

// API endpoint to initiate a new scan
app.post('/api/scan', (req, res) => {
  // In a real implementation, this would trigger an actual scan
  const { targets, scanType } = req.body;
  
  // Mock response
  res.json({
    success: true,
    scanId: 'scan-' + Date.now(),
    message: `Scan initiated for ${targets.length} targets with scan type: ${scanType}`,
    estimatedTime: '45 minutes'
  });
});

// For any request not handled by the API, serve the React app in production
if (process.env.NODE_ENV === 'production') {
  app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, 'client/build', 'index.html'));
  });
}

// Start the server
app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});