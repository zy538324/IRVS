const express = require('express');
const router = express.Router();

// Get all systems
router.get('/', (req, res) => {
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

// Get system by ID
router.get('/:id', (req, res) => {
  const systemId = parseInt(req.params.id);
  
  // Mock detailed system data
  const systemDetails = {
    1: {
      id: 1,
      hostname: 'srv-web-01',
      ipAddress: '192.168.1.101',
      operatingSystem: 'Ubuntu 22.04 LTS',
      lastScan: '2023-08-15',
      vulnerabilityCount: 3,
      systemDetails: {
        cpuCores: 4,
        memoryGB: 16,
        diskSpaceGB: 500,
        uptime: '45 days',
        services: ['nginx', 'docker', 'ssh'],
        installedSoftware: [
          { name: 'nginx', version: '1.18.0' },
          { name: 'OpenSSH', version: '8.2p1' },
          { name: 'Docker', version: '20.10.18' }
        ]
      },
      vulnerabilities: [
        { id: 3, name: 'CVE-2023-9876', severity: 'Medium', status: 'Open' },
        { id: 5, name: 'CVE-2023-4567', severity: 'Low', status: 'Open' },
        { id: 8, name: 'CVE-2023-7654', severity: 'High', status: 'In Progress' }
      ]
    },
    2: {
      id: 2,
      hostname: 'srv-db-01',
      ipAddress: '192.168.1.102',
      operatingSystem: 'Windows Server 2019',
      lastScan: '2023-08-14',
      vulnerabilityCount: 5,
      systemDetails: {
        cpuCores: 8,
        memoryGB: 32,
        diskSpaceGB: 1000,
        uptime: '30 days',
        services: ['SQL Server', 'IIS', 'RDP'],
        installedSoftware: [
          { name: 'Microsoft SQL Server', version: '2019' },
          { name: 'IIS', version: '10.0' }
        ]
      },
      vulnerabilities: [
        { id: 1, name: 'CVE-2023-1234', severity: 'High', status: 'Open' },
        { id: 4, name: 'CVE-2023-2345', severity: 'Medium', status: 'Open' },
        { id: 6, name: 'CVE-2023-3456', severity: 'Medium', status: 'Open' },
        { id: 9, name: 'CVE-2023-5432', severity: 'Critical', status: 'In Progress' },
        { id: 10, name: 'CVE-2023-6543', severity: 'Low', status: 'Open' }
      ]
    },
    3: {
      id: 3,
      hostname: 'srv-app-01',
      ipAddress: '192.168.1.103',
      operatingSystem: 'CentOS 8',
      lastScan: '2023-08-15',
      vulnerabilityCount: 2,
      systemDetails: {
        cpuCores: 4,
        memoryGB: 16,
        diskSpaceGB: 500,
        uptime: '60 days',
        services: ['httpd', 'tomcat', 'ssh'],
        installedSoftware: [
          { name: 'Apache', version: '2.4.37' },
          { name: 'Tomcat', version: '9.0.50' },
          { name: 'OpenSSH', version: '8.0p1' }
        ]
      },
      vulnerabilities: [
        { id: 2, name: 'CVE-2023-5678', severity: 'Critical', status: 'Open' },
        { id: 7, name: 'CVE-2023-8765', severity: 'Low', status: 'Resolved' }
      ]
    }
  };
  
  if (systemDetails[systemId]) {
    return res.json(systemDetails[systemId]);
  } else {
    return res.status(404).json({ error: 'System not found' });
  }
});

// Add a new system
router.post('/', (req, res) => {
  const { hostname, ipAddress, operatingSystem } = req.body;
  
  // Validate required fields
  if (!hostname || !ipAddress || !operatingSystem) {
    return res.status(400).json({ error: 'Please provide hostname, IP address and operating system' });
  }
  
  // In a real app, you would add this to your database
  // For now, just return success with mock ID
  res.status(201).json({
    success: true,
    message: 'System added successfully',
    system: {
      id: Date.now(),
      hostname,
      ipAddress,
      operatingSystem,
      lastScan: 'Never',
      vulnerabilityCount: 0
    }
  });
});

// Update system
router.put('/:id', (req, res) => {
  const systemId = parseInt(req.params.id);
  const { hostname, ipAddress, operatingSystem } = req.body;
  
  // In production, validate system exists and update in DB
  
  res.json({
    success: true,
    message: `System ${systemId} updated successfully`,
    system: {
      id: systemId,
      hostname: hostname || 'Updated hostname',
      ipAddress: ipAddress || '192.168.1.xxx',
      operatingSystem: operatingSystem || 'Updated OS',
      lastScan: 'Updated',
      vulnerabilityCount: 0
    }
  });
});

// Delete system
router.delete('/:id', (req, res) => {
  const systemId = parseInt(req.params.id);
  
  // In production, validate system exists and delete from DB
  
  res.json({
    success: true,
    message: `System ${systemId} deleted successfully`
  });
});

module.exports = router;